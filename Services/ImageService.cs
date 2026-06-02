using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CBTSecureDesktop.Services
{
    public class ImageService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _cacheDirectory;
        private readonly string _baseUrl;
        private static readonly ConcurrentQueue<string> _downloadFailures = new();

        static ImageService()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public ImageService(string baseUrl = "http://127.0.0.1:8000/api/image/")
        {
            _baseUrl = baseUrl;

            // Set up local cache directory in AppData
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _cacheDirectory = Path.Combine(appDataPath, "CBTSecureDesktop", "ImageCache");

            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        /// <summary>
        /// Retrieves an image from cache or downloads it from the API if not cached.
        /// </summary>
        public async Task<BitmapImage> GetImageAsync(string imageId, string token = "")
        {
            string cleanImageId = SanitizeFileName(imageId);
            string cacheFilePath = Path.Combine(_cacheDirectory, $"{cleanImageId}.img");

            // 1. Check Local Cache first
            if (File.Exists(cacheFilePath))
            {
                try
                {
                    byte[] cachedBytes = await File.ReadAllBytesAsync(cacheFilePath);
                    return CreateBitmapImageFromBytes(cachedBytes);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load image from cache: {ex.Message}");
                    // Fall through to try downloading again
                }
            }

            return await DownloadImageAndCacheAsync(imageId, token, cacheFilePath);
        }

        public static string[] ConsumeDownloadFailures()
        {
            var failures = new System.Collections.Generic.List<string>();
            while (_downloadFailures.TryDequeue(out var failure))
            {
                failures.Add(failure);
            }

            return failures.ToArray();
        }

        private async Task<BitmapImage> DownloadImageAndCacheAsync(string imageId, string token, string cacheFilePath)
        {
            try
            {
                string requestUrl = $"{_baseUrl}{imageId}";
                if (!string.IsNullOrEmpty(token))
                {
                    requestUrl += $"?token={token}";
                }

                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                    _ = SaveToCacheAsync(cacheFilePath, imageBytes);
                    return CreateBitmapImageFromBytes(imageBytes);
                }

                string errorMessage = $"{imageId} - Status {response.StatusCode}";
                System.Diagnostics.Debug.WriteLine($"Gagal mendownload gambar: {errorMessage}");
                EnqueueDownloadFailure(imageId, errorMessage);
                return GetDefaultPlaceholderImage();
            }
            catch (Exception ex)
            {
                string errorMessage = $"{imageId} - {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error download gambar: {errorMessage}");
                EnqueueDownloadFailure(imageId, errorMessage);
                return GetDefaultPlaceholderImage();
            }
        }

        public async Task<BitmapImage> RefreshImageCacheAsync(string imageId, string token = "")
        {
            string cleanImageId = SanitizeFileName(imageId);
            string cacheFilePath = Path.Combine(_cacheDirectory, $"{cleanImageId}.img");

            if (File.Exists(cacheFilePath))
            {
                try
                {
                    File.Delete(cacheFilePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete cache: {ex.Message}");
                }
            }

            return await DownloadImageAndCacheAsync(imageId, token, cacheFilePath);
        }

        private static void EnqueueDownloadFailure(string imageId, string detail)
        {
            _downloadFailures.Enqueue($"{imageId}: {detail}");
        }

        /// <summary>
        /// Deletes a single cache file for the given imageId if it exists.
        /// Safe to call from any thread.
        /// </summary>
        public void DeleteCacheFile(string imageId)
        {
            try
            {
                string cleanImageId = SanitizeFileName(imageId);
                string cacheFilePath = Path.Combine(_cacheDirectory, $"{cleanImageId}.img");
                if (File.Exists(cacheFilePath))
                {
                    File.Delete(cacheFilePath);
                    System.Diagnostics.Debug.WriteLine($"Deleted cached image: {cacheFilePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete cache file for {imageId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes multiple cache files asynchronously in background using parallelism for speed.
        /// </summary>
        public Task DeleteCacheFilesAsync(IEnumerable<string> imageIds)
        {
            return Task.Run(() =>
            {
                try
                {
                    var options = new System.Threading.Tasks.ParallelOptions
                    {
                        MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
                    };

                    Parallel.ForEach(imageIds, options, id =>
                    {
                        DeleteCacheFile(id);
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting cache files in parallel: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Converts a raw byte array into a WPF BitmapImage.
        /// </summary>
        private BitmapImage CreateBitmapImageFromBytes(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return GetDefaultPlaceholderImage();

            var bitmap = new BitmapImage();
            using (var stream = new MemoryStream(imageData))
            {
                stream.Position = 0;

                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Crucial for MemoryStream so it can be closed
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }

            // Freeze ensures the image can be accessed across UI threads if needed safely
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>
        /// Asynchronously saves the downloaded image bytes to the local cache file.
        /// </summary>
        private async Task SaveToCacheAsync(string filePath, byte[] data)
        {
            try
            {
                await File.WriteAllBytesAsync(filePath, data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving image to cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns a standard fallback image if the download fails.
        /// </summary>
        private BitmapImage GetDefaultPlaceholderImage()
        {
            try
            {
                // Fallback to local embedded resource placeholder if possible (ensure path matches)
                return new BitmapImage(new Uri("pack://application:,,,/Assets/Images/polinema-logo.png"));
            }
            catch
            {
                // Absolute fallback returning an empty image if somehow pack url fails
                return new BitmapImage(); 
            }
        }

        private string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }
    }
}