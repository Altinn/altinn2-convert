using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Altinn2Convert.Models.Altinn2;
using Altinn2Convert.Models.Altinn3;

namespace Altinn2Convert.Helpers
{
    public static class Page2Layout
    {
        ///<summary>Merge layout lists for multiple languages and extract texts</summary>
        public static MergeLangResult MergeLang(List<string> languages, List<List<LayoutComponentTemp>> layouts, string textKeyPrefix)
        {
            var ret = new MergeLangResult();
            // All layouts are equal (except language texts)
            // Use the first layout list for everything not language related.
            var mainLayout = layouts[0];

            for (var i = 0; i < mainLayout.Count; i++)
            {
                // Todo: Provide a way to generate component id from content
                var id = Guid.NewGuid().ToString();

                // Temporary variables 
                var textResourceBindings = new Dictionary<string, string>();
                var bindingsKeys = new List<Tuple<string, string>>();

                // Add possible bindings from all languages
                for (var l = 0; l < languages.Count; l++)
                {
                    var languageLayout = layouts[l][i];
                    foreach (var binding in languageLayout.TextResources.Keys)
                    {
                        if (!bindingsKeys.Any((el) => { return el.Item1 == binding; }))
                        {
                            var key = $"{textKeyPrefix}.{id}.{binding}";
                            bindingsKeys.Add(new Tuple<string, string>(binding, key));
                            textResourceBindings[binding] = key;
                        }
                    }
                }

                // Add all text to the text resources
                for (var l = 0; l < languages.Count; l++)
                {
                    var language = languages[l];
                    var languageLayoutResources = layouts[l][i].TextResources;
                    foreach (var (binding, key) in bindingsKeys)
                    {
                        var value = languageLayoutResources[binding];
                        ret.SetText(key, value, language);
                    }
                }

                ret.Layout.Add(new Models.Altinn3.layout.Component
                {
                    Id = id,
                    Type = mainLayout[i].Type,
                    DataModelBindings = mainLayout[i].DataModelBindings,
                    TextResourceBindings = textResourceBindings,
                }); 
            }

            return ret;
        }

        public class MergeLangResult
        {
            public Models.Altinn3.layout.Layout Layout { get; set; } = new();

            ///<summary>Dictionary of texts for field in the current language: Texts[lang][key] = text )</summary>
            public Dictionary<string, Dictionary<string, string>> Texts { get; set; } = new();
            
            public void SetText(string key, string value, string lang)
            {
                if(!Texts.ContainsKey(lang))
                {
                    Texts[lang] = new();
                }

                Texts[lang][key] = value;
            }
        }

        public class LayoutComponentTemp
        {
            public Models.Altinn3.layout.ComponentType Type { get; set; }

            public Dictionary<string, string> DataModelBindings { get; set; }

            ///<summary>Dictionary of texts for field in the current language: (key = (title/help/...), value = text in language )</summary>
            public Dictionary<string, string> TextResources { get; set; }
        }

