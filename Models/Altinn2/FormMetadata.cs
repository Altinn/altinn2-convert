using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Altinn2Convert.Models.Altinn2.FormMetadata
{
    public class FormMetadata
    {
        public FormMetadata(string name, string caption, string transform, int sequence, string pageType)
        {
            Name = name;
            Caption = caption;
            Transform = transform;
            Sequence = sequence;
            PageType = pageType;
        }

        public string Name { get; internal set; }

        public string Caption { get; internal set; }

        private static readonly Regex _invalidChars = new Regex(@"[^a-zA-Z0-9-_.]");

        private static readonly Regex _dash = new Regex(@"[-]+");
        
        private static readonly Regex _dotDash = new Regex(@"[.][-]");

        private string _sanitize (string input)
        {
            input = input
                .Replace("æ", "ae")
                .Replace("ø", "oe")
                .Replace("å", "aa")
                .Replace("Æ", "Ae")
                .Replace("Ø", "Oe")
                .Replace("Å", "Aa");
            return _dotDash.Replace(_dash.Replace(_invalidChars.Replace(input, "-"), "-"), ".");
        }
        
        public string A3PageName
        {
            get { return SanitizedCaption; }
        }
        
        public string SanitizedCaption
        { 
            get { return _sanitize(Caption); } 
        }
        
        public string SanitizedName 
        {
            get { return _sanitize(Name); }
        }

        public string Transform { get; internal set; }
        
        public int Sequence { get; internal set; }

        public string PageType { get; internal set; }
    }

    [XmlRoot(ElementName="FormPages")]
	public class FormPages
    {
		[XmlElement(ElementName="Page")]
		public List<Page> Page { get; set; }

		[XmlAttribute(AttributeName="formatVersion")]
		public string FormatVersion { get; set; }

		[XmlAttribute(AttributeName="fp", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Fp { get; set; }

        public List<FormMetadata> GetFormMetadata()
        {
            return this.Page.Select(p => new FormMetadata(
                p.Name,
                p.Property.Find(m => m.Name == "Caption")?.TextCode,
                p.Property.Find(m => m.Name == "Transform")?.Value,
                int.Parse(p.Property.Find(m => m.Name == "Sequence")?.Value ?? "0"),
                p.Property.Find(m => m.Name == "PageType")?.Value))
                .OrderBy(m => m.Sequence).ToList();
        }
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
