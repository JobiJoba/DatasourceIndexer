using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using DatasourceIndexer.Helpers;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Resources;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Text;

namespace DatasourceIndexer.Field
{
    /// <summary>
    /// Multilist which take his source from the Datasource Template of a Sublayout
    /// </summary>
    public class MultiListDatasourceField : MultilistEx
    {

        protected override void GetSelectedItems(Item[] sources, out ArrayList selected, out IDictionary unselected)
        {
            Assert.ArgumentNotNull(sources, "sources");
            ListString listString = new ListString(this.Value);
            unselected = new SortedList(System.StringComparer.Ordinal);
            selected = new System.Collections.ArrayList(listString.Count);
            for (int i = 0; i < listString.Count; i++)
            {
                selected.Add(listString[i]);
            }
            for (int j = 0; j < sources.Length; j++)
            {
                Item item = sources[j];
                string item2 = item.ID.ToString();
                int num = listString.IndexOf(item2);
                if (num >= 0)
                {
                    selected[num] = item;
                }
                else
                {
                    unselected.Add(MainUtil.GetSortKey(item.Name), item);
                }
            }
        }

        /// <summary>
        /// Renders the control.
        /// </summary>
        /// <param name="output">The output.</param>
        protected override void DoRender(HtmlTextWriter output)
        {
            Assert.ArgumentNotNull(output, "output");
            Item item = Sitecore.Context.ContentDatabase.GetItem(this.ItemID);
            if (string.IsNullOrEmpty(item["Datasource Template"]))
            {
                output.Write("<br><b>You have to select a datasource Template to activate this MultiList</b></br>");
                return;
            }

            List<Item> sourceField = DatasourceIndexerHelper.GetFieldsOfASublayoutItem(item, Sitecore.Context.ContentDatabase).ToList();
            string text = string.Empty;

            System.Collections.ArrayList arrayList;
            System.Collections.IDictionary dictionary;
            this.GetSelectedItems(sourceField.ToArray(), out arrayList, out dictionary);
            base.ServerProperties["ID"] = this.ID;

            if (this.ReadOnly)
            {
                text = " disabled=\"disabled\"";
            }
            output.Write(string.Concat(new string[]
            {
                "<input id=\"",
                this.ID,
                "_Value\" type=\"hidden\" value=\"",
                StringUtil.EscapeQuote(this.Value),
                "\" />"
            }));
            output.Write("<table" + this.GetControlAttributes() + ">");
            output.Write("<tr>");
            output.Write("<td class=\"scContentControlMultilistCaption\" width=\"50%\">" + Translate.Text("All") + "</td>");
            output.Write("<td width=\"20\">" + Images.GetSpacer(20, 1) + "</td>");
            output.Write("<td class=\"scContentControlMultilistCaption\" width=\"50%\">" + Translate.Text("Selected") + "</td>");
            output.Write("<td width=\"20\">" + Images.GetSpacer(20, 1) + "</td>");
            output.Write("</tr>");
            output.Write("<tr>");
            output.Write("<td valign=\"top\" height=\"100%\">");
            output.Write(string.Concat(new string[]
            {
                "<select id=\"",
                this.ID,
                "_unselected\" class=\"scContentControlMultilistBox\" multiple=\"multiple\" size=\"10\"",
                text,
                " ondblclick=\"javascript:scContent.multilistMoveRight('",
                this.ID,
                "')\" onchange=\"javascript:document.getElementById('",
                this.ID,
                "_all_help').innerHTML=this.selectedIndex>=0?this.options[this.selectedIndex].innerHTML:''\" >"
            }));
            foreach (System.Collections.DictionaryEntry dictionaryEntry in dictionary)
            {
                Item item2 = dictionaryEntry.Value as Item;
                if (item2 != null)
                {
                    output.Write(string.Concat(new string[]
                    {
                        "<option value=\"",
                        this.GetItemValue(item2),
                        "\">",
                        item2.DisplayName,
                        "</option>"
                    }));
                }
            }
            output.Write("</select>");
            output.Write("</td>");
            output.Write("<td valign=\"top\">");
            this.RenderButton(output, "Core/16x16/arrow_blue_right.png", "javascript:scContent.multilistMoveRight('" + this.ID + "')");
            output.Write("<br />");
            this.RenderButton(output, "Core/16x16/arrow_blue_left.png", "javascript:scContent.multilistMoveLeft('" + this.ID + "')");
            output.Write("</td>");
            output.Write("<td valign=\"top\" height=\"100%\">");
            output.Write(string.Concat(new string[]
            {
                "<select id=\"",
                this.ID,
                "_selected\" class=\"scContentControlMultilistBox\" multiple=\"multiple\" size=\"10\"",
                text,
                " ondblclick=\"javascript:scContent.multilistMoveLeft('",
                this.ID,
                "')\" onchange=\"javascript:document.getElementById('",
                this.ID,
                "_selected_help').innerHTML=this.selectedIndex>=0?this.options[this.selectedIndex].innerHTML:''\">"
            }));
            for (int i = 0; i < arrayList.Count; i++)
            {
                Item item3 = arrayList[i] as Item;
                if (item3 != null)
                {
                    output.Write(string.Concat(new string[]
                    {
                        "<option value=\"",
                        this.GetItemValue(item3),
                        "\">",
                        item3.DisplayName,
                        "</option>"
                    }));
                }
                else
                {
                    string text2 = arrayList[i] as string;
                    if (text2 != null)
                    {
                        Item item4 = Sitecore.Context.ContentDatabase.GetItem(text2);
                        string text3;
                        if (item4 != null)
                        {
                            text3 = item4.DisplayName + ' ' + Translate.Text("[Not in the selection List]");
                        }
                        else
                        {
                            text3 = text2 + ' ' + Translate.Text("[Item not found]");
                        }
                        output.Write(string.Concat(new string[]
                        {
                            "<option value=\"",
                            text2,
                            "\">",
                            text3,
                            "</option>"
                        }));
                    }
                }
            }
            output.Write("</select>");
            output.Write("</td>");
            output.Write("<td valign=\"top\">");
            this.RenderButton(output, "Core/16x16/arrow_blue_up.png", "javascript:scContent.multilistMoveUp('" + this.ID + "')");
            output.Write("<br />");
            this.RenderButton(output, "Core/16x16/arrow_blue_down.png", "javascript:scContent.multilistMoveDown('" + this.ID + "')");
            output.Write("</td>");
            output.Write("</tr>");
            output.Write("<tr>");
            output.Write("<td valign=\"top\">");
            output.Write("<div style=\"border:1px solid #999999;font:8pt tahoma;padding:2px;margin:4px 0px 4px 0px;height:14px\" id=\"" + this.ID + "_all_help\"></div>");
            output.Write("</td>");
            output.Write("<td></td>");
            output.Write("<td valign=\"top\">");
            output.Write("<div style=\"border:1px solid #999999;font:8pt tahoma;padding:2px;margin:4px 0px 4px 0px;height:14px\" id=\"" + this.ID + "_selected_help\"></div>");
            output.Write("</td>");
            output.Write("<td></td>");
            output.Write("</tr>");
            output.Write("</table>");
        }

        protected void RenderButton(HtmlTextWriter output, string icon, string click)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(icon, "icon");
            Assert.ArgumentNotNull(click, "click");
            ImageBuilder builder = new ImageBuilder
            {
                Src = icon,
                Width = 0x10,
                Height = 0x10,
                Margin = "2px"
            };
            if (!ReadOnly)
            {
                builder.OnClick = click;
            }
            output.Write(builder.ToString());
        }


    }
}