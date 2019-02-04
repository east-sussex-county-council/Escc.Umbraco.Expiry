using System;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// An interface for reading and writing the expiry date to a simple cache
    /// </summary>
    public interface ICacheStrategy
    {
        /// <summary>
        /// Adds a value to the cache with a fixed maximum expiration time.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        void AddToCache(string key, DateTime value);

        /// <summary>
        /// Reads a value from the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        DateTime ReadFromCache(string key);

        /// <summary>
        /// Removes a value from the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        void RemoveFromCache(string key);
    }
}