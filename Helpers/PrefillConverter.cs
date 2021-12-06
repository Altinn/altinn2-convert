using System.Linq;

using Altinn2Convert.Models.Altinn2.FormFieldPrefill;
using Altinn2Convert.Models.Altinn3.prefill;
using Prefill = Altinn2Convert.Models.Altinn3.prefill.Test;

namespace Altinn2Convert.Helpers
{
    public static class PrefillConverter
    {
        public static Prefill Convert(FormFieldPrefill prefill)
        {
            var ret = new Prefill()
            {
                ER = new (),
                DSF = new (),
                UserProfile = new (),
            };
            prefill?.Register?.FirstOrDefault(r => r.Name == "ER")?.Field?.ForEach(field =>
            {
                // var path = XpathToJsonPath(field.XPath);
                // switch (field.RegisterField)
                // {
                //     // TODO: Find all prefill codes in altinn2 er
                //     // case "SocialSecurityNumber":
                //     //     return ret.ER.
                // }
            });
            prefill?.Register?.FirstOrDefault(r => r.Name == "DSF")?.Field?.ForEach(field =>
            {
                var path = XpathToJsonPath(field.XPath);
                switch (field.RegisterField)
                {
                    // TODO: Find all prefill codes in altinn2 er
                    case "SocialSecurityNumber":
                        ret.DSF.SSN = path;
                        break;
                    case "FirstName":
                        ret.DSF.FirstName = path;
                        break;
                    case "MiddleName":
                        ret.DSF.MiddleName = path;
                        break;
                    case "LastName":
                        ret.DSF.LastName = path;
                        break;
                    case "Address":
                        // TODO: figure out what is right here
                        //       all of theese seems to match a single field in Altinn2
                        ret.DSF.AddressStreetName = path;
                        ret.DSF.AddressHouseNumber = path;
                        ret.DSF.AddressHouseLetter = path;
                        break;
                    case "PostalCode":
                        ret.DSF.AddressPostalCode = path;
                        ret.DSF.MailingPostalCode = path;
                        break;
                    case "PostalCity":
                        ret.DSF.AddressCity = path;
                        ret.DSF.MailingPostalCity = path;
                        break;
                    case "PlaceName":
                        ret.DSF.AddressCity = path;
                        ret.DSF.MailingPostalCity = path;
                        break;
                    // TOOD: Complete list
                }
            });
            prefill?.Register?.FirstOrDefault(r => r.Name == "DLS")?.Field?.ForEach(field =>
            {
                var path = XpathToJsonPath(field.XPath);
                switch (field.RegisterField)
                {
                    // TODO: Fix mapping
                    case "PostalCity":
                        ret.UserProfile.PartyPersonAddressCity = path;
                        ret.UserProfile.PartyPersonMailingPostalCity = path;
                        ret.UserProfile.PartyOrganizationMailingPostalCity = path;
                        ret.UserProfile.PartyOrganizationBusinessPostalCity = path;
                        break;
                    case "PlcaceName":
                        ret.UserProfile.PartyPersonAddressCity = path;
                        ret.UserProfile.PartyPersonMailingPostalCity = path;
                        ret.UserProfile.PartyOrganizationMailingPostalCity = path;
                        ret.UserProfile.PartyOrganizationBusinessPostalCity = path;
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