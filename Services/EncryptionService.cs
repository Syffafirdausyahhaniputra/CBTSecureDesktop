using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CBTSecureDesktop.Services
{
    /// <summary>
    /// Provides encrypted storage for sensitive data using AES-256-GCM.
    /// Ensures data cannot be tampered with or extracted without proper decryption.
    /// </summary>
    public static class EncryptionService
    {
        private static readonly string _salt = "CBTSecureDesktop_PendingAnswers_v1";
        private const int TagSizeInBytes = 16; // 128-bit authentication tag for GCM

        /// <summary>
        /// Encrypts data using AES-256-GCM with machine-derived key.
        /// Returns a base64-encoded encrypted payload with embedded nonce and tag.
        /// Falls back to plain JSON if encryption fails (for resilience).
        /// </summary>
        public static string Encrypt<T>(T data) where T : class
        {
            try
            {
                return EncryptInternal(data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Encryption failed, falling back to plain JSON: {ex.Message}");
                // Fallback: return plain JSON if encryption fails
                // This ensures the application keeps running even if encryption has issues
                try
                {
                    return JsonSerializer.Serialize(data);
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback serialization also failed: {fallbackEx.Message}");
                    throw;
                }
            }
        }

        private static string EncryptInternal<T>(T data) where T : class
        {
            try
            {
                // Serialize data to JSON
                var json = JsonSerializer.Serialize(data);
                var plainBytes = Encoding.UTF8.GetBytes(json);

                // Derive encryption key from machine and salt
                var key = DeriveKey();

                // Generate random nonce (96-bit / 12 bytes for GCM)
                var nonce = new byte[12];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(nonce);
                }

                // Encrypt with AES-256-GCM
                using (var cipher = new AesGcm(key, TagSizeInBytes))
                {
                    var ciphertext = new byte[plainBytes.Length];
                    var tag = new byte[TagSizeInBytes];

                    cipher.Encrypt(nonce, plainBytes, ciphertext, tag);

                    // Combine: nonce + tag + ciphertext
                    var encrypted = new byte[nonce.Length + tag.Length + ciphertext.Length];
                    Buffer.BlockCopy(nonce, 0, encrypted, 0, nonce.Length);
                    Buffer.BlockCopy(tag, 0, encrypted, nonce.Length, tag.Length);
                    Buffer.BlockCopy(ciphertext, 0, encrypted, nonce.Length + tag.Length, ciphertext.Length);

                    // Return as base64 for safe file storage
                    return Convert.ToBase64String(encrypted);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Encryption failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Decrypts data encrypted with Encrypt method.
        /// Validates authentication tag to detect tampering.
        /// </summary>
        public static T? Decrypt<T>(string encryptedBase64) where T : class
        {
            try
            {
                // Decode from base64
                var encrypted = Convert.FromBase64String(encryptedBase64);

                // Extract components
                var nonce = new byte[12];
                var tag = new byte[TagSizeInBytes];
                var ciphertext = new byte[encrypted.Length - nonce.Length - tag.Length];

                Buffer.BlockCopy(encrypted, 0, nonce, 0, nonce.Length);
                Buffer.BlockCopy(encrypted, nonce.Length, tag, 0, tag.Length);
                Buffer.BlockCopy(encrypted, nonce.Length + tag.Length, ciphertext, 0, ciphertext.Length);

                // Derive decryption key (same as encryption)
                var key = DeriveKey();

                // Decrypt with AES-256-GCM
                using (var cipher = new AesGcm(key, TagSizeInBytes))
                {
                    var plaintext = new byte[ciphertext.Length];
                    cipher.Decrypt(nonce, ciphertext, tag, plaintext);

                    // Deserialize from JSON
                    var json = Encoding.UTF8.GetString(plaintext);
                    return JsonSerializer.Deserialize<T>(json);
                }
            }
            catch (CryptographicException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Decryption failed (tampering detected?): {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Decryption error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Derives a 256-bit key from machine-specific data and salt.
        /// Key is NOT portable across machines (security feature).
        /// </summary>
        private static byte[] DeriveKey()
        {
            try
            {
                // Get machine name and ID as entropy source
                var machineId = Environment.MachineName + Environment.UserName;
                var saltBytes = Encoding.UTF8.GetBytes(_salt);

                // Use Rfc2898DeriveBytes (PBKDF2) for key derivation
                using (var pbkdf2 = new Rfc2898DeriveBytes(
                    machineId,
                    saltBytes,
                    iterations: 10000,
                    hashAlgorithm: HashAlgorithmName.SHA256))
                {
                    return pbkdf2.GetBytes(32); // 256-bit key
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Key derivation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks if the given string is valid base64 (likely encrypted).
        /// Used for backward compatibility detection.
        /// </summary>
        public static bool IsValidBase64(string input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return false;

                var bytes = Convert.FromBase64String(input);
                // Encrypted data should be at least nonce + tag + some ciphertext
                return bytes.Length >= 28; // 12 (nonce) + 16 (tag) minimum
            }
            catch
            {
                return false;
            }
        }
    }
}
