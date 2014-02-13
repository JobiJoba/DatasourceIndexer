using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DatasourceIndexer.ComputedAbstract;
using DatasourceIndexer.Helpers;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.ComputedFields;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Reflection;
using Constants = DatasourceIndexer.Helpers.Constants;

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
                            if (!renderingSettings.DataSource.IsGuid()) continue;

                            var datasourceItem = Factory.GetDatabase("master").GetItem(renderingSettings.DataSource);
                            if (datasourceItem == null)
                            {
                                //Broken Link
                                continue;
                            }

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
                                    if (obj != null) datasourceIndexed += string.Format(" {0} ", obj.Run(item, renderingSettings));
                                }
                                catch (FileNotFoundException ex)
                                {
                                    datasourceIndexed += string.Empty;
                                    Log.Warn("Assembly not found " + indexClass, ex, this);
                                }

                            }
                            else if (!string.IsNullOrEmpty(renderingItem[Constants.IndexAllFieldFieldID]))
                            {
                                //Take all the field value of the datasource item
                                datasourceIndexed += DatasourceIndexerHelper.GetFieldValueFromItem(datasourceItem);
                            }
                            else if (!string.IsNullOrEmpty(renderingItem[Constants.MultiListFieldID]))
                            {
                                var listField =
                                    ((MultilistField)renderingItem.Fields[Constants.MultiListFieldID]).GetItems().Select(o => o.Name);
                                datasourceIndexed += ConcatField(listField, datasourceItem);
                            }
                        }
                    }
                }
                return datasourceIndexed;
            }
            else
            {
                MediaItemContentExtractor m = new MediaItemContentExtractor();
                return m.ComputeFieldValue(indexable);
            }
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


        public string FieldName { get; set; }
        public string ReturnType { get; set; }

        public bool IsTextField(Sitecore.Data.Fields.Field field)
        {
            return DatasourceIndexerHelper.TextFieldTypes.Contains(field.Type);
        }
    }
}