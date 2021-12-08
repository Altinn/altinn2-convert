using System;
using System.Collections.Generic;
using System.Linq;

using Altinn2Convert.Models.Altinn3.layout;

namespace Altinn2Convert.Helpers
{
    public static class MergeLanguageResults
    {
                ///<summary>Merge layout lists for multiple languages and extract texts</summary>
        public static MergeLangResult MergeLang(List<string> languages, List<Page2Layout> layouts, string textKeyPrefix)
        {
            var ret = new MergeLangResult();
            // All layouts are equal (except language texts)
            // Use the first layout list for everything not language related.
            var mainLayout = layouts[0].Components;

            for (var i = 0; i < mainLayout.Count; i++)
            {
                var mainComponent = mainLayout[i];

                // Handle special components that might differ
                switch ( mainComponent)
                {
                    case ImageComponent mainImage:
                        for (var l = 1; l < languages.Count; l++)
                        {
                            var languageImage = (ImageComponent)layouts[l].Components[i];
                            mainImage.Image.Src[languages[l]] = languageImage.Image.Src[languages[l]];
                        }

                        break;
                    case RadioButtonsComponent mainRadio:
                        for (var l = 1; l < languages.Count; l++)
                        {
                            var languageRadio = (RadioButtonsComponent)layouts[l].Components[i];
                            // TODO: Translate option lists
                        }
                        
                        break;
                }

                // Temporary variables 
                var textResourceBindings = new Dictionary<string, string>();
                var bindingsKeys = new List<Tuple<string, string>>();

                // Add possible bindings from all languages
                for (var l = 0; l < languages.Count; l++)
                {
                    var languageCompoment = layouts[l].Components[i];

                    foreach (var binding in languageCompoment?.TextResourceBindings?.Keys ?? new List<string>())
                    {
                        if (binding == "help")
                        {
                            // Help bindings already have unique keys
                            textResourceBindings["help"] = languageCompoment?.TextResourceBindings?["help"];
                        }
                        else if (!bindingsKeys.Any((el) => { return el.Item1 == binding; }))
                        {
                            var key = $"{textKeyPrefix}-{mainComponent.Id}-{binding}";
                            bindingsKeys.Add(new Tuple<string, string>(binding, key));
                            textResourceBindings[binding] = key;
                        }
                    }
                }

                // Add all text to the text resources
                for (var l = 0; l < languages.Count; l++)
                {
                    var language = languages[l];
                    var languageLayoutResources = layouts[l].Components[i].TextResourceBindings;
                    if (languageLayoutResources != null)
                    {
                        foreach (var (binding, key) in bindingsKeys)
                        {
                            var value = languageLayoutResources[binding];
                            ret.SetText(key, value, language);
                        }
                    }
                }

                // Add textResourceBindings to main component
                mainComponent.TextResourceBindings = textResourceBindings;
                ret.Layout.Add(mainComponent);
            }

            // Add texts with no compoment connection
            for (var l = 0; l < languages.Count; l++)
            {
                var language = languages[l];
                layouts[l].UnusedTexts.Select((text, index) =>
                {
                    ret.SetText($"{textKeyPrefix}-unknown-{index}", text, language);
                    return 1;
                }).ToList();
            }

            return ret;
        }

        public class MergeLangResult
        {
            public Models.Altinn3.layout.Layout Layout { get; set; } = new ();

            ///<summary>Dictionary of texts for field in the current language: Texts[lang][key] = text )</summary>
            public Dictionary<string, Dictionary<string, string>> Texts { get; set; } = new ();
            
            public void SetText(string key, string value, string lang)
            {
                if (!Texts.ContainsKey(lang))
                {
                    Texts[lang] = new ();
                }

                Texts[lang][key] = value;
            }
        }
    }
}