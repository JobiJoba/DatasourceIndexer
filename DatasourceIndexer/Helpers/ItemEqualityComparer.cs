using System.Collections.Generic;
using Sitecore.Data.Items;

namespace DatasourceIndexer.Helpers
{
    /// <summary>
    /// Compares two Sitecore items. (Coming from Content Usage Tools Module : http://marketplace.sitecore.net/en/Modules/Sitecore_Content_Usage_Tools.aspx )
    /// </summary>
    internal class ItemEqualityComparer : IEqualityComparer<Item>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type <paramref name="T" /> to compare.</param>
        /// <param name="y">The second object of type <paramref name="T" /> to compare.</param>
        /// <returns>
        /// true if the specified objects are equal; otherwise, false.
        /// </returns>
        public bool Equals(Item x, Item y)
        {
            return x.ID.Equals(y.ID);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public int GetHashCode(Item obj)
        {
            return obj.ID.GetHashCode();
        }
    }
}