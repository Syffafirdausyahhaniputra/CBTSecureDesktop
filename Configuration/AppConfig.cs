namespace CBTSecureDesktop.Configuration
{
    /// <summary>
    /// Centralized application configuration with encoded defaults.
    /// Values can be overridden by environment variables for easier maintenance.
    /// </summary>
    public static class AppConfig
    {
        private static readonly string[] _dbConnectionBase64Parts =
        {
            "U2VydmVyPWNidC5ydW50aW1lL",
            "ndlYi5pZDtQb3J0PTMzMDY7RGF0Y",
            "WJhc2U9c2tyaXBzaS1jYnQ7VXNlc",
            "iBJZD1za3JpcHNpLWNidDtQYXNzd",
            "29yZD1pNDd4ODJ5d2lOTkJ6TDRuOw=="
        };

        private static readonly string[] _apiBaseUrlBase64Parts =
        {
            "aHR0cHM6Ly9jYnQucnVudGltZS53",
            "ZWIuaWQ="
        };

        public static string GetDatabaseConnectionString()
        {
            return GetValue("CBT_DB_CONNECTION", _dbConnectionBase64Parts);
        }

        public static string GetApiBaseUrl()
        {
            return GetValue("CBT_API_BASE_URL", _apiBaseUrlBase64Parts).TrimEnd('/');
        }

        public static string GetImageApiBaseUrl()
        {
            return $"{GetApiBaseUrl()}/api/image/";
        }

        private static string GetValue(string environmentKey, IReadOnlyList<string> encodedParts)
        {
            var environmentValue = Environment.GetEnvironmentVariable(environmentKey);
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                return environmentValue;
            }

            var encoded = string.Concat(encodedParts);
            var bytes = Convert.FromBase64String(encoded);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
