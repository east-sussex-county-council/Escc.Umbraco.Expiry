﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Web.Script.Serialization;
using System.Configuration;

namespace Escc.Umbraco.Expiry.Web
{
    public class HomeController : Controller
    {
        private IExpiryLogRepository _logRepository = new SqlServerExpiryLogRepository();

        [Authorize]
        public ActionResult Index()
        {
            // Instantiate the ViewModel and Lists
            ExpiryEmailStatsViewModel model = new ExpiryEmailStatsViewModel();
            List<ExpiryLogEntry> EmailFailures = new List<ExpiryLogEntry>();
            List<ExpiryLogEntry> EmailSuccess = new List<ExpiryLogEntry>();

            // Populate the Lists of ExpiryLogs
            EmailSuccess = _logRepository.GetExpiryLogSuccessDetails();
            EmailFailures = _logRepository.GetExpiryLogFailureDetails();

            // Assign variables to the ViewModel
            model.FailedEmails.Table = CreateTable(EmailFailures);
            model.SuccessfulEmails.Table = CreateTable(EmailSuccess);

            model.ExpiringPages.Table = CreateExpiringPagesTable();

            // Return the Index view and pass it the ViewModel
            return View("Index", model);
        }

        public ActionResult GetPages(int ID)
        {
            // Create the ViewModel
            var model = new PagesViewModel();
            // Get the LogEntry         
            model.User = _logRepository.GetExpiryLogById(ID);
            // Deserialize the Json pages string.
            List<UmbracoPage> Pages = new JavaScriptSerializer().Deserialize<List<UmbracoPage>>(model.User.Pages);

            // Create the Pages Datatable
            model.Pages.Table = new DataTable();
            model.Pages.Table.Columns.Add("ID", typeof(int));
            model.Pages.Table.Columns.Add("Name", typeof(string));
            model.Pages.Table.Columns.Add("Published Link", typeof(HtmlString));
            model.Pages.Table.Columns.Add("Edit Link", typeof(HtmlString));
            model.Pages.Table.Columns.Add("Expiry Date", typeof(string));

            //Populate the Datatable
            foreach (var page in Pages)
            {
                HtmlString PublishedLink = getPublishLink(page);
                HtmlString EditLink = new HtmlString(string.Format("<a href=\"{0}{1}\">Edit</a>", ConfigurationManager.AppSettings["EditURI"], page.PageId));
                if(page.ExpiryDate == null)
                {
                    model.Pages.Table.Rows.Add(page.PageId, page.PageName, PublishedLink, EditLink, "Never Expires");
                }
                else
                {
                    model.Pages.Table.Rows.Add(page.PageId, page.PageName, PublishedLink, EditLink, page.ExpiryDate.ToString());
                }
            }

            // Return the view model to the GetPages View
            return View(model);
        }

        public int CountPages(int ID)
        {
            // Find the Log for the User ID
            var LogEntry = _logRepository.GetExpiryLogById(ID);
            // Deserialize the Json pages string.
            List<UmbracoPage> Pages = new JavaScriptSerializer().Deserialize<List<UmbracoPage>>(LogEntry.Pages);
            return Pages.Count;
        }

        private static HtmlString getPublishLink(UmbracoPage page)
        {
            HtmlString PublishedLink;
            if (page.PageUrl == "#")
            {
                page.PageUrl = "This page has an expiry date but is not published - This is usually caused by an unpublished parent page.";
                PublishedLink = new HtmlString(string.Format("{0}", page.PageUrl));
            }
            else
            {
                PublishedLink = new HtmlString(string.Format("<a href=\"{0}{1}\">{2}</a>", ConfigurationManager.AppSettings["PublishedURI"], page.PageUrl, page.PageUrl));
            }
            return PublishedLink;
        }

