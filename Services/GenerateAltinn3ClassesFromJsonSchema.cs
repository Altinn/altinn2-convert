using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;

namespace Altinn2Convert.Services
{
    /// <summary>
    /// Http client for fetching schemas from CDN
    /// </summary>
    public class GenerateAltinn3ClassesFromJsonSchema
    {
        /// <summary>
        /// Http client for fetching schemas from CDN
        /// </summary>
        private HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        public GenerateAltinn3ClassesFromJsonSchema()
        {
            _httpClient = new HttpClient();
        }
        
        /// <summary>
        /// Settings for C# class generation
        /// </summary>
        private CSharpGeneratorSettings GetSettings(string folderName)
        {
            return new CSharpGeneratorSettings
            {
                Namespace = $"Altinn2Convert.Models.Altinn3.{folderName}",
                GenerateNullableReferenceTypes = true,
                GenerateDataAnnotations = true,
                GenerateOptionalPropertiesAsNullable = true
            };
        }

        /// <summary>
        /// Download the json schemas from cdn
        /// </summary>
        private async Task<JsonSchema[]> GetJsonSchema()
        {
            var urls = new string[]
            {
                // "https://altinncdn.no/schemas/json/component/number-format.schema.v1.json",
                // "https://altinncdn.no/schemas/json/layout/layout-sets.schema.v1.json",
                "https://altinncdn.no/schemas/json/layout/layout.schema.v1.json",
                "https://altinncdn.no/schemas/json/layout/layoutSettings.schema.v1.json",
                "https://altinncdn.no/schemas/json/policy/policy.schema.v1.json",
                "https://altinncdn.no/schemas/json/prefill/prefill.schema.v1.json",
                "https://altinncdn.no/schemas/json/widget/widget.schema.v1.json"
            };
            var tasks = urls.Select(url => JsonSchema.FromUrlAsync(url));
            var result = await Task.WhenAll(tasks);
            return result;
        }

        /// <summary>
        /// Extracts Layout from InfoPath views, and writes the result to disk
        /// </summary>
        public async Task Generate()
        {
            var jsons = await GetJsonSchema();
            foreach (var json in jsons)
            {
                var documentPathSplit = json.DocumentPath.Split('/');
                var filename = documentPathSplit[documentPathSplit.Length - 1].Split('.').First();
                // var folder = documentPathSplit[documentPathSplit.Length - 2];
                var path = Path.Join("Models", "Altinn3", filename);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var generator = new CSharpGenerator(null, GetSettings(filename));
                var file = generator.GenerateFile(json, "test");
                await File.WriteAllTextAsync(Path.Join(path, $"{filename}.cs"), file, System.Text.Encoding.UTF8);
            }
        }
    }

}