using Sitecore.Data.Items;

namespace DatasourceIndexer.ComputedAbstract
{
    /// <summary>
    /// Abstract class that should be inherited if you use the Index Class field on a Sublayout.
    /// You must implement the Run method.
    /// </summary>
    public abstract class DatasourceComputed
    {
        /// <summary>
        /// Return the string that will be added to the index
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public abstract string Run(Item item);
    }
}