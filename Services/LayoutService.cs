using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using Altinn2Convert.Enums;
using Altinn2Convert.Helpers;
using Altinn2Convert.Models;

namespace Altinn2Convert.Services
{
    public class LayoutService
    {
        public Dictionary<string, FormLayout> GetLayout(string xsnPath)
        {
            byte[] file = File.ReadAllBytes(xsnPath);
            InfoPathParser parser = new InfoPathParser(file);
            var formFields = parser.GetFormFields();
            var views = parser.GetViews();
            var layouts = new Dictionary<string, FormLayout>();

            for (int i = 0; i < views.Count; i++)
            {
                var layout = new List<BaseComponent>();
                var view = views[i];
                foreach (var field in formFields.FindAll(f => f.PageName == view.Name))
                {
                    if (field.Key.Contains("@my:") || field.Key.Contains("GetDOM"))
                    {
                        continue;
                    }

                    int rootIndex = field.Key.Substring(1).IndexOf("/");
                    string dataModelBinding = field.Key.Substring(rootIndex + 2).Replace("/", ".");
                    BaseComponent component = new BaseComponent
                    {
                        ID = Guid.NewGuid().ToString(),
                        Type = GetComponentType(field.ControlType),
                        DataModelBindings = new Dictionary<string, string>(),
                        TextResourceBindings = new Dictionary<string, string>
                        {
                            { "title", field.TextKey }
                        },
                        ReadOnly = field.Disabled,
                    };

                    if (component.Type != ComponentType.Paragraph)
                    {
                        component.DataModelBindings.Add("simpleBinding", dataModelBinding);
                    }

                    if (component.Type == ComponentType.Dropdown || component.Type == ComponentType.RadioButtons)
                    {
                        component.Options = new List<Options>
                    {
                        new Options
                        {
                            Label = "Label",
                            Value = "Value",
                        }
                    };
                    }

                    layout.Add(component);
                }

                if (i == views.Count - 1)
                {
                    layout.Add(new BaseComponent
                    {
                        ID = "submit-button",
                        Type = ComponentType.Button,
                        DataModelBindings = new Dictionary<string, string>(),
                        TextResourceBindings = new Dictionary<string, string>
                        {
                            { "title", "Send inn" }
                        },
                        CustomType = "Standard",
                    });
                }
                else
                {
                    layout.Add(new BaseComponent
                    {
                        ID = $"nav-button-{i}",
                        Type = ComponentType.NavigationButtons,
                        DataModelBindings = new Dictionary<string, string>(),
                        TextResourceBindings = new Dictionary<string, string>(),
                    });
                }

                FormLayout formLayout = new FormLayout
                {
                    Data = new Data
                    {
                        Layout = layout,
                    }
                };

                layouts.Add($"{i}.{view.Name.Replace(" ", string.Empty)}", formLayout);
            }

            return layouts;
        }

        private ComponentType GetComponentType(string type)
        {
            return type switch
            {
                "OptionButton" => ComponentType.RadioButtons,
                "dropdown" => ComponentType.Dropdown,
                "ExpressionBox" => ComponentType.Paragraph,
                "PlainText_multiline" => ComponentType.TextArea,
                _ => ComponentType.Input,
            };
        }
    }
}
