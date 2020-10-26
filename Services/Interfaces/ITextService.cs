using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Altinn2Convert.Models;
using Azure.Core;
using Azure.Identity;

namespace Altinn2Convert.Services
{
    /// <summary>
    /// Service that handles extraction of texts.
    /// </summary>
    public interface ITextService
    {
        /// <summary>
        /// Retrieves an access token for the provided context.
        /// </summary>
        /// <param name="xsl">XSL file</param>
        /// <returns>The access token.</returns>
        public Task<Dictionary<string, string>> GetTextsFromXsl(string xsl);

        /// <summary>
        /// Gets the form texts from an InfoPath xsn file
        /// </summary>
        /// <param name="xsnPath">The path to the infopath xsn file</param>
        /// <returns></returns>
        public Task<List<TextResourceItem>> GetFormTexts(string xsnPath);

        /// <summary>
        /// Gets the texts from the translation files
        /// </summary>
        /// <param name="filePath">The path to the translation file</param>
        /// <returns></returns>
        public List<TextResourceItem> GetTranslationTexts(string filePath);
    }
}
