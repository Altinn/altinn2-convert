using Newtonsoft.Json;

namespace Altinn2Convert.Models.Altinn3.layoutSettings
{
    public partial class Test
    {
        [JsonProperty("$schema", Order = int.MinValue)]
        public string schema { get; } = "https://altinncdn.no/schemas/json/layout/layoutSettings.schema.v1.json";
    }
}