        public static List<LayoutComponentTemp> GetLayoutComponents(XmlDocument page, string textKeyPrefix)
        {
            var ret = new List<LayoutComponentTemp>();
            var xml = page;

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
            namespaceManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            namespaceManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");

            // Get root node of the InfoPath Control Xpath; this will be the "match" attribute of the "xsl:template" node that has the <body> tag
            XmlNode bodyNode = xml.SelectSingleNode("/xsl:stylesheet/xsl:template/html/body", namespaceManager);
            string rootNodeValue = "/" + bodyNode.ParentNode.ParentNode.Attributes["match"].Value; // <xsl:template match="melding">

            // var table = bodyNode.SelectNodes("");
            ret.Add(new ()
            {
                DataModelBindings = new () { {"simpleBinding", "a.b.c"} },
                TextResources = new () { {"title", "titteltekst"} },
                Type = Models.Altinn3.layout.ComponentType.Input,
            });

            // // Process 'sections' in the document (Altinn3 Groups)
            // XmlNodeList sections = bodyNode.SelectNodes(".//xsl:apply-templates [@mode]", namespaceManager);
            // List<FormField> fieldsFromSections = new List<FormField>();
            // foreach (XmlNode section in sections)
            // {
            //     List<FormField> sectionFields = GetRepeatingFields(viewName, xml, rootNodeValue, section, RepeatingGroupType.Section);
            //     fieldsFromSections.AddRange(sectionFields);
            // }

            //     // Process 'repeating tables' in the document
            //     XmlNodeList repeatingTables = bodyNode.SelectNodes(".//xsl:for-each", namespaceManager);
            //     List<FormField> fieldsFromRepeatingTables = new List<FormField>();
            //     foreach (XmlNode repeatingTable in repeatingTables)
            //     {
            //         List<FormField> repeatingTableFields = GetRepeatingFields(viewName, xml, rootNodeValue, repeatingTable, RepeatingGroupType.Table);

            //         //Check repeating fields of same name in next repeating table
            //         foreach (FormField repeatingtablefield in repeatingTableFields)
            //         {
            //             bool ctrlExists = fieldsFromRepeatingTables.Exists(existingrepeatingtablefield => (existingrepeatingtablefield.ControlID == repeatingtablefield.ControlID));
            //             if (!ctrlExists)
            //             {
            //                 fieldsFromRepeatingTables.Add(repeatingtablefield);

            //             }
            //         }
            //     }

            //     // Process 'non-repeating' fields, which are not part of the repeating fields            
            //     XmlNodeList fields = bodyNode.SelectNodes(".//*[@xd:binding]", namespaceManager);
            //     XmlNodeList rows = bodyNode.SelectNodes(".//tr", namespaceManager);
            //     List<FormField> regularFields = new List<FormField>();

            //     foreach (XmlNode row in rows)
            //     {
            //         List<XmlNode> formFields = new List<XmlNode>();
            //         List<XmlNode> textFields = new List<XmlNode>();
            //         XmlNodeList fieldsInRow = row.SelectNodes(".//*[@xd:binding]", namespaceManager);
            //         foreach (XmlNode field in fieldsInRow)
            //         {
            //             if (field.Attributes.GetNamedItem("xd:xctname") != null && field.Attributes.GetNamedItem("xd:xctname").Value == "ExpressionBox")
            //             {
            //                 textFields.Add(field);
            //             }
            //             else if (field.Attributes.GetNamedItem("xd:xctname") != null)
            //             {
            //                 if (!field.Attributes.GetNamedItem("xd:CtrlId").Value.Contains("HelpText"))
            //                 {
            //                     formFields.Add(field);
            //                 }
            //             }
            //         }

            //         if (formFields.Count == 0 && textFields.Count > 0)
            //         {
            //             textFields.ForEach(field =>
            //             {
            //                 string controlId = field.Attributes.GetNamedItem("xd:CtrlId").Value;
            //                 string classes = field.Attributes.GetNamedItem("class").Value;
            //                 string textKey = $"{field.Attributes.GetNamedItem("xd:CtrlId").Value}_{viewName.Replace(" ", string.Empty)}";
            //                 if (classes.Contains("xdBehavior_Formatting"))
            //                 {
            //                     textKey = field.Attributes.GetNamedItem("xd:binding").Value;
            //                 }

            //                 FormField formField = new FormField
            //                 {
            //                     Key = controlId,
            //                     PageName = viewName,
            //                     ControlType = field.Attributes.GetNamedItem("xd:xctname").Value,
            //                     ControlID = controlId,
            //                     TextKey = textKey,
            //                 };

            //                 regularFields.Add(formField);
            //             });
            //         }
            //         else if (formFields.Count == textFields.Count)
            //         {
            //             for (int i = 0; i < formFields.Count; i++)
            //             {
            //                 var field = formFields[i];
            //                 FormField formField = new FormField();
            //                 string fieldKey = rootNodeValue + "/" + field.Attributes.GetNamedItem("xd:binding").Value;
            //                 XmlNode controlIDNode = field.Attributes.GetNamedItem("xd:CtrlId");
            //                 string controlID = string.Empty;
            //                 if (controlIDNode != null)
            //                 {
            //                     controlID = field.Attributes.GetNamedItem("xd:CtrlId").Value;
            //                 }

            //                 if (fieldKey.Contains('/'))
            //                 {
            //                     int startIndex = fieldKey.LastIndexOf('/') + 1;
            //                     int length = fieldKey.Length - startIndex;
            //                     formField.Name = fieldKey.Substring(startIndex, length);
            //                 }
            //                 else
            //                 {
            //                     formField.Name = fieldKey;
            //                 }

            //                 string controlType = field.Attributes.GetNamedItem("xd:xctname").Value;
            //                 if (controlType == "PlainText")
            //                 {
            //                     XmlNode formatNode = field.Attributes.GetNamedItem("xd:datafmt");
            //                     if (formatNode != null && formatNode.Value.Contains("plainMultiline"))
            //                     {
            //                         controlType += "_multiline";
            //                     }
            //                 }

            //                 formField.Key = Utils.UnbundlePath(fieldKey);
            //                 formField.PageName = viewName;
            //                 formField.ControlType = controlType;
            //                 formField.ControlID = controlID;
            //                 formField.TextKey = $"{textFields[i].Attributes.GetNamedItem("xd:CtrlId").Value}_{viewName.Replace(" ", string.Empty)}";

            //                 if (field.Attributes.GetNamedItem("xd:disableEditing") != null && field.Attributes.GetNamedItem("xd:disableEditing").Value == "yes")
            //                 {
            //                     formField.Disabled = true;
            //                 }

            //                 bool ctrlExists = fieldsFromRepeatingTables.Exists(repeatingtablefield => (repeatingtablefield.Key == formField.Key || repeatingtablefield.ControlID == formField.ControlID));

            //                 if (!ctrlExists)
            //                 {
            //                     regularFields.Add(formField);
            //                 }
            //             }
            //         }
            //         else
            //         {
            //             foreach (XmlNode field in fieldsInRow)
            //             {
            //                 if (field.Attributes.GetNamedItem("xd:CtrlId") == null)
            //                 {
            //                     continue;
            //                 }

            //                 FormField formField = new FormField();

            //                 string controlType = field.Attributes.GetNamedItem("xd:xctname").Value;
            //                 string controlID = string.Empty;
            //                 XmlNode controlIDNode = field.Attributes.GetNamedItem("xd:CtrlId");
            //                 if (controlIDNode != null)
            //                 {
            //                     controlID = field.Attributes.GetNamedItem("xd:CtrlId").Value;
            //                 }

            //                 if (controlType != "ExpressionBox")
            //                 {
            //                     string fieldKey = rootNodeValue + "/" + field.Attributes.GetNamedItem("xd:binding").Value;

            //                     if (fieldKey.Contains('/'))
            //                     {
            //                         int startIndex = fieldKey.LastIndexOf('/') + 1;
            //                         int length = fieldKey.Length - startIndex;
            //                         formField.Name = fieldKey.Substring(startIndex, length);
            //                     }
            //                     else
            //                     {
            //                         formField.Name = fieldKey;
            //                     }

            //                     formField.Key = Utils.UnbundlePath(fieldKey);
            //                     formField.TextKey = fieldKey;

            //                     if (field.Attributes.GetNamedItem("xd:disableEditing") != null && field.Attributes.GetNamedItem("xd:disableEditing").Value == "yes")
            //                     {
            //                         formField.Disabled = true;
            //                     }

            //                     // Support for multiline text area
            //                     if (controlType == "PlainText")
            //                     {
            //                         XmlNode formatNode = field.Attributes.GetNamedItem("xd:datafmt");
            //                         if (formatNode != null && formatNode.Value.Contains("plainMultiline"))
            //                         {
            //                             controlType += "_multiline";
            //                         }
            //                     }
            //                 }
            //                 else
            //                 {
            //                     formField.Key = controlID;
            //                     string textKey = $"{field.Attributes.GetNamedItem("xd:CtrlId").Value}_{viewName.Replace(" ", string.Empty)}";
            //                     string classes = field.Attributes.GetNamedItem("class").Value;
            //                     if (classes.Contains("xdBehavior_Formatting"))
            //                     {
            //                         textKey = field.Attributes.GetNamedItem("xd:binding").Value;
            //                     }

            //                     formField.TextKey = textKey;
            //                 }
                            
            //                 formField.PageName = viewName;
            //                 formField.ControlType = controlType;
            //                 formField.ControlID = controlID;

            //                 bool ctrlExists = fieldsFromRepeatingTables.Exists(repeatingtablefield => (repeatingtablefield.Key == formField.Key || repeatingtablefield.ControlID == formField.ControlID));

            //                 if (!ctrlExists)
            //                 {
            //                     regularFields.Add(formField);
            //                 }
            //             }
            //         }
            //     }

            //     List<FormField> formFieldsList = new List<FormField>();
            //     formFieldsList.AddRange(fieldsFromSections);
            //     formFieldsList.AddRange(fieldsFromRepeatingTables);
            //     formFieldsList.AddRange(regularFields);
            //     foreach (FormField newField in formFieldsList)
            //     {
            //         bool keyExists = formfields.Exists(formField => (formField.Key == newField.Key));
            //         if (!keyExists)
            //         {
            //             formfields.Add(newField);
            //         }
            //     }
            // }

            return ret;
        }
    }
}