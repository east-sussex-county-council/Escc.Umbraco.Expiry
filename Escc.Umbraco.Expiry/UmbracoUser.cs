using System;

namespace Escc.Umbraco.Expiry
{
    /// <summary>
    /// A back office user in Umbraco
    /// </summary>
    public class UmbracoUser
    {
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">other - other</exception>
        public override bool Equals(object other)
        {
            if (!(other is UmbracoUser)) throw new ArgumentException(nameof(other) + " is not an UmbracoUserModel", nameof(other));

            var otherUser = (UmbracoUser) other;

            return UserId == otherUser.UserId;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return UserId.GetHashCode();
        }

        /// <summary>
        /// Gets or sets the username of the user.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance in activity logs
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("[UserName:{0},FullName:{1},EmailAddress:{2},UserId:{3}]", UserName, FullName, EmailAddress, UserId);
        }
    }
}