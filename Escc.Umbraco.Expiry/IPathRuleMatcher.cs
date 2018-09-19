namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Checks whether a node with a particular path matches any of a given set of path expiry rules
    /// </summary>
    public interface IPathRuleMatcher
    {
        /// <summary>
        /// Check for and process an override based on the page Url
        /// </summary>
        /// <param name="pagePath">The Url to check</param>
        /// <returns>
        /// Returns the matched rule, or <c>null</c> if no rule is found
        /// </returns>
        IExpiryRule MatchRule(string pagePath);
    }
}