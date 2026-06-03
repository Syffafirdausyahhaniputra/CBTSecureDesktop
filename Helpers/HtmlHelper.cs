using System;
using System.Net;
using System.Text.RegularExpressions;

namespace CBTSecureDesktop.Helpers
{
    public static class HtmlHelper
    {
        /// <summary>
        /// Mengonversi teks raw HTML dari WYSIWYG editor menjadi Plain Text murni.
        /// </summary>
        /// <param name="rawHtml">String HTML dari database</param>
        /// <returns>Plain text yang sudah dibersihkan</returns>
        public static string ConvertToPlainText(string rawHtml)
        {
            if (string.IsNullOrWhiteSpace(rawHtml))
            {
                return string.Empty;
            }

            string result = rawHtml;

            // 1. Replace semua variasi tag <br> menjadi newline (\n)
            result = Regex.Replace(result, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);

            // 2. Replace tag penutup blok HTML (seperti </div> dan </p>) menjadi newline (\n)
            result = Regex.Replace(result, @"</(div|p)>", "\n", RegexOptions.IgnoreCase);

            // 3. Hapus semua sisa tag HTML (<...>) yang masih ada
            result = Regex.Replace(result, @"<[^>]+>", string.Empty);

            // 4. Decode HTML entities untuk mengembalikan &nbsp; jadi spasi dan karakter escape lainnya
            result = WebUtility.HtmlDecode(result);

            // Standarisasi line break jikalau ada sisa format \r\n menjadi \n
            result = result.Replace("\r\n", "\n").Replace("\r", "\n");

            // 5. Bersihkan newline berlebih (3 atau lebih '\n' yang beruntun akan dijadikan 2 '\n' saja).
            result = Regex.Replace(result, @"(\n\s*){3,}", "\n\n");

            // Hapus spasi atau newline kosong yang tersisa di awal dan di akhir string
            return result.Trim();
        }
    }
}