using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

using Altinn2Convert.Enums;

namespace Altinn2Convert.Models
{
    /// <summary>
    /// Representation of a base layout component
    /// </summary>
    public class BaseComponent
    {
        /// <summary>
        /// The component ID
        /// </summary>
        [JsonPropertyName("id")]
        public string ID { get; set; }

        /// <summary>
        /// The component type
        /// </summary>
        [JsonPropertyName("type")]
        public ComponentType Type { get; set; }

        /// <summary>
        /// Data model bindings for component
        /// </summary>
        [JsonPropertyName("dataModelBindings")]
        public Dictionary<string, string> DataModelBindings { get; set; }

        /// <summary>
        /// Text resource bindings for component
        /// </summary>
        [JsonPropertyName("textResourceBindings")]
        public Dictionary<string, string> TextResourceBindings { get; set; }

        /// <summary>
        /// Options (for components with options)
        /// </summary>
        [JsonPropertyName("options")]
        public List<Options> Options { get; set; }

        /// <summary>
        /// Is component read only
        /// </summary>
        [JsonPropertyName("readOnly")]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Is component required
        /// </summary>
        [JsonPropertyName("required")]
        public bool Required { get; set; }

        /// <summary>
        /// Custom type specification
        /// </summary>
        [JsonPropertyName("customType")]
        public string CustomType { get; set; }
    }

    /// <summary>
    /// Options
    /// </summary>
    public class Options
    {
        /// <summary>
        /// Label
        /// </summary>
        [JsonPropertyName("label")]
        public string Label { get; set; }

        /// <summary>
        /// Label
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
