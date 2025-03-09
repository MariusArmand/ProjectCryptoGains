using System;

namespace ProjectCryptoGains.Common.Utils
{
    class ExceptionUtils
    {
        public static string BuildErrorMessage(Exception ex)
        {
            if (ex.InnerException != null)
            {
                return $"{ex.Message}{Environment.NewLine}{ex.InnerException.Message}";
            }

            return ex.Message;
        }

        public class DatabaseReadException : Exception
        {
            public DatabaseReadException(string message) : base(message)
            {
                // This constructor calls the base Exception class's constructor with the provided message
            }

            public DatabaseReadException(string message, Exception innerException) : base(message, innerException)
            {
                // This constructor chains to the base Exception constructor that accepts both a message and an inner exception
            }
        }

        public class DatabaseWriteException : Exception
        {
            public DatabaseWriteException(string message) : base(message) { }
            public DatabaseWriteException(string message, Exception innerException) : base(message, innerException) { }
        }

        public class ValidationException : Exception
        {
            public ValidationException(string message) : base(message) { }
            public ValidationException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
