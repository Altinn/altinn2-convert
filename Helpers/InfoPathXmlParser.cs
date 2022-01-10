using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Altinn2Convert.Helpers
{
    public class InfoPathXmlParser
    {
        private string _rootPath;

        public void Extract(string tmpDir, string language, string xsnPath)
        {
            var e = new CabLib.Extract();
            var extractedXsnPath = Path.Join(tmpDir, "form", language);

            e.ExtractFile(xsnPath, extractedXsnPath);
            e.CleanUp();
            _rootPath = extractedXsnPath;
        }

        public XmlDocument GetManifest()
        {
            var x = new XmlDocument();
            x.Load(Path.Join(_rootPath, "manifest.xsf"));
            return x;
        }

        public Dictionary<string, XDocument> GetPages(List<string> pageIds)
        {
            var ret = new Dictionary<string, XDocument>();
            foreach (var page in pageIds)
            {
                ret[page] = XDocument.Load(Path.Join(_rootPath, page));
            }

            return ret;
        }
        
        public string GetXSDDocument()
        {
            var path = Path.Join(_rootPath, "myschema.xsd");
            if (!File.Exists(path))
            {
                return null;
            }

            var x = new XmlDocument();
            x.Load(path);
            var schemaLocation = x.GetElementsByTagName("import", "http://www.w3.org/2001/XMLSchema").Item(0).Attributes.GetNamedItem("schemaLocation").Value;
            return File.ReadAllText(Path.Join(_rootPath, schemaLocation));
        }
        
    }
}