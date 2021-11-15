using Newtonsoft.Json;

namespace Altinn2Convert.Models.Altinn3.layout
{
    public partial class Test
    {
        [JsonProperty("$schema", Order = int.MinValue)]
        public string schema { get; } =  "https://altinncdn.no/schemas/json/layout/layout.schema.v1.json";
    }

    public partial class Component
    {
        public Component()
        {
            Type = (ComponentType)(-1);
        }
    }

    public partial class AddressComponent : Component
    {
        public AddressComponent()
        {
            Type = ComponentType.AddressComponent;
        }
    }

    public partial class AttachmentListComponent : Component
    {
        public AttachmentListComponent()
        {
            Type = ComponentType.AttachmentList;
        }
    }

    public partial class ButtonComponent : Component
    {
        public ButtonComponent()
        {
            Type = ComponentType.Button;
        }
    }

    public partial class CheckboxesComponent : Component
    {
        public CheckboxesComponent()
        {
            Type = ComponentType.Checkboxes;
        }
    }

    public partial class DatepickerComponent : Component
    {
        public DatepickerComponent()
        {
            Type = ComponentType.Datepicker;
        }
    }

    public partial class FileUploadComponent : Component
    {
        public FileUploadComponent()
        {
            Type = ComponentType.FileUpload;
        }
    }

    public partial class GroupComponent : Component
    {
        public GroupComponent()
        {
            Type = ComponentType.Group;
        }
    }

    public partial class HeaderComponent : Component
    {
        public HeaderComponent()
        {
            Type = ComponentType.Header;
        }
    }

    public partial class ImageComponent : Component
    {
        public ImageComponent()
        {
            Type = ComponentType.Image;
        }
    }

    public partial class InputComponent : Component
    {
        public InputComponent()
        {
            Type = ComponentType.Input;
        }
    }

    public partial class NavigationButtonsComponent : Component
    {
        public NavigationButtonsComponent()
        {
            Type = ComponentType.NavigationButtons;
        }
    }

    public partial class ParagraphComponent : Component
    {
        public ParagraphComponent()
        {
            Type = ComponentType.Paragraph;
        }
    }

    public partial class SelectionComponents : Component
    {
    }

    public partial class RadioButtonsComponent : SelectionComponents
    {
        public RadioButtonsComponent()
        {
            Type = ComponentType.RadioButtons;
        }
    }

    public partial class DropdownComponent : SelectionComponents
    {
        public DropdownComponent()
        {
            Type = ComponentType.Dropdown;
        }
    }

    public partial class SummaryComponent : Component
    {
        public SummaryComponent()
        {
            Type = ComponentType.Summary;
        }
    }

    public partial class TextAreaComponent : Component
    {
        public TextAreaComponent()
        {
            Type = ComponentType.TextArea;
        }
    }

}