using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Altinn2Convert.Models;
using Altinn2Convert.Services;
using McMaster.Extensions.CommandLineUtils;

namespace Altinn2Convert.Commands.Extract
{
    /// <summary>
    /// Info command handler. Returns metadata about a data element.
    /// </summary>
    [Command(
      Name = "texts",
      OptionsComparison = StringComparison.InvariantCultureIgnoreCase,
      UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue)]
    public class Texts : IBaseCmd
    {
        /// <summary>
        /// Instance guid
        /// </summary>
        [Option(
            CommandOptionType.SingleValue,
            ShortName = "p",
            LongName = "path",
            ShowInHelpText = true,
            Description = "Full path to zip-file containing Altinn 2 service")]
        [Required]
        public string PackagePath { get; set; }

        /// <summary>
        /// Instance guid
        /// </summary>
        [Option(
            CommandOptionType.SingleValue,
            ShortName = "o",
            LongName = "outputPath",
            ShowInHelpText = true,
            Description = "Full path to where output files should be saved. If omitted, current working directory will be used.")]
        [Required]
        public string OutputPath { get; set; }

        /// <summary>
        /// Collection of all texts for service
        /// </summary>
        public Dictionary<string, List<TextResourceItem>> AllTexts { get; set; }

        private readonly ITextService _textService;
        private static readonly string TmpDir = "tmp/extractedFiles";
        private Dictionary<string, string> languageMapping = new Dictionary<string, string>
        {
            {"1033", "en" },
            {"1044", "nb" },
            {"2068", "nn" },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="Texts"/> class.
        /// </summary>
        public Texts(ITextService textService)
        {
            _textService = textService;
        }

        /// <summary>
        /// Extracts texts from InfoPath and Translation files, and writes the result to disk
        /// </summary>
        protected override async Task OnExecuteAsync(CommandLineApplication app)
        {
            try
            {
                Console.WriteLine($"In command EXTRACT TEXTS");
                Console.WriteLine($"PackagePath: {PackagePath}");
                if (File.Exists(PackagePath))
                {
                    ZipFile.ExtractToDirectory(PackagePath, TmpDir);
                    SetupOutputDir();
                    ServiceEditionVersion sev = null;
                    AllTexts = new Dictionary<string, List<TextResourceItem>>();
                    using (var fileStream = File.Open(Path.Join(TmpDir, "manifest.xml"), FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(ServiceEditionVersion));
                        sev = (ServiceEditionVersion)serializer.Deserialize(fileStream);
                    }

                    DataArea formDetails = sev.DataAreas.Find(d => d.Type == "Form");
                    var formFiles = formDetails?.LogicalForm.Files.FindAll(f => f.FileType == "FormTemplate");
                    GetTextsFromFormFiles(formFiles);

                    var translationFiles = sev.Translations.Files;
                    GetTextsFromTranslations(translationFiles);
                    var encoderSettings = new TextEncoderSettings();
                    encoderSettings.AllowCharacters('\u0027');
                    encoderSettings.AllowRange(UnicodeRanges.All);
                    var options = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        WriteIndented = true,
                    };

                    foreach (var textResouce in AllTexts)
                    {
                        string savePath = Path.Join(OutputPath, "config", "texts", $"resource.{languageMapping[textResouce.Key]}.json");
                        string content = JsonSerializer.Serialize(
                            new TextResource
                            {
                                Resources = textResouce.Value,
                                Language = languageMapping[textResouce.Key]
                            }, options);
                        File.WriteAllText(savePath, content, Encoding.UTF8);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine($"An error occured.");
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine(e);
            }
            finally
            {
                CleanUp();
            }
        }

        private void GetTextsFromFormFiles(List<ServiceFile> files)
        {
            files.ForEach(async (file) =>
            {
                var formTexts = await _textService.GetFormTexts(Path.Join(TmpDir, file.Name));
                AddTexts(file.Language, formTexts);
            });
        }

        private void GetTextsFromTranslations(List<ServiceFile> files)
        {
            files.ForEach(file =>
            {
                var translationTexts = _textService.GetTranslationTexts(Path.Join(TmpDir, file.Name));
                AddTexts(file.Language, translationTexts);
            });
        }

        private void AddTexts(string language, List<TextResourceItem> texts)
        {
            if (!AllTexts.ContainsKey(language))
            {
                AllTexts.Add(language, new List<TextResourceItem>());
            }

            AllTexts[language].AddRange(texts);
        }

        private void CleanUp()
        {
            PackagePath = string.Empty;
            OutputPath = string.Empty;
            Directory.Delete(TmpDir, true);
        }

        private void SetupOutputDir()
        {
            if (string.IsNullOrEmpty(OutputPath))
            {
                OutputPath = "output";
            }

            if (!Directory.Exists(Path.Join(OutputPath, "config", "texts")))
            {
                Directory.CreateDirectory(Path.Join(OutputPath, "config", "texts"));
            }
        }
    }
}
