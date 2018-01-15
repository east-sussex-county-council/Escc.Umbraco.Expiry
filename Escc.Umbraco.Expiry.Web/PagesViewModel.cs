using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Escc.Umbraco.Expiry.Web
{
    public class PagesViewModel
    {
        public TableModel Pages { get; set; }
        public ExpiryLogEntry User { get; set; }
        public PagesViewModel()
        {
            Pages = new TableModel("PagesTable");
        }
    }
}