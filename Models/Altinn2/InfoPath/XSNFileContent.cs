using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace Altinn2Convert.Models.Altinn2.InfoPath
{
    public class XSNFileContent
    {
        public string XSDDocument { get; set; }

        public XmlDocument Manifest { get; set; }

        public Dictionary<string, XDocument> Pages { get; set; }

        // public byte[] PrimaryXsd { get; set; } 
    }
}