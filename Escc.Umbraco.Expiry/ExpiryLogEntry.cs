using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Escc.Umbraco.Expiry
{
    public class ExpiryLogEntry
    {
        public int Id { get; set; }
        public string EmailAddress { get; set; }
        public DateTime DateAdded { get; set; }
        public bool EmailSuccess { get; set; }
        public string Pages { get; set; }
        public int PageCount { get; set; }

        public ExpiryLogEntry(int id, string emailAddress, DateTime dateAdded, bool emailSuccess, string pages)
        {
            Id = id;
            EmailAddress = emailAddress;
            DateAdded = dateAdded;
            EmailSuccess = emailSuccess;
            Pages = pages;
        }
        public ExpiryLogEntry()
        {

        }
    }
}