        private HtmlString GetAuthorsHtmlString(List<string> authors, int pageID)
        {
            var authorString = string.Format("<div class=\"dropdown\"><button class=\"btn btn-default dropdown-toggle\" type=\"button\" id=\"{0}\" data-toggle=\"dropdown\" aria-haspopup=\"true\" aria-expanded=\"true\">Authors<span class=\"caret\"></span>  </button>  <ul class=\"dropdown-menu\" aria-labelledby=\"{1}\">", pageID, pageID);
            foreach (var author in authors)
            {
                authorString += string.Format("<li><a href=mailto:\"{0}\">{1}</a></li>", author, author);
            }
            authorString += string.Format("</ul></div>");

            HtmlString AuthorsHtmlString = new HtmlString(authorString);
            return AuthorsHtmlString;
        }

        private DataTable CreateTable(List<ExpiryLogEntry> modelList)
        {

            List<ExpiryLogEntry> EmailLogList = new List<ExpiryLogEntry>();
            foreach (var item in modelList)
            {
                var log = _logRepository.GetExpiryLogById(item.Id);


            }
       
            //Create a new DataTable
            DataTable table = new DataTable();
            table.Columns.Add("ID", typeof(int));
            table.Columns.Add("Email", typeof(HtmlString));
            table.Columns.Add("Date", typeof(string));
            table.Columns.Add("Pages", typeof(HtmlString));

            //Populate the DataTable
            foreach (var model in modelList)
            {
                HtmlString Email = new HtmlString("<a href='mailto:" + model.EmailAddress + "'>" + model.EmailAddress + "</a>");
                var pagesString = this.Url.Action("GetPages", "Home", new { ID = model.Id }, this.Request.Url.Scheme);
                HtmlString Pages = new HtmlString(string.Format("<a href=\"{0}\" class=\"btn btn-info\">Pages</a> <span class=\"badge badge-info\">{1}</span>", pagesString, CountPages(model.Id)));
                table.Rows.Add(model.Id, Email, model.DateAdded, Pages);
            }
            return table;
        }

        private DataTable CreateExpiringPagesTable()
        {
            // Instantiate our lists and get all of the logs from the database
            List<ExpiryLogEntry> ExpiryLogs = _logRepository.GetExpiryLogs();
            List<UmbracoPage> ExpiringPages = new List<UmbracoPage>();

            // Create the pages datatable
            var table = new DataTable();
            table.Columns.Add("ID", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Published Link", typeof(HtmlString));
            table.Columns.Add("Edit Link", typeof(HtmlString));
            table.Columns.Add("Authors", typeof(HtmlString));
            table.Columns.Add("Expiry Date", typeof(DateTime));

            // Go through each log
            foreach (var log in ExpiryLogs)
            {
                // Deserialize the pages for each log into a list of pages.
                List<UmbracoPage> LogPages = new JavaScriptSerializer().Deserialize<List<UmbracoPage>>(log.Pages);
                // for each page model in the pages list
                foreach (var page in LogPages)
                {
                    // if its expire date is greater than todays date
                    if (page.ExpiryDate >= DateTime.Now)
                    {
                        // if the page isn't already in the expiring pages list
                        if (!ExpiringPages.Any(x => x.PageId == page.PageId))
                        {
                            // add the page to the expiring pages list
                            // Add Author for that page.
                            page.Authors = new List<string>();
                            page.Authors.Add(log.EmailAddress);
                            ExpiringPages.Add(page);
                        }
                        else
                        {
                            // If the page is already in the table then just add the authors to the page
                            var ExpiringPage = ExpiringPages.Single(x => x.PageId == page.PageId);
                            // if the author isn't already added.
                            if (!ExpiringPage.Authors.Contains(log.EmailAddress))
                            {
                                ExpiringPage.Authors.Add(log.EmailAddress);
                            }
                        }
                    }
                }
            }

            // Populate the datatable with the pages from the expiring pages list
            foreach (var page in ExpiringPages)
            {
                HtmlString Authors = GetAuthorsHtmlString(page.Authors, page.PageId);
                HtmlString PublishedLink = getPublishLink(page);
                HtmlString EditLink = new HtmlString(string.Format("<a href=\"{0}{1}\">Edit</a>", ConfigurationManager.AppSettings["EditURI"], page.PageId));
                table.Rows.Add(page.PageId, page.PageName, PublishedLink, EditLink, Authors, page.ExpiryDate);
            }

            // return the datatable
            return table;
        }
    }
}