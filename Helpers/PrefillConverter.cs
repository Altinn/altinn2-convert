using System.Linq;

using Altinn2Convert.Models.Altinn2.FormFieldPrefill;
using Altinn2Convert.Models.Altinn3.prefill;
using Prefill = Altinn2Convert.Models.Altinn3.prefill.Test;

namespace Altinn2Convert.Helpers
{
    public static class PrefillConverter
    {
        public static Prefill Convert(FormFieldPrefill a2prefill)
        {
            var ret = new Prefill()
            {
                ER = new (),
                DSF = new (),
                UserProfile = new (),
            };
            a2prefill?.Register?.FirstOrDefault(r => r.Name == "ER")?.Field?.ForEach(field =>
            {
                var path = XpathToJsonPath(field.XPath);
                if (path.StartsWith("@my"))
                {
                    return;
                }

// ChosenOrgNr" registerField="BusinessAddress" />
// ChosenOrgNr" registerField="BusinessPostalCity" />
// ChosenOrgNr" registerField="BusinessPostalCode" />
// ChosenOrgNr" registerField="Name" />
// ChosenOrgNr" registerField="OrganizationNumber" />
// ChosenOrgNr" registerField="PostalAddress" />
// ChosenOrgNr" registerField="PostalPostalCity" />
// ChosenOrgNr" registerField="PostalPostalCode" />
                switch (field.RegisterField)
                {
                    // TODO: Find all prefill codes in altinn2 er
                    case "OrganizationNumber":
                        ret.ER.OrgNumber = path;
                        break;
                    case "Name":
                        ret.ER.Name = path;
                        break;
                    case "PostalAddress":
                        ret.ER.MailingAddress = path;
                        break;
                    case "PostalPostalCode":
                        ret.ER.MailingPostalCode = path;
                        break;
                    case "PostalPostalCity":
                        break;
                }
            });
            a2prefill?.Register?.FirstOrDefault(r => r.Name == "DSF")?.Field?.ForEach(field =>
            {
                var path = XpathToJsonPath(field.XPath);
                if (path.StartsWith("@my"))
                {
                    return;
                }

// Person" registerField="Address" />
// Person" registerField="FirstName" />
// Person" registerField="LastName" />
// Person" registerField="MiddleName" />
// Person" registerField="Name" />
// Person" registerField="PlaceName" />
// Person" registerField="PostalCity" />
// Person" registerField="PostalCode" />
// Person" registerField="SocialSecurityNumber" />
// Person" registerField="StreetName" />
                switch (field.RegisterField)
                {
                    // TODO: Find all prefill codes in altinn2 er
                    case "Address":
                        // TODO: figure out what is right here
                        //       all of theese seems to match a single field in Altinn2
                        ret.DSF.AddressStreetName = path;
                        ret.DSF.AddressHouseNumber = path;
                        ret.DSF.AddressHouseLetter = path;
                        break;
                    case "FirstName":
                        ret.DSF.FirstName = path;
                        break;
                    case "LastName":
                        ret.DSF.LastName = path;
                        break;
                    case "MiddleName":
                        ret.DSF.MiddleName = path;
                        break;
                    case "Name":
                        ret.DSF.Name = path;
                        break;
                    case "PlaceName":
                        ret.DSF.AddressCity = path;
                        // ret.DSF.MailingPostalCity = path;
                        break;
                    case "PostalCity":
                        ret.DSF.AddressCity = path;
                        // ret.DSF.MailingPostalCity = path;
                        break;
                    case "PostalCode":
                        ret.DSF.AddressPostalCode = path;
                        // ret.DSF.MailingPostalCode = path;
                        break;
                    case "SocialSecurityNumber":
                        ret.DSF.SSN = path;
                        break;
                    // TOOD: Complete list
                }
            });
            a2prefill?.Register?.FirstOrDefault(r => r.Name == "DLS")?.Field?.ForEach(field =>
            {
                var path = XpathToJsonPath(field.XPath);
                if (path.StartsWith("@my"))
                {
                    return;
                }

// Profile" registerField="ProfileEmail" />
// Profile" registerField="ProfileLastName" />
// Profile" registerField="ProfileMobile" />
// Profile" registerField="ProfileSSN" />
                switch (field.RegisterField)
                {
                    // TODO: Fix mapping
                    case "ProfileEmail":
                        ret.UserProfile.Email = path;
                        break;
                    case "ProfileLastName":
                        ret.UserProfile.PartyPersonLastName = path;
                        break;
                    case "ProfileMobile":
                        ret.UserProfile.PartyPersonMobileNumber = path;
                        break;
                    case "ProfileSSN":
                        ret.UserProfile.PartySSN = path;
                        break;
                }
            });
            return ret;
        }

        public static string XpathToJsonPath(string xpath)
        {
            int rootIndex = xpath.Substring(1).IndexOf("/");
            return xpath.Substring(rootIndex + 2).Replace("/", ".") + ".value";
        }
    }
}