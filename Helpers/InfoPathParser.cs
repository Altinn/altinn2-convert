using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

using Altinn2Convert.Enums;
using Altinn2Convert.Models;
using Altinn2Convert.Models.InfoPath;

using CabLib;

namespace Altinn2Convert.Helpers
{
    /// <summary>
    /// Parser for InfoPath files
    /// </summary>
    /// <remarks>
    /// Enables parsing of the InfoPath files in TUL and SBL, uses the CabLib library to extract files
    /// <see cref="GetViews"/>
    /// <see cref="GetFormFields()"/>
    /// <see cref="GetFormFields(string)"/>
    /// <see cref="GetHelpButtons"/>
    /// <see cref="GetXmlDocument"/>
    /// <see cref="GetFile"/> </remarks>
    /// <author>
    /// Manesh Karunakaran</author>
    /// <lastUpdated>
    /// April 21, 2009</lastUpdated>
    public class InfoPathParser
    {
        #region Class Level Variables
        private byte[] _infoPathFileBuffer;
        private byte[] _lastExtractedFile;
        private const string LOCALIZINGFILESERROR = "LocalizingFilesError";
        #endregion

        #region Constants
        private const string MEMORY_STREAM = "MEMORY";
        private const string MANIFEST_XSF = "manifest.xsf";
        private const string XSD = "xsd";
        private const string MYSCHEMA_XSD = "myschema.xsd";
        private const string FORMSET_XSD = "formSet.xsd";
        private const string HELP_TEXT = "HelpText_";
        private const string OPTION = "option";
        private const string VALUE = "value";
        private const string SELECT = "select";
        private const string GETCODELIST = "GetCodeList";
        private const string GETFILTEREDCODELIST = "GetFilteredCodeList";
        private const string _dataConnectionName = "GetFieldAuthorization";
        private const string _field = @"/dfs:myFields/dfs:dataFields/{0}:GetFieldAuthorizationResponse/{1}:GetFieldAuthorizationResult/FieldPermissions/FieldPermission/@PermissionOnOperation[../@XPath =&quot;{2}&quot;]";
        private const string XSN_URN = "urn:schemas-microsoft-com:office:infopath:";
        private const string TEMPLATE_XML = "template.xml";
        private const string TULFormResourceTextFileName = "ressurstekster.xml";

        private const string UNEXPECTED_ERROR = "An unexpected error occurred while parsing the InfoPath.";
        private const string INFOPATH_FORMAT_ERROR = " The InfoPath file may not be in the required format.";
        private const string FAILED_DURING_GETVIEWS = UNEXPECTED_ERROR + " GetView operation failed." + INFOPATH_FORMAT_ERROR;
        private const string FAILED_DURING_GETFORMFIEDS = UNEXPECTED_ERROR + " GetFields operation failed." + INFOPATH_FORMAT_ERROR;
        private const string FAILED_DURING_GETHELPBUTTONS = UNEXPECTED_ERROR + " GetHelpButtons operation failed." + INFOPATH_FORMAT_ERROR;
        private const string FAILED_DURING_GETFORMTEXT = UNEXPECTED_ERROR + " GetFormText operation failed." + INFOPATH_FORMAT_ERROR;
        private const string XML_FORMAT_ERROR = "Failed to create XmlDocument.";
        private const string EXTRACT_FAILED = "InfoPath Extract failed";
        private const string TEMPORARY_DIRECTORY_NOT_CREATED = "Failed to create temporary directory.";

        #endregion       

        /// <summary>
        /// Defeult Constructor</summary>
        public InfoPathParser()
        {
        }

        /// <summary>
        /// Constructor</summary>
        /// <param name="infoPathFile">InfoPath file as byte[]</param>
        public InfoPathParser(byte[] infoPathFile)
        {
            this._infoPathFileBuffer = infoPathFile;
        }

        /// <summary>
        /// Returns all form views</summary>
        /// <exception cref="InfoPathParsingFailedException">
        /// Throws <c>InfoPathParsingFailedException</c> when an InfoPath parsing operation fails</exception>
        /// <returns>List of form views</returns>
        public List<FormView> GetViews()
        {
            XmlDocument xml = GetXmlDocument(MANIFEST_XSF);
            if (xml == null)
            {
                throw new InfoPathParsingFailedException(FAILED_DURING_GETVIEWS);
            }

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
            namespaceManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");

            List<FormView> formViews = new List<FormView>();
            XmlNodeList views = xml.SelectNodes("//xsf:view", namespaceManager);

            foreach (XmlNode view in views)
            {
                FormView formView = new FormView();
                formView.Name = view.Attributes.GetNamedItem("caption").Value;
                formView.TransformationFile = view.SelectSingleNode("./xsf:mainpane", namespaceManager).Attributes.GetNamedItem("transform").Value;
                formViews.Add(formView);
            }

            return formViews;
        }

        /// <summary>
        /// Returns all CodeListXML</summary>
        /// <exception cref="InfoPathParsingFailedException">
        /// Throws <c>InfoPathParsingFailedException</c> when an InfoPath parsing operation fails</exception>
        /// <returns>List of CodeListXML</returns>
        public List<FormView> GetCodeListXML()
        {
            XmlDocument xml = GetXmlDocument(MANIFEST_XSF);
            if (xml == null)
            {
                throw new InfoPathParsingFailedException(INFOPATH_FORMAT_ERROR);
            }

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
            namespaceManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");

            List<FormView> formViews = new List<FormView>();
            XmlNodeList codeListList = xml.SelectNodes("//xsf:dataObjects/xsf:dataObject", namespaceManager);

            foreach (XmlNode codeList in codeListList)
            {
                FormView formView = new FormView();
                formView.Name = codeList.Attributes.GetNamedItem("name").Value;
                XmlDocument innerXML = new XmlDocument();
                innerXML.LoadXml(codeList.InnerXml);
                XmlNode codeListNode = innerXML.SelectSingleNode("//xsf:query/xsf:webServiceAdapter/xsf:operation/xsf:input", namespaceManager);
                if (codeListNode != null)
                {
                    formView.TransformationFile = codeListNode.Attributes.GetNamedItem("source").Value;
                    formViews.Add(formView);
                }
            }

            return formViews;
        }

        /// <summary>
        /// Returns a list of unique form fields from all the views</summary>
        /// <exception cref="InfoPathParsingFailedException">
        /// Throws <c>InfoPathParsingFailedException</c> when an InfoPath parsing operation fails</exception>
        /// <returns>List of form fields</returns>
        public List<FormField> GetFormFields()
        {
            List<FormView> formViews = GetViews();

            Dictionary<string, FormField> uniqueFields = new Dictionary<string, FormField>();
            foreach (FormView formView in formViews)
            {
                List<FormField> formFieldsInView = GetFormFields(formView.TransformationFile);
                foreach (FormField formField in formFieldsInView)
                {
                    formField.PageName = formView.Name;
                    if (!uniqueFields.ContainsKey(formField.Key))
                    {
                        uniqueFields.Add(formField.Key, formField);
                    }
                }
            }

            List<FormField> formFields = new List<FormField>(uniqueFields.Values);
            return formFields;
        }

        /// <summary>
        /// Returns the form fields for the page</summary>
        /// <exception cref="InfoPathParsingFailedException">
        /// Throws <c>InfoPathParsingFailedException</c> when an InfoPath parsing operation fails</exception>
        /// <param name="pageName">Page Name</param>
        /// <returns>List of form fields for the page</returns>
        public List<FormField> GetFormFields(string pageName)
        {
            List<FormField> formfields = new List<FormField>();
            List<FormView> formViews = GetViews();
            FormView formView = formViews.Find(delegate(FormView view)
            {
                return view.TransformationFile == pageName;
            });
            if (formView != null)
            {
                string viewName = formView.Name;
                string transformationFile = formView.TransformationFile;

                XmlDocument xml = GetXmlDocument(pageName);
                if (xml == null)
                {
                    throw new InfoPathParsingFailedException(FAILED_DURING_GETVIEWS);
                }

                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
                namespaceManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
                namespaceManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");

                // Get root node of the InfoPath Control Xpath; this will be the "match" attribute of the "xsl:template" node that has the <body> tag
                XmlNode bodyNode = xml.SelectSingleNode("/xsl:stylesheet/xsl:template/html/body", namespaceManager);
                string rootNodeValue = "/" + bodyNode.ParentNode.ParentNode.Attributes["match"].Value; // <xsl:template match="blah">

                // Process 'sections' in the document
                XmlNodeList sections = bodyNode.SelectNodes(".//xsl:apply-templates [@mode]", namespaceManager);
                List<FormField> fieldsFromSections = new List<FormField>();
                foreach (XmlNode section in sections)
                {
                    List<FormField> sectionFields = GetRepeatingFields(viewName, xml, rootNodeValue, section, RepeatingGroupType.Section);
                    fieldsFromSections.AddRange(sectionFields);
                }

                // Process 'repeating tables' in the document
                XmlNodeList repeatingTables = bodyNode.SelectNodes(".//xsl:for-each", namespaceManager);
                List<FormField> fieldsFromRepeatingTables = new List<FormField>();
                foreach (XmlNode repeatingTable in repeatingTables)
                {
                    List<FormField> repeatingTableFields = GetRepeatingFields(viewName, xml, rootNodeValue, repeatingTable, RepeatingGroupType.Table);

                    //Check repeating fields of same name in next repeating table
                    foreach (FormField repeatingtablefield in repeatingTableFields)
                    {
                        bool ctrlExists = fieldsFromRepeatingTables.Exists(existingrepeatingtablefield => (existingrepeatingtablefield.ControlID == repeatingtablefield.ControlID));
                        if (!ctrlExists)
                        {
                            fieldsFromRepeatingTables.Add(repeatingtablefield);

                        }
                    }
                }

                // Process 'non-repeating' fields, which are not part of the repeating fields            
                XmlNodeList fields = bodyNode.SelectNodes(".//*[@xd:binding]", namespaceManager);
                List<FormField> regularFields = new List<FormField>();
                foreach (XmlNode field in fields)
                {
                    XmlNode node = field.Attributes.GetNamedItem("xd:xctname");
                    if (node != null)
                    {
                        if (field.Attributes.GetNamedItem("xd:xctname").Value != "ExpressionBox")
                        {
                            FormField formField = new FormField();
                            string fieldKey = rootNodeValue + "/" + field.Attributes.GetNamedItem("xd:binding").Value;
                            XmlNode controlIDNode = field.Attributes.GetNamedItem("xd:CtrlId");
                            string controlID = string.Empty;
                            if (controlIDNode != null)
                            {
                                controlID = field.Attributes.GetNamedItem("xd:CtrlId").Value;
                            }

                            if (fieldKey.Contains('/'))
                            {
                                int startIndex = fieldKey.LastIndexOf('/') + 1;
                                int length = fieldKey.Length - startIndex;
                                formField.Name = fieldKey.Substring(startIndex, length);
                            }
                            else
                            {
                                formField.Name = fieldKey;
                            }

                            formField.Key = fieldKey;
                            formField.ControlID = controlID;
                            formField.PageName = viewName;
                            formField.ControlType = node.Value;

                            if (field.Attributes.GetNamedItem("xd:disableEditing") != null && field.Attributes.GetNamedItem("xd:disableEditing").Value == "yes")
                            {
                                formField.Disabled = true;
                            }

                            bool ctrlExists = fieldsFromRepeatingTables.Exists(repeatingtablefield => (repeatingtablefield.Key == formField.Key || repeatingtablefield.ControlID == formField.ControlID));

                            if (!ctrlExists)
                            {
                                regularFields.Add(formField);
                            }
                        }
                    }
                }

                List<FormField> formFieldsList = new List<FormField>();
                formFieldsList.AddRange(fieldsFromSections);
                formFieldsList.AddRange(fieldsFromRepeatingTables);
                formFieldsList.AddRange(regularFields);
                foreach (FormField newField in formFieldsList)
                {
                    bool keyExists = formfields.Exists(formField => (formField.Key == newField.Key));
                    if (!keyExists)
                    {
                        formfields.Add(newField);
                    }
                }
            }

            return formfields;
        }

        private List<FormField> GetRepeatingFields(string viewName, XmlDocument xml, string groupXpath, XmlNode repeatingControl, RepeatingGroupType repeatingControlType)
        {
            if (repeatingControlType == RepeatingGroupType.Section)
            {
                return ProcessSection(viewName, xml, groupXpath, repeatingControl);
            }
            else
            {
                return ProcessRepeatingTable(viewName, xml, groupXpath, repeatingControl);
            }
        }

