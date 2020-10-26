using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Altinn2Convert.Configuration;
using Altinn2Convert.Helpers;
using Altinn2Convert.Models;
using Altinn2Convert.Services;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Options;

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
            Description = "Full path to where output files should be saved.")]
        [Required]
        public string OutputPath { get; set; }

        /// <summary>
        /// Collection of all texts for service
        /// </summary>
        public Dictionary<string, List<TextResourceItem>> AllTexts { get; set; }

        private readonly ITextService _textService;
        private readonly GeneralSettings _generalSettings;
        private readonly Dictionary<string, string> languageMapping = new Dictionary<string, string>
        {
            {"1033", "en" },
            {"1044", "nb" },
            {"2068", "nn" },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="Texts"/> class.
        /// </summary>
        public Texts(ITextService textService, IOptions<GeneralSettings> settings)
        {
            _textService = textService;
            _generalSettings = settings.Value;
        }

        /// <summary>
        /// Extracts texts from InfoPath and Translation files, and writes the result to disk
        /// </summary>
        protected override async Task OnExecuteAsync(CommandLineApplication app)
        {
            try
            {
                Console.WriteLine($"Running command EXTRACT TEXTS");
                ServiceEditionVersion sev = Utils.RunSetup(PackagePath, OutputPath, "texts", _generalSettings.TmpDir);
                DataArea formDetails = sev.DataAreas.Find(d => d.Type == "Form");
                var formFiles = formDetails?.LogicalForm.Files.FindAll(f => f.FileType == "FormTemplate");
                var translationFiles = sev.Translations.Files;
                AllTexts = _textService.GetTexts(formFiles, translationFiles);

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

        private void CleanUp()
        {
            PackagePath = string.Empty;
            OutputPath = string.Empty;
            Directory.Delete(_generalSettings.TmpDir, true);
        }
    }
}
