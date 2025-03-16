namespace ProjectCryptoGains.Common.Utils
{
    public static class CsvUtils
    {
        public static string CsvEscapeValue(string value)
        {
            // Return "" for null/empty input
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            // Escape quotes and wrap if special chars present
            if (value.Contains("\"") || value.Contains(",") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            // Wrap in quotes if no special chars
            return $"\"{value}\"";
        }

        public static string CsvStripValue(string value)
        {
            // Return empty string for null or empty input
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            // Remove quotes, commas, and newlines
            return value
                .Replace("\"", "")
                .Replace(",", "")
                .Replace("\n", "");
        }
    }
}
