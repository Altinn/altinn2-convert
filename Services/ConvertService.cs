using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Altinn2Convert.Helpers;
using Altinn2Convert.Models.Altinn2;
using Altinn2Convert.Models.Altinn3;
using Newtonsoft.Json;

namespace Altinn2Convert.Services
{
    public class ConvertService
    {
        public JsonSerializerSettings serializerOptions { get; set; } = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public async Task<Altinn2AppData> ParseAltinn2File(string zipPath, string outDir = null)
        {
            var a2 = new Altinn2AppData();
            var tmpDir = outDir ?? $"tmpDir{new Random().Next(10000)}";
            if (!File.Exists(zipPath))
            {
                throw new Exception($"Altinn2 file '{zipPath}' does not exist");
            }

            try
            {
                ZipFile.ExtractToDirectory(zipPath, tmpDir);
                var tulPackageParser = new TulPackageParser(tmpDir);
                a2.Languages.AddRange(tulPackageParser.GetLanguages());
                a2.ServiceEditionVersion = tulPackageParser.GetServiceEditionVersion();
                a2.FormMetadata = tulPackageParser.GetFormMetadata();
                a2.AttachmentTypes = tulPackageParser.GetAttachmentTypes();
                a2.AutorizationRules = tulPackageParser.GetAuthorizationRules();
                a2.FormFieldPrefill = tulPackageParser.GetFormFieldPrefill();
                a2.FormTrack = tulPackageParser.GetFormTrack();

                foreach (var language in a2.Languages)
                {
                    // Handle translations
                    a2.TranslationsParsed[language] = tulPackageParser.GetTranslationParsed(language);
                    a2.TranslationsXml[language] = tulPackageParser.GetTranslationXml(language);

                    // Parse xsn content
                    var xsnPath = tulPackageParser.GetXsnPath(language);
                    if (xsnPath == null)
                    {
                        continue;
                    }

                    var infoPath = new InfoPathXmlParser(tmpDir, language, xsnPath);

                    a2.XSNFiles[language] = new Models.Altinn2.InfoPath.XSNFileContent
                    {
                        XSDDocument = infoPath.GetXSDDocument(),
                        Manifest = infoPath.GetManifest(),
                        Pages = infoPath.GetPages(a2.FormMetadata.Select(m => m.Transform).ToList()),
                    };
                }
            }
            finally
            {
                if (outDir == null)
                {
                    Directory.Delete(tmpDir, true);
                }
            }

            return a2;
        }

        public async Task DumpAltinn2Data(Altinn2AppData a2, string path)
        {
            await File.WriteAllTextAsync(path, JsonConvert.SerializeObject(a2, Newtonsoft.Json.Formatting.Indented, serializerOptions), Encoding.UTF8);
        }

