using System;
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

        public ImageService(string baseUrl = "http://127.0.0.1:8000/api/image/")
        {
            _baseUrl = baseUrl;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

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

            // 2. Download from API
            try
            {
                string requestUrl = $"{_baseUrl}{imageId}";
                if (!string.IsNullOrEmpty(token))
                {
                    requestUrl += $"?token={token}";
                }

                // Request the image as a byte array
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                    // 3. Save to Cache
                    _ = SaveToCacheAsync(cacheFilePath, imageBytes);

                    // 4. Convert and Return
                    return CreateBitmapImageFromBytes(imageBytes);
                }
                else
                {
                    string errorMessage = $"Gagal mendownload gambar '{imageId}'. Status: {response.StatusCode}";
                    System.Diagnostics.Debug.WriteLine(errorMessage);
                    System.Windows.MessageBox.Show(errorMessage, "Download Gambar Gagal", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return GetDefaultPlaceholderImage();
                }
            }
            catch (HttpRequestException httpEx)
            {
                string errorMessage = $"Tidak dapat mendownload gambar '{imageId}'. Pastikan server menyala.\nError: {httpEx.Message}";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                System.Windows.MessageBox.Show(errorMessage, "Error Koneksi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return GetDefaultPlaceholderImage();
            }
            catch (Exception ex)
            {
                string errorMessage = $"Terjadi kesalahan saat mengambil gambar '{imageId}': {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                System.Windows.MessageBox.Show(errorMessage, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return GetDefaultPlaceholderImage();
            }
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