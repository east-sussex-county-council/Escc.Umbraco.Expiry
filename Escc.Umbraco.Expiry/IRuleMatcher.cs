namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Checks whether any rules in a set of expiry rules should apply to a content node
    /// </summary>
    public interface IRuleMatcher
    {
        /// <summary>
        /// Determines whether any of the rules in this set match the content node
        /// </summary>
        /// <returns>
        /// Returns the matched rule, or <c>null</c> if no rule is found
        /// </returns>
        IExpiryRule MatchRule();
    }
}