using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CBTSecureDesktop.Services
{
    public class ImageService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _cacheDirectory;
        private readonly string _optionCacheDirectory;
        private readonly string _baseUrl;
        private static readonly ConcurrentQueue<string> _downloadFailures = new();

        static ImageService()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public ImageService(string baseUrl = "http://127.0.0.1:8000/api/image/")
        {
            _baseUrl = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";

            // Set up local cache directory in AppData
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var cacheRoot = Path.Combine(appDataPath, "CBTSecureDesktop", "ImageCache");
            _cacheDirectory = cacheRoot;
            _optionCacheDirectory = Path.Combine(cacheRoot, "Options");

            Directory.CreateDirectory(_cacheDirectory);
            Directory.CreateDirectory(_optionCacheDirectory);
        }

        /// <summary>
        /// Retrieves a question image from cache or downloads it from the API if not cached.
        /// </summary>
        public async Task<BitmapImage> GetImageAsync(string imageId, string token = "")
        {
            if (string.IsNullOrWhiteSpace(imageId))
            {
                return GetDefaultPlaceholderImage();
            }

            string cleanImageId = SanitizeFileName(imageId);
            string cacheFilePath = Path.Combine(_cacheDirectory, $"{cleanImageId}.img");
            string resolvedToken = ResolveToken(token);
            string requestUrl = BuildRequestUrl($"{_baseUrl}{cleanImageId}", resolvedToken);

            return await GetOrDownloadImageAsync(cacheFilePath, requestUrl, cleanImageId);
        }

        /// <summary>
        /// Retrieves an option image from cache or downloads it from the API if not cached.
        /// </summary>
        public async Task<BitmapImage> GetOptionImageAsync(int optionId, string token = "")
        {
            if (optionId <= 0)
            {
                return GetDefaultPlaceholderImage();
            }

            string cacheFilePath = Path.Combine(_optionCacheDirectory, $"{optionId}.png");
            string resolvedToken = ResolveToken(token);
            string requestUrl = BuildRequestUrl($"{_baseUrl}option/{optionId}", resolvedToken);

            return await GetOrDownloadImageAsync(cacheFilePath, requestUrl, optionId.ToString());
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

        private async Task<BitmapImage> GetOrDownloadImageAsync(string cacheFilePath, string requestUrl, string imageId)
        {
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
                }
            }

            return await DownloadImageAndCacheAsync(requestUrl, cacheFilePath, imageId);
        }

        private async Task<BitmapImage> DownloadImageAndCacheAsync(string requestUrl, string cacheFilePath, string imageId)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                    await SaveToCacheAsync(cacheFilePath, imageBytes);
                    return CreateBitmapImageFromBytes(imageBytes);
                }

                string errorMessage = $"{imageId} - Status {(int)response.StatusCode} ({response.ReasonPhrase})";
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
            if (string.IsNullOrWhiteSpace(imageId))
            {
                return GetDefaultPlaceholderImage();
            }

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

            string resolvedToken = ResolveToken(token);
            string requestUrl = BuildRequestUrl($"{_baseUrl}{cleanImageId}", resolvedToken);
            return await DownloadImageAndCacheAsync(requestUrl, cacheFilePath, cleanImageId);
        }

        /// <summary>
        /// Refreshes a cached option image by forcing a re-download from the option image endpoint.
        /// </summary>
        public async Task<BitmapImage> RefreshOptionImageCacheAsync(int optionId, string token = "")
        {
            if (optionId <= 0)
            {
                return GetDefaultPlaceholderImage();
            }

            string cacheFilePath = Path.Combine(_optionCacheDirectory, $"{optionId}.png");

            if (File.Exists(cacheFilePath))
            {
                try
                {
                    File.Delete(cacheFilePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete option cache: {ex.Message}");
                }
            }

            string resolvedToken = ResolveToken(token);
            string requestUrl = BuildRequestUrl($"{_baseUrl}option/{optionId}", resolvedToken);
            return await DownloadImageAndCacheAsync(requestUrl, cacheFilePath, optionId.ToString());
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
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

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

        private static string ResolveToken(string token)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            var envToken = Environment.GetEnvironmentVariable("IMAGE_API_TOKEN");
            if (!string.IsNullOrWhiteSpace(envToken))
            {
                return envToken;
            }

            return ReadAppSetting("IMAGE_API_TOKEN") ?? string.Empty;
        }

        private static string? ReadAppSetting(string key)
        {
            try
            {
                var configType = Type.GetType("System.Configuration.ConfigurationManager, System.Configuration.ConfigurationManager");
                var appSettingsProperty = configType?.GetProperty("AppSettings", BindingFlags.Public | BindingFlags.Static);
                if (appSettingsProperty?.GetValue(null) is NameValueCollection appSettings)
                {
                    return appSettings[key];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read app setting '{key}': {ex.Message}");
            }

            return null;
        }

        private static string BuildRequestUrl(string baseRequestUrl, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return baseRequestUrl;
            }

            return $"{baseRequestUrl}?token={Uri.EscapeDataString(token)}";
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