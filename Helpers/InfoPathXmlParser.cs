using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace Altinn2Convert.Helpers
{
    public class InfoPathXmlParser
    {
        private string _rootPath;

        public InfoPathXmlParser( string tmpDir, string language, string xsnPath)
        {
            var e = new CabLib.Extract();
            var extractedXsnPath = Path.Join(tmpDir, "form", language);
            e.ExtractFile(xsnPath, extractedXsnPath);
            _rootPath = extractedXsnPath;
        }

        public XmlDocument GetManifest()
        {
            var x = new XmlDocument();
            x.Load(Path.Join(_rootPath, "manifest.xsf"));
            return x;
        }

        public XmlDocument GetPage(string page)
        {
            var x = new XmlDocument();
            x.Load(Path.Join(_rootPath, page));
            return x;
        }
        
        public string GetXSDDocument()
        {
            var x = new XmlDocument();
            x.Load(Path.Join(_rootPath, "myschema.xsd"));
            var schemaLocation = x.GetElementsByTagName("import","http://www.w3.org/2001/XMLSchema").Item(0).Attributes.GetNamedItem("schemaLocation").Value;
            return File.ReadAllText(Path.Join(_rootPath,schemaLocation));
        }
        
    }
}