using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HenkakuWikiAgg
{
   class Program
   {
      static void Main(string[] args)
      {
         //test any single page
         //var pages = WikiPageRepository.GetPage("SceAVConfig");

         var pages = WikiPageRepository.GetPages();
         
         var modules = WikiDataRepository.DataModelToObjectModel(pages);

         Console.WriteLine("---------------");

         WikiDataDumper.DumpModuleDescriptionsAsYaml(modules);

         Console.WriteLine("---------------");

         WikiDataDumper.DumpModuleDescriptionsAsCpp(modules);
      }
   }
}
