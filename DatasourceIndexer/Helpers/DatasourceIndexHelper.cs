using System.Collections.Generic;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace DatasourceIndexer.Helpers
{
    public static class DatasourceIndexerHelper
    {
        /// <summary>
        /// Coming from http://www.techphoria414.com/Blog/2013/November/Sitecore-7-Computed-Fields-All-Templates-and-Datasource-Content, 
        /// Hashet that determine which field type we take
        /// </summary>
        public static HashSet<string> TextFieldTypes = new HashSet<string>(new[]
        {
            "Single-Line Text", 
            "Rich Text", 
            "Multi-Line Text", 
            "text", 
            "rich text", 
            "html", 
            "memo", 
            "Word Document"
        });


        /// <summary>
        /// Return the fields (as item) that are set on the Datasource Template of a Sublayout
        /// </summary>
        /// <param name="item"></param>
        /// <param name="database"></param>
        /// <returns></returns>
        public static List<Item> GetFieldsOfASublayoutItem(Item item, Database database)
        {
            List<Item> sourceField = null;
            if (!string.IsNullOrEmpty(item[Constants.DatasourceTemplateFieldID]))
            {
                //We get the templateId if it's set
                var firstTemplateID = database.GetItem(item[Constants.DatasourceTemplateFieldID]).ID;
                if (!firstTemplateID.IsNull)
                {
                    sourceField = RetrieveFieldItem(database, firstTemplateID);
                }
            }
            return sourceField;
        }

        /// <summary>
        /// Return the fields displayname of a given Item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="database"></param>
        /// <returns></returns>
        public static List<string> GetFieldNameFromItem(Item item, Database database)
        {
            List<string> sourceField = new List<string>();

            var firstTemplateID = item.TemplateID;
            if (!firstTemplateID.IsNull)
            {
                sourceField = RetrieveFieldItem(database, firstTemplateID).Select(i => i.DisplayName).ToList();
            }
            return sourceField;
        }


        private static List<Item> RetrieveFieldItem(Database database, ID templateId)
        {
            List<Item> sourceField = new List<Item>();
            List<TemplateItem> templateItems = new List<TemplateItem>();
            // We retrieve him as a TemplateItem
            var firstTemplate = database.GetTemplate(templateId);
            if (firstTemplate != null)
            {
                // We add to the list the firstTemplate and his BaseTemplate to retrieve all the field.
                templateItems.Add(firstTemplate);
                templateItems.AddRange(firstTemplate.BaseTemplates);

                foreach (Item template in templateItems)
                {
                    //We retrieve each section
                    var sections =
                        template.Children.Where(
                            o => o.TemplateID.Equals(Constants.TemplateSectionsID));
                    // Foreach sections, we will take the field except the standard field ("__") 
                    foreach (var section in sections.Distinct(new ItemEqualityComparer()))
                    {
                        foreach (Item fieldItem in section.Children.Where(o => !o.Name.StartsWith("__") && TextFieldTypes.Contains(o[Constants.TypeFieldID])))
                        {
                            sourceField.Add(fieldItem);
                        }
                    }
                }
            }
            return sourceField;
        } 

    }
}