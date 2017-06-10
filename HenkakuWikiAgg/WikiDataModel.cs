using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HenkakuWikiAgg
{
   enum WikiItemTypes
   {
      Table,
      Source,
      Curly,
      Category,
      PlainText,
      Redirect,
      SectionHeader,
      ListItem,
      Section,
      Unclassified,
      Page,
      PageHeader,
      WikiFile
   }

   abstract class IWikiItem
   {
      public abstract WikiItemTypes GetWikiType { get; }
   }

   class PageHeader : IWikiItem
   {
      public override WikiItemTypes GetWikiType { get { return WikiItemTypes.PageHeader; } }

      public int Id { get; set; }
      public string Title { get; set; }

      public override string ToString()
      {
         return Title;
      }
   }

   class Page : IWikiItem
   {
      public override WikiItemTypes GetWikiType { get { return WikiItemTypes.Page; } }

      public IWikiItem Header { get; set; }

      public List<IWikiItem> Items { get; set; }

      public Page()
      {
         Items = new List<IWikiItem>();
      }

      public override string ToString()
      {
         return Header.ToString();
      }
   }

   class Section : IWikiItem
   {
      public override WikiItemTypes GetWikiType { get { return WikiItemTypes.Section; } }

      public IWikiItem Header { get; set; }

      public List<IWikiItem> Items { get; set; }

      public Section()
      {
         Items = new List<IWikiItem>();
      }

      public override string ToString()
      {
         return Header.ToString();
      }
   }

   class Unclassified : IWikiItem
   {
      public override WikiItemTypes GetWikiType { get { return WikiItemTypes.Unclassified; } }

      public string Text { get; set; }
   }

   class SectionHeader : IWikiItem
   {
      public override WikiItemTypes GetWikiType { get { return WikiItemTypes.SectionHeader; } }

      public int Level { get; set; }
      public string Name { get; set; }

      public override string ToString()
      {
         return string.Format("{0}: {1}", Level, Name);
      }
   }

   class Category : IWikiItem
   {
      public override WikiItemTypes GetWikiType { get { return WikiItemTypes.Category; } }

      public string Text { get; set; }
   }

   class WikiFile : IWikiItem
   {
      public override WikiItemTypes GetWikiType { get { return WikiItemTypes.WikiFile; } }

      public string Text { get; set; }
   }

   class Redirect : IWikiItem
   {
      public override WikiItemTypes GetWikiType { get { return WikiItemTypes.Redirect; } }

      public string Direction { get; set; }
   }

   class PlainText : IWikiItem
   {
      public override WikiItemTypes GetWikiType { get { return WikiItemTypes.PlainText; } }

      public string Text { get; set; }
   }

   class Table : IWikiItem
   {
      public override WikiItemTypes GetWikiType { get { return WikiItemTypes.Table; } }

      public List<Dictionary<string, string>> TableData { get; set; }
   }

   class Source : IWikiItem
   {
      public override WikiItemTypes GetWikiType { get { return WikiItemTypes.Source; } }

      public string Text { get; set; }
   }

   class Curly : IWikiItem
   {
      public override WikiItemTypes GetWikiType { get { return WikiItemTypes.Curly; } }

      public string Text { get; set; }
   }

   class ListItem : IWikiItem
   {
      public override WikiItemTypes GetWikiType { get { return WikiItemTypes.ListItem; } }

      public string Text { get; set; }
   }
}
