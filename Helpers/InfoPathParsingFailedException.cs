using System;
using System.Runtime.Serialization;

namespace Altinn2Convert.Helpers
{
    /// <summary>
    /// The exception that is thrown when an InfoPath parsing operation fails </summary>
    [Serializable]
    public class InfoPathParsingFailedException : Exception
    {
        /// <summary>
        /// InfoPath parsing failed exception
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
        public InfoPathParsingFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// InfoPath parsing failed exception
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="innerException">Inner Exception</param>
        public InfoPathParsingFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// InfoPath parsing failed exception
        /// </summary>
        /// <param name="message">Message</param>
        public InfoPathParsingFailedException(string message) : base(message)
        {
        }

        /// <summary>
        /// InfoPath parsing failed exception
        /// </summary>
        public InfoPathParsingFailedException()
        {
        }
    }
}
