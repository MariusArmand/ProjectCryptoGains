﻿using System.Data;
using static ProjectCryptoGains.Common.Utils.Utils;

namespace ProjectCryptoGains.Common.Utils
{
    public static class ReaderUtils
    {
        public static decimal GetDecimalOrDefault(this IDataReader reader, int columnIndex, decimal defaultValue = 0.0000000000m)
        {
            return reader.IsDBNull(columnIndex) ? defaultValue : ConvertStringToDecimal(reader.GetString(columnIndex), defaultValue);
        }

        public static decimal? GetDecimalOrNull(this IDataReader reader, int columnIndex)
        {
            return reader.IsDBNull(columnIndex) ? null : ConvertStringToDecimal(reader.GetString(columnIndex));
        }

        public static string GetStringOrEmpty(this IDataReader reader, int columnIndex)
        {
            return reader.IsDBNull(columnIndex) ? "" : reader.GetString(columnIndex);
        }

        public static string? GetStringOrNull(this IDataReader reader, int columnIndex)
        {
            return reader.IsDBNull(columnIndex) ? null : reader.GetString(columnIndex);
        }
    }
}