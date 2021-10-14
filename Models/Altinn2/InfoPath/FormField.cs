namespace Altinn2Convert.Models.Altinn2.InfoPath
{
    /// <summary>
    /// Describes a field in a form
    /// </summary>
    public class FormField
    {
        /// <summary>
        /// Form field key </summary>        
        public string Key { get; set; }

        /// <summary>
        /// Form field Name </summary>                
        public string Name { get; set; }

        /// <summary>
        /// Page Name</summary>        
        public string PageName { get; set; }

        /// <summary>
        /// Service Edition ID</summary>        
        public int ServiceEditionID { get; set; }

        /// <summary>
        /// Is the field a help field </summary>        
        public bool IsHelpField { get; set; }

        /// <summary>
        /// Control ID </summary>        
        public string ControlID { get; set; }

        /// <summary>
        /// Control type
        /// </summary>
        public string ControlType { get; set; }

        /// <summary>
        /// Is the field disabled
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Text key for the field
        /// </summary>
        public string TextKey { get; set; }
    }
}
