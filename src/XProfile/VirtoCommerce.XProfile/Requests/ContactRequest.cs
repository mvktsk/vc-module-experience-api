using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ExperienceApiModule.XProfile.Requests
{
    public partial class ContactRequest : MemberRequest
    {
        public string FullName { get; set; }
        /// <summary>
        /// Returns the first name of the customer.
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// Returns the last name of the customer.
        /// </summary>
        public string LastName { get; set; }

        public string MiddleName { get; set; }

        public string Salutation { get; set; }

        public string PhotoUrl { get; set; }

        public string TimeZone { get; set; }
        public string DefaultLanguage { get; set; }

        public Address DefaultBillingAddress { get; set; }
        public Address DefaultShippingAddress { get; set; }

        public string OrganizationId { get; set; }
        public OrganizationRequest Organization { get; set; }
        //TODO: It needs to be rework to support only a multiple  organizations for a customer by design.
        public IList<string> OrganizationsIds { get; set; } = new List<string>();

        /// <summary>
        /// Returns true if the customer accepts marketing, returns false if the customer does not.
        /// </summary>
        public bool AcceptsMarketing { get; set; }

        /// <summary>
        /// Returns the default customer_address.
        /// </summary>
        public Address DefaultAddress { get; set; }

        /// <summary>
        /// All contact security accounts
        /// </summary>
        // TODO
        //public IEnumerable<SecurityAccount> SecurityAccounts { get; set; }
    }
}