using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using Altinn2Convert.Helpers;
using Altinn2Convert.Models.Altinn2;
using Altinn2Convert.Models.Altinn3;
using Newtonsoft.Json;

namespace Altinn2Convert.Services
{
    public class ConvertService
    {
        private JsonSerializerSettings serializerOptions { get; set; } = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new ComponentPropsFirstContractResolver(),
        };

        public async Task<Altinn2AppData> ParseAltinn2File(string zipPath, string outDir)
        {
            outDir = Path.Join(outDir, "TULPACKAGE");
            var a2 = new Altinn2AppData();
            if (!File.Exists(zipPath))
            {
                throw new Exception($"Altinn2 file '{zipPath}' does not exist");
            }

            ZipFile.ExtractToDirectory(zipPath, outDir);
            var tulPackageParser = new TulPackageParser(outDir);
            a2.Manifest = tulPackageParser.Xmanifest;
            a2.Languages.AddRange(tulPackageParser.GetLanguages());
            a2.ServiceEditionVersion = tulPackageParser.GetServiceEditionVersion();
            a2.FormMetadata = tulPackageParser.GetFormMetadata();
            a2.AttachmentTypes = tulPackageParser.GetAttachmentTypes();
            a2.AutorizationRules = tulPackageParser.GetAuthorizationRules();
            a2.FormFieldPrefill = tulPackageParser.GetFormFieldPrefill();
            a2.FormTrack = tulPackageParser.GetFormTrack();
            a2.Org = tulPackageParser.GetOrg();
            a2.App = tulPackageParser.GetApp();

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

                var infoPath = new InfoPathXmlParser();
                infoPath.Extract(outDir, language, xsnPath);

                a2.XSNFiles[language] = new Models.Altinn2.InfoPath.XSNFileContent
                {
                    XSDDocument = infoPath.GetXSDDocument(),
                    Manifest = infoPath.GetManifest(),
                    Pages = infoPath.GetPages(a2.FormMetadata.Select(m => m.Transform).ToList()),
                };
            }

