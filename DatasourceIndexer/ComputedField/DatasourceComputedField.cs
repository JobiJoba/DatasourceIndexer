using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DatasourceIndexer.ComputedAbstract;
using DatasourceIndexer.Helpers;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.ComputedFields;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Reflection;

namespace DatasourceIndexer.ComputedField
{
    /// <summary>
    /// This class compute all the text field of a Datasource item which is in the Content path. 
    /// If it's not on the Content, the default class (MediaItemContentExtractor) do is normal job.
    /// </summary>
    public class DatasourceComputedField : IComputedIndexField
    {
        public object ComputeFieldValue(IIndexable indexable)
        {
            string datasourceIndexed = string.Empty;
            Item item = (indexable as SitecoreIndexableItem);

            if (item == null)
                return null;
            if (item.Paths.IsContentItem)
            {
                using (new LanguageSwitcher(item.Language))
                {
                   
                    var renderings = item.Visualization.GetRenderings(
                        DeviceItem.ResolveDevice(Factory.GetDatabase("master")), true);

                    if (!renderings.Any()) return null;

                    foreach (var renderingReference in renderings)
                    {
                        var renderingSettings = renderingReference.Settings;
                        if (!string.IsNullOrEmpty(renderingSettings.DataSource))
                        {

                            var renderingItem = renderingReference.RenderingItem.InnerItem;

                            if (string.IsNullOrEmpty(renderingItem[Constants.IsIndexed])) continue;

                            var datasourceItem = Factory.GetDatabase("master").GetItem(renderingSettings.DataSource);
                            string indexClass = renderingItem[Constants.IndexClassFieldID];
                            if (!string.IsNullOrEmpty(indexClass))
                            {
                                string assemblyName = string.Empty;
                                int num = indexClass.IndexOf(',');
                                if (num >= 0)
                                {
                                    assemblyName = indexClass.Substring(num + 1).Trim();
                                    indexClass = indexClass.Substring(0, num).Trim();
                                }
                                try
                                {
                                    var assembly = ReflectionUtil.LoadAssembly(assemblyName);
                                    if (assembly == null) return null;
                                    Type type = assembly.GetType(indexClass, false, true);
                                    if (type == null) return null;
                                    var obj = ReflectionUtil.CreateObject(type) as DatasourceComputed;
                                    if (obj != null) datasourceIndexed += string.Format(" {0} ", obj.Run(item));
                                }
                                catch (FileNotFoundException ex)
                                {
                                    datasourceIndexed += string.Empty;
                                    Log.Warn("Assembly not found " + indexClass, ex, this);
                                }

                            }
                            else if (!string.IsNullOrEmpty(renderingItem[Constants.IndexAllFieldFieldID]))
                            {
                                var listField = DatasourceIndexerHelper.GetFieldNameFromItem(datasourceItem,
                                    Factory.GetDatabase("master"));
                                datasourceIndexed = ConcatField(listField, datasourceItem);
                            }
                            else if (!string.IsNullOrEmpty(renderingItem[Constants.MultiListFieldID]))
                            {
                                var listField =
                                    ((MultilistField)renderingItem.Fields[Constants.MultiListFieldID]).GetItems().Select(o => o.Name);
                                datasourceIndexed = ConcatField(listField, datasourceItem);
                            }
                        }
                    }
                }
                return datasourceIndexed;
            }
            else
            {
                SitecoreIndexableItem sitecoreIndexableItem = (indexable as SitecoreIndexableItem);

                var field = item.Fields["Mime Type"];
                IComputedIndexField computedIndexField;
                if (field != null && !string.IsNullOrEmpty(field.Value) && this.mimeTypeComputedFields.TryGetValue(field.Value.ToLowerInvariant(), out computedIndexField))
                {
                    return computedIndexField.ComputeFieldValue(sitecoreIndexableItem);
                }
                var field2 = item.Fields["Extension"];
                if (field2 != null && !string.IsNullOrEmpty(field2.Value) && this.extensionComputedFields.TryGetValue(field2.Value.ToLowerInvariant(), out computedIndexField))
                {
                    return computedIndexField.ComputeFieldValue(sitecoreIndexableItem);
                }
                foreach (IComputedIndexField current in this.fallbackComputedIndexFields)
                {
                    object obj = current.ComputeFieldValue(sitecoreIndexableItem);
                    if (obj != null)
                    {
                        return obj;
                    }
                }
                return null;
            }
            return null;
        }

