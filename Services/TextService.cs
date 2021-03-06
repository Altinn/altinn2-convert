﻿using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
        public Dictionary<string, List<TextResourceItem>> GetTexts(List<ServiceFile> formFiles, List<ServiceFile> translationFiles)
        {
            var result = new Dictionary<string, List<TextResourceItem>>();

            // Form texts
            formFiles.ForEach((file) =>
            {
                var formTexts = GetFormTexts(Path.Join(_settings.TmpDir, file.Name));
                AddTexts(file.Language, formTexts, result);
            });

            // Other translations
            translationFiles.ForEach(file =>
            {
                var translationTexts = GetTranslationTexts(Path.Join(_settings.TmpDir, file.Name));
                AddTexts(file.Language, translationTexts, result);
            });

            return result;
        }

        private List<TextResourceItem> GetFormTexts(string xsnPath)
        {
            var result = new List<TextResourceItem>();
            byte[] file = File.ReadAllBytes(xsnPath);
            InfoPathParser infoPathParser = new InfoPathParser(file);
            var formTexts = infoPathParser.GetFormTexts();
            var views = infoPathParser.GetViews();
            formTexts.ForEach(formText =>
            {
                string key = GetTextKeyFromFormText(formText, views);
                result.Add(new TextResourceItem
                {
                    Id = key,
                    Value = formText.TextContent,
                });
            });
            return result;
        }

        private List<TextResourceItem> GetTranslationTexts(string filePath)
        {
            var result = new List<TextResourceItem>();
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

        private string GetTextKeyFromFormText(FormText formText, List<FormView> views)
        {
            var regex = new Regex(@"\/\/[a-z]* \[@xd:CtrlId = '(.*)']");
            Match match = regex.Match(formText.TextCode);
            GroupCollection groups = match.Groups;
            string page = views.Find(v => v.TransformationFile == formText.Page).Name;
            return $"{groups[1]?.Value}_{page.Replace(" ", string.Empty)}";
        }

        private void MapTranslationTexts(List<TranslationText> texts, List<TextResourceItem> result)
        {
            texts.ForEach(text =>
            {
                string key = $"{text.TextCode}_{text.TextType}";
                result.Add(new TextResourceItem
                {
                    Id = key,
                    Value = text.Value,
                });
            });
        }

        private void AddTexts(string language, List<TextResourceItem> texts, Dictionary<string, List<TextResourceItem>> allTexts)
        {
            if (!allTexts.ContainsKey(language))
            {
                allTexts.Add(language, new List<TextResourceItem>());
            }

            allTexts[language].AddRange(texts);
        }
    }
}