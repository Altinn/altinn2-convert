using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Altinn2Convert.Models
{
    /// <summary>
    /// Service that handles extraction of texts.
    /// </summary>
    public class ServiceEditionVersion
    {
        /// <summary>
        /// Package info
        /// </summary>
        [XmlArray]
        [XmlArrayItem(ElementName = "Property")]
        public List<Property> PackageInfo { get; set; }

        /// <summary>
        /// Data areas
        /// </summary>
        [XmlArray]
        [XmlArrayItem(ElementName = "DataArea")]
        public List<DataArea> DataAreas { get; set; }

        /// <summary>
        /// Translations
        /// </summary>
        public Translations Translations { get; set; }
    }

    /// <summary>
    /// Data Area
    /// </summary>
    public class DataArea
    {
        /// <summary>
        /// Data area type
        /// </summary>
        [XmlAttribute("type")]
        public string Type { get; set; }

        /// <summary>
        /// Data area properties
        /// </summary>
        [XmlArray]
        [XmlArrayItem(ElementName = "Property")]
        public List<Property> Properties { get; set; }

        /// <summary>
        /// Logical form 
        /// </summary>
        public LogicalForm LogicalForm { get; set; }

        /// <summary>
        /// Translation texts
        /// </summary>
        [XmlArray]
        [XmlArrayItem(ElementName = "Text")]
        public List<TranslationText> Texts { get; set; }
    }

    /// <summary>
    /// Logical form 
    /// </summary>
    public class LogicalForm
    {
        /// <summary>
        /// Logical form properties
        /// </summary>
        [XmlArray]
        [XmlArrayItem(ElementName = "Property")]
        public List<Property> Properties { get; set; }

        /// <summary>
        /// Data area properties
        /// </summary>
        [XmlArray]
        [XmlArrayItem(ElementName = "File")]
        public List<ServiceFile> Files { get; set; }

        /// <summary>
        /// Translation texts
        /// </summary>
        public List<TranslationText> Texts { get; set; }
    }

    /// <summary>
    /// Property
    /// </summary>
    public class Property
    {
        /// <summary>
        /// Name
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [XmlAttribute("value")]
        public string Value { get; set; }
    }

    /// <summary>
    /// Translation
    /// </summary>
    public class Translations
    {
        /// <summary>
        /// Translation file list
        /// </summary>
        [XmlArray]
        [XmlArrayItem(ElementName = "File")]
        public List<ServiceFile> Files { get; set; }
    }

    /// <summary>
    /// Translation file
    /// </summary>
    public class ServiceFile
    {
        /// <summary>
        /// Language
        /// </summary>
        [XmlAttribute("language")]
        public string Language { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        [XmlAttribute("version")]
        public string Version { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// File type
        /// </summary>
        [XmlAttribute("fileType")]
        public string FileType { get; set; }
    }
}