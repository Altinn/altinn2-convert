﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Altinn2Convert.Models.Altinn3
{
    /// <summary>
    /// Representation of a text resource file structure
    /// </summary>
    public class TextResource
    {
        /// <summary>
        /// Language of the text resource
        /// </summary>
        [JsonProperty("language")]
        public string Language { get; set; }

        /// <summary>
        /// Collection of text resource items
        /// </summary>
        [JsonProperty("resources")]
        public List<TextResourceItem> Resources { get; set; }
    }
    
    /// <summary>
    /// Represents a text resource item
    /// </summary>
    public class TextResourceItem
    {
        /// <summary>
        /// The text resource ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// The text resource value
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
