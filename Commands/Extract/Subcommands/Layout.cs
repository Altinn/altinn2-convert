using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Altinn2Convert.Configuration;
using Altinn2Convert.Helpers;
using Altinn2Convert.Models.Altinn2;
using Altinn2Convert.Services;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Options;

namespace Altinn2Convert.Commands.Extract
{
    /// <summary>
    ///  Command to extract form layout from infopath views
    /// </summary>
    [Command(
      Name = "layout",
      OptionsComparison = StringComparison.InvariantCultureIgnoreCase,
      UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue)]
    public class Layout: IBaseCmd
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

        private readonly ILayoutService _layoutService;
        private readonly GeneralSettings _generalSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="Layout"/> class.
        /// </summary>
        /// <param name="layoutService">The layout service</param>
        /// <param name="generalSettings">General settings</param>
        public Layout(ILayoutService layoutService, IOptions<GeneralSettings> generalSettings)
        {
            _layoutService = layoutService;
            _generalSettings = generalSettings.Value;
        }

        /// <summary>
        /// Extracts Layout from InfoPath views, and writes the result to disk
        /// </summary>
        protected override async Task OnExecuteAsync(CommandLineApplication app)
        {
            try
            {
                Console.WriteLine("Run command EXTRACT LAYOUT");
                ServiceEditionVersion sev = Utils.RunSetup(PackagePath, OutputPath, "layout", _generalSettings.TmpDir);

                DataArea formDetails = sev.DataAreas.Find(d => d.Type == "Form");
                var formFiles = formDetails?.LogicalForm.Files.FindAll(f => f.FileType == "FormTemplate");
                string filePath = Path.Join(_generalSettings.TmpDir, formFiles.Find(file => file.Language == "1044").Name);
                var layouts = _layoutService.GetLayout(filePath);
                var serializerOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true,
                    IgnoreNullValues = true,
                };
                serializerOptions.Converters.Add(new JsonStringEnumConverter());

                foreach (var layout in layouts)
                {
                    string savePath = Path.Join(OutputPath, "ui", "layouts", $"{layout.Key}.json");
                    string content = JsonSerializer.Serialize(layout.Value, serializerOptions);
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
