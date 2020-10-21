using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Altinn2Convert.Configuration;
using Altinn2Convert.Helpers;
using Altinn2Convert.Models;
using Altinn2Convert.Models.InfoPath;
using Microsoft.Extensions.Options;

namespace Altinn2Convert.Services
{
    /// <inheritdoc/>
    public class TextService : ITextService
    {
        private readonly GeneralSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextService"/> class.
        /// </summary>
        /// <param name="settings">The general settings.</param>
        public TextService(IOptions<GeneralSettings> settings)
        {
            _settings = settings.Value;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, string>> GetTextsFromXsl(string xslPath)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            
            using (var fileStream = File.OpenText(xslPath))
            {
                using XmlReader reader = XmlReader.Create(fileStream);
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            string test = ProcessElementNode(reader);
                            if (!string.IsNullOrEmpty(test))
                            {
                                string[] keyValue = test.Split(";");
                                result.Add(keyValue[0], keyValue[1]);
                            }

                            break;
                        default:
                            break;
                    }
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<List<TextResource>> GetFormTexts(string xsnPath)
        {
            var result = new List<TextResource>();
            byte[] file = File.ReadAllBytes(xsnPath);
            InfoPathParser infoPathParser = new InfoPathParser(file);
            var formTexts = infoPathParser.GetFormTexts();
            formTexts.ForEach(formText =>
            {
                string key = GetTextKeyFromFormText(formText);
                result.Add(new TextResource
                {
                    Id = key,
                    Value = formText.TextContent,
                });
            });
            return result;
        }

        /// <inheritdoc/>
        public List<TextResource> GetTranslationTexts(string filePath)
        {
            var result = new List<TextResource>();
            Translation translationFile;
            using var fileStream = File.Open(filePath, FileMode.Open);
            XmlSerializer serializer = new XmlSerializer(typeof(Translation));
            translationFile = (Translation)serializer.Deserialize(fileStream);

            translationFile.DataAreas.ForEach(dataArea =>
            {
                if (dataArea.Texts != null && dataArea.Texts.Count > 0)
                {
                    MapTranslationTexts(dataArea.Texts, result);
                }

                if (dataArea.LogicalForm != null && dataArea.LogicalForm.Texts != null && dataArea.LogicalForm.Texts.Count > 0)
                {
                    MapTranslationTexts(dataArea.LogicalForm.Texts, result);
                }
            });

            return result;
        }

        private string GetTextKeyFromFormText(FormText formText)
        {
            var regex = new Regex(@"\/\/[a-z]* \[@xd:CtrlId = '(.*)']");
            Match match = regex.Match(formText.TextCode);
            GroupCollection groups = match.Groups;
            return $"{groups[1]?.Value}_{formText.Page}";
        }

        private void MapTranslationTexts(List<TranslationText> texts, List<TextResource> result)
        {
            texts.ForEach(text =>
            {
                string key = $"{text.TextCode}_{text.TextType}";
                result.Add(new TextResource
                {
                    Id = key,
                    Value = text.Value,
                });
            });
        }

        private string ProcessElementNode(XmlReader reader)
        {
            string result = string.Empty;
            if (reader.Name == "span"
                && reader.GetAttribute("class") == "xdExpressionBox xdDataBindingUI"
                && reader.GetAttribute("xctname", "xd") == "ExpressionBox")
            {
                string controlId = reader.GetAttribute("CtrlId", "xd");
                string text = reader.GetAttribute("binding", "xd");
                result = string.Concat(controlId, ";", text);
            }

            return result;
        }
    }
}