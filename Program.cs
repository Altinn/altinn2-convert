using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Altinn2Convert.Services;

namespace Altinn2Convert
{
    /// <summary>
    /// Program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main method.
        /// </summary>
        public static async Task Main()
        {
            CultureInfo.CurrentCulture = new CultureInfo("en_US");
            // var mode = "generate";
            var mode = "test";
            // var mode = "run";
            if (mode == "generate")
            {
                var generateClass = new GenerateAltinn3ClassesFromJsonSchema();
                await generateClass.Generate();
            }
            
            if (mode == "test")
            {
                var service = new ConvertService();
                var targetDirectory = "out";
                if (Directory.Exists(Path.Join(targetDirectory)))
                {
                    Directory.Delete(Path.Join(targetDirectory), recursive: true);
                }

                var a2 = await service.ParseAltinn2File("TULPACKAGE.zip", targetDirectory);
                await service.DumpAltinn2Data(a2, targetDirectory);
                var a3 = await service.Convert(a2);
                await service.DeduplicateTests(a3);
                service.CopyAppTemplate(targetDirectory);
                await service.UpdateAppTemplateFiles(targetDirectory, a3);
                await service.WriteAltinn3Files(a3, targetDirectory);
            }

            if (mode == "run")
            {
                var homeFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                var tulFolder = Path.Join(homeFolder, "TUL");
                var altinn3Folder = Path.Join(homeFolder, "TULtoAltinn3");

                var bs = new BatchService();
                await bs.ConvertAll(tulFolder, altinn3Folder);
            }
        }
    }
}