        public async Task<Altinn3AppData> Convert(Altinn2AppData a2)
        {
            var a3 = new Altinn3AppData();

            // Add extra texts
            a2.Languages.ForEach(language => 
            {
                var t = a2.TranslationsXml[language];
                var serviceName = t.SelectSingleNode("//Translation/DataAreas/DataArea[@type=\"Service\"]/Texts/Text[@textType=\"ServiceName\"]");
                a3.AddText(language, "ServiceName", serviceName?.InnerText);
                var serviceEditionName = t.SelectSingleNode("//Translation/DataAreas/DataArea[@type=\"ServiceEdition\"]/Texts/Text[@textType=\"ServiceEditionName\"]");
                a3.AddText(language, "ServiceEditionName", serviceEditionName?.InnerText);
                var receiptText = t.SelectSingleNode("//Translation/DataAreas/DataArea[@type=\"ServiceEdition\"]/Texts/Text[@textType=\"ReceiptText\"]");
                a3.AddText(language, "ReceiptText", receiptText?.InnerText);
                var receiptEmailText = t.SelectSingleNode("//Translation/DataAreas/DataArea[@type=\"ServiceEdition\"]/Texts/Text[@textType=\"ReceiptEmailText\"]");
                a3.AddText(language, "ReceiptEmailText", receiptEmailText?.InnerText);
                var receiptInformationText = t.SelectSingleNode("//Translation/DataAreas/DataArea[@type=\"ServiceEdition\"]/Texts/Text[@textType=\"ReceiptInformationText\"]");
                a3.AddText(language, "ReceiptInformationText", receiptInformationText?.InnerText);
                
                // Add translation for page name
                a2.FormMetadata?.ForEach(formMetadata =>
                {
                    var pageDisplayName = t.SelectSingleNode($"//Translation/DataAreas/DataArea[@type=\"Form\"]/LogicalForm/Texts/Text[@textType=\"PageDisplayName\"][@textCode=\"{formMetadata.Name}\"]");
                    a3.AddText(language, formMetadata.A3PageName, pageDisplayName?.InnerText);
                });

                foreach (XmlElement helpText in t.SelectNodes($"//Translation/DataAreas/DataArea[@type=\"Form\"]/LogicalForm/Texts/Text[@textType=\"HelpText\"]"))
                {
                    a3.AddText(language, helpText?.GetAttribute("textCode"), helpText?.InnerText);
                }
            });

            // Add layouts and texts for layout components 
            a2.FormMetadata?.OrderBy(f => f.Sequence).ToList().ForEach(formMetadata =>
            {
                var layouts = new Models.Altinn3.layout.Layout();

                // Read layout only from 
                var pages = a2.Languages.Select(language => a2.XSNFiles[language].Pages[formMetadata.Transform]).ToList();
                
                var layoutLists = a2.Languages.Select(language =>
                    Page2Layout.GetLayoutComponents(a2.XSNFiles[language].Pages[formMetadata.Transform], formMetadata.Name)).ToList();
                
                var mergedLang = Page2Layout.MergeLang(a2.Languages, layoutLists, textKeyPrefix: formMetadata.SanatizedName);
                
                // Add Layout to List of layout files
                a3.AddLayout(formMetadata.A3PageName, mergedLang.Layout);

                // Add texts for this page
                a3.AddTexts(mergedLang.Texts);
            });

            // TODO: Add form prefill
            a3.ModelName = "convertedMessage";
            a3.Prefill = PrefillConverter.Convert(a2.FormFieldPrefill);

            // Copy xsd from one of the xsn files
            a3.Xsd = a2.XSNFiles.Values?.FirstOrDefault()?.XSDDocument;

            // TODO: Add extra layout field for attachment types
            // a2.AttachmentTypes
            return a3;
        }

        public async Task WriteAltinn3Files(Altinn3AppData A3, string path)
        {
            // Write settings
            var settingsFolder = Path.Join(path, "ui");
            Directory.CreateDirectory(settingsFolder);
            string settingsContent = JsonConvert.SerializeObject(A3.LayoutSettings, Newtonsoft.Json.Formatting.Indented, serializerOptions);
            await File.WriteAllTextAsync(Path.Join(settingsFolder, "settings.json"), settingsContent, Encoding.UTF8);

            // Write layouts
            var layoutsFolder = Path.Join(path, "ui", "layouts");
            Directory.CreateDirectory(layoutsFolder);
            foreach (var page in A3.LayoutSettings.Pages.Order)
            {
                string content = JsonConvert.SerializeObject(A3.Layouts[page], Newtonsoft.Json.Formatting.Indented, serializerOptions);
                await File.WriteAllTextAsync(Path.Join(layoutsFolder, $"{page}.json"), content, Encoding.UTF8);
            }

            // Write texts
            var textsFolder = Path.Join(path, "config", "texts");
            Directory.CreateDirectory(textsFolder);
            foreach (var text in A3.Texts.Values)
            {
                string content = JsonConvert.SerializeObject(text, Newtonsoft.Json.Formatting.Indented, serializerOptions);
                await File.WriteAllTextAsync(Path.Join(textsFolder, $"resource.{text.Language}.json"), content, Encoding.UTF8);
            }

            // Prepare models directory
            var models = Path.Join(path, "models");
            Directory.CreateDirectory(models);

            // Write xsd
            await File.WriteAllTextAsync(Path.Join(models, $"{A3.ModelName}.xsd"), A3.Xsd, Encoding.UTF8);
            
            // Write prefills
            string prefillContent = JsonConvert.SerializeObject(A3.Prefill, Newtonsoft.Json.Formatting.Indented, serializerOptions);
            await File.WriteAllTextAsync(Path.Join(models, $"{A3.ModelName}.prefill.json"), prefillContent, Encoding.UTF8);
            
            // TODO: generate c# class for model from xsd
            // TODO: generate json schema for model from xsd
        }
    }
}