            return a2;
        }

        public async Task DumpAltinn2Data(Altinn2AppData a2, string path)
        {
            var target = Path.Join(path, "altinn2.json");
            await File.WriteAllTextAsync(target, JsonConvert.SerializeObject(a2, Newtonsoft.Json.Formatting.Indented, serializerOptions), Encoding.UTF8);
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
                // Read layout only from 
                var pages = a2.Languages.Select(language => a2.XSNFiles[language].Pages[formMetadata.Transform]).ToList();
                
                var layoutLists = a2.Languages.Select(language =>
                {
                    var page2layout = new Page2Layout(a2.XSNFiles[language].Pages[formMetadata.Transform], language);
                    page2layout.FillLayoutComponents();
                    return page2layout;
                }).ToList();
                
                var mergedLang = MergeLanguageResults.MergeLang(a2.Languages, layoutLists, textKeyPrefix: formMetadata.SanitizedName);
                
                // Add Layout to List of layout files
                a3.AddLayout(formMetadata.A3PageName, mergedLang.Layout);

                // Add texts for this page
                a3.AddTexts(mergedLang.Texts);
            });

            // Try to convert prefills
            a3.Prefill = PrefillConverter.Convert(a2.FormFieldPrefill);
            

            // Read xsd from xsn files and convert to altinn3 set of models
            a3.ModelFiles = ModelConverter.Convert(a2, out var modelName);
            a3.ModelName = modelName;

            // Create summary page
            var summaryLayout = new Models.Altinn3.layout.Layout();
            a3.LayoutSettings?.Pages?.Order?.ToList().ForEach(pageName =>
            {
                summaryLayout.Add(new Models.Altinn3.layout.HeaderComponent
                {
                    Id = Regex.Replace(pageName.ToLower(), "[^0-9a-zA-Z-]", "") + "-summary",
                    TextResourceBindings = new Dictionary<string, string>
                    {
                        {"title", pageName}
                    },
                    Size = Models.Altinn3.layout.HeaderComponentSize.H2,
                });
                a3.Layouts[pageName]?.Data?.Layout?.ToList().ForEach(layout =>
                {
                    switch (layout.Type)
                    {
                        case Models.Altinn3.layout.ComponentType.Group:
                        case Models.Altinn3.layout.ComponentType.Header:
                        case Models.Altinn3.layout.ComponentType.InstantiationButton:
                        case Models.Altinn3.layout.ComponentType.Image:
                        case Models.Altinn3.layout.ComponentType.Paragraph:
                        case Models.Altinn3.layout.ComponentType.NavigationButtons:
                        case Models.Altinn3.layout.ComponentType.Button:
                        case Models.Altinn3.layout.ComponentType.Summary:
                            break;
                        default:
                            summaryLayout.Add(new Altinn2Convert.Models.Altinn3.layout.SummaryComponent
                            {
                                Id = Regex.Replace(pageName.ToLower(), "[^0-9a-zA-Z-]", "") + "-" + layout.Id + "-summary",
                                ComponentRef = layout.Id,
                                PageRef = pageName,
                            });
                            break;
                    }
                });
            });
            a3.AddLayout("Summary", summaryLayout);
            a3.LayoutSettings?.Pages?.ExcludeFromPdf?.Add("Summary");
            

            // Fill info into applicationMetadata
            a3.ApplicationMetadata.Id = $"{a2.Org.ToLower()}/{Regex.Replace(a2.App.ToLower(), "[^0-9a-zA-Z-]", "")}";
            a3.ApplicationMetadata.Org = a2.Org.ToLower();
            a3.ApplicationMetadata.Title ??= new ();
            a3.ApplicationMetadata.Title["nb"] = a2.App;
            a3.ApplicationMetadata.DataTypes ??= new ();
            if (!string.IsNullOrWhiteSpace(a3.ModelName))
            {
                a3.ApplicationMetadata.DataTypes.Add(new ()
                {
                    Id = "model",
                    AllowedContentTypes = new ()
                    {
                        "application/xml"
                    },
                    AppLogic = new ()
                    {
                        AutoCreate = true,
                        ClassRef = $"Altinn.App.Models.{a3.ModelName}"
                    },
                    TaskId = "Task_1",
                    MaxCount = 1,
                    MinCount = 1,
                });
            }

            // TODO: get from manifest.xml
            a3.ApplicationMetadata.PartyTypesAllowed = new ()
            {
                BankruptcyEstate = true,
                Organisation = true,
                Person = true,
                SubUnit = true,
            };

            a3.ApplicationMetadata.AutoDeleteOnProcessEnd = false;
            a3.ApplicationMetadata.Created = DateTime.ParseExact(a2.Manifest.XPathSelectElement("/ServiceEditionVersion/DataAreas/DataArea[@type=\"Service\"]/Property[@name=\"LastUpdated\"]")?.Attribute("value")?.Value, "dd.MM.yyyy", new CultureInfo("no-NB"));
            a3.ApplicationMetadata.CreatedBy = a2.Manifest.XPathSelectElement("/ServiceEditionVersion/PackageInfo/Property[@name=\"CreatedBy\"]")?.Attribute("value")?.Value;
            a3.ApplicationMetadata.LastChangedBy = "altinn2-convert";
        

            // TODO: Add extra layout field for attachment types
            // a2.AttachmentTypes
            return a3;
        }

        public async Task DeduplicateTests(Altinn3AppData A3)
        {
            // TODO: Implement
        }

        public async Task UpdateAppTemplateFiles(string root, Altinn3AppData a3)
        {
            var path = Path.Join(root, "App", "config", "authorization", "policy.xml");
            var policy = await File.ReadAllTextAsync(path);
            policy = policy.Replace("[ORG]", a3.ApplicationMetadata.Org).Replace("[APP]", a3.ApplicationMetadata.Id.Split('/')[1]);
            await File.WriteAllTextAsync(path, policy, Encoding.UTF8);
        }

        public void CopyAppTemplate(string root)
        {
            CopyDirs("../altinn-studio/src/studio/AppTemplates/AspNet", root);
        }

        private void CopyDirs(string src, string dest)
        {
            Directory.CreateDirectory(dest);
            var srcDir = new DirectoryInfo(src);
            foreach (var file in srcDir.GetFiles())
            {
                file.CopyTo(Path.Join(dest, file.Name));
            }

            foreach (var dir in srcDir.GetDirectories())
            {
                CopyDirs(Path.Join(src, dir.Name), Path.Join(dest, dir.Name));
            }
        }

        public async Task WriteAltinn3Files(Altinn3AppData A3, string root)
        {
            var appPath = Path.Join(root, "App");
            // Write settings
            var settingsFolder = Path.Join(appPath, "ui");
            Directory.CreateDirectory(settingsFolder);
            string settingsContent = JsonConvert.SerializeObject(A3.LayoutSettings, Newtonsoft.Json.Formatting.Indented, serializerOptions);
            await File.WriteAllTextAsync(Path.Join(settingsFolder, "settings.json"), settingsContent, Encoding.UTF8);

            // Write layouts
            var layoutsFolder = Path.Join(appPath, "ui", "layouts");
            Directory.CreateDirectory(layoutsFolder);
            foreach (var page in A3.LayoutSettings.Pages.Order)
            {
                string content = JsonConvert.SerializeObject(A3.Layouts[page], Newtonsoft.Json.Formatting.Indented, serializerOptions);
                await File.WriteAllTextAsync(Path.Join(layoutsFolder, $"{page}.json"), content, Encoding.UTF8);
            }

            // Write texts
            var textsFolder = Path.Join(appPath, "config", "texts");
            Directory.CreateDirectory(textsFolder);
            foreach (var text in A3.Texts.Values)
            {
                string content = JsonConvert.SerializeObject(text, Newtonsoft.Json.Formatting.Indented, serializerOptions);
                await File.WriteAllTextAsync(Path.Join(textsFolder, $"resource.{text.Language}.json"), content, Encoding.UTF8);
            }

            // Prepare models directory
            var models = Path.Join(appPath, "models");
            Directory.CreateDirectory(models);

            // Write model files
            foreach (var (file, content) in A3.ModelFiles)
            {
                await File.WriteAllTextAsync(Path.Join(models, file), content, Encoding.UTF8);
            }
            
            // Write prefills
            string prefillContent = JsonConvert.SerializeObject(A3.Prefill, Newtonsoft.Json.Formatting.Indented, serializerOptions);
            await File.WriteAllTextAsync(Path.Join(models, $"model.prefill.json"), prefillContent, Encoding.UTF8);

            // Copy referenced images
            foreach (var language in A3.Texts.Keys)
            {
                var files = A3.Layouts.SelectMany(
                    kv => kv.Value.Data.Layout
                        .Where(l => l.Type == Models.Altinn3.layout.ComponentType.Image)
                        .Select(l => ((Models.Altinn3.layout.ImageComponent)l)?.Image?.Src?[language]?.Replace("wwwroot/images/", "")))
                        .Where(url => !string.IsNullOrWhiteSpace(url))
                        .ToList();
                if (files.Count > 0)
                {
                    var imagesFolder = Path.Join(appPath, "wwwroot", "images");
                    Directory.CreateDirectory(imagesFolder);
                    foreach (var file in files)
                    {
                        File.Copy(Path.Join(root, "TULPACKAGE", "form", language, file), Path.Join(imagesFolder, file), overwrite: true);
                    }
                }
            }
            
            // write applicationmetadata.json
            var applicationMetadata = JsonConvert.SerializeObject(A3.ApplicationMetadata, Newtonsoft.Json.Formatting.Indented, serializerOptions);
            await File.WriteAllTextAsync(Path.Join(appPath, "config", "applicationmetadata.json"), applicationMetadata);
        }
    }
}