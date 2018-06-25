using Escc.Umbraco.Caching;
using Examine;
using System;
using System.Globalization;
using System.Linq;

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
        private DateTime? _expiryDate;
        private bool _expiryDateAlreadyAvailable;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpiryDateFromExamine"/> class.
        /// </summary>
        /// <param name="nodeId">The node identifier of the page.</param>
        /// <param name="examineSearcher">The Examine searcher for the 'External' index.</param>
        /// <exception cref="ArgumentNullException">examineSearcher</exception>
        public ExpiryDateFromExamine(int nodeId, ISearcher examineSearcher)
        {
            _nodeId = nodeId;
            _examineSearcher = examineSearcher ?? throw new ArgumentNullException(nameof(examineSearcher));
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
                if (!_expiryDateAlreadyAvailable)
                {
                    var query = _examineSearcher.CreateSearchCriteria();
                    query.Field("__NodeId", _nodeId.ToString(CultureInfo.InvariantCulture));
                    var matchedNode = _examineSearcher.Search(query).FirstOrDefault<SearchResult>();

                    if (matchedNode == null) return null;
                    if (!matchedNode.Fields.ContainsKey("expireDate")) return null;
                    if (matchedNode.Fields["expireDate"] == "99991231235959") return null; // DateTime.MaxValue as a proxy for "never expire"

                    var expireDate = matchedNode.Fields["expireDate"].ToString();
                    _expiryDate = new DateTime(Int32.Parse(expireDate.Substring(0, 4)), Int32.Parse(expireDate.Substring(4, 2)), Int32.Parse(expireDate.Substring(6, 2)), Int32.Parse(expireDate.Substring(8, 2)), Int32.Parse(expireDate.Substring(10, 2)), Int32.Parse(expireDate.Substring(12, 2)));
                    _expiryDateAlreadyAvailable = true;
                }
                return _expiryDate;
            }
        }
    }
}