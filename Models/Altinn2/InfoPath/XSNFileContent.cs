using System;
using System.Collections.Generic;
using System.Xml;

namespace Altinn2Convert.Models.Altinn2.InfoPath
{
    public class XSNFileContent
    {
        public string XSDDocument { get; set; }

        public XmlDocument Manifest { get; set; }

        public Dictionary<string, XmlDocument> Pages { get; set; }

        // public byte[] PrimaryXsd { get; set; } 
    }

}