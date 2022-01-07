using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

using Altinn2Convert.Models.Altinn2;
using FormFieldPrefill = Altinn2Convert.Models.Altinn2.FormFieldPrefill.FormFieldPrefill;
using FormMetadata = Altinn2Convert.Models.Altinn2.FormMetadata.FormMetadata;
using FormPages = Altinn2Convert.Models.Altinn2.FormMetadata.FormPages;

namespace Altinn2Convert.Helpers
{
    public class TulPackageParser
    {
        private string _rootPath;

        private ServiceEditionVersion _manifest;

        public XDocument Xmanifest { get; }

        public TulPackageParser(string rootPath)
        {
            _rootPath = rootPath;
            var manifest_file = Path.Join(rootPath, "manifest.xml");
            Xmanifest = XDocument.Load(manifest_file);
            using var fileStream = File.Open(manifest_file, FileMode.Open);
            XmlSerializer serializer = new XmlSerializer(typeof(ServiceEditionVersion));
            _manifest = (ServiceEditionVersion)serializer.Deserialize(fileStream);
        }

        public ServiceEditionVersion GetServiceEditionVersion()
        {
            return _manifest;
        }

        public List<string> GetLanguages()
        {
            return _manifest.Translations.Files.Select(f => TULToISOLang(f.Language)).ToList();
        }

        public XmlDocument GetTranslationXml(string language)
        {
            var lan = ISOToTULLang(language);
            var file = _manifest.Translations.Files.First(f => f.Language == lan);
            var x = new XmlDocument();
            x.Load(Path.Join(_rootPath, file.Name));
            return x;
        }

        public Translation GetTranslationParsed(string language)
        {
            var lan = ISOToTULLang(language);
            var file = _manifest.Translations.Files.First(f => f.Language == lan);
            var filePath = Path.Join(_rootPath, file.Name);
            using var fileStream = File.Open(filePath, FileMode.Open);
            XmlSerializer serializer = new XmlSerializer(typeof(Translation));
            return (Translation)serializer.Deserialize(fileStream);
        }

        public List<FormMetadata> GetFormMetadata()
        {
            var path = _manifest.DataAreas.Find(d => d.Type == "Form")?.LogicalForm.Files.Find(f => f.FileType == "FormPageMetadata");
            if (path == null)
            {
                return null;
            }

            // var x = new XmlDocument();
            // x.Load(Path.Join(_rootPath, path.Name));
            // return x;
            using var fileStream = File.Open(Path.Join(_rootPath, path.Name), FileMode.Open);
            var serializer = new XmlSerializer(typeof(FormPages));
            return ((FormPages)serializer.Deserialize(fileStream)).GetFormMetadata();
        }

        public XmlDocument GetFormTrack()
        {
            var path = _manifest.DataAreas.Find(d => d.Type == "Form")?.LogicalForm.Files.Find(f=>f.FileType == "FormTrack")?.Name;
            if (path == null)
            {
                return null;
            }

            var x = new XmlDocument();
            x.Load(Path.Join(_rootPath, path));
            return x;
        }

        public FormFieldPrefill GetFormFieldPrefill()
        {
            var path = _manifest.DataAreas.Find(d => d.Type == "Form")?.LogicalForm.Files.Find(f=>f.FileType == "FormFieldPrefill");
            if (path == null)
            {
                return null;
            }

            using var fileStream = File.Open(Path.Join(_rootPath, path.Name), FileMode.Open);
            var serializer = new XmlSerializer(typeof(FormFieldPrefill));
            return (FormFieldPrefill)serializer.Deserialize(fileStream);
        }

        public string GetXsnPath(string language)
        {
            var lan = ISOToTULLang(language);
            var xsnPath = _manifest.DataAreas
                    .Find(d => d.Type == "Form")
                    ?.LogicalForm
                    .Files
                    .Find(f=>f.FileType == "FormTemplate" && f.Language == lan)?.Name;
            if (xsnPath == null)
            {
                return null;
            }

            return Path.Join(_rootPath, xsnPath);
        }

        public XmlDocument GetAttachmentTypes()
        {
            var path = _manifest?.DataAreas?.Find(d => d.Type == "AttachmentTypes")?.Files?.Find(f=>f.FileType == "AttachmentTypes");
            if (path == null)
            {
                return null;
            }

            var x = new XmlDocument();
            x.Load(Path.Join(_rootPath, path.Name));
            return x;
        }

        public XmlDocument GetAuthorizationRules()
        {
            var path = _manifest.DataAreas.Find(d => d.Type == "Security")?.Files.Find(f=>f.FileType == "AuthorizationRules");
            var x = new XmlDocument();
            x.Load(Path.Join(_rootPath, path!.Name));
            return x;
        }

        public XmlDocument GetWorkflowDefinition()
        {
            var path = _manifest.DataAreas.Find(d => d.Type == "Workflow")?.Files.Find(f=>f.FileType == "WorkflowDefinition");
            var x = new XmlDocument();
            x.Load(Path.Join(_rootPath, path!.Name));
            return x;
        }

        public string GetOrg()
        {
            return Xmanifest.XPathSelectElement("/ServiceEditionVersion/DataAreas/DataArea[@type=\"Service\"]/Property[@name=\"ServiceOwnerCode\"]")?.Attribute("value")?.Value;
        }

        public string GetApp()
        {
            return Xmanifest.XPathSelectElement("/ServiceEditionVersion/DataAreas/DataArea[@type=\"Service\"]/Property[@name=\"ServiceName\"]")?.Attribute("value")?.Value;
        }
        
        public static string TULToISOLang(string lang)
        {
            switch (lang)
            {
                case "1033":
                    return "en";
                case "1044":
                    return "nb";
                case "2068":
                    return "nn";
                case "1083":
                    return "se";
            }

            throw new ArgumentException("Unknown TUL language " + lang);
        }

        public static string ISOToTULLang(string language)
        {
            switch (language)
            {
                case "en":
                    return "1033";
                case "nb":
                    return "1044";
                case "nn":
                    return "2068";
                case "se":
                    return "1083";
            }

            throw new ArgumentException("Unknown TUL language " + language);
        }
    }
}