        private string ConcatField(IEnumerable<string> fields, Item datasourceItem)
        {

            string datasourceIndexed = string.Empty;
            if (datasourceItem.Versions.Count > 0)
            {
                foreach (var fieldName in fields)
                {
                    var field = datasourceItem.Fields[fieldName];
                    if (IsTextField(field))
                    {
                        datasourceIndexed += string.Format(" {0} ", field.Value);
                    }
                }
            }
            return datasourceIndexed;
        }

        protected void AddMediaItemContentExtractorByMimeType(string mimeType, IComputedIndexField computedField)
        {
            Assert.ArgumentNotNull(mimeType, "mimeType");
            Assert.ArgumentNotNull(computedField, "computedField");
            this.mimeTypeComputedFields[mimeType] = computedField;
        }
        protected void AddMediaItemContentExtractorByFileExtension(string extension, IComputedIndexField computedField)
        {
            Assert.ArgumentNotNull(extension, "extension");
            Assert.ArgumentNotNull(computedField, "computedField");
            this.extensionComputedFields[extension] = computedField;
        }
        protected void AddFallbackMediaItemContentExtractor(IComputedIndexField computedField)
        {
            Assert.ArgumentNotNull(computedField, "computedField");
            this.fallbackComputedIndexFields.Insert(0, computedField);
        }
        protected virtual void Initialize()
        {
        }
        private readonly System.Collections.Generic.Dictionary<string, IComputedIndexField> mimeTypeComputedFields = new System.Collections.Generic.Dictionary<string, IComputedIndexField>();
        private readonly System.Collections.Generic.Dictionary<string, IComputedIndexField> extensionComputedFields = new System.Collections.Generic.Dictionary<string, IComputedIndexField>();
        private readonly System.Collections.Generic.List<IComputedIndexField> fallbackComputedIndexFields = new System.Collections.Generic.List<IComputedIndexField>();

        public DatasourceComputedField()
        {
            MediaItemIFilterTextExtractor computedField = new MediaItemIFilterTextExtractor();
            this.AddMediaItemContentExtractorByMimeType("application/pdf", computedField);
            this.AddMediaItemContentExtractorByMimeType("text/html", new MediaItemHtmlTextExtractor());
            this.AddMediaItemContentExtractorByMimeType("text/plain", computedField);
            this.AddMediaItemContentExtractorByFileExtension("rtf", computedField);
            this.AddMediaItemContentExtractorByFileExtension("odt", computedField);
            this.AddMediaItemContentExtractorByFileExtension("doc", computedField);
            this.AddMediaItemContentExtractorByFileExtension("dot", computedField);
            this.AddMediaItemContentExtractorByFileExtension("docx", computedField);
            this.AddMediaItemContentExtractorByFileExtension("dotx", computedField);
            this.AddMediaItemContentExtractorByFileExtension("docm", computedField);
            this.AddMediaItemContentExtractorByFileExtension("dotm", computedField);
            this.AddMediaItemContentExtractorByFileExtension("xls", computedField);
            this.AddMediaItemContentExtractorByFileExtension("xlt", computedField);
            this.AddMediaItemContentExtractorByFileExtension("xla", computedField);
            this.AddMediaItemContentExtractorByFileExtension("xlsx", computedField);
            this.AddMediaItemContentExtractorByFileExtension("xltx", computedField);
            this.AddMediaItemContentExtractorByFileExtension("xlsm", computedField);
            this.AddMediaItemContentExtractorByFileExtension("xltm", computedField);
            this.AddMediaItemContentExtractorByFileExtension("xlam", computedField);
            this.AddMediaItemContentExtractorByFileExtension("xlsb", computedField);
            this.AddMediaItemContentExtractorByFileExtension("ppt", computedField);
            this.AddMediaItemContentExtractorByFileExtension("pot", computedField);
            this.AddMediaItemContentExtractorByFileExtension("pps", computedField);
            this.AddMediaItemContentExtractorByFileExtension("ppa", computedField);
            this.AddMediaItemContentExtractorByFileExtension("pptx", computedField);
            this.AddMediaItemContentExtractorByFileExtension("potx", computedField);
            this.AddMediaItemContentExtractorByFileExtension("ppsx", computedField);
            this.AddMediaItemContentExtractorByFileExtension("ppam", computedField);
            this.AddMediaItemContentExtractorByFileExtension("pptm", computedField);
            this.AddMediaItemContentExtractorByFileExtension("potm", computedField);
            this.AddMediaItemContentExtractorByFileExtension("ppsm", computedField);
            this.AddFallbackMediaItemContentExtractor(computedField);
            this.Initialize();
        }

        public string FieldName { get; set; }
        public string ReturnType { get; set; }

        public bool IsTextField(Sitecore.Data.Fields.Field field)
        {
            return DatasourceIndexerHelper.TextFieldTypes.Contains(field.Type);
        }
    }
}