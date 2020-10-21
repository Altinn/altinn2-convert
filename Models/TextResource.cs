using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn2Convert.Models
{
    /// <summary>
    /// Represents a text resource object
    /// </summary>
    public class TextResource
    {
        /// <summary>
        /// The text resource ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// The text resource value
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
