using Umbraco.Core.Models;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Builds the URL for an Umbraco content node
    /// </summary>
    public interface INodeUrlBuilder
    {
        /// <summary>
        /// Gets the node URL.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        string GetNodeUrl(IContent node);
    }
}