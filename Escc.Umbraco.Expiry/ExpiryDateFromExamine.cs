using Escc.Umbraco.Caching;
using Examine;
using Exceptionless;
using Lucene.Net.Store;
using System;
using System.Globalization;
using System.Linq;
using Umbraco.Core.Logging;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Reads the expiry date for a node from Examine, where it was saved by code in <see cref="SaveExpiryDateToExamineEventHandler" />
    /// </summary>
    /// <seealso cref="Escc.Umbraco.Caching.IExpiryDateSource" />
    public class ExpiryDateFromExamine : IExpiryDateSource
    {
        private readonly int _nodeId;
        private readonly ISearcher _examineSearcher;
        private readonly ICacheStrategy _cache;
        private DateTime? _expiryDate;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpiryDateFromExamine"/> class.
        /// </summary>
        /// <param name="nodeId">The node identifier of the page.</param>
        /// <param name="examineSearcher">The Examine searcher for the 'External' index.</param>
        /// <param name="cache">A method of caching the returned date</param>
        /// <exception cref="ArgumentNullException">examineSearcher or cache</exception>
        public ExpiryDateFromExamine(int nodeId, ISearcher examineSearcher, ICacheStrategy cache)
        {
            _nodeId = nodeId;
            _examineSearcher = examineSearcher ?? throw new ArgumentNullException(nameof(examineSearcher));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Gets the expiry date.
        /// </summary>
        /// <value>
        /// The expiry date.
        /// </value>
        public DateTime? ExpiryDate
        {
            get
            {
                var fromCache = _cache.ReadFromCache("ExpiryDateFromExamine_" + _nodeId.ToString());
                if (fromCache == default(DateTime))
                {
                    try
                    {
                        var query = _examineSearcher.CreateSearchCriteria();
                    query.Field("__NodeId", _nodeId.ToString(CultureInfo.InvariantCulture));
                    var matchedNode = _examineSearcher.Search(query).FirstOrDefault<SearchResult>();

                        if (matchedNode == null)
                        {
                            _expiryDate = null;
                        }
                        else if (!matchedNode.Fields.ContainsKey("expireDate"))
                        {
                            _expiryDate = null;
                        }
                        else if (matchedNode.Fields["expireDate"] == "99991231235959") // DateTime.MaxValue as a proxy for "never expire"
                        {
                            _expiryDate = null;
                        }
                        else
                        {
                            var expireDate = matchedNode.Fields["expireDate"].ToString();
                            _expiryDate = new DateTime(Int32.Parse(expireDate.Substring(0, 4)), Int32.Parse(expireDate.Substring(4, 2)), Int32.Parse(expireDate.Substring(6, 2)), Int32.Parse(expireDate.Substring(8, 2)), Int32.Parse(expireDate.Substring(10, 2)), Int32.Parse(expireDate.Substring(12, 2)));
                        }

                        if (_expiryDate.HasValue)
                        {
                            _cache.AddToCache("ExpiryDateFromExamine_" + _nodeId.ToString(), _expiryDate.Value);
                        }
                        else
                        {
                            _cache.AddToCache("ExpiryDateFromExamine_" + _nodeId.ToString(), DateTime.MaxValue); // DateTime.MaxValue as a proxy for "null", as some cache implementations cannot cache a null value
                        }

                    }
                    catch (AlreadyClosedException ex)
                    {
                        // This error can happen under load - report it and then report no expiry date as that is better than 
                        // throwing an exception that can crash a consuming page
                        LogHelper.Error<ExpiryDateFromExamine>(ex.Message, ex);
                        ex.ToExceptionless().Submit();
                        return null;
                    }
                }
                else
                {
                    if (fromCache == DateTime.MaxValue)
                    {
                        return null;
                    }
                    else return fromCache;
                }
                return _expiryDate;
            }
        }
    }
}