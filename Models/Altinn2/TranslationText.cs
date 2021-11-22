using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Altinn2Convert.Models.Altinn2
{
    /// <summary>
    /// Representation of Translation Text object
    /// </summary>
    public class TranslationText
    {
        /// <summary>
        /// Text type
        /// </summary>
        [XmlAttribute("textType")]
        public string TextType { get; set; }

        /// <summary>
        /// Text code
        /// </summary>
        [XmlAttribute("textCode")]
        public string TextCode { get; set; }

        /// <summary>
        /// The text value
        /// </summary>
        [XmlText]
        public string Value { get; set; }
    }
}
