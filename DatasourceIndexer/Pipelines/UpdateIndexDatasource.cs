using System;
using System.Linq;
using Sitecore;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Pipelines.GetDependencies;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace DatasourceIndexer.Pipelines
{
    /// <summary>
    /// This class will reindex the linked item if a datasource item is reindexed.
    /// Come from : http://www.techphoria414.com/Blog/2013/November/Sitecore-7-Computed-Fields-All-Templates-and-Datasource-Content
    /// </summary>
    public class UpdateIndexDatasource : Sitecore.ContentSearch.Pipelines.GetDependencies.BaseProcessor
    {
        public override void Process(GetDependenciesArgs args)
        {
            Assert.IsNotNull(args.IndexedItem, "indexed item");
            Assert.IsNotNull(args.Dependencies, "dependencies");
            Func<ItemUri, bool> func = null;
            Item item = (Item)(args.IndexedItem as SitecoreIndexableItem);

            if (item != null)
            {
                if (func == null)
                {
                    func = uri => (bool)((uri != null) && ((bool)(uri != item.Uri)));
                }
                System.Collections.Generic.IEnumerable<ItemUri> source =
                    Enumerable.Where<ItemUri>(
                        from l in Globals.LinkDatabase.GetReferrers(item) select l.GetSourceItem().Uri, func)
                        .Distinct<ItemUri>();
                args.Dependencies.AddRange(source.Select(x => (SitecoreItemUniqueId)x));
            }
        }
    }
}