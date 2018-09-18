using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Checks whether a node with a particular path matches any of a given set of path expiry rules
    /// </summary>
    /// <seealso cref="Escc.Umbraco.Expiry.IRuleMatcher" />
    public class PathRuleMatcher : IRuleMatcher
    {
        private readonly IEnumerable<PathExpiryRule> _expiryRules;
        private readonly string _pagePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathRuleMatcher"/> class.
        /// </summary>
        /// <param name="expiryRules">The expiry rules.</param>
        /// <param name="pagePath">The Url to check</param>
        public PathRuleMatcher(IEnumerable<PathExpiryRule> expiryRules, string pagePath)
        {
            _expiryRules = expiryRules;
            foreach (var rule in _expiryRules)
            {
                rule.Path = NormalisePath(rule.Path);
            }
            _pagePath = NormalisePath(pagePath);
        }

        /// <summary>
        /// Check for and process an override based on the page Url
        /// </summary>
        /// <returns>
        /// True if an override exists
        /// </returns>
        public bool IsMatch()
        {
            // If parameters were not passed, return false (no override)
            if (_expiryRules == null) return false;
            if (string.IsNullOrEmpty(_pagePath)) return false;

            // Look for the specific Url in the overrides list
            if (_expiryRules.Any(n => n.Path == _pagePath)) return true;

            // ================================================================
            // pagePath may be a child of an override Url
            // E.g.
            // <add name="/educationandlearning/schools/" children="" />
            // <add name="/educationandlearning/schools/findingaschool/" children="*" />
            // 
            // pagePath = /educationandlearning/schools/findingaschool/mynewpage
            //
            // Second item is the most specific, find it by ordering the Paths by the number of '/' characters in the string
            // ================================================================
            var entry = _expiryRules.Where(n => _pagePath.StartsWith(n.Path)).OrderByDescending(c => c.Path.Count(f => f == '/')).FirstOrDefault();

            // No override(s) found
            if (entry == null) return false;

            // The override applies to all children too, so return true
            if (entry.Children == "*") return true;

            // All other tests failed, so return false (no override)
            return false;
        }

        public string NormalisePath(string path)
        {
            return path.TrimEnd('/') + "/";
        }
    }
}