        private List<FormField> ProcessRepeatingTable(string viewName, XmlDocument xml, string groupXpath, XmlNode repeatingTable)
        {
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
            namespaceManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            namespaceManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            List<FormField> formfields = new List<FormField>();

            string relativeGroupXpath = groupXpath + "/" + repeatingTable.Attributes["select"].Value;

            // Process 'sections'inside the repeating table
            XmlNodeList sections = repeatingTable.SelectNodes(".//xsl:apply-templates [@mode]", namespaceManager);
            List<FormField> fieldsFromSections = new List<FormField>();
            foreach (XmlNode section in sections)
            {
                List<FormField> sectionFields = GetRepeatingFields(viewName, xml, relativeGroupXpath, section, RepeatingGroupType.Section);
                fieldsFromSections.AddRange(sectionFields);
            }

            // Process 'repeating tables' inside the repeating tables
            XmlNodeList repeatingTables = repeatingTable.SelectNodes(".//xsl:for-each", namespaceManager);
            List<FormField> fieldsFromRepeatingTables = new List<FormField>();
            foreach (XmlNode table in repeatingTables)
            {
                List<FormField> repeatingTableFields = GetRepeatingFields(viewName, xml, relativeGroupXpath, table, RepeatingGroupType.Table);
                fieldsFromRepeatingTables.AddRange(repeatingTableFields);
            }

            // Process 'non-repeating' fields            
            XmlNodeList fields = repeatingTable.SelectNodes(".//*[@xd:binding]", namespaceManager);
            List<FormField> regularFields = new List<FormField>();
            if (fields != null)
            {
                foreach (XmlNode field in fields)
                {
                    if (field.Attributes == null)
                    {
                        continue;
                    }

                    string xctname = field.Attributes.GetNamedItem("xd:xctname").Value;
                    if (xctname != "ExpressionBox")
                    {
                        FormField formField = new FormField();
                        string fieldKey;
                        if (field.Attributes.GetNamedItem("xd:binding").Value != ".")
                        {
                            fieldKey = relativeGroupXpath + "/" + field.Attributes.GetNamedItem("xd:binding").Value;
                        }
                        else
                        {
                            fieldKey = relativeGroupXpath;
                        }

                        XmlNode controlIDNode = field.Attributes.GetNamedItem("xd:CtrlId");
                        string controlID = string.Empty;
                        if (controlIDNode != null)
                        {
                            controlID = controlIDNode.Value;
                        }
                        else
                        {
                            if (xctname.Equals("DTPicker_DTText"))
                            {
                                XmlNode tableParentNode = field.ParentNode;
                                if (tableParentNode != null)
                                {
                                    if (tableParentNode.Attributes != null)
                                    {
                                        XmlNode tableParentNameNode = tableParentNode.Attributes.GetNamedItem("xd:xctname");
                                        if (tableParentNameNode != null && tableParentNameNode.Value.Equals("DTPicker"))
                                        {
                                            controlIDNode = tableParentNode.Attributes.GetNamedItem("xd:CtrlId");
                                            if (controlIDNode != null)
                                            {
                                                controlID = controlIDNode.Value;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (fieldKey.Contains('/'))
                        {
                            int startIndex = fieldKey.LastIndexOf('/') + 1;
                            int length = fieldKey.Length - startIndex;
                            formField.Name = fieldKey.Substring(startIndex, length);
                        }
                        else
                        {
                            formField.Name = fieldKey;
                        }

                        formField.Key = Utils.UnbundlePath(fieldKey);
                        formField.ControlID = controlID;
                        formField.PageName = viewName;
                        regularFields.Add(formField);
                    }
                }
            }

            List<FormField> formFieldsList = new List<FormField>();
            formFieldsList.AddRange(fieldsFromSections);
            formFieldsList.AddRange(fieldsFromRepeatingTables);
            formFieldsList.AddRange(regularFields);
            foreach (FormField newField in formFieldsList)
            {
                bool keyExists = formfields.Exists(formField => formField.Key == newField.Key);
                if (!keyExists)
                {
                    formfields.Add(newField);
                }
            }

            return formfields;
        }

        private List<FormField> ProcessSection(string viewName, XmlDocument xml, string groupXpath, XmlNode sectionNode)
        {
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
            namespaceManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            namespaceManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            List<FormField> formfields = new List<FormField>();

            string relativeGroupXpath = Utils.UnbundlePath(groupXpath + "/" + sectionNode.Attributes["select"].Value);
            string sectionMode = sectionNode.Attributes["mode"].Value;
            string sectionXpath = "//xsl:template [@mode='" + sectionMode + "']";
            XmlNode sectionTemplate = xml.SelectSingleNode(sectionXpath, namespaceManager);

            // Process 'sections'inside the repeating table
            XmlNodeList sections = sectionTemplate.SelectNodes(".//xsl:apply-templates [@mode]", namespaceManager);
            List<FormField> fieldsFromSections = new List<FormField>();
            foreach (XmlNode section in sections)
            {
                List<FormField> sectionFields = GetRepeatingFields(viewName, xml, relativeGroupXpath, section, RepeatingGroupType.Section);
                fieldsFromSections.AddRange(sectionFields);
            }

            // Process 'repeating tables' inside the repeating tables
            XmlNodeList repeatingTables = sectionTemplate.SelectNodes(".//xsl:for-each", namespaceManager);
            List<FormField> fieldsFromRepeatingTables = new List<FormField>();
            foreach (XmlNode table in repeatingTables)
            {
                List<FormField> repeatingTableFields = GetRepeatingFields(viewName, xml, relativeGroupXpath, table, RepeatingGroupType.Table);
                fieldsFromRepeatingTables.AddRange(repeatingTableFields);
            }

            // Process 'non-repeating' fields            
            XmlNodeList fields = sectionTemplate.SelectNodes(".//*[@xd:binding]", namespaceManager);
            List<FormField> regularFields = new List<FormField>();
            foreach (XmlNode field in fields)
            {
                if (field.Attributes.GetNamedItem("xd:xctname") != null && field.Attributes.GetNamedItem("xd:xctname").Value != "ExpressionBox")
                {
                    FormField formField = new FormField();
                    string fieldKey;
                    if (field.Attributes.GetNamedItem("xd:binding").Value != ".")
                    {
                        fieldKey = relativeGroupXpath + "/" + field.Attributes.GetNamedItem("xd:binding").Value;
                    }
                    else
                    {
                        fieldKey = relativeGroupXpath;
                    }

                    if (fieldKey.Contains('/'))
                    {
                        int startIndex = fieldKey.LastIndexOf('/') + 1;
                        int length = fieldKey.Length - startIndex;
                        formField.Name = fieldKey.Substring(startIndex, length);
                    }
                    else
                    {
                        formField.Name = fieldKey;
                    }

                    formField.Key = Utils.UnbundlePath(fieldKey);
                    formField.PageName = viewName;
                    formField.ControlType = field.Attributes.GetNamedItem("xd:xctname").Value;
                    formField.ControlID = field.Attributes.GetNamedItem("xd:CtrlId").Value;

                    if (field.Attributes.GetNamedItem("xd:disableEditing") != null && field.Attributes.GetNamedItem("xd:disableEditing").Value == "yes")
                    {
                        formField.Disabled = true;
                    }

                    regularFields.Add(formField);
                }
            }

            List<FormField> formFieldsList = new List<FormField>();
            formFieldsList.AddRange(fieldsFromSections);
            formFieldsList.AddRange(fieldsFromRepeatingTables);
            formFieldsList.AddRange(regularFields);
            foreach (FormField newField in formFieldsList)
            {
                bool keyExists = formfields.Exists(formField => formField.Key == newField.Key);
                if (!keyExists)
                {
                    formfields.Add(newField);
                }
            }

            return formfields;
        }

        /// <summary>
        /// Returns all the help buttons</summary>
        /// <exception cref="InfoPathParsingFailedException">
        /// Throws <c>InfoPathParsingFailedException</c> when an InfoPath parsing operation fails</exception>
        /// <param name="pageName">Page Name</param>
        /// <returns>List of help buttons available</returns>
        public List<FormHelpButton> GetHelpButtons(string pageName)
        {
            List<FormView> formViews = GetViews();
            FormView formView = formViews.Find(delegate (FormView view)
            {
                return view.Name == pageName;
            });
            string viewName = formView.Name;
            string transformationFile = formView.TransformationFile;

            XmlDocument xml = GetXmlDocument(transformationFile);
            if (xml == null)
            {
                throw new InfoPathParsingFailedException(INFOPATH_FORMAT_ERROR);
            }

            List<FormText> formTexts = new List<FormText>();

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
            namespaceManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");
            namespaceManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            namespaceManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");

            string contolXPathForButton = GetControlXPath(PageControlType.Button);
            XmlNodeList controlNodesForButton = xml.SelectNodes(contolXPathForButton, namespaceManager);
            Dictionary<string, FormHelpButton> uniqueHelpTexts = new Dictionary<string, FormHelpButton>();
            foreach (XmlNode controlNode in controlNodesForButton)
            {
                string ctrlID = controlNode.InnerText;
                if (ctrlID.Contains(HELP_TEXT))
                {
                    FormHelpButton formHelpButton = new FormHelpButton();
                    formHelpButton.Name = ctrlID;

                    if (formHelpButton.Name.Length > HELP_TEXT.Length)
                    {
                        if (formHelpButton.Name.Substring(0, HELP_TEXT.Length).CompareTo(HELP_TEXT) == 0) 
                        {
                            formHelpButton.PageName = pageName;
                            if (!uniqueHelpTexts.ContainsKey(formHelpButton.Name))
                            {
                                uniqueHelpTexts.Add(formHelpButton.Name, formHelpButton);
                            }
                        }
                    }
                }
            }

            string contolXPathForPictureButton = GetControlXPath(PageControlType.PictureButton);
            XmlNodeList controlNodesForPictureButton = xml.SelectNodes(contolXPathForPictureButton, namespaceManager);
            foreach (XmlNode controlNode in controlNodesForPictureButton)
            {
                string ctrlID = controlNode.InnerText;
                if (ctrlID.Contains(HELP_TEXT))
                {
                    FormHelpButton formHelpButton = new FormHelpButton();
                    formHelpButton.Name = ctrlID;

                    if (formHelpButton.Name.Length > HELP_TEXT.Length)
                    {
                        if (formHelpButton.Name.Substring(0, HELP_TEXT.Length).CompareTo(HELP_TEXT) == 0)
                        {
                            formHelpButton.PageName = pageName;
                            if (!uniqueHelpTexts.ContainsKey(formHelpButton.Name))
                            {
                                uniqueHelpTexts.Add(formHelpButton.Name, formHelpButton);
                            }
                        }
                    }
                }
            }

            List<FormHelpButton> formHelpButtons = new List<FormHelpButton>(uniqueHelpTexts.Values);
            return formHelpButtons;
        }

        /// <summary>
        /// Loads and returns an <c>XmlDocument</c> object from a speficied file in the InfoPath</summary>
        /// <exception cref="InfoPathParsingFailedException">
        /// Throws <c>InfoPathParsingFailedException</c> when an InfoPath parsing operation fails</exception>
        /// <see cref="XmlDocument"/>        
        /// <param name="fileName">File inside InfoPath</param>
        /// <returns>XmlDocument</returns>
        public XmlDocument GetXmlDocument(string fileName)
        {
            byte[] fileBuffer = GetFile(fileName);
            if (fileBuffer == null)
            {
                return null;
            }

            XmlDocument xmlDocument = new XmlDocument();

            if (fileBuffer != null)
            {
                MemoryStream fileStream = new MemoryStream(fileBuffer);
                xmlDocument.Load(fileStream);
                fileStream.Close();
            }

            return xmlDocument;
        }

        /// <summary>
        /// Returns a specific file from the InfoPath</summary>
        /// <param name="fileName">File Name</param>
        /// <returns></returns>
        public byte[] GetFile(string fileName)
        {
            try
            {
                MemoryStream infoPathFileStream = new MemoryStream(this._infoPathFileBuffer);
                Extract cab = new Extract();
                cab.SetSingleFile(fileName);
                cab.evAfterCopyFile += new Extract.delAfterCopyFile(Cab_AfterFileExtractFromStream);
                cab.ExtractStream(infoPathFileStream, MEMORY_STREAM);
                infoPathFileStream.Close(); // Closing so that CabLib doesnt leak this.
                return _lastExtractedFile;  // See cab_AfterFileExtractFromStream event handler [CabLib library - Extract.delAfterCopyFile]            
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the xsd for My Fields
        /// </summary>
        /// <returns></returns>
        public string GetMySchemaXsd()
        {
            string xsd = string.Empty;
            XmlDocument xml = GetXmlDocument(MANIFEST_XSF);
            if (xml == null)
            {
                throw new InfoPathParsingFailedException(INFOPATH_FORMAT_ERROR);
            }

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
            namespaceManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");

            XmlNodeList rootSchemas = xml.SelectNodes("//xsf:documentSchema", namespaceManager);
            foreach (XmlNode rootSchema in rootSchemas)
            {
                string primaryXsdFileName = rootSchema.Attributes.GetNamedItem("location").Value;

                if (primaryXsdFileName.Contains(MYSCHEMA_XSD))
                {
                    // if you start with an empty infopath form when designing, infopath will add a timestamp and schema url in the
                    // location attribute (for example: <xsf:documentSchema rootSchema="yes" location="http://schemas.microsoft.com/office/infopath/2003/myXSD/2009-11-26T11:47:56 myschema.xsd"></xsf:documentSchema>. 
                    // This is not reflected in the file name, it will alway be myschema.xsd.
                    primaryXsdFileName = MYSCHEMA_XSD;

                    byte[] fileBuffer = GetFile(primaryXsdFileName);
                    if (fileBuffer == null)
                    {
                        throw new InfoPathParsingFailedException(EXTRACT_FAILED);
                    }

                    UTF8Encoding encoding = new UTF8Encoding();
                    xsd = encoding.GetString(fileBuffer);
                }
            }

            return xsd;
        }

        /// <summary>
        /// Returns the primary Xsd for the InfoPath file
        /// </summary>
        /// <returns>Primary Xsd as byte array</returns>
        public byte[] GetPrimaryXsd()
        {
            XmlDocument xml = GetXmlDocument(MANIFEST_XSF);
            if (xml == null)
            {
                throw new InfoPathParsingFailedException(INFOPATH_FORMAT_ERROR);
            }

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
            namespaceManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");

            XmlNode rootSchema = xml.SelectSingleNode("//xsf:documentSchema [@rootSchema='yes']", namespaceManager);
            string primaryXsdFileName = rootSchema.Attributes.GetNamedItem("location").Value;

            // Sometimes the location value contains a namespace declaration before the xsd file name. This seems to depend on the structure of the xsd file(s).
            // This is always separated by a space so to get the filename the the substring after the space is used.
            if (primaryXsdFileName.Contains(" "))
            {
                primaryXsdFileName = primaryXsdFileName.Substring(primaryXsdFileName.LastIndexOf(" ") + 1, primaryXsdFileName.Length - (primaryXsdFileName.LastIndexOf(" ") + 1));
            }

            if (primaryXsdFileName.Contains(MYSCHEMA_XSD))
            {
                // if you start with an empty infopath form when designing, infopath will add a timestamp and schema url in the
                // location attribute (for example: <xsf:documentSchema rootSchema="yes" location="http://schemas.microsoft.com/office/infopath/2003/myXSD/2009-11-26T11:47:56 myschema.xsd"></xsf:documentSchema>. 
                // This is not reflected in the file name, it will alway be myschema.xsd.
                primaryXsdFileName = MYSCHEMA_XSD;
            }

            byte[] fileBuffer = GetFile(primaryXsdFileName);
            if (fileBuffer == null)
            {
                throw new InfoPathParsingFailedException(EXTRACT_FAILED);
            }

            return fileBuffer;
        }

        /// <summary>
        /// Get XSD
        /// </summary>
        /// <returns>The xsd</returns>
        public byte[] GetXSD()
        {
            Extract cab = new Extract();
            MemoryStream infoPathFileStream = new MemoryStream(this._infoPathFileBuffer);
            cab.evAfterCopyFile += new Extract.delAfterCopyFile(Cab_AfterFileExtractFromStream);
            cab.evBeforeCopyFile += new Extract.delBeforeCopyFile(OnBeforeCopyFile);
            cab.ExtractStream(infoPathFileStream, MEMORY_STREAM);
            infoPathFileStream.Close(); // Closing so that CabLib doesnt leak this.
            return _lastExtractedFile;  // See cab_AfterFileExtractFromStream event handler [CabLib library - Extract.delAfterCopyFile]            
        }

        /// <summary>
        /// Get form texts
        /// </summary>
        /// <returns>A list of form texts</returns>
        public List<FormText> GetFormTexts()
        {
            List<FormView> formViews = GetViews();
            List<FormText> formTexts = new List<FormText>();
            foreach (FormView formView in formViews)
            {
                List<FormText> pageTexts = GetFormTexts(formView.TransformationFile);
                formTexts.AddRange(pageTexts);
            }

            //List<FormText> resourceTexts = GetFormTexts(null);
            //formTexts.AddRange(resourceTexts);
            //formTexts.AddRange(GetFormTexts(MANIFEST_XSF));

            List<FormText> uniqueFormTexts = new List<FormText>();
            foreach (FormText formText in formTexts)
            {
                if (!uniqueFormTexts.Exists(text => ((text.TextCode == formText.TextCode) && (text.Page == formText.Page))))
                {
                    uniqueFormTexts.Add(formText);
                }
            }

            return uniqueFormTexts;
        }

        /// <summary>
        /// Publish InfoPath file
        /// </summary>
        /// <param name="pathToXsn">path to xsn file</param>
        /// <returns></returns>
        public string PublishInfoPath(string pathToXsn)
        {
            string fileName = Path.GetFileName(pathToXsn);
            string directoryName = Path.GetDirectoryName(pathToXsn);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(pathToXsn);
            string cabFileName = directoryName + Path.DirectorySeparatorChar + fileNameWithoutExt + ".cab";
            string urn = XSN_URN + fileNameWithoutExt + ":";

            using (FileStream fileStream = new FileStream(pathToXsn, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                byte[] infoPathBuffer = new byte[fileStream.Length];
                fileStream.Read(infoPathBuffer, 0, (int)fileStream.Length);
                _infoPathFileBuffer = infoPathBuffer;
            }

            XmlDocument manifestXsf = new XmlDocument
            {
                PreserveWhitespace = false
            };
            manifestXsf = GetXmlDocument("Manifest.xsf");

            manifestXsf.DocumentElement.RemoveAttribute("publishUrl");
            manifestXsf.DocumentElement.RemoveAttribute("trustLevel");
            manifestXsf.DocumentElement.RemoveAttribute("trustSetting");

            if (manifestXsf.DocumentElement.Attributes.GetNamedItem("requireFullTrust") == null)
            {
                XmlAttribute attribute = manifestXsf.CreateAttribute("requireFullTrust");
                attribute.Value = "yes";
                manifestXsf.DocumentElement.Attributes.Append(attribute);
            }

            if (manifestXsf.DocumentElement.Attributes.GetNamedItem("name") != null)
            {
                manifestXsf.DocumentElement.Attributes.GetNamedItem("name").Value = urn;
            }

            XmlNode node = manifestXsf.GetElementsByTagName("xsf:fileNew")[0];
            node.FirstChild.Attributes.GetNamedItem("caption").Value = fileNameWithoutExt;

            File.Copy(pathToXsn, cabFileName);
            string extractPath = directoryName + Path.DirectorySeparatorChar + default(Guid).ToString() + Path.DirectorySeparatorChar;

            ExtractCab(cabFileName, extractPath);
            manifestXsf.Save(extractPath + MANIFEST_XSF); // Ovewrites the file.

            XmlDocument xdoc = new XmlDocument();
            xdoc.PreserveWhitespace = false;
            xdoc.Load(extractPath + TEMPLATE_XML);
            xdoc.ChildNodes[1].Value = SetUrnInTemplate(urn, xdoc.ChildNodes[1].Value);
            xdoc.PreserveWhitespace = true;
            xdoc.Save(extractPath + TEMPLATE_XML);

            CompressXSN(extractPath, pathToXsn);
            Directory.Delete(extractPath);

            return extractPath;
        }

        /// <summary>
        /// Replaces the urn inside the template file.
        /// </summary>
        /// <remarks>This is string manipulation because "name"  is not an attribute in the node.</remarks>
        /// <param name="urn">The urn to set</param>
        /// <param name="nodeValue">Node value</param>
        /// <returns></returns>
        private string SetUrnInTemplate(string urn, string nodeValue)
        {
            string afterName = nodeValue.Substring(nodeValue.IndexOf("name=\"") + 6);
            string stringToReplace = afterName.Substring(0, afterName.IndexOf("\""));
            string nodeValueWithUrn = nodeValue.Replace(stringToReplace, urn);
            return nodeValueWithUrn;
        }

        /// <summary>
        /// Localize the InfoPath form
        /// </summary>
        /// <param name="mainLanguageInfoPathFile">The InfoPath file in the main language</param>
        /// <param name="requiredLanguage">The required language</param>
        /// <param name="focusPageXSL">Focus page xsl</param>
        /// <param name="formTextsInTargetLanguage">Form texts in target language</param>
        /// <param name="mainLanguage">Value indicating if this is the main language or not</param>
        /// <returns></returns>
        public byte[] LocalizeForm(byte[] mainLanguageInfoPathFile, LanguageType requiredLanguage, string focusPageXSL, List<FormText> formTextsInTargetLanguage, bool mainLanguage)
        {
            #region Constants
            const string TEMP_DIR = "c:\\windows\\temp";
            const string RELATIVE_SOURCE_URL = "tul\\{0}\\source\\";
            const string RELATIVE_DESINATION_URL = "tul\\{0}\\destination\\";
            const string FORM_NAME = "form.xsn";
            const string UNDERSCORE = "_";
            const string DOT_XSN = ".xsn";
            const string SLASH = "\\";
            const char DOT = '.';
            byte[] localizedForm = null;
            #endregion Constants

            Console.WriteLine("Starting form localization at " + DateTime.Now.ToString("hh:mm:ss.fff tt") + ".");
            try
            {
                DirectoryInfo tempDirectory = new DirectoryInfo(TEMP_DIR);
                string randomString = Guid.NewGuid().ToString();
                string sourceUrl = string.Format(RELATIVE_SOURCE_URL, randomString);
                string destinationUrl = string.Format(RELATIVE_DESINATION_URL, randomString);
                tempDirectory.CreateSubdirectory(sourceUrl);
                tempDirectory.CreateSubdirectory(destinationUrl);

                string infopathFileName = FORM_NAME;
                sourceUrl = TEMP_DIR + SLASH + sourceUrl + infopathFileName;
                destinationUrl = TEMP_DIR + SLASH + destinationUrl;
                FileStream fileStream = new FileStream(sourceUrl, FileMode.CreateNew);
                fileStream.Write(mainLanguageInfoPathFile, 0, mainLanguageInfoPathFile.Length);
                fileStream.Close();
                DirectoryInfo temporaryDirectory = new DirectoryInfo(destinationUrl);
                InfoPathParser parser;
                XmlNamespaceManager namespaceManager;

                ExtractCab(sourceUrl, destinationUrl);

                if (!temporaryDirectory.Exists)
                {
                    throw new InfoPathParsingFailedException(TEMPORARY_DIRECTORY_NOT_CREATED);
                }

                parser = new InfoPathParser(mainLanguageInfoPathFile);
                XmlDocument manifestXsf = parser.GetXmlDocument(MANIFEST_XSF);

                if (manifestXsf == null)
                {
                    throw new InfoPathParsingFailedException(INFOPATH_FORMAT_ERROR);
                }

                namespaceManager = new XmlNamespaceManager(manifestXsf.NameTable);
                namespaceManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");

                if (!mainLanguage)
                {
                    LocalizeViewReferencesInManifest(requiredLanguage, manifestXsf, namespaceManager);
                    LocalizeFormLocaleReferencesInManifest(requiredLanguage, manifestXsf);

                    LocalizeValidationAndActionButtonInManifest(formTextsInTargetLanguage, manifestXsf, namespaceManager);

                    List<FormText> resourceTexts = GetResourceTextsFromFormTextList(formTextsInTargetLanguage);

                    // LocalizeValidationTextInResourceTextFile(destinationUrl, resourceTexts);
                }

                SetCorrectFocusPageInManifest(focusPageXSL, parser, namespaceManager);

                // Save changes for Manifest.xsf
                manifestXsf.PreserveWhitespace = true;
                manifestXsf.Save(destinationUrl + MANIFEST_XSF); // This overwrites the file in the folder

                // Only for secondary languages
                if (!mainLanguage)
                {
                    LocalizeFormTexts(formTextsInTargetLanguage, destinationUrl, temporaryDirectory, parser);

                    LocalizeCodelist(requiredLanguage, destinationUrl, parser, namespaceManager);
                }

                // For all languages
                // Recab and return the infoPath
                infopathFileName = sourceUrl.Remove(sourceUrl.LastIndexOf(DOT)) + UNDERSCORE + (int)requiredLanguage + DOT_XSN;
                CompressXSN(destinationUrl, infopathFileName);
                localizedForm = GetFileAsByteArray(infopathFileName);

                Directory.Delete(TEMP_DIR + "\\tul\\" + randomString, true);

                Console.WriteLine("Done localizing at " + DateTime.Now.ToString("hh:mm:ss.fff tt") + ".");

            }
            catch (Exception ex)
            {
                throw new Exception(LOCALIZINGFILESERROR, ex);
            }

            return localizedForm;
        }

        /// <summary>
        /// Localizes all form texts in an Infopath form that have 
        /// </summary>
        /// <param name="formTextsInTargetLanguage">List of form texts in target language"</param>
        /// <param name="destinationUrl">Destination url</param>
        /// <param name="directory">Directory</param>
        /// <param name="parser">InfoPath parser</param>
        private static void LocalizeFormTexts(List<FormText> formTextsInTargetLanguage, string destinationUrl, DirectoryInfo directory, InfoPathParser parser)
        {
            Dictionary<string, FormText> buttonAndExpressionBoxDict = new Dictionary<string, FormText>();
            Dictionary<string, FormText> hintTextDict = new Dictionary<string, FormText>();
            Dictionary<string, FormText> dropdownDict = new Dictionary<string, FormText>();
            Dictionary<string, FormText> anchorDict = new Dictionary<string, FormText>();

            #region Fill dictionaries
            int startIndex = 0;
            int stopIndex = 0;

            foreach (FormText formText in formTextsInTargetLanguage)
            {
                switch (formText.TextType)
                {
                    case TextType.Button:
                    case TextType.ExpressionBox:
                        /* 
                         * FormTexts of type Button and ExpressionBox will have the following TextCode-formats:
                         * 
                         *  Button: //input [@xd:CtrlId = 'HelpText_orid_2207']
                         *  ExpressionBox: //span [@xd:CtrlId = 'CTRL45']/xsl:value-of
                         */
                        startIndex = formText.TextCode.IndexOf("CtrlId = '") + "CtrlId = '".Length;
                        stopIndex = formText.TextCode.IndexOf("'", startIndex);
                        string ctrlId = formText.TextCode.Substring(startIndex, stopIndex - startIndex);
                        if (buttonAndExpressionBoxDict.ContainsKey(formText.Page + ctrlId))
                        {
                            buttonAndExpressionBoxDict[formText.Page + ctrlId] = formText;
                        }
                        else
                        {
                            buttonAndExpressionBoxDict.Add(formText.Page + ctrlId, formText);
                        }

                        break;
                    case TextType.DropdownListBox:
                    case TextType.ListBox:
                        /*  
                         * FormTexts of type DropDownListBox and ListBox will have the following TextCode-formats:
                         *  
                         *  DropDownListBox: //select [@xd:CtrlId = 'CTRL29']/option [@value='oktober']
                         */
                        startIndex = formText.TextCode.IndexOf("CtrlId = '") + "CtrlId = '".Length;
                        stopIndex = formText.TextCode.IndexOf("'", startIndex);
                        string selectCtrlId = formText.TextCode.Substring(startIndex, stopIndex - startIndex);

                        startIndex = formText.TextCode.IndexOf("@value='") + "@value='".Length;
                        stopIndex = formText.TextCode.IndexOf("'", startIndex);
                        string optionValue = formText.TextCode.Substring(startIndex, stopIndex - startIndex);

                        if (dropdownDict.ContainsKey(selectCtrlId + "_" + optionValue))
                        {
                            dropdownDict[selectCtrlId + "_" + optionValue] = formText;
                        }
                        else
                        {
                            dropdownDict.Add(selectCtrlId + "_" + optionValue, formText);
                        }

                        break;

                    case TextType.HintText:
                        if (formText.TextCode.StartsWith("//div"))
                        {
                            /* 
                             * FormTexts of type HintText will have the following TextCode-format:
                             *  
                             * HintText: //div [@class='optionalPlaceholder' and @xd:xmlToEdit='RealisasjonerSalg-grp-6705_36']
                             */
                            startIndex = formText.TextCode.IndexOf("@class='") + "@class='".Length;
                            stopIndex = formText.TextCode.IndexOf("'", startIndex);
                            string classname = formText.TextCode.Substring(startIndex, stopIndex - startIndex);

                            startIndex = formText.TextCode.IndexOf("@xd:xmlToEdit='") + "@xd:xmlToEdit='".Length;
                            stopIndex = formText.TextCode.IndexOf("'", startIndex);
                            string xmlToEdit = formText.TextCode.Substring(startIndex, stopIndex - startIndex);

                            if (hintTextDict.ContainsKey(xmlToEdit))
                            {
                                hintTextDict[xmlToEdit] = formText;
                            }
                            else
                            {
                                hintTextDict.Add(xmlToEdit, formText);
                            }
                        }

                        break;

                    case TextType.HyperLink:
                        /* 
                         * FormTexts of type HyperLink will have the following TextCode-formats:
                         *  
                         * HyperLink: //a [@href='http://www.ssb.no/skjema/finmark/rapport/orbof/veil/Veiledning1050-2010-endelig.doc']
                         */

                        startIndex = formText.TextCode.IndexOf("@href='") + "@href='".Length;
                        stopIndex = formText.TextCode.IndexOf("'", startIndex);
                        string href = formText.TextCode.Substring(startIndex, stopIndex - startIndex);

                        if (anchorDict.ContainsKey(href))
                        {
                            anchorDict[href] = formText;
                        }
                        else
                        {
                            anchorDict.Add(href, formText);
                        }

                        break;

                    case TextType.TableHeader:
                        break;
                }
            }
            #endregion

            const string ALL_STYLE_SHEETS = "*.xsl";

            // Setting the target language texts
            FileInfo[] viewFiles = directory.GetFiles(ALL_STYLE_SHEETS);
            List<FormView> formViews = parser.GetViews();

            foreach (FormView formView in formViews)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    XmlDocument formViewXml = parser.GetXmlDocument(formView.TransformationFile);
                    if (formViewXml == null)
                    {
                        throw new InfoPathParsingFailedException(FAILED_DURING_GETVIEWS);
                    }

                    LocalizeFormView(buttonAndExpressionBoxDict, hintTextDict, dropdownDict, anchorDict, formViewXml, formView.TransformationFile);

                    // To ensure BOM characters are not included
                    XmlTextWriter xmlTextWriter = new XmlTextWriter(destinationUrl + formView.TransformationFile, new UTF8Encoding(false));
                    formViewXml.Save(xmlTextWriter);
                    xmlTextWriter.Close();
                }
            }
        }

        /// <summary>
        /// Translate all nodes in an Infopath view (XSL) that have translations in one of the passed dictionaries.
        /// </summary>
        /// <param name="buttonAndExpressionBoxDict">buttonAndExpressionBoxDict</param>
        /// <param name="hintTextDict">hintTextDict</param>
        /// <param name="dropdownDict">dropdownDict</param>
        /// <param name="anchorDict">anchorDict</param>
        /// <param name="formViewXml">formViewXml</param>
        /// <param name="formViewName">The name of the InfoPath page</param>
        private static void LocalizeFormView(
            Dictionary<string, FormText> buttonAndExpressionBoxDict,
            Dictionary<string, FormText> hintTextDict,
            Dictionary<string, FormText> dropdownDict,
            Dictionary<string, FormText> anchorDict,
            XmlDocument formViewXml,
            string formViewName)
        {
#pragma warning disable SA1305 // Field names should not use Hungarian notation
            XmlNamespaceManager nsManager = new XmlNamespaceManager(formViewXml.NameTable);
#pragma warning restore SA1305 // Field names should not use Hungarian notation
            nsManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            nsManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            nsManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");

            XmlNodeList expressionBoxNodes = formViewXml.SelectNodes("//span/xsl:value-of", nsManager);
            XmlNodeList buttonNodes = formViewXml.SelectNodes("//input", nsManager);
            XmlNodeList dropdownNodes = formViewXml.SelectNodes("//select", nsManager);
            XmlNodeList anchorNodes = formViewXml.SelectNodes("//a", nsManager);
            XmlNodeList hintTextNodes = formViewXml.SelectNodes("//div [@class='optionalPlaceholder']", nsManager);

            TranslateExpressionBoxNodes(buttonAndExpressionBoxDict, expressionBoxNodes, formViewName);
            TranslateButtonNodes(buttonAndExpressionBoxDict, buttonNodes, formViewName);
            TranslateDropdownNodes(dropdownDict, dropdownNodes, nsManager);
            TranslateAnchorNodes(anchorDict, anchorNodes);
            TranslateHintTextNodes(hintTextDict, hintTextNodes);
        }

        /// <summary>
        /// Translates all hint text nodes in the nodelist, given that their translation exist in the hintTextDict.
        /// </summary>
        /// <param name="hintTextDict">hintTextDict</param>
        /// <param name="hintTextNodes">hintTextNodes</param>
        private static void TranslateHintTextNodes(Dictionary<string, FormText> hintTextDict, XmlNodeList hintTextNodes)
        {
            foreach (XmlNode hintTextNode in hintTextNodes)
            {
                TranslateHintTextNode(hintTextDict, hintTextNode);
            }
        }

        /// <summary>
        /// Translates all anchor nodes in the nodelist, given that their translation exist in the anchorDict.
        /// </summary>
        /// <param name="anchorDict">anchorDict</param>
        /// <param name="anchorNodes">anchorNodes</param>
        private static void TranslateAnchorNodes(Dictionary<string, FormText> anchorDict, XmlNodeList anchorNodes)
        {
            foreach (XmlNode anchorNode in anchorNodes)
            {
                TranslateAnchorNode(anchorDict, anchorNode);
            }
        }

        /// <summary>
        /// Translates all dropdown nodes in the nodelist, given that their translation exist in the dropdownDict.
        /// </summary>
        /// <param name="dropdownDict">dropdownDict</param>
        /// <param name="dropdownNodes">dropdownNodes</param>
        /// <param name="nsManager">nsManager</param>
        private static void TranslateDropdownNodes(Dictionary<string, FormText> dropdownDict, XmlNodeList dropdownNodes, XmlNamespaceManager nsManager)
        {
            foreach (XmlNode selectNode in dropdownNodes)
            {
                TranslateDropdownNode(dropdownDict, selectNode, nsManager);
            }
        }

        /// <summary>
        /// Translates the button nodes, given that their translation exist in the buttonAndExpressionBoxDict.
        /// </summary>
        /// <param name="buttonAndExpressionBoxDict">buttonAndExpressionBoxDict</param>
        /// <param name="buttonNodes">buttonNodes</param>
        /// <param name="formViewName">The name of the InfoPath page</param>
        private static void TranslateButtonNodes(Dictionary<string, FormText> buttonAndExpressionBoxDict, XmlNodeList buttonNodes, string formViewName)
        {
            foreach (XmlNode node in buttonNodes)
            {
                TranslateButtonNode(buttonAndExpressionBoxDict, node, formViewName);
            }
        }

        /// <summary>
        /// Translates the expression nodes, given that their translation exist in the buttonAndExpressionBoxDict.
        /// </summary>
        /// <param name="buttonAndExpressionBoxDict">buttonAndExpressionBoxDict</param>
        /// <param name="expressionBoxNodes">expressionBoxNodes</param>
        /// <param name="formViewName">The name of the InfoPath page</param>
        private static void TranslateExpressionBoxNodes(Dictionary<string, FormText> buttonAndExpressionBoxDict, XmlNodeList expressionBoxNodes, string formViewName)
        {
            foreach (XmlNode node in expressionBoxNodes)
            {
                TranslateExpressionBoxNode(buttonAndExpressionBoxDict, node, formViewName);
            }
        }

        /// <summary>
        /// Translates the given dropdown node from the Infopath view if it is present in the dropdown dictionary.
        /// </summary>
        /// <param name="dropdownDict">dropdownDict</param>
        /// <param name="selectNode">selectNode</param>
        /// <param name="nsManager">nsManager</param>
        private static void TranslateDropdownNode(Dictionary<string, FormText> dropdownDict, XmlNode selectNode, XmlNamespaceManager nsManager)
        {
            string key = string.Empty;
            FormText formText;
            XmlAttribute ctrlIdAttribute = selectNode.Attributes["xd:CtrlId"];

            if (ctrlIdAttribute == null)
            {
                return;
            }

            XmlNodeList optionNodes = selectNode.SelectNodes("./option", nsManager);

            foreach (XmlNode optionNode in optionNodes)
            {
                XmlAttribute valueAttribute = optionNode.Attributes["value"];
                if (valueAttribute == null)
                {
                    continue;
                }

                key = ctrlIdAttribute.Value + "_" + valueAttribute.Value;

                if (dropdownDict.ContainsKey(key))
                {
                    formText = dropdownDict[key];

                    if (optionNode.InnerXml.Length > optionNode.InnerXml.LastIndexOf(">") + 1)
                    {
                        optionNode.InnerXml = optionNode.InnerXml.Remove(optionNode.InnerXml.LastIndexOf('>') + 1);
                        optionNode.InnerXml += formText.TextContent;
                    }
                }
            }
        }

        /// <summary>
        /// Translates the given anchorNode from the Infopath view if it is present in the anchor dictionary.
        /// </summary>
        /// <param name="anchorDict">anchorDict</param>
        /// <param name="anchorNode">anchorNode</param>
        private static void TranslateAnchorNode(Dictionary<string, FormText> anchorDict, XmlNode anchorNode)
        {
            string key = string.Empty;
            FormText formText;
            XmlAttribute hrefAttribute = anchorNode.Attributes["href"];

            if (hrefAttribute == null)
            {
                return;
            }

            key = hrefAttribute.Value;

            if (anchorDict.ContainsKey(key))
            {
                formText = anchorDict[key];
                anchorNode.InnerText = formText.TextContent;
            }
        }

        /// <summary>
        /// Translates the given hintTextNode from the Infopath view if it is present in the hintText dictionary.
        /// </summary>
        /// <param name="hintTextDict">Hint text dictionary</param>
        /// <param name="hintTextNode">Hint text node</param>
        private static void TranslateHintTextNode(Dictionary<string, FormText> hintTextDict, XmlNode hintTextNode)
        {
            string key = string.Empty;

            FormText formText;
            XmlAttribute xmlToEditAttribute = hintTextNode.Attributes["xd:xmlToEdit"];

            if (xmlToEditAttribute == null)
            {
                return;
            }

            key = xmlToEditAttribute.Value;

            if (hintTextDict.ContainsKey(key))
            {
                formText = hintTextDict[key];
                hintTextNode.InnerText = formText.TextContent;
            }
        }

        /// <summary>
        /// Translates the given node from the Infopath view if it is present in the buttonAndExpressionBox dictionary
        /// </summary>
        /// <param name="buttonAndExpressionBoxDict">buttonAndExpressionBox dictionary</param>
        /// <param name="node">buttonAndExpressionBox node</param>
        /// <param name="formViewName">The name of the InfoPath page</param>
        private static void TranslateExpressionBoxNode(Dictionary<string, FormText> buttonAndExpressionBoxDict, XmlNode node, string formViewName)
        {
            const string QUOTE = "\"";
            string key = string.Empty;

            if (node.ParentNode == null)
            {
                return;
            }

            XmlAttribute ctrlIdAttribute = node.ParentNode.Attributes["xd:CtrlId"];

            if (ctrlIdAttribute != null)
            {
                key = formViewName + ctrlIdAttribute.Value;

                if (buttonAndExpressionBoxDict.ContainsKey(key))
                {
                    FormText formText = buttonAndExpressionBoxDict[key];

                    if (!formText.TextContent.Contains(QUOTE))
                    {
                        formText.TextContent = QUOTE + formText.TextContent + QUOTE;
                    }

                    ReplaceExpressionBoxText(node, formText);
                }
            }
        }

        private static void ReplaceExpressionBoxText(XmlNode selectNode, FormText formText)
        {
            // Set translated text in select node
            if (selectNode.Attributes["select"] != null)
            {
                selectNode.Attributes["select"].Value = formText.TextContent;
            }
            else
            {
                selectNode.InnerText = formText.TextContent;
            }

            // Set translated text in ancestor nodes
            if (selectNode.ParentNode != null)
            {
                XmlNode spanNode = selectNode.ParentNode;

                if (spanNode.Attributes["xd:binding"] != null)
                {
                    spanNode.Attributes["xd:binding"].Value = formText.TextContent;
                }
                else if (spanNode.ParentNode != null)
                {
                    XmlNode fontNode = spanNode.ParentNode;

                    if (fontNode.Attributes["xd:binding"] != null)
                    {
                        if (fontNode.Attributes["xd:xctname"] != null)
                        {
                            if (fontNode.Attributes["xd:xctname"].Value == "ExpressionBox")
                            {
                                fontNode.Attributes["xd:binding"].Value = formText.TextContent;
                            }
                        }
                        else
                        {
                            fontNode.Attributes["xd:binding"].Value = formText.TextContent;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Translates the given node from the Infopath view if it is present in the buttonAndExpressionBox dictionary
        /// </summary>
        private static void TranslateButtonNode(Dictionary<string, FormText> buttonAndExpressionBoxDict, XmlNode node, string formViewName)
        {
            string key = string.Empty;

            XmlAttribute ctrlIdAttribute = ctrlIdAttribute = node.Attributes["xd:CtrlId"];
            if (ctrlIdAttribute != null)
            {
                key = formViewName + ctrlIdAttribute.Value;

                if (buttonAndExpressionBoxDict.ContainsKey(key))
                {
                    FormText formText = buttonAndExpressionBoxDict[key];
                    node.Attributes["value"].Value = formText.TextContent;
                }
            }
        }

        /// <summary>
        /// Replaces validation and actionbutton texts in manifest if a translation is present in the formTextsInTargetLangauge list.
        /// </summary>
        /// <param name="formTextsInTargetLanguage">The complete set of translated texts for this Infopath form.</param>
        /// <param name="manifestXsf">Manifest for Infopath file.</param>
        /// <param name="namespaceManager">Namespace manager</param>
        private static void LocalizeValidationAndActionButtonInManifest(List<FormText> formTextsInTargetLanguage, XmlDocument manifestXsf, XmlNamespaceManager namespaceManager)
        {
            foreach (FormText text in formTextsInTargetLanguage)
            {
                switch (text.TextType)
                {
                    case TextType.ValidationText1:
                        XmlNode validation1node = manifestXsf.SelectSingleNode(text.TextCode, namespaceManager);
                        if (validation1node != null)
                        {
                            validation1node.InnerText = text.TextContent;
                        }

                        break;

                    case TextType.ValidationText2:
                        XmlNode validation2node = manifestXsf.SelectSingleNode(text.TextCode, namespaceManager);
                        if (validation2node != null)
                        {
                            validation2node.Attributes["shortMessage"].Value = text.TextContent;
                        }

                        break;
                    case TextType.HintText:
                        if (text.Page.Contains(MANIFEST_XSF))
                        {
                            XmlNode actionButtonnode = manifestXsf.SelectSingleNode(text.TextCode, namespaceManager);
                            if (actionButtonnode != null)
                            {
                                actionButtonnode.Attributes["caption"].Value = text.TextContent;
                            }
                        }

                        break;
                }
            }
        }

        ///// <summary>
        ///// Replaces the resource TULFormResourceTextFileName
        ///// </summary>
        ///// <param name="destinationUrl">Destination url</param>
        ///// <param name="resourceTexts">Translated resource texts for this Infopath form.</param>
        //private static void LocalizeValidationTextInResourceTextFile(string destinationUrl, List<FormText> resourceTexts)
        //{
        //    // Substitute all validation 3 and 4 texts in extra resource text file
        //    XmlDocument resourceTextFile = new XmlDocument();
        //    if (File.Exists(destinationUrl + "/" + AltinnConfiguration.TULFormResourceTextFileName))
        //        resourceTextFile.Load(destinationUrl + "/" + AltinnConfiguration.TULFormResourceTextFileName);
        //    else
        //        resourceTextFile = null;

        //    if (resourceTextFile != null)
        //    {
        //        foreach (FormText text in resourceTexts)
        //        {
        //            string textCode = text.TextCode.Substring(0, text.TextCode.IndexOf('$'));

        //            XmlNode node = resourceTextFile.SelectSingleNode("/ResourceTexts/ResourceText[@name='" + textCode + "']");
        //            if (node != null)
        //            {
        //                replaceTextForNodeInResourceFile(text, node);
        //            }
        //        }
        //        resourceTextFile.Save(destinationUrl + AltinnConfiguration.TULFormResourceTextFileName);
        //    }
        //}

        /// <summary>
        /// Replaces inner text of node with content of text if postfix is A or B.
        /// </summary>
        private static void ReplaceTextForNodeInResourceFile(FormText text, XmlNode node)
        {
            string postfix = text.TextCode.Substring(text.TextCode.IndexOf('$') + 1, 1);

            if (postfix == "A")
            {
                XmlNode message = node.SelectSingleNode("Message");
                if (message != null && text.TextContent != string.Empty)
                {
                    message.InnerText = text.TextContent;
                }
            }
            else if (postfix == "B")
            {
                XmlNode message = node.SelectSingleNode("MessageDetail");
                if (message != null && text.TextContent != string.Empty)
                {
                    message.InnerText = text.TextContent;
                }
            }
        }

        /// <summary>
        /// Returns all FormTexts from the input list of type ResourceText1 and ResourceText2
        /// </summary>
        /// <param name="formTextsInTargetLanguage">The complete set of translated texts for this Infopath form.</param>
        /// <returns>All FormTexts from the input list of type ResourceText1 and ResourceText2.</returns>
        private static List<FormText> GetResourceTextsFromFormTextList(List<FormText> formTextsInTargetLanguage)
        {
            List<FormText> resourceTexts = new List<FormText>();

            foreach (FormText text in formTextsInTargetLanguage)
            {
                switch (text.TextType)
                {
                    case TextType.ResourceText1:
                    case TextType.ResourceText2:
                        resourceTexts.Add(text);
                        break;
                }
            }

            return resourceTexts;
        }

        /// <summary>
        /// Replaces the default view of the Infopath form with focusPageXSL.
        /// </summary>
        /// <param name="focusPageXSL">Name of the default view.</param>
        /// <param name="parser">Initialized Infopath parser</param>
        /// <param name="namespaceManager">Namespace manager</param>
        private static void SetCorrectFocusPageInManifest(string focusPageXSL, InfoPathParser parser, XmlNamespaceManager namespaceManager)
        {
            if (string.IsNullOrEmpty(focusPageXSL))
            {
                return;
            }

            XmlDocument manifestXsf = parser.GetXmlDocument(MANIFEST_XSF);

            foreach (FormView view in parser.GetViews())
            {
                // Ensuring that invalid names are omitted
                if (view.TransformationFile == focusPageXSL)
                {
                    XmlNode viewsNode = manifestXsf.SelectSingleNode("//xsf:views", namespaceManager);
                    viewsNode.Attributes["default"].Value = view.Name; // Set focus 
                    break;
                }
            }
        }

        /// <summary>
        /// Replaces all FormLocale values in the manifest with the correct CultureInfo name.
        /// </summary>
        /// <param name="requiredLanguage">The language id.</param>
        /// <param name="manifestXsf">Manifest</param>
        private static void LocalizeFormLocaleReferencesInManifest(LanguageType requiredLanguage, XmlDocument manifestXsf)
        {
            XmlNamespaceManager namespaceManager2 = new XmlNamespaceManager(manifestXsf.NameTable);
            namespaceManager2.AddNamespace("xsf2", " http://schemas.microsoft.com/office/infopath/2006/solutionDefinition/extensions");
            XmlNodeList formLoacleNodes = manifestXsf.SelectNodes("//@formLocale", namespaceManager2);
            foreach (XmlNode node in formLoacleNodes)
            {
                CultureInfo cultureInfo = new CultureInfo((int)requiredLanguage);
                node.Value = cultureInfo.Name;
            }
        }

        /// <summary>
        /// Replaces the value of the lang property for all view-references in the manifest.
        /// </summary>
        /// <param name="requriredLanguage">The language id to be inserted.</param>
        /// <param name="manifestXsf">Manifest</param>
        /// <param name="namespaceManager">Namespace manager</param>
        private static void LocalizeViewReferencesInManifest(LanguageType requriredLanguage, XmlDocument manifestXsf, XmlNamespaceManager namespaceManager)
        {
            XmlNodeList languageNodes = manifestXsf.SelectNodes("//xsf:property [@name='lang' and @type='string']", namespaceManager);
            if (languageNodes != null)
            {
                foreach (XmlNode language in languageNodes)
                {
                    language.Attributes["value"].Value = ((int)requriredLanguage).ToString();
                }
            }
        }

        /// <summary>
        /// Returns the file referenced by the filename as byte array.
        /// </summary>
        /// <param name="fileName">Full path for file</param>
        /// <returns>File as byte array</returns>
        private byte[] GetFileAsByteArray(string fileName)
        {
            byte[] localizedForm = null;

            using (FileStream xsnStream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                MemoryStream stream = new MemoryStream();
                CopyStream(xsnStream, stream);
                localizedForm = stream.ToArray();
            }

            return localizedForm;
        }

        /// <summary>
        /// Replaces the language id in the codelist xml files referenced by the manifest.
        /// </summary>
        /// <param name="requriredLanguage">The language id to be inserted.</param>
        /// <param name="codeListPath">Temporary path for uncompressed Infopath file. </param>
        /// <param name="parser">Initialized InfopathParser</param>
        /// <param name="namespaceManager">Namespace manager</param>
        private static void LocalizeCodelist(LanguageType requriredLanguage, string codeListPath, InfoPathParser parser, XmlNamespaceManager namespaceManager)
        {
            List<FormView> codeListViews = parser.GetCodeListXML();
            foreach (FormView codeList in codeListViews)
            {
                if (codeList.TransformationFile.Contains(GETCODELIST) || codeList.TransformationFile.Contains(GETFILTEREDCODELIST))
                {
                    XmlDocument xmlCodeList = parser.GetXmlDocument(codeList.TransformationFile);
                    if (xmlCodeList == null)
                    {
                        throw new InfoPathParsingFailedException(FAILED_DURING_GETVIEWS);
                    }

                    XmlNamespaceManager namespaceManagerCodeList = new XmlNamespaceManager(xmlCodeList.NameTable);
                    namespaceManager.AddNamespace("dfs", "http://schemas.microsoft.com/office/infopath/2003/dataFormSolution");
                    XmlNamespaceManager namespaceManagerCodeList2 = new XmlNamespaceManager(xmlCodeList.NameTable);
                    namespaceManagerCodeList2.AddNamespace("tns", "http://www.altinn.no/services/ServiceEngine/ServiceMetaData/2009/10");

                    XmlNodeList codeListList = null;
                    if (xmlCodeList.InnerXml.Contains(GETCODELIST))
                    {
                        codeListList = xmlCodeList.SelectNodes("//tns:GetCodeList/tns:languageID", namespaceManagerCodeList2);
                    }
                    else if (xmlCodeList.InnerXml.Contains(GETFILTEREDCODELIST))
                    {
                        codeListList = xmlCodeList.SelectNodes("//tns:GetFilteredCodeList/tns:languageID", namespaceManagerCodeList2);
                    }

                    if (codeListList != null)
                    {
                        foreach (XmlNode singleCodeList in codeListList)
                        {
                            singleCodeList.InnerText = ((int)requriredLanguage).ToString();
                        }
                    }

                    xmlCodeList.PreserveWhitespace = true;
                    xmlCodeList.Save(codeListPath + codeList.TransformationFile);
                }
            }
        }

        /// <summary>
        /// Add conditional formatting
        /// </summary>
        /// <param name="viewXsl">The xsl for the view</param>
        /// <param name="fieldPermissionList">The list of FieldPermissions</param>
        /// <returns></returns>
        public string AddConditionalFormatting(string viewXsl, FieldPermissionList fieldPermissionList)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(viewXsl);

            XmlNamespaceManager nsManager = new XmlNamespaceManager(xDoc.NameTable);
            nsManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            nsManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            nsManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");

            foreach (FieldPermission permission in fieldPermissionList)
            {
                string expval = string.Empty;
                if (permission.XPath != null && permission.XPath.Contains("/"))
                {
                    int startindex = permission.XPath.IndexOf("/", 1) + 1;
                    int length = permission.XPath.Length - startindex;
                    expval = permission.XPath.Substring(startindex, length);
                }

                XmlNode node = xDoc.DocumentElement.SelectSingleNode("//*[@xd:binding='" + expval + "']", nsManager);
                if (node == null)
                {
                    Console.WriteLine("Node is null for " + permission.XPath);
                }

                if (node != null)
                {
                    string nodeClassName = node.Attributes["class"].Value;

                    if (nodeClassName.Contains(ControlType.XdTextBox.ToString()))
                    {
                        XmlNode xslSVOnode = xDoc.DocumentElement.SelectSingleNode("//span[@xd:binding='" + expval + "']/xsl:value-of [@select= '" + expval + "']", nsManager);

                        if (xslSVOnode != null)
                        {
                            node.RemoveChild(xslSVOnode);
                        }

                        if (node != null)
                        {
                            UpdateNode(xDoc, node, permission.XPath, expval);
                        }

                    }
                    else if (nodeClassName.Contains(ControlType.XdBehavior_Boolean.ToString()))
                    {
                        XmlNode xslvalueatt = xDoc.DocumentElement.SelectSingleNode("//input[@xd:binding='" + expval + "']/xsl:attribute [@name='xd:value']", nsManager);
                        XmlNode xslif = xDoc.DocumentElement.SelectSingleNode("//input[@xd:binding='" + expval + "']/xsl:if", nsManager);
                        if (xslvalueatt != null)
                        {
                            node.RemoveChild(xslvalueatt);
                        }

                        if (xslif != null)
                        {
                            node.RemoveChild(xslif);
                        }

                        if (node != null)
                        {
                            UpdateNodeCheckBox(xDoc, node, permission.XPath, expval);
                        }

                    }
                    else if (nodeClassName.Contains(ControlType.XdRichTextBox.ToString()))
                    {
                        string coexpval = expval + "/node()";
                        XmlNode xslCOnode = xDoc.DocumentElement.SelectSingleNode("//span[@xd:binding='" + expval + "']/xsl:copy-of [@select= '" + coexpval + "']", nsManager);
                        if (xslCOnode != null)
                        {
                            node.RemoveChild(xslCOnode);
                        }

                        if (node != null)
                        {
                            UpdateNodeRichText(xDoc, node, permission.XPath, expval);
                        }
                    }
                    else if (nodeClassName.Contains(ControlType.XdComboBox.ToString()))
                    {
                        if (node != null)
                        {
                            UpdateComboAndListBox(xDoc, node, permission.XPath, expval);
                        }
                    }
                    else if (nodeClassName.Contains(ControlType.XdListBox.ToString()))
                    {
                        if (node != null)
                        {
                            UpdateComboAndListBox(xDoc, node, permission.XPath, expval);
                        }
                    }
                }
            }

            string xslString = xDoc.InnerXml.Replace("amp;", string.Empty);
            return xslString;
        }

        private static XmlNode ReturnIfNode(XmlDocument xDoc, XmlNode node, string nodeName, string prefix, string nsUrl)
        {
            XmlNamespaceManager nsManager = new XmlNamespaceManager(xDoc.NameTable);
            nsManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            nsManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            nsManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");
            XmlNodeList nodeList = node.SelectNodes("xsl:if[@test]", nsManager);
            XmlNode ifNode = null;

            foreach (XmlNode ifNodeTemp in nodeList)
            {
                if (ifNodeTemp.Attributes["test"].Value == "function-available('xdXDocument:GetDOM')")
                {
                    ifNode = ifNodeTemp;
                }
            }

            if (ifNode == null)
            {
                Dictionary<string, string> ifAttributes = new Dictionary<string, string>();
                ifAttributes.Add("test", "function-available('xdXDocument:GetDOM')");
                XmlNode xslIf = CreateXslNodeElement(xDoc, ifAttributes, "if", "xsl", "http://www.w3.org/1999/XSL/Transform");

                XmlNodeList childNodes = node.ChildNodes;
                int count = childNodes.Count;
                for (int i = 0; i <= count - 1; i++)
                {
                    XmlNode newNode = childNodes[0].CloneNode(true);
                    xslIf.AppendChild(newNode);
                    node.RemoveChild(childNodes[0]);
                }

                return xslIf;
            }
            else
            {
                return ifNode;
            }
        }

        /// <summary>
        /// Creates/Gets the Choose Node for Conditional formatting. If the Choose node already exists it will append the "when" node to the existing "choose" Node
        /// </summary>
        private static XmlNode ReturnStandAloneChooseNodeForEnableDisable(XmlDocument xDoc, XmlNode node, string xPath)
        {
            string expval = string.Empty;
            if (xPath.Contains("/"))
            {
                int startindex = xPath.IndexOf("/", 1) + 1;
                int length = xPath.Length - startindex;
                expval = xPath.Substring(startindex, length);
            }

            XmlNamespaceManager nsManager = new XmlNamespaceManager(xDoc.NameTable);
            nsManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            nsManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            nsManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");
            string prefix = GetNameSpacePrefix(xDoc);
            XmlNodeList nodeList = node.SelectNodes("xsl:if[@test]", nsManager);
            XmlNode ifNode = null;
            foreach (XmlNode ifNodeTemp in nodeList)
            {
                if (ifNodeTemp.Attributes["test"].Value == "function-available('xdXDocument:GetDOM')")
                {
                    ifNode = ifNodeTemp;
                }
            }

            if (ifNode != null)
            {
                if (ifNode.SelectSingleNode("//choose", nsManager) != null)
                {
                    XmlNode chooseNode = ifNode.SelectSingleNode("//choose", nsManager);
                    if (chooseNode.SelectSingleNode("//when[@test ='" + expval + "']") == null)
                    {
                        // create hide when
                        chooseNode.AppendChild(CreateWhenNode(xDoc, xPath, true, prefix));

                        // Create when for ReadOnly
                        XmlNode whenNode = CreateWhenNode(xDoc, xPath, false, prefix);
                        Dictionary<string, string> xslAttributeAttributes = new Dictionary<string, string>();
                        xslAttributeAttributes.Add("name", "contentEditable");
                        XmlNode xslAttribute = CreateXslNodeElement(xDoc, xslAttributeAttributes, "attribute", "xsl", "http://www.w3.org/1999/XSL/Transform");
                        xslAttribute.InnerXml = "false";
                        whenNode.AppendChild(xslAttribute);
                        chooseNode.AppendChild(whenNode);
                    }

                    return chooseNode;
                }
                else
                {
                    // Create Choose Node
                    XmlNode xslStandAloneChoose = CreateXslNodeElement(xDoc, new Dictionary<string, string>(), "choose", "xsl", "http://www.w3.org/1999/XSL/Transform");
                    if (xslStandAloneChoose.SelectSingleNode("//when[@test ='" + expval + "']") == null)
                    {
                        // create hide when
                        xslStandAloneChoose.AppendChild(CreateWhenNode(xDoc, xPath, true, prefix));

                        // Create when for ReadOnly
                        XmlNode whenNode = CreateWhenNode(xDoc, xPath, false, prefix);
                        Dictionary<string, string> xslAttributeAttributes = new Dictionary<string, string>();
                        xslAttributeAttributes.Add("name", "contentEditable");
                        XmlNode xslAttribute = CreateXslNodeElement(xDoc, xslAttributeAttributes, "attribute", "xsl", "http://www.w3.org/1999/XSL/Transform");
                        xslAttribute.InnerXml = "false";
                        whenNode.AppendChild(xslAttribute);
                        xslStandAloneChoose.AppendChild(whenNode);
                    }

                    return xslStandAloneChoose;
                }
            }
            
            return null;
        }

        private static string GetNameSpacePrefix(XmlDocument xDoc)
        {
            //string prefix = xDoc.DocumentElement.GetPrefixOfNamespace(AltinnConfiguration.PrefixValueForConditionalFormattingInFieldLevelOverrides);
            //if (string.IsNullOrEmpty(prefix))
            //{
            //    prefix = "tns";
            //}
            return "tns";
        }

        private static XmlNode ReturnStyleAttributeNode(XmlDocument xDoc, XmlNode node, string xPath)
        {
            string expval = string.Empty;
            if (xPath.Contains("/"))
            {
                int startindex = xPath.IndexOf("/", 1) + 1;
                int length = xPath.Length - startindex;
                expval = xPath.Substring(startindex, length);
            }

            XmlNamespaceManager nsManager = new XmlNamespaceManager(xDoc.NameTable);
            nsManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            nsManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            nsManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");
            string prefix = GetNameSpacePrefix(xDoc);
            XmlNodeList nodeList = node.SelectNodes("xsl:if[@test]", nsManager);
            XmlNode ifNode = null;
            foreach (XmlNode ifNodeTemp in nodeList)
            {
                if (ifNodeTemp.Attributes["test"].Value == "function-available('xdXDocument:GetDOM')")
                {
                    ifNode = ifNodeTemp;
                }
            }

            if (ifNode != null)
            {
                XmlNode xslStyleChoose = CreateXslNodeElement(xDoc, new Dictionary<string, string>(), "choose", "xsl", "http://www.w3.org/1999/XSL/Transform");
                XmlNode hideWhenNode = CreateWhenNode(xDoc, xPath, true, prefix);
                hideWhenNode.InnerXml = "DISPLAY:NONE";
                XmlNode readOnlyWhenNode = CreateWhenNode(xDoc, xPath, false, prefix);
                if (ifNode.SelectSingleNode("//attribute[@name = 'style']", nsManager) != null)
                {
                    XmlNode styleAttributeNode = ifNode.SelectSingleNode("//attribute[@name = 'style']", nsManager);
                    string innerXmlofChoose = string.Empty;
                    if (styleAttributeNode.InnerXml.Contains("xsl:choose"))
                    {
                        int start = styleAttributeNode.InnerXml.IndexOf("<xsl:when");
                        int end = styleAttributeNode.InnerXml.IndexOf("</xsl:choose>");
                        innerXmlofChoose = styleAttributeNode.InnerXml.Substring(start, end - start);
                        xslStyleChoose.InnerXml = innerXmlofChoose;
                        styleAttributeNode.InnerXml = styleAttributeNode.InnerXml.Replace(innerXmlofChoose, string.Empty);
                    }

                    if (xslStyleChoose.SelectSingleNode("//when[@test ='" + expval + "']") == null)
                    {
                        xslStyleChoose.AppendChild(hideWhenNode);
                        xslStyleChoose.AppendChild(readOnlyWhenNode);
                    }

                    styleAttributeNode.InnerXml = styleAttributeNode.InnerXml.Insert(styleAttributeNode.InnerXml.LastIndexOf(";"), xslStyleChoose.OuterXml);
                    return styleAttributeNode;
                }
                else
                {
                    string styleAttributeValue = string.Empty;
                    Dictionary<string, string> stylexslAttributeAttributes = new Dictionary<string, string>();
                    stylexslAttributeAttributes.Add("name", "style");
                    XmlNode styleXslAttribute = CreateXslNodeElement(xDoc, stylexslAttributeAttributes, "attribute", "xsl", "http://www.w3.org/1999/XSL/Transform");
                    xslStyleChoose.AppendChild(hideWhenNode);
                    xslStyleChoose.AppendChild(readOnlyWhenNode);
                    foreach (XmlAttribute xatt in node.Attributes)
                    {
                        if (xatt.Name == "style")
                        {
                            styleAttributeValue = xatt.Value;
                        }
                    }

                    styleXslAttribute.InnerXml = styleAttributeValue + ";";
                    styleXslAttribute.InnerXml = styleXslAttribute.InnerXml.Insert(styleXslAttribute.InnerXml.Length, xslStyleChoose.OuterXml);
                    return styleXslAttribute;
                }
            }

            return null;
        }

        private static XmlNode ReturnCheckboxChoose(XmlDocument xDoc, XmlNode node, string xPath)
        {
            string expval = string.Empty;
            if (xPath.Contains("/"))
            {
                int startindex = xPath.IndexOf("/", 1) + 1;
                int length = xPath.Length - startindex;
                expval = xPath.Substring(startindex, length);
            }

            XmlNamespaceManager nsManager = new XmlNamespaceManager(xDoc.NameTable);
            nsManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            nsManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            nsManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");
            string prefix = GetNameSpacePrefix(xDoc);
            XmlNodeList nodeList = node.SelectNodes("xsl:if[@test]", nsManager);
            XmlNode ifNode = null;
            foreach (XmlNode ifNodeTemp in nodeList)
            {
                if (ifNodeTemp.Attributes["test"].Value == "function-available('xdXDocument:GetDOM')")
                {
                    ifNode = ifNodeTemp;
                }
            }

            if (ifNode != null)
            {
                if (ifNode.SelectSingleNode("//choose", nsManager) != null)
                {
                    XmlNode chooseNode = ifNode.SelectSingleNode("//choose", nsManager);
                    if (chooseNode.SelectSingleNode("//when[@test ='" + expval + "']") == null)
                    {
                        // create disable when
                        XmlNode diableWhenNode = CreateWhenNode(xDoc, xPath, false, prefix);
                        Dictionary<string, string> xslAttributeAttributes = new Dictionary<string, string>();
                        xslAttributeAttributes.Add("name", "disabled");
                        XmlNode xslAttribute = CreateXslNodeElement(xDoc, xslAttributeAttributes, "attribute", "xsl", "http://www.w3.org/1999/XSL/Transform");
                        xslAttribute.InnerText = "true";
                        diableWhenNode.AppendChild(xslAttribute);
                        chooseNode.AppendChild(diableWhenNode);
                    }

                    return chooseNode;
                }
                else
                {
                    // Create Choose Node
                    XmlNode chooseNode = CreateXslNodeElement(xDoc, new Dictionary<string, string>(), "choose", "xsl", "http://www.w3.org/1999/XSL/Transform");
                    if (chooseNode.SelectSingleNode("//when[@test ='" + expval + "']") == null)
                    {
                        XmlNode diableWhenNode = CreateWhenNode(xDoc, xPath, false, prefix);
                        Dictionary<string, string> xslAttributeAttributes = new Dictionary<string, string>();
                        xslAttributeAttributes.Add("name", "disabled");
                        XmlNode xslAttribute = CreateXslNodeElement(xDoc, xslAttributeAttributes, "attribute", "xsl", "http://www.w3.org/1999/XSL/Transform");
                        xslAttribute.InnerText = "true";
                        diableWhenNode.AppendChild(xslAttribute);
                        chooseNode.AppendChild(diableWhenNode);
                    }

                    return chooseNode;
                }
            }
                
            return null;
        }

        /// <summary>
        /// Returns Choose node for Combo and ListBox controls for Field Level Authorization
        /// </summary>
        private static XmlNode ReturnComboandListboxChoose(XmlDocument xDoc, XmlNode node, string xPath)
        {
            string expval = string.Empty;
            if (xPath.Contains("/"))
            {
                int startindex = xPath.IndexOf("/", 1) + 1;
                int length = xPath.Length - startindex;
                expval = xPath.Substring(startindex, length);
            }

            XmlNamespaceManager nsManager = new XmlNamespaceManager(xDoc.NameTable);
            nsManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            nsManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            nsManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");
            string prefix = GetNameSpacePrefix(xDoc);
            XmlNodeList nodeList = node.SelectNodes("xsl:if[@test]", nsManager);
            XmlNode ifNode = null;
            foreach (XmlNode ifNodeTemp in nodeList)
            {
                if (ifNodeTemp.Attributes["test"].Value == "function-available('xdXDocument:GetDOM')")
                {
                    ifNode = ifNodeTemp;
                }
            }

            if (ifNode != null)
            {
                if (ifNode.SelectSingleNode("//choose", nsManager) != null)
                {
                    XmlNode chooseNode = ifNode.SelectSingleNode("//choose", nsManager);
                    if (chooseNode.SelectSingleNode("//when[@test ='" + expval + "']") == null)
                    {
                        // create hide when
                        chooseNode.AppendChild(CreateWhenNode(xDoc, xPath, true, prefix));

                        // Create when for ReadOnly
                        XmlNode whenNode = CreateWhenNode(xDoc, xPath, false, prefix);
                        Dictionary<string, string> xslAttributeAttributes = new Dictionary<string, string>();
                        xslAttributeAttributes.Add("name", "disabled");
                        XmlNode xslAttribute = CreateXslNodeElement(xDoc, xslAttributeAttributes, "attribute", "xsl", "http://www.w3.org/1999/XSL/Transform");
                        xslAttribute.InnerXml = "true";
                        whenNode.AppendChild(xslAttribute);
                        chooseNode.AppendChild(whenNode);
                    }

                    return chooseNode;
                }
                else
                {
                    // Create Choose Node
                    XmlNode xslStandAloneChoose = CreateXslNodeElement(xDoc, new Dictionary<string, string>(), "choose", "xsl", "http://www.w3.org/1999/XSL/Transform");
                    if (xslStandAloneChoose.SelectSingleNode("//when[@test ='" + expval + "']") == null)
                    {
                        // create hide when
                        xslStandAloneChoose.AppendChild(CreateWhenNode(xDoc, xPath, true, prefix));

                        // Create when for ReadOnly
                        XmlNode whenNode = CreateWhenNode(xDoc, xPath, false, prefix);
                        Dictionary<string, string> xslAttributeAttributes = new Dictionary<string, string>();
                        xslAttributeAttributes.Add("name", "disabled");
                        XmlNode xslAttribute = CreateXslNodeElement(xDoc, xslAttributeAttributes, "attribute", "xsl", "http://www.w3.org/1999/XSL/Transform");
                        xslAttribute.InnerXml = "true";
                        whenNode.AppendChild(xslAttribute);
                        xslStandAloneChoose.AppendChild(whenNode);
                    }

                    return xslStandAloneChoose;
                }
            }
                
            return null;
        }

        /// <summary>
        /// Creates an XmlNode element
        /// </summary>
        /// <param name="xDoc">the XmlDocument in which to create the Node</param>
        /// <param name="attributes">Dictionary Key Value pair containing the list of attributes for the node</param>
        /// <param name="nodeName">Name of the node</param>
        /// <param name="prefix">NameSpace prefix for the node</param>
        /// <param name="nsUrl">NameSpace Url for the node</param>
        /// <returns></returns>
        public static XmlNode CreateXslNodeElement(XmlDocument xDoc, Dictionary<string, string> attributes, string nodeName, string prefix, string nsUrl)
        {
            XmlNode node = xDoc.CreateElement(prefix, nodeName, nsUrl);
            foreach (KeyValuePair<string, string> attribute in attributes)
            {
                XmlAttribute xmlAttribute = xDoc.CreateAttribute(attribute.Key);
                xmlAttribute.Value = attribute.Value;
                node.Attributes.Append(xmlAttribute);
            }

            return node;
        }

        private static XmlNode CreateWhenNode(XmlDocument xDoc, string xPath, bool hide, string prefix)
        {
            string expval = string.Empty;
            if (xPath.Contains("/"))
            {
                int startindex = xPath.IndexOf("/", 1) + 1;
                int length = xPath.Length - startindex;
                expval = xPath.Substring(startindex, length);
            }

            // create hide else create ReadOnly
            if (hide)
            {
                Dictionary<string, string> hideWhenAttributes = new Dictionary<string, string>();
                hideWhenAttributes.Add("test", "xdXDocument:GetDOM('" + _dataConnectionName + "')" + string.Format(_field, prefix, prefix, xPath) + "=3");
                XmlNode hidexslWhen = CreateXslNodeElement(xDoc, hideWhenAttributes, "when", "xsl", "http://www.w3.org/1999/XSL/Transform");
                return hidexslWhen;
            }
            else
            {
                Dictionary<string, string> readOnlyWhenAttributes = new Dictionary<string, string>();
                readOnlyWhenAttributes.Add("test", "xdXDocument:GetDOM('" + _dataConnectionName + "')" + string.Format(_field, prefix, prefix, xPath) + "=1");
                XmlNode xslWhen = CreateXslNodeElement(xDoc, readOnlyWhenAttributes, "when", "xsl", "http://www.w3.org/1999/XSL/Transform");
                return xslWhen;
            }
        }

        private static void UpdateNode(XmlDocument xDoc, XmlNode node, string xPath, string expval)
        {
            XmlNamespaceManager nsManager = new XmlNamespaceManager(xDoc.NameTable);
            nsManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            nsManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            nsManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");
            XmlNodeList nodeList = node.SelectNodes("xsl:if[@test]", nsManager);

            XmlNode ifNode = ReturnIfNode(xDoc, node, "if", "xsl", "http://www.w3.org/1999/XSL/Transform");

            node.AppendChild(ifNode);

            // Stand Alone Choose for Attribute Level ConditionalFormatting
            XmlNode standAloneChoose = ReturnStandAloneChooseNodeForEnableDisable(xDoc, node, xPath);
            if (standAloneChoose == null)
            {
                throw new Exception("Error At - StandAloneChoose");
            }

            if (ifNode.SelectSingleNode("//choose") == null)
            {
                ifNode.PrependChild(standAloneChoose);
            }

            // Choose node inside Attribute[@style]'s inner XML
            XmlNode styleXslAttribute = ReturnStyleAttributeNode(xDoc, node, xPath);
            if (styleXslAttribute == null)
            {
                throw new Exception("Error At -  styleXSLAttribute");
            }

            ifNode.PrependChild(styleXslAttribute);

            string nodeClassName = node.Attributes["class"].Value;
            if (!nodeClassName.Equals("xdTextBox xdBehavior_Formatting"))
            {
                Dictionary<string, string> valueofselectattributes = new Dictionary<string, string>();
                valueofselectattributes.Add("select", expval);
                XmlNode valueofselect = CreateXslNodeElement(xDoc, valueofselectattributes, "value-of", "xsl", "http://www.w3.org/1999/XSL/Transform");
                ifNode.AppendChild(valueofselect);
            }
        }

        private static void UpdateNodeCheckBox(XmlDocument xDoc, XmlNode node, string xPath, string expval)
        {
            XmlNamespaceManager nsManager = new XmlNamespaceManager(xDoc.NameTable);
            nsManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            nsManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            nsManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");
            XmlNodeList nodeList = node.SelectNodes("xsl:if[@test]", nsManager);

            XmlNode ifNode = ReturnIfNode(xDoc, node, "if", "xsl", "http://www.w3.org/1999/XSL/Transform");
            node.AppendChild(ifNode);

            XmlNode xslChoose = ReturnCheckboxChoose(xDoc, node, xPath);
            if (xslChoose == null)
            {
                throw new Exception(string.Empty);
            }

            if (ifNode.SelectSingleNode("//choose") == null)
            {
                ifNode.AppendChild(xslChoose);
            }

            Dictionary<string, string> attributenodeattributes = new Dictionary<string, string>();
            attributenodeattributes.Add("name", "xd:value");
            XmlNode attributenode = CreateXslNodeElement(xDoc, attributenodeattributes, "attribute", "xsl", "http://www.w3.org/1999/XSL/Transform");

            Dictionary<string, string> valueofselectattributes = new Dictionary<string, string>();
            valueofselectattributes.Add("select", expval);
            XmlNode valueofnode = CreateXslNodeElement(xDoc, valueofselectattributes, "value-of", "xsl", "http://www.w3.org/1999/XSL/Transform");
            attributenode.AppendChild(valueofnode);
            ifNode.AppendChild(attributenode);

            Dictionary<string, string> ifnodeattributes = new Dictionary<string, string>();
            ifnodeattributes.Add("test", expval + "='true'");
            XmlNode checkedIfNode = CreateXslNodeElement(xDoc, ifnodeattributes, "if", "xsl", "http://www.w3.org/1999/XSL/Transform");
            Dictionary<string, string> ifattributenodeattributes = new Dictionary<string, string>();
            ifattributenodeattributes.Add("name", "CHECKED");
            XmlNode attributeIfNode = CreateXslNodeElement(xDoc, ifattributenodeattributes, "attribute", "xsl", "http://www.w3.org/1999/XSL/Transform");
            attributeIfNode.InnerText = "CHECKED";
            checkedIfNode.AppendChild(attributeIfNode);
            ifNode.AppendChild(checkedIfNode);
            node.AppendChild(ifNode);
        }

        private static void UpdateNodeRichText(XmlDocument xDoc, XmlNode node, string xPath, string expval)
        {
            XmlNamespaceManager nsManager = new XmlNamespaceManager(xDoc.NameTable);
            nsManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            nsManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            nsManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");
            XmlNodeList nodeList = node.SelectNodes("xsl:if[@test]", nsManager);

            XmlNode ifNode = ReturnIfNode(xDoc, node, "if", "xsl", "http://www.w3.org/1999/XSL/Transform");
            node.AppendChild(ifNode);

            // Stand Alone Choose for Attribute Level ConditionalFormatting
            XmlNode standAloneChoose = ReturnStandAloneChooseNodeForEnableDisable(xDoc, node, xPath);
            if (standAloneChoose == null)
            {
                throw new Exception(string.Empty);
            }

            if (ifNode.SelectSingleNode("//choose") == null)
            {
                ifNode.AppendChild(standAloneChoose);
            }

            // Choose node inside Attribute[@style]'s inner XML
            XmlNode styleXslAttribute = ReturnStyleAttributeNode(xDoc, node, xPath);
            if (styleXslAttribute == null)
            {
                throw new Exception(string.Empty);
            }

            ifNode.AppendChild(styleXslAttribute);

            Dictionary<string, string> copyofselectattributes = new Dictionary<string, string>
            {
                { "select", expval }
            };
            XmlNode copyofselect = CreateXslNodeElement(xDoc, copyofselectattributes, "copy-of", "xsl", "http://www.w3.org/1999/XSL/Transform");
            ifNode.AppendChild(copyofselect);
        }

        private static void UpdateComboAndListBox(XmlDocument xDoc, XmlNode node, string xPath, string expval)
        {
            XmlNamespaceManager nsManager = new XmlNamespaceManager(xDoc.NameTable);
            nsManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            nsManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            nsManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");
            XmlNodeList nodeList = node.SelectNodes("xsl:if[@test]", nsManager);

            XmlNode ifNode = ReturnIfNode(xDoc, node, "if", "xsl", "http://www.w3.org/1999/XSL/Transform");
            node.AppendChild(ifNode);

            XmlNode standAloneChoose = ReturnComboandListboxChoose(xDoc, node, xPath);
            if (standAloneChoose == null)
            {
                throw new Exception(string.Empty);
            }

            if (ifNode.SelectSingleNode("//choose") == null)
            {
                ifNode.PrependChild(standAloneChoose);
            }

            XmlNode styleXslAttribute = ReturnStyleAttributeNode(xDoc, node, xPath);
            if (styleXslAttribute == null)
            {
                throw new Exception(string.Empty);
            }

            ifNode.PrependChild(styleXslAttribute);

            Dictionary<string, string> attributenodeattributes = new Dictionary<string, string>();
            attributenodeattributes.Add("name", "value");
            XmlNode attributenode = CreateXslNodeElement(xDoc, attributenodeattributes, "attribute", "xsl", "http://www.w3.org/1999/XSL/Transform");

            Dictionary<string, string> valueofselectattributes = new Dictionary<string, string>();
            valueofselectattributes.Add("select", expval);
            XmlNode valueofnode = CreateXslNodeElement(xDoc, valueofselectattributes, "value-of", "xsl", "http://www.w3.org/1999/XSL/Transform");
            attributenode.AppendChild(valueofnode);
            ifNode.AppendChild(attributenode);
        }

        private void CopyStream(Stream source, Stream target)
        {
            const int bufferSize = 0x1000;
            byte[] buffer = new byte[bufferSize];
            int bytesRead = 0;
            while ((bytesRead = source.Read(buffer, 0, bufferSize)) > 0)
            {
                target.Write(buffer, 0, bytesRead);
            }
        }

        private List<FormText> GetFormTexts(string pageName)
        {
            List<FormText> formTexts = new List<FormText>();

            if (pageName != null)
            {
                List<FormText> dropDownTexts = GetControlTextsFromPage(pageName, PageControlType.DropDown);
                List<FormText> listBoxTexts = GetControlTextsFromPage(pageName, PageControlType.ListBox);
                List<FormText> buttonTexts = GetControlTextsFromPage(pageName, PageControlType.Button);
                List<FormText> pictureButtonTexts = GetControlTextsFromPage(pageName, PageControlType.PictureButton);
                List<FormText> expressionBoxTexts = GetControlTextsFromPage(pageName, PageControlType.ExpressionBox);
                List<FormText> tableHeaderTexts = GetControlTextsFromPage(pageName, PageControlType.TableHeader);
                List<FormText> hintTexts = GetControlTextsFromPage(pageName, PageControlType.Hint);
                List<FormText> hyperlinkTexts = GetControlTextsFromPage(pageName, PageControlType.Hyperlink);
                List<FormText> validation1Texts = GetControlTextsFromPage(pageName, PageControlType.ValidationText1);
                List<FormText> validation2Texts = GetControlTextsFromPage(pageName, PageControlType.ValidationText2);
                List<FormText> actionButtonTexts = GetControlTextsFromPage(pageName, PageControlType.ActionButton);
                List<FormText> copyExpressionBoxTexts = GetControlTextsFromPage(pageName, PageControlType.ExpressionBoxCopy);

                formTexts.AddRange(dropDownTexts);
                formTexts.AddRange(listBoxTexts);
                formTexts.AddRange(buttonTexts);
                formTexts.AddRange(pictureButtonTexts);
                formTexts.AddRange(expressionBoxTexts);
                formTexts.AddRange(copyExpressionBoxTexts);
                formTexts.AddRange(tableHeaderTexts);
                formTexts.AddRange(hintTexts);
                formTexts.AddRange(hyperlinkTexts);
                formTexts.AddRange(validation1Texts);
                formTexts.AddRange(validation2Texts);
                formTexts.AddRange(actionButtonTexts);
            }
            else
            {
                List<FormText> validation3Texts = GetControlTextsFromResourceFile(TULFormResourceTextFileName, PageControlType.ResourceText1);
                List<FormText> validation4Texts = GetControlTextsFromResourceFile(TULFormResourceTextFileName, PageControlType.ResourceText2);

                formTexts.AddRange(validation3Texts);
                formTexts.AddRange(validation4Texts);
            }

            return formTexts;
        }

        private List<FormText> GetControlTextsFromResourceFile(string fileName, PageControlType controlType)
        {
            List<FormText> resourceTextList = new List<FormText>();
            XmlDocument resourceFile;
            try
            {
                resourceFile = GetXmlDocument(fileName);
                if (resourceFile == null)
                {
                    throw new InfoPathParsingFailedException(INFOPATH_FORMAT_ERROR);
                }
            }
            catch (Exception)
            {
                return resourceTextList;
            }

            XmlNodeList resourceTexts = resourceFile.SelectNodes("/ResourceTexts/ResourceText");
            foreach (XmlNode resourceText in resourceTexts)
            {
                if (!string.IsNullOrEmpty(resourceText.Attributes["name"].Value))
                {
                    if (controlType == PageControlType.ResourceText1)
                    {
                        XmlNode xmlMessage = resourceText.SelectSingleNode("Message");
                        if (xmlMessage != null)
                        {
                            string message = xmlMessage.InnerText;
                            if (!string.IsNullOrEmpty(message))
                            {
                                resourceTextList.Add(new FormText { Page = fileName, TextCode = resourceText.Attributes["name"].Value + "$A", TextContent = message, TextType = TextType.ResourceText1 });
                            }
                        }
                    }
                    else if (controlType == PageControlType.ResourceText2)
                    {
                        XmlNode xmlMessage = resourceText.SelectSingleNode("MessageDetail");
                        if (xmlMessage != null)
                        {
                            string messageDetail = xmlMessage.InnerText;
                            if (!string.IsNullOrEmpty(messageDetail))
                            {
                                resourceTextList.Add(new FormText { Page = fileName, TextCode = resourceText.Attributes["name"].Value + "$B", TextContent = messageDetail, TextType = TextType.ResourceText2 });
                            }
                        }
                    }
                }
            }

            return resourceTextList;
        }

        private List<FormText> GetControlTextsFromPage(string pageName, PageControlType controlType)
        {
            XmlDocument xml = GetXmlDocument(pageName);
            if (xml == null)
            {
                throw new InfoPathParsingFailedException(FAILED_DURING_GETVIEWS);
            }

            List<FormText> formTexts = new List<FormText>();

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
            namespaceManager.AddNamespace("xsf", "http://schemas.microsoft.com/office/infopath/2003/solutionDefinition");
            namespaceManager.AddNamespace("xd", "http://schemas.microsoft.com/office/infopath/2003");
            namespaceManager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");

            // Get validation text 1 from manifest xsf
            if (controlType == PageControlType.ValidationText1)
            {
                XmlNodeList customValidationNodes = xml.SelectNodes("//xsf:customValidation/xsf:errorCondition [@match]", namespaceManager);
                foreach (XmlNode validationNode in customValidationNodes)
                {
                    string match = validationNode.Attributes["match"].Value;
                    string expressionContext = validationNode.Attributes["expressionContext"].Value;
                    string expression = validationNode.Attributes["expression"].Value;
                    string textFieldXPath = "//xsf:customValidation/xsf:errorCondition [@match='" + match + "'"
                                            + " and @expressionContext='" + expressionContext + "'"
                                            + " and @expression='" + expression + "']/xsf:errorMessage[@shortMessage]";

                    FormText validationText = new FormText
                    {
                        Page = pageName,
                        TextType = TextType.ValidationText1,
                        TextContent = validationNode.SelectSingleNode("./xsf:errorMessage", namespaceManager).InnerText,
                        TextCode = textFieldXPath
                    };
                    if (!string.IsNullOrEmpty(validationText.TextContent))
                    {
                        formTexts.Add(validationText);
                    }
                }
            }

            // Get validation text 2 from manifest xsf
            else if (controlType == PageControlType.ValidationText2)
            {
                XmlNodeList customValidationNodes = xml.SelectNodes("//xsf:customValidation/xsf:errorCondition", namespaceManager);
                foreach (XmlNode validationNode in customValidationNodes)
                {
                    string match = validationNode.Attributes["match"].Value;
                    string expressionContext = validationNode.Attributes["expressionContext"].Value;
                    string expression = validationNode.Attributes["expression"].Value;
                    string textFieldXPath = "//xsf:customValidation/xsf:errorCondition [@match='" + match + "'"
                                            + " and @expressionContext='" + expressionContext + "'"
                                            + " and @expression='" + expression + "']/xsf:errorMessage";

                    FormText validationText = new FormText();
                    validationText.Page = pageName;
                    validationText.TextType = TextType.ValidationText2;
                    validationText.TextContent = validationNode.SelectSingleNode("./xsf:errorMessage", namespaceManager).Attributes["shortMessage"].Value;
                    validationText.TextCode = textFieldXPath;
                    if (!string.IsNullOrEmpty(validationText.TextContent))
                    {
                        formTexts.Add(validationText);
                    }
                }
            }
            else if (controlType == PageControlType.ActionButton)
            {
                XmlNodeList customActionButtonNodes = xml.SelectNodes("//xsf:menuArea /xsf:button  [@action]", namespaceManager);
                foreach (XmlNode actionButtonNode in customActionButtonNodes)
                {
                    string viewName = actionButtonNode.ParentNode.ParentNode.Attributes["name"].Value;
                    string action = actionButtonNode.Attributes["action"].Value;
                    string xmlToEdit = actionButtonNode.Attributes["xmlToEdit"].Value;
                    string textFieldXPath = "//xsf:view[@name='" + viewName + "']/xsf:menuArea /xsf:button [@action='" + action + "']" + "[@xmlToEdit='" + xmlToEdit + "']";

                    FormText actionButtonText = new FormText();
                    actionButtonText.Page = pageName + "_" + viewName;
                    actionButtonText.TextType = TextType.HintText;
                    actionButtonText.TextContent = actionButtonNode.Attributes["caption"].Value;
                    actionButtonText.TextCode = textFieldXPath;
                    if (!string.IsNullOrEmpty(actionButtonText.TextContent))
                    {
                        formTexts.Add(actionButtonText);
                    }
                }
            }
            else if (controlType == PageControlType.PictureButton)
            {
                string contolXPath = GetControlXPath(controlType);
                XmlNodeList pictureButtonNodes = xml.SelectNodes(contolXPath, namespaceManager);
                foreach (XmlNode pictureButtonNode in pictureButtonNodes)
                {
                    string ctrlID = pictureButtonNode.InnerText;
                    string valueGroupXPath = GetValueXPath(controlType, ctrlID);
                    XmlNodeList valueGroupNodes = xml.SelectNodes(valueGroupXPath, namespaceManager);

                    if (valueGroupNodes.Count == 0)
                    {
                        valueGroupXPath = valueGroupXPath.Replace("/xsl", "//xsl");
                        valueGroupNodes = xml.SelectNodes(valueGroupXPath, namespaceManager);
                        if (valueGroupNodes.Count == 0)
                        {
                            valueGroupXPath = valueGroupXPath.Replace("//xsl", "/xsl");
                        }
                    }

                    foreach (XmlNode valueGroupNode in valueGroupNodes)
                    {
                        string textFieldXPath = string.Empty;
                        string textValue = string.Empty;
                        textFieldXPath = valueGroupXPath;
                        XmlAttributeCollection valueGroupNodeAttributeCollection = valueGroupNode.Attributes;
                        if (valueGroupNodeAttributeCollection.Count > 0)
                        {
                            textValue = valueGroupNodeAttributeCollection.GetNamedItem("xd:xctname").Value;
                        }

                        if (!string.IsNullOrEmpty(textValue))
                        {
                            FormText formText = new FormText();
                            formText.Page = pageName;
                            formText.TextType = TextType.PictureButton;
                            formText.TextContent = textValue;
                            formText.TextCode = textFieldXPath;
                            formTexts.Add(formText);
                        }
                    }
                }
            }
            else
            {
                string contolXPath = GetControlXPath(controlType);
                XmlNodeList controlNodes = xml.SelectNodes(contolXPath, namespaceManager);
                foreach (XmlNode controlNode in controlNodes)
                {
                    string ctrlID = controlNode.InnerText;
                    string valueGroupXPath = GetValueXPath(controlType, ctrlID);
                    XmlNodeList valueGroupNodes = xml.SelectNodes(valueGroupXPath, namespaceManager);

                    // Start fix for FR:
                    if (valueGroupNodes.Count == 0)
                    {
                        valueGroupXPath = valueGroupXPath.Replace("/xsl", "//xsl");
                        valueGroupNodes = xml.SelectNodes(valueGroupXPath, namespaceManager);
                        if (valueGroupNodes.Count == 0)
                        {
                            valueGroupXPath = valueGroupXPath.Replace("//xsl", "/xsl");
                        }
                    }

                    foreach (XmlNode valueGroupNode in valueGroupNodes)
                    {
                        string textFieldXPath = string.Empty;

                        switch (controlType)
                        {
                            case PageControlType.DropDown:
                            case PageControlType.ListBox:
                                XmlNodeList valueNodes = valueGroupNode.SelectNodes("./option [@value]", namespaceManager);
                                foreach (XmlNode valueNode in valueNodes)
                                {
                                    string value = valueNode.Attributes["value"].Value;
                                    textFieldXPath = valueGroupXPath + "/option [@value='" + value + "']";

                                    FormText formText = new FormText();
                                    formText.Page = pageName;
                                    formText.TextType = TextType.DropdownListBox;
                                    formText.TextContent = valueNode.InnerXml.Substring(valueNode.InnerXml.LastIndexOf('>') + 1);
                                    formText.TextCode = textFieldXPath;
                                    formTexts.Add(formText);
                                }

                                break;
                            case PageControlType.Button:
                                textFieldXPath = valueGroupXPath;
                                XmlNode node = valueGroupNode.Attributes["value"];
                                if (node != null)
                                {
                                    string textValue = valueGroupNode.Attributes["value"].Value;
                                    if (!string.IsNullOrEmpty(textValue))
                                    {
                                        FormText formText = new FormText();
                                        formText.Page = pageName;
                                        formText.TextType = TextType.Button;
                                        formText.TextContent = textValue;
                                        formText.TextCode = textFieldXPath;
                                        formTexts.Add(formText);
                                    }
                                }

                                break;
                            case PageControlType.ExpressionBox:
                                textFieldXPath = valueGroupXPath;
                                string selectValue = valueGroupNode.Attributes["select"].Value;
                                if (selectValue.StartsWith("&quot;") || selectValue.StartsWith("\""))
                                {
                                    selectValue = Regex.Replace(selectValue, "^&quot;", string.Empty);        // remove &quot; at the beginning
                                    selectValue = Regex.Replace(selectValue, "&quot;$", string.Empty);        // remove trailing &quot;
                                    selectValue = Regex.Replace(selectValue, "'$", string.Empty);             // remove trailing '
                                    selectValue = Regex.Replace(selectValue, "^'", string.Empty);             // remove ' at the beginning
                                    selectValue = Regex.Replace(selectValue, @"\s&quot;", " '");    // replace starting &quot; if present within the sentence
                                    selectValue = Regex.Replace(selectValue, @"\&quot;", "'");      // replace ending" &quot; if present within the sentence         
                                    selectValue = Regex.Replace(selectValue, "\"", string.Empty);             // replace " with whitespace

                                    if (!string.IsNullOrEmpty(selectValue))
                                    {
                                        FormText formText = new FormText();
                                        formText.Page = pageName;
                                        formText.TextType = TextType.ExpressionBox;
                                        formText.TextContent = selectValue;
                                        formText.TextCode = textFieldXPath;
                                        formTexts.Add(formText);
                                    }
                                }

                                break;
                            case PageControlType.ExpressionBoxCopy:
                                textFieldXPath = valueGroupXPath;
                                XmlNode conditionalExpressionBox = valueGroupNode.SelectSingleNode("xsl:value-of", namespaceManager);
                                XPathNodeIterator conditionalExpressionBoxWithDataFormattingList = valueGroupNode.CreateNavigator().SelectDescendants(System.Xml.XPath.XPathNodeType.All, true);
                                bool selectNodeExists = false;
                                while (conditionalExpressionBoxWithDataFormattingList.MoveNext() && !selectNodeExists)
                                {
                                    if (conditionalExpressionBoxWithDataFormattingList.Current.Name.Equals("xsl:value-of"))
                                    {
                                        selectNodeExists = true;
                                    }
                                }

                                string nodeInnerValue = string.Empty;

                                if (!(valueGroupNode.Attributes["select"] != null) && (conditionalExpressionBox == null) && !selectNodeExists)
                                {
                                    nodeInnerValue = valueGroupNode.InnerText;
                                }

                                if (!string.IsNullOrEmpty(nodeInnerValue))
                                {
                                    FormText formText = new FormText();
                                    formText.Page = pageName;
                                    formText.TextType = TextType.ExpressionBox;
                                    formText.TextContent = nodeInnerValue;
                                    formText.TextCode = textFieldXPath;
                                    formTexts.Add(formText);
                                }

                                break;
                            case PageControlType.Hyperlink:
                                string anchorText = valueGroupNode.InnerText;
                                string hrefValue = valueGroupNode.Attributes["href"].Value;
                                textFieldXPath = valueGroupXPath.Remove(valueGroupXPath.Length - 1) + "='" + hrefValue + "']";

                                if (!string.IsNullOrEmpty(hrefValue))
                                {
                                    FormText formText = new FormText();
                                    formText.Page = pageName;
                                    formText.TextType = TextType.HyperLink;
                                    formText.TextContent = anchorText;
                                    formText.TextCode = textFieldXPath;
                                    formTexts.Add(formText);
                                }

                                break;
                            case PageControlType.Hint:
                                textFieldXPath = valueGroupXPath;

                                if (!string.IsNullOrEmpty(valueGroupNode.InnerText))
                                {
                                    FormText hintText = new FormText();
                                    hintText.Page = pageName;
                                    hintText.TextType = TextType.HintText;
                                    hintText.TextContent = valueGroupNode.InnerText;
                                    hintText.TextCode = textFieldXPath;
                                    formTexts.Add(hintText);
                                }

                                break;
                            case PageControlType.TableHeader:
                                textFieldXPath = valueGroupXPath;

                                if (!string.IsNullOrEmpty(valueGroupNode.InnerText))
                                {
                                    FormText hintText = new FormText();
                                    hintText.Page = pageName;
                                    hintText.TextType = TextType.HintText;
                                    hintText.TextContent = valueGroupNode.InnerText;
                                    hintText.TextCode = textFieldXPath;
                                    formTexts.Add(hintText);
                                }

                                break;
                        }
                    }
                }
            }

            return formTexts;
        }

        private string GetControlXPath(PageControlType controlType)
        {
            string xPath = string.Empty;
            switch (controlType)
            {
                case PageControlType.DropDown: xPath = "//select [@xd:xctname='dropdown']/@xd:CtrlId"; break;
                case PageControlType.ListBox: xPath = "//select[@xd:xctname='ListBox']/@xd:CtrlId"; break;
                case PageControlType.Button: xPath = "//input[@xd:xctname='Button']/@xd:CtrlId"; break;
                case PageControlType.ExpressionBox: xPath = "//span[@xd:xctname='ExpressionBox']/@xd:CtrlId"; break;
                case PageControlType.ExpressionBoxCopy: xPath = "//span[@xd:xctname='ExpressionBox']/@xd:CtrlId"; break;
                case PageControlType.Hyperlink: xPath = "//a [@href]"; break;
                case PageControlType.TableHeader: xPath = "//table [@xd:CtrlId]"; break;
                case PageControlType.Hint: xPath = "//div [@class='optionalPlaceholder']/@xd:xmlToEdit"; break;
                case PageControlType.PictureButton: xPath = "//button[@xd:xctname='PictureButton']/@xd:CtrlId"; break;
            }

            return xPath;
        }

        private string GetValueXPath(PageControlType controlType, string controlID)
        {
            string xPath = string.Empty;
            switch (controlType)
            {
                case PageControlType.DropDown:
                    xPath = "//select [@xd:CtrlId = '" + controlID.ToString() + "']";
                    break;
                case PageControlType.ListBox:
                    xPath = "//select [@xd:CtrlId = '" + controlID.ToString() + "']";
                    break;
                case PageControlType.Button:
                    xPath = "//input [@xd:CtrlId = '" + controlID.ToString() + "']";
                    break;
                case PageControlType.ExpressionBox:
                    xPath = "//span [@xd:CtrlId = '" + controlID.ToString() + "']/xsl:value-of";
                    break;
                case PageControlType.Hyperlink:
                    xPath = "//a [@href]";
                    break;
                case PageControlType.TableHeader:
                    xPath = "//table [@xd:CtrlId='" + controlID.ToString() + "']/tbody [@class='xdTableHeader']//strong";
                    break;
                case PageControlType.Hint:
                    xPath = "//div [@class='optionalPlaceholder' and @xd:xmlToEdit='" + controlID.ToString() + "']";
                    break; // groupID  

                case PageControlType.ExpressionBoxCopy:
                    xPath = "//span [@xd:CtrlId = '" + controlID.ToString() + "']";
                    break;
                case PageControlType.PictureButton:
                    xPath = "//button [@xd:CtrlId = '" + controlID.ToString() + "']";
                    break;
            }

            return xPath;
        }

        /// <summary>
        /// Compress the XSN Files
        /// </summary>
        /// <param name="sourcePath">Source Path</param>
        /// <param name="destPath">Dest Path</param>
        public void CompressXSN(string sourcePath, string destPath)
        {
            Compress compress = new Compress();
            compress.CompressFolder(sourcePath, destPath, null, false, false, 0);
            compress = null;
        }

        /// <summary>
        /// Extract the XSN Files
        /// </summary>
        /// <param name="sourcePath">Source Path</param>
        /// <param name="destPath">Dest Path</param>
        public void ExtractCab(string sourcePath, string destPath)
        {
            Extract cab = new Extract();
            cab.ExtractFile(sourcePath, destPath);
        }

        /// <summary>
        /// Event handler that adds a handle to the byte array in memory </summary>
        /// <param name="fileName">File Name</param>
        /// <param name="byteArray">Extracted File</param>
        private void Cab_AfterFileExtractFromStream(string fileName, byte[] byteArray)
        {
            this._lastExtractedFile = byteArray;
        }

        private bool OnBeforeCopyFile(Extract.kCabinetFileInfo k_Info)
        {
            bool shouldFileExtracted = false;

            if (k_Info.s_RelPath.ToLower().Contains(XSD)
                && !k_Info.s_RelPath.ToLower().Contains(MYSCHEMA_XSD)
                && !k_Info.s_RelPath.ToLower().Contains(FORMSET_XSD))
            {
                shouldFileExtracted = true;
            }

            return shouldFileExtracted;
        }

        private enum ControlType
        {
            ExpressionBox = 1,
            XdBehavior_Boolean = 2,
            XdTextBox = 3,
            XdRichTextBox = 4,
            XdListBox = 5,
            HyperLink = 6,
            XdComboBox = 7,
            DatePicker = 8
        }
    }
}
