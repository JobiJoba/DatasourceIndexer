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
    /// Inspired from : http://www.techphoria414.com/Blog/2013/November/Sitecore-7-Computed-Fields-All-Templates-and-Datasource-Content
    /// </summary>
    public class UpdateIndexDatasource : BaseProcessor
    {
        public override void Process(GetDependenciesArgs args)
        {
            Assert.IsNotNull(args.IndexedItem, "indexed item");
            Assert.IsNotNull(args.Dependencies, "dependencies");
            
            Item item = (Item)(args.IndexedItem as SitecoreIndexableItem);

            if (item != null && item.Paths.IsContentItem)
            {
                var linkedItems = Globals.LinkDatabase.GetReferrers(item).Select(it => it.GetSourceItem()).Where(uri => uri.Uri != null && (uri.Uri != item.Uri))
                    .Select(o => o.Uri).Distinct<ItemUri>().Select(z => (SitecoreItemUniqueId)z);

                args.Dependencies.AddRange(linkedItems);
            }
        }
    }
}