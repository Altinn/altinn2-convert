namespace Altinn2Convert.Enums
{
    /// <summary>
    /// Classifies the type of repeating group that contains the controls
    /// </summary>
    public enum RepeatingGroupType
    {
        /// <summary>
        /// The repeating control is a section, repeating section or an optional section
        /// </summary>
        Section = 1,

        /// <summary>
        /// The repeating control is a horizontal or vertical repeating table
        /// </summary>
        Table = 2
    }
}
