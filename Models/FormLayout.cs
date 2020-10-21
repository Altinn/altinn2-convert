using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Altinn2Convert.Models
{
    /// <summary>
    /// Representation of form layout json file
    /// </summary>
    public class FormLayout
    {
        /// <summary>
        /// Layout data object
        /// </summary>
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }

    /// <summary>
    /// Representation of form layout data object
    /// </summary>
    public class Data
    {
        /// <summary>
        /// Layout list of components
        /// </summary>
        [JsonPropertyName("layout")]
        public List<BaseComponent> Layout { get; set; }
    }
}
