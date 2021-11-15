using System;
using System.IO;
using System.Threading.Tasks;

namespace Altinn2Convert.Services
{
    public class BatchService
    {
        public async Task ConvertAll(string sourceDirectory, string targetDirectory)
        {
            // Ensure targetDirectory exists
            Directory.CreateDirectory(targetDirectory);

            var zipFiles = new DirectoryInfo(sourceDirectory);
            foreach (var zipFileInfo in zipFiles.EnumerateFiles())
            {
                var zipFile = zipFileInfo.Name;

                var name = zipFile.Replace(".zip", string.Empty);
                Console.WriteLine($"Converting {zipFile}");
                PrepareTargetDirectory(zipFile, targetDirectory, name);
                try
                {
                    await DoConversion(zipFile, sourceDirectory, Path.Join(targetDirectory, name));
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(e.StackTrace);
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine($"\nFailed to convert {zipFile}\n");
                    Console.ResetColor();
                    return;
                }
            }
        }

        public void PrepareTargetDirectory(string zipFile, string targetDirectory, string name)
        {
            // ensure existing folders with the same name are cleared
            if (Directory.Exists(Path.Join(targetDirectory, name)))
            {
                Directory.Delete(Path.Join(targetDirectory, name), recursive: true);
            }

            Directory.CreateDirectory(Path.Join(targetDirectory, name));  
        }

        public async Task DoConversion(string zipFile, string sourceDirectory, string targetDirectory)
        {
            var service = new ConvertService();
            var a2 = await service.ParseAltinn2File(Path.Join(sourceDirectory, zipFile),targetDirectory);
            await service.DumpAltinn2Data(a2, targetDirectory);
            var a3 = await service.Convert(a2);
            await service.DeduplicateTests(a3);
            await service.WriteAltinn3Files(a3, targetDirectory);
        }
    }
}