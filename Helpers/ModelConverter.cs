#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

using Altinn.Studio.Designer.Factories.ModelFactory;
using Altinn.Studio.Designer.ModelMetadatalModels;

using Altinn2Convert.Models.Altinn2;
using Altinn2Convert.Models.Altinn3;

using Manatee.Json.Schema;
using Newtonsoft.Json;

namespace Altinn2Convert.Helpers
{
    public static class ModelConverter
    {
        public static Dictionary<string,string> Convert(Altinn2AppData a2, out string? modelName)
        {
            modelName = null;
            var ret = new Dictionary<string, string>();
            if(a2.XSNFiles.Count == 0)
            {
                return ret;
            }

            // Get xsd from first xsn file (all languages are equal)
            string xsd = a2.XSNFiles.First().Value.XSDDocument;
            if(xsd == null)
            {
                return ret;
            }

            ret.Add("model.xsd", xsd);
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xsd)))
            {
                var reader = XmlReader.Create(stream);
                XsdToJsonSchema xsdToJsonSchemaConverter = new XsdToJsonSchema(reader);

                JsonSchema schemaJsonSchema = xsdToJsonSchemaConverter.AsJsonSchema();
                ret.Add("model.schema.json", new Manatee.Json.Serialization.JsonSerializer().Serialize(schemaJsonSchema).GetIndentedString(0));

                JsonSchemaToInstanceModelGenerator converter = new JsonSchemaToInstanceModelGenerator(a2.Org, a2.App, schemaJsonSchema);
                ModelMetadata modelMetadata = converter.GetModelMetadata();
                ret.Add("model.metadata.json", JsonConvert.SerializeObject(modelMetadata, Newtonsoft.Json.Formatting.Indented));

                modelName = modelMetadata.Elements["melding"].TypeName;
                // generate c# model
                JsonMetadataParser modelGenerator = new JsonMetadataParser();
                string classes = modelGenerator.CreateModelFromMetadata(modelMetadata);
                ret.Add("model.cs", classes);

                // HandleTexts(org, app, converter.GetTexts());
            }

            return ret;
        }
    }

}