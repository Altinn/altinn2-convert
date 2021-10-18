using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Xml.Serialization;

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

        public async Task<Altinn2AppData> ParseAltinn2File(string zipPath)
        {
            var a2 = new Altinn2AppData();
            var tmpDir = $"tmpDir{new Random().Next(10000)}";
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
                tulPackageParser.GetAuthorizationRules();
                tulPackageParser.GetFormFieldPrefill();
                tulPackageParser.GetFormTrack();
                var metadata = tulPackageParser.GetFormMetadata();
                //TODO: stor all results form TUL package
                foreach (var language in a2.Languages)
                {
                    var xsnPath = tulPackageParser.GetXsnPath(language);
                    var infoPath = new InfoPathXmlParser(tmpDir, language, xsnPath);

                    tulPackageParser.GetTranslation(language);
                    a2.XSNFiles[language] = new Models.Altinn2.InfoPath.XSNFileContent
                    {
                        XSDDocument = infoPath.GetXSDDocument(),
                        Manifest = infoPath.GetManifest(),
                        // Pages = infoPath.GetPages()

                        //TODO: Store xpath file content
                    };
                }
            }
            finally
            {
                Directory.Delete(tmpDir, true);
            }

            return a2;
        }

        public async Task DumpAltinn2Data(Altinn2AppData a2, string path)
        {
            await File.WriteAllTextAsync(path, JsonConvert.SerializeObject(a2, Formatting.Indented, serializerOptions), Encoding.UTF8);
        }

        public async Task<Altinn3AppData> Convert(Altinn2AppData A2)
        {
            var a3 = new Altinn3AppData();
            a3.LayoutSettings.Pages = new Models.Altinn3.layoutSettings.Pages
            {
                ExcludeFromPdf = new Models.Altinn3.layoutSettings.ExcludeFromPdf { },
                Order = new Models.Altinn3.layoutSettings.Order { },
                Triggers = new Models.Altinn3.layoutSettings.Triggers { },
            };
            a3.AddText("nb", "test2", "en test text");
            a3.AddText("en", "test2", "en test text");

            return a3;
        }

        public async Task WriteAltinn3Files(Altinn3AppData A3, string path)
        {
            // Write settings
            var settingsFolder = Path.Join(path, "ui");
            Directory.CreateDirectory(settingsFolder);
            string settingsContent = JsonConvert.SerializeObject(A3.LayoutSettings, Formatting.Indented, serializerOptions);
            await File.WriteAllTextAsync(Path.Join(settingsFolder, "settings.json"), settingsContent, Encoding.UTF8);

            // Write layouts
            var layoutsFolder = Path.Join(path, "ui", "layouts");
            Directory.CreateDirectory(layoutsFolder);
            foreach (var page in A3.LayoutSettings.Pages.Order)
            {
                string content = JsonConvert.SerializeObject(A3.Layouts[page], Formatting.Indented, serializerOptions);
                await File.WriteAllTextAsync(Path.Join(layoutsFolder, $"{page}.json"), content, Encoding.UTF8);
            }

            // Write texts
            var textsFolder = Path.Join(path, "config", "texts");
            Directory.CreateDirectory(textsFolder);
            foreach (var text in A3.Texts.Values)
            {
                string content = JsonConvert.SerializeObject(text, Formatting.Indented, serializerOptions);
                await File.WriteAllTextAsync(Path.Join(textsFolder, $"resource.{text.Language}.json"), content, Encoding.UTF8);
            }

        }
    }
}