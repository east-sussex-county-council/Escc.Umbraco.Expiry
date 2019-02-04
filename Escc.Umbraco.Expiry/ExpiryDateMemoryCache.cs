using System;
using System.Runtime.Caching;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// Cache the expiry date using the default memory cache
    /// </summary>
    /// <seealso cref="ICacheStrategy" />
    public class ExpiryDateMemoryCache : ICacheStrategy
    {
        private readonly TimeSpan _cacheDuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpiryDateMemoryCache"/> class.
        /// </summary>
        /// <param name="cacheDuration">Duration of the cache.</param>
        public ExpiryDateMemoryCache(TimeSpan cacheDuration)
        {
            _cacheDuration = cacheDuration;
        }

        /// <summary>
        /// Adds a value to the cache with a fixed maximum expiration time.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void AddToCache(string key, DateTime value)
        {
            if (value != null)
            {
                MemoryCache.Default.Set(key, value, (DateTime.Now + _cacheDuration));
            }
        }

        /// <summary>
        /// Reads a value from the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public DateTime ReadFromCache(string key)
        {
            if (MemoryCache.Default.Contains(key))
            {
                return (DateTime)MemoryCache.Default[key];
            }
            return default(DateTime);
        }

        /// <summary>
        /// Removes a value from the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        public void RemoveFromCache(string key)
        {
            if (MemoryCache.Default.Contains(key))
            {
                MemoryCache.Default.Remove(key);
            }
        }
    }
}
