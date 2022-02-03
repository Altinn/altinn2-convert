using Newtonsoft.Json;

namespace Altinn2Convert.Models.Altinn3.prefill
{
    public partial class Test
    {
        [JsonProperty("$schema", Order = int.MinValue)]
        public string schema { get; } = "https://altinncdn.no/schemas/json/prefill/prefill.schema.v1.json";
    }
}