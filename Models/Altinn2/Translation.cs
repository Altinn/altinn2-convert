using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Altinn2Convert.Models.Altinn2
{
    /// <summary>
    /// Representation of Translation XML document
    /// </summary>
    public class Translation
    {
        /// <summary>
        /// The language of the translation
        /// </summary>
        [XmlAttribute("language")]
        public string Language { get; set; }

        /// <summary>
        /// Data areas
        /// </summary>
        [XmlArray]
        [XmlArrayItem(ElementName = "DataArea")]
        public List<DataArea> DataAreas { get; set; }
    }
}
