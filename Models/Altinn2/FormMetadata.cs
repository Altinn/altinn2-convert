using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Altinn2Convert.Models.Altinn2.FormMetadata
{
    [XmlRoot(ElementName="FormPages")]
	public class FormMetadata
    {
		[XmlElement(ElementName="Page")]
		public List<Page> Page { get; set; }

		[XmlAttribute(AttributeName="formatVersion")]
		public string FormatVersion { get; set; }

		[XmlAttribute(AttributeName="fp", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Fp { get; set; }
	}

	[XmlRoot(ElementName="Property")]
	public class Property
    {
		[XmlAttribute(AttributeName="name")]
		public string Name { get; set; }

		[XmlAttribute(AttributeName="textCode")]
		public string TextCode { get; set; }

		[XmlAttribute(AttributeName="value")]
		public string Value { get; set; }
	}

	[XmlRoot(ElementName="Page")]
	public class Page
    {
		[XmlElement(ElementName="Property")]
		public List<Property> Property { get; set; }
        
		[XmlAttribute(AttributeName="name")]
		public string Name { get; set; }
	}
}
