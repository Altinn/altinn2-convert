   /* 
    Licensed under the Apache License, Version 2.0
    
    http://www.apache.org/licenses/LICENSE-2.0
    */
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Altinn2Convert.Models.Altinn2.FormFieldPrefill
{
    [XmlRoot(ElementName="FormRegisterPrefill")]
	public class FormFieldPrefill 
    {
		[XmlElement(ElementName="Register")]
		public List<Register> Register { get; set; }

		[XmlElement(ElementName="PredefinedDefinition")]
		public PredefinedDefinition PredefinedDefinition { get; set; }

		[XmlElement(ElementName="OtherPrefillSettings")]
		public OtherPrefillSettings OtherPrefillSettings { get; set; }

		[XmlAttribute(AttributeName="formatVersion")]
		public string FormatVersion { get; set; }
        
		[XmlAttribute(AttributeName="frp", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Frp { get; set; }
	}

	[XmlRoot(ElementName="field")]
	public class Field 
    {
		[XmlAttribute(AttributeName="xPath")]
		public string XPath { get; set; }

		[XmlAttribute(AttributeName="page")]
		public string Page { get; set; }

		[XmlAttribute(AttributeName="contextType")]
		public string ContextType { get; set; }

		[XmlAttribute(AttributeName="registerField")]
		public string RegisterField { get; set; }
	}

	[XmlRoot(ElementName="Register")]
	public class Register
    {
		[XmlElement(ElementName="field")]
		public List<Field> Field { get; set; }

		[XmlAttribute(AttributeName="name")]
		public string Name { get; set; }
	}

	[XmlRoot(ElementName="PredefinedDefinition")]
	public class PredefinedDefinition
    {
		[XmlAttribute(AttributeName="name")]
		public string Name { get; set; }
	}

	[XmlRoot(ElementName="KeyValuePrefill")]
	public class KeyValuePrefill
    {
		[XmlAttribute(AttributeName="enabled")]
		public string Enabled { get; set; }
	}
    
	[XmlRoot(ElementName="OtherPrefillSettings")]
	public class OtherPrefillSettings
    {
		[XmlElement(ElementName="KeyValuePrefill")]
		public KeyValuePrefill KeyValuePrefill { get; set; }
	}
}
