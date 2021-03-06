﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Checks whether a node with a particular path matches any of a given set of path expiry rules
    /// </summary>
    public class PathRuleMatcher : IPathRuleMatcher
    {
        private readonly IEnumerable<PathExpiryRule> _expiryRules;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathRuleMatcher"/> class.
        /// </summary>
        /// <param name="expiryRules">The expiry rules.</param>
        public PathRuleMatcher(IEnumerable<PathExpiryRule> expiryRules)
        {
            _expiryRules = expiryRules;
            foreach (var rule in _expiryRules)
            {
                rule.Path = NormalisePath(rule.Path);
            }
        }

        /// <summary>
        /// Check for and process an override based on the page Url
        /// </summary>
        /// <param name="pagePath">The Url to check</param>
        /// <returns>
        /// Returns the matched rule, or <c>null</c> if no rule is found
        /// </returns>
        public IExpiryRule MatchRule(string pagePath)
        {
            // If parameters were not passed, return no override
            if (_expiryRules == null) return null;
            if (string.IsNullOrEmpty(pagePath)) return null;
            pagePath = NormalisePath(pagePath);

            // Look for the specific Url in the overrides list
            var matchedRule = _expiryRules.FirstOrDefault(n => n.Path == pagePath);
            if (matchedRule != null) return matchedRule;

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
            matchedRule = _expiryRules.Where(n => pagePath.StartsWith(n.Path)).OrderByDescending(c => c.Path.Count(f => f == '/')).FirstOrDefault();

            // No override(s) found
            if (matchedRule == null) return null;

            // The override applies to all children too, so return the rule
            if (matchedRule.ApplyToDescendantPages) return matchedRule;

            // All other tests failed, so return no override
            return null;
        }

        private string NormalisePath(string path)
        {
            return path.TrimEnd('/') + "/";
        }
    }
}
