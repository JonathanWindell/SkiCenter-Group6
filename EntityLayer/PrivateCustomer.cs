using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;

namespace EntityLayer
{
    public class PrivateCustomer : Customer
    {
        [Required]
        public decimal CreditLimit { get; set; }

        // Parameterless constructor for EF 
        public PrivateCustomer() { }

        public PrivateCustomer(string firstName, string lastName, string address, string email, string phoneNumber, decimal creditLimit) : base(firstName, lastName, address, email, phoneNumber)
        {
            CreditLimit = creditLimit;
        }
    }
}
