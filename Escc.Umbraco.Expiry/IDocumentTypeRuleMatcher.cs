namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Checks whether a specific document type in use at a specific level in the content tree matches any expiry rules
    /// </summary>
    public interface IDocumentTypeRuleMatcher
    {
        /// <summary>
        /// Check for and process an override based on the page document type
        /// </summary>
        /// <param name="documentTypeAlias">Doc Type to check</param>
        /// <param name="levelInContentTree">Optional level to check at</param>
        /// <returns>
        /// Returns the matched rule, or <c>null</c> if no rule is found
        /// </returns>
        IExpiryRule MatchRule(string documentTypeAlias, int? levelInContentTree);
    }
}