using System;
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

namespace Altinn2Convert.Helpers
{
    public static class ModelConverter
    {
        public static void Convert(Altinn2AppData a2, Altinn3AppData a3)
        {
            // Get xsd from first (random) xsn file
            string xsd = a2.XSNFiles.First().Value.XSDDocument;
            a3.Xsd = xsd;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xsd)))
            {
                var reader = XmlReader.Create(stream);
                XsdToJsonSchema xsdToJsonSchemaConverter = new XsdToJsonSchema(reader, logger: null);

                JsonSchema schemaJsonSchema = xsdToJsonSchemaConverter.AsJsonSchema();

                JsonSchemaToInstanceModelGenerator converter = new JsonSchemaToInstanceModelGenerator(a2.Org, a2.App, schemaJsonSchema);
                ModelMetadata modelMetadata = converter.GetModelMetadata();

                // HandleTexts(org, app, converter.GetTexts());
            }
        }
    }

}