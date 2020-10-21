using System;
using System.Collections.Generic;
using System.Text;

namespace Altinn2Convert.Enums
{
    /// <summary>
    /// Text type enum
    /// </summary>
    public enum TextType : int
    {
        /// <summary>
        /// Not defined, to be used in TextQuery objects
        /// </summary>
        NotDefined = 0,

        /// <summary>
        /// There has been no changes to this section 
        /// of the parameters since the edition was created
        /// </summary>
        Help = 01,

        /// <summary>
        /// There has been changes to this section of parameters since the edition was
        /// created, but the section has required parameters that are not yet filled out.
        /// </summary>
        WorkflowParameter = 02, 

        /// <summary>
        /// All required parameters in this section has been filled out
        /// </summary>
        PageDisplayName = 03, 

        /// <summary>
        /// The parameters in this section has not been changed since last migration
        /// </summary>
        ServiceName = 04,

        /// <summary>
        /// The parameters in this section has not been changed since last migration
        /// </summary>
        ServiceEditionName = 05,

        /// <summary>
        /// The parameters in this section has not been changed since last migration
        /// </summary>
        LogicalForm = 06,

        /// <summary>
        /// It is used for Service Metadata,Service Edition Metadata,Page Metadata,Workflow texts
        /// </summary>
        Parameter = 07,

        /// <summary>
        /// It is used for Service Metadata,Service Edition Metadata,Page Metadata,Workflow texts
        /// </summary>
        ExpressionBox = 09,

        /// <summary>
        /// It is used for Service Metadata,Service Edition Metadata,Page Metadata,Workflow texts
        /// </summary>
        DropdownListBox = 10,

        /// <summary>
        /// It is used for Service Metadata,Service Edition Metadata,Page Metadata,Workflow texts
        /// </summary>
        ListBox = 11,

        /// <summary>
        /// It is used for Service Metadata,Service Edition Metadata,Page Metadata,Workflow texts
        /// </summary>
        Button = 12,

        /// <summary>
        /// It is used for Service Metadata,Service Edition Metadata,Page Metadata,Workflow texts
        /// </summary>
        HyperLink = 13, 

        /// <summary>
        /// It is used for Service Metadata,Service Edition Metadata,Page Metadata,Workflow texts
        /// </summary>
        TableHeader = 14,

        /// <summary>
        /// It is used for Service Metadata,Service Edition Metadata,Page Metadata,Workflow texts
        /// </summary>
        HintText = 15,

        /// <summary>
        /// It is used for Service Metadata,Service Edition Metadata,Page Metadata,Workflow texts
        /// </summary>
        ValidationText1 = 16,

        /// <summary>
        /// It is used for Service Metadata,Service Edition Metadata,Page Metadata,Workflow texts
        /// </summary>
        ValidationText2 = 17,
        
        /// <summary>
        /// It is used for Service Edition Metadata for ReceiptText
        /// </summary>
        ReceiptText = 18,

        /// <summary>
        /// It is used for Service Metadata,Service Edition Metadata,Page Metadata,Workflow texts
        /// </summary>
        FormName = 19,

        /// <summary>
        /// It is used for Service Edition Metadata for ReceiptText
        /// </summary>
        SenderName = 20,

        /// <summary>
        /// Information to be displayed in the Right Pane of the LookUp Service
        /// </summary>
        LookUpInfoRightPane = 21,

        /// <summary>
        /// Name of a State in the Collaboration Service State Model
        /// </summary>
        StateName = 22,

        /// <summary>
        /// Name of an event in the Collaboration Service State Model
        /// </summary>
        EventName = 23,

        /// <summary>
        /// Dialog Component Title
        /// </summary>
        Title = 24,

        /// <summary>
        /// Dialog Component Image Name
        /// </summary>
        ImageUrl = 25,

        /// <summary>
        /// Dialog Component Information Text
        /// </summary>
        Text = 26,

        /// <summary>
        /// Dialog Component Status Text
        /// </summary>
        StatusText = 27,

        /// <summary>
        /// Dialog Component Link Text
        /// </summary>
        LinkList = 28,

        /// <summary>
        /// Name of the Notice in Collaboration Services
        /// </summary>
        NoticeName = 29,

        /// <summary>
        /// Role Type Name
        /// </summary>
        RoleTypeName = 30,

        /// <summary>
        /// Role Type description
        /// </summary>
        RoleTypeDescription = 31,

        /// <summary>
        /// ER Description
        /// </summary>
        ERDiscription = 32,
        
        /// <summary>
        /// Image Alternative
        /// </summary>
        AlternateText = 33,

        /// <summary>
        /// ToolTip Text
        /// </summary>
        TooltipText = 34,

        /// <summary>
        /// TextType representing all the Dialog Page related TextTypes
        /// </summary>
        AllDialogPageTexts = 35,

        /// <summary>
        /// It is used for Service Edition Metadata for ReceiptText
        /// </summary>
        ResourceText1 = 36,

        /// <summary>
        /// It is used for Service Edition Metadata for ReceiptText
        /// </summary>
        ResourceText2 = 37,

        /// <summary>
        /// It is used for Service Edition Metadata for ReceiptText
        /// </summary>
        PictureButton = 38,

        /// <summary>
        /// Custom Help URL
        /// </summary>
        CustomHelpURL = 39,

        /// <summary>
        /// It is used for Mapping LEDE Text Attribute from a XML schema Doc
        /// </summary>
        XSD_CaptionText = 40,

        /// <summary>
        /// It is used for mapping HJELP Text Attribute from a XML schema Doc
        /// </summary>
        XSD_HelpText = 41,

        /// <summary>
        /// It is used for mapping FEIL Text Attribute from a XML schema Doc
        /// </summary>
        XSD_ErrorText = 42,

        /// <summary>
        /// It is used for mapping DSE Text Attribute from a XML schema Doc
        /// </summary>
        XSD_DSEText = 43,

        /// <summary>
        /// It is used for Service Metadata,Service Edition Metadata,Page Metadata,Workflow texts 
        /// and Help text .
        /// </summary>
        All = 08,

        /// <summary>
        /// It is used for Service Edition Metadata for ReceiptText
        /// </summary>
        ReceiptEmailText = 44,

        /// <summary>
        /// It is used for Service Edition Metadata for ReceiptText
        /// </summary>
        ReceiptInformationText = 45,

        /// <summary>
        /// It is used for access consent description
        /// </summary>
        AccessConsentDescription = 46,

        /// <summary>
        /// It is used for access consent details
        /// </summary>
        AccessConsentDetails = 47,

        /// <summary>
        /// It is used for Service Edition Metadata for DelegationText
        /// </summary>
        DelegationDescriptionText = 48
    }
}
