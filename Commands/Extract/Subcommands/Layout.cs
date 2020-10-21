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
            Description = "Full path to where output files should be saved. If omitted, current working directory will be used.")]
        public string OutputPath { get; set; }

        private static readonly string TmpDir = "tmp/extractedFiles";
        private readonly ILayoutService _layoutService;

        /// <summary>
        /// Initializes a new instance of the <see cref="Layout"/> class.
        /// </summary>
        /// <param name="layoutService">The layout service</param>
        public Layout(ILayoutService layoutService)
        {
            _layoutService = layoutService;
        }

        /// <summary>
        /// Extracts Layout from InfoPath views, and writes the result to disk
        /// </summary>
        protected override async Task OnExecuteAsync(CommandLineApplication app)
        {
            try
            {
                Console.WriteLine("IN COMMAND LAYOUT");
                if (File.Exists(PackagePath))
                {
                    ZipFile.ExtractToDirectory(PackagePath, TmpDir);
                    SetupOutputDir();

                    ServiceEditionVersion sev = null;

                    using (var fileStream = File.Open(Path.Join(TmpDir, "manifest.xml"), FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(ServiceEditionVersion));
                        sev = (ServiceEditionVersion)serializer.Deserialize(fileStream);
                    }

                    DataArea formDetails = sev.DataAreas.Find(d => d.Type == "Form");
                    var formFiles = formDetails?.LogicalForm.Files.FindAll(f => f.FileType == "FormTemplate");
                    string filePath = Path.Join(TmpDir, formFiles.Find(file => file.Language == "1044").Name);
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
            Directory.Delete(TmpDir, true);
        }

        private void SetupOutputDir()
        {
            if (string.IsNullOrEmpty(OutputPath))
            {
                OutputPath = "output";
            }

            if (!Directory.Exists(Path.Join(OutputPath, "ui", "layouts")))
            {
                Directory.CreateDirectory(Path.Join(OutputPath, "ui", "layouts"));
            }
        }
    }
}
