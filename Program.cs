using System;
using System.IO;
using System.Threading.Tasks;
using Altinn2Convert.Commands.Extract;
using Altinn2Convert.Configuration;
using Altinn2Convert.Services;

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            // var generateClass = new GenerateAltinn3ClassesFromJsonSchema();
            // await generateClass.Generate();
            
            // var service = new ConvertService();
            // var a2 = await service.ParseAltinn2File("TULPACKAGE.zip");
            // await service.DumpAltinn2Data(a2, Path.Join("out", "altinn2.json"));
            // var a3 = await service.Convert(a2);
            // await service.WriteAltinn3Files(a3, "out");

            var homeFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            var tulFolder = Path.Join(homeFolder, "TUL");
            var altinn3Folder = Path.Join(homeFolder, "TULtoAltinn3");

            var bs = new BatchService();
            await bs.ConvertAll(tulFolder, altinn3Folder);
        }
    }
}
