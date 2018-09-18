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
        ///   <c>true</c> if a rule matches; otherwise, <c>false</c>.
        /// </returns>
        bool IsMatch();
    }
}