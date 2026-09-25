using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;

namespace EntityLayer
{
    public class CorporateCustomer : Customer
    {
        [Required]
        public string OrganisationNumber { get; set; }

        [Required]
        public string ContactPerson { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal DiscountRate { get; set; }
        public bool IsApproved { get; set; } // Decided by marketing manager

        public CorporateCustomer(string firstName, string lastName, string address, string email, string phoneNumber, string orgNumber, string contactPerson) : base(firstName, lastName, address, email, phoneNumber)
        {
            OrganisationNumber = orgNumber;
            ContactPerson = contactPerson;
            IsApproved = false; // Standard value (Pending)
            CreditLimit = 0;
            DiscountRate = 0;
        }
    }
}
