using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var extractedXsnPath = Path.Join(tmpDir, "form", language);
            Directory.CreateDirectory(extractedXsnPath);
            var proc = new ProcessStartInfo();
            proc.FileName = @"C:\Windows\System32\expand.exe";
            proc.ArgumentList.Add(xsnPath);
            proc.ArgumentList.Add("-F:*");
            proc.ArgumentList.Add(extractedXsnPath);
            var p = Process.Start(proc);
            p.WaitForExit();
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