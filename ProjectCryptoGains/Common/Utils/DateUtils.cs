using System;
using System.Globalization;

namespace ProjectCryptoGains.Common.Utils
{
    class DateUtils
    {
        public static string? ConvertDateTimeToString(DateTime? date, string format = "yyyy-MM-dd HH:mm:ss")
        {
            return date.HasValue ? date.Value.ToString(format) : null;
        }

        public static DateTime ConvertStringToIsoDate(string date)
        {
            return DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static DateTime ConvertStringToIsoDateTime(string datetime)
        {
            return DateTime.ParseExact(datetime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        public static string GetTodayAsIsoDate()
        {
            return DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static bool IsValidDateFormat(string date, string format)
        {
            // Try to parse the date using the specified format
            return DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }
    }
}
