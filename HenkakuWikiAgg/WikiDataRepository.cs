using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HenkakuWikiAgg
{
   class WikiDataRepository
   {
      static Tuple<int, int> FindMinMaxLevel(List<IWikiItem> pages)
      {
         var levels = new List<int>();

         foreach (var ipage in pages)
         {
            var page = ipage as Page;

            foreach (var ipageItem in page.Items)
            {
               switch (ipageItem.GetWikiType)
               {
                  case WikiItemTypes.Redirect:
                     break;
                  case WikiItemTypes.Section:
                     {
                        var section = ipageItem as Section;
                        levels.Add((section.Header as SectionHeader).Level);
                     }
                     break;
               }
            }
         }

         var min = levels.Min();
         var max = levels.Max();
         return new Tuple<int, int>(min, max);
      }

      static Tuple<int, int> FindMinMaxLevelinPage(IWikiItem ipage)
      {
         var levels = new List<int>();

         var page = ipage as Page;

         foreach (var ipageItem in page.Items)
         {
            switch (ipageItem.GetWikiType)
            {
               case WikiItemTypes.Redirect:
                  break;
               case WikiItemTypes.Section:
                  {
                     var section = ipageItem as Section;
                     levels.Add((section.Header as SectionHeader).Level);
                  }
                  break;
            }
         }

         if (!levels.Any())
            return new Tuple<int, int>(-1, -1);

         var min = levels.Min();
         var max = levels.Max();
         return new Tuple<int, int>(min, max);
      }

      const int sectionRootLevel = 2;

      static void MergeSections(List<IWikiItem> pages)
      {
         foreach (var ipage in pages)
         {
            var page = ipage as Page;

            var minMax = FindMinMaxLevelinPage(ipage);

            if (minMax.Item1 != 2 || minMax.Item2 != 3)
            {
               var prevColor = Console.ForegroundColor;
               Console.ForegroundColor = ConsoleColor.Red;
               Console.WriteLine(string.Format("Error in hierarchy: Skip page {0}", (page.Header as PageHeader).Title));
               Console.ForegroundColor = prevColor;

               continue;
            }

            Section currentSection = null;

            for (int i = 0; i < page.Items.Count; i++)
            {
               var ipageItem = page.Items[i];

               switch (ipageItem.GetWikiType)
               {
                  case WikiItemTypes.Redirect:
                     break;
                  case WikiItemTypes.Section:
                     {
                        var sec = ipageItem as Section;
                        var secHeader = sec.Header as SectionHeader;
                        if (secHeader.Level == sectionRootLevel)
                        {
                           currentSection = sec;
                        }
                        else if (secHeader.Level > sectionRootLevel)
                        {
                           if (currentSection == null)
                           {
                              throw new Exception("Unexpected");
                           }
                           else
                           {
                              currentSection.Items.Add(sec);
                              page.Items[i] = null;
                           }
                        }
                        else
                        {
                           throw new Exception("Unexpected");
                        }
                     }
                     break;
                  default:
                     throw new InvalidDataException("Wrong item");
               }
            }

            page.Items = page.Items.Where(e => e != null).ToList();
         }
      }

      static float TryParseVersionFloat(string page, string section, string ver, float def)
      {
         try
         {
            return float.Parse(ver);
         }
         catch (Exception e)
         {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(string.Format("Problem parsing version {0} page {1} section {2}", ver, page, section));
            Console.ForegroundColor = prevColor;

            return def;
         }
      }

      static Tuple<string, string> AggGenericNIDTable2(string page, string section, Table table)
      {
         if (!table.TableData.Any())
            return new Tuple<string, string>(string.Empty, string.Empty);

         var versionCol = table.TableData.First().FirstOrDefault(e => e.Key == "Version");
         if (string.IsNullOrEmpty(versionCol.Key))
            throw new Exception("Version column not found");

         var maxVer = table.TableData.Select(e => TryParseVersionFloat(page, section, e["Version"], 0)).Max().ToString();

         var maxVerModule = table.TableData.FirstOrDefault(e => e["Version"].Contains(maxVer)); //contains instead of == because 3.60 gets 0 dropped after float parse

         var nidCol = table.TableData.First().FirstOrDefault(e => e.Key == "NID");
         if (string.IsNullOrEmpty(nidCol.Key))
            throw new Exception("NID column not found");

         //select first item if problem with version parsing
         if (maxVerModule == null)
            maxVerModule = table.TableData.First();

         var obj = new Tuple<string, string>(maxVerModule["Version"], maxVerModule["NID"]);
         return obj;
      }

      static List<Tuple<string, string, string>> AggGenericNIDTable3(string page, string section, Table table)
      {
         if (!table.TableData.Any())
            return new List<Tuple<string, string, string>>();

         var nameCol = table.TableData.First().FirstOrDefault(e => e.Key == "Name");
         if (string.IsNullOrEmpty(nameCol.Key))
            throw new Exception("Name column not found");

         //aggregate modules by name
         var byName = new Dictionary<string, List<Dictionary<string, string>>>();
         foreach (var entry in table.TableData)
         {
            if (!byName.ContainsKey(entry["Name"]))
               byName[entry["Name"]] = new List<Dictionary<string, string>>();

            byName[entry["Name"]].Add(entry);
         }

         var objList = new List<Tuple<string, string, string>>();

         foreach (var module in byName)
         {
            var versionCol = module.Value.First().FirstOrDefault(e => e.Key == "Version");
            if (string.IsNullOrEmpty(versionCol.Key))
               throw new Exception("Version column not found");

            var maxVer = module.Value.Select(e => TryParseVersionFloat(page, section, e["Version"], 0)).Max().ToString();

            var maxVerModule = module.Value.First(e => e["Version"].Contains(maxVer)); //contains instead of == because 3.60 gets 0 dropped after float parse

            var nidCol = module.Value.First().FirstOrDefault(e => e.Key == "NID");
            if (string.IsNullOrEmpty(nidCol.Key))
               throw new Exception("NID column not found");

            //select first item if problem with version parsing
            if (maxVerModule == null)
               maxVerModule = table.TableData.First();

            var obj = new Tuple<string, string, string>(maxVerModule["Name"], maxVerModule["Version"], maxVerModule["NID"]);
            objList.Add(obj);
         }

         return objList;
      }

      static List<Module> AggModuleTable(string page, string section, Table table)
      {
         return AggGenericNIDTable3(page, section, table).Select(e => new Module()
         {
            Name = e.Item1,
            Version = e.Item2,
            NID = e.Item3
         }).ToList();
      }

      static string ExtractNameFromLink(string link)
      {
         if (link.Contains("[") && link.Contains("|"))
         {
            var name = link.Trim('[').Trim(']').Split('|')[1];
            return name;
         }
         else
         {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(string.Format("Error: Missing link in {0}", link));
            Console.ForegroundColor = prevColor;

            return link;
         }
      }

      static List<Library> AggLibraryTable(string page, string section, Table table)
      {
         var libraries = AggGenericNIDTable3(page, section, table).Select(e => new Library()
         {
            Name = e.Item1,
            Version = e.Item2,
            NID = e.Item3
         }).ToList();

         //fix library names
         foreach (var library in libraries)
         {
            library.Name = ExtractNameFromLink(library.Name);
         }

         return libraries;
      }

      static Function AggFunctionTable(string page, string section, Table table)
      {
         var raw = AggGenericNIDTable2(page, section, table);
         return new Function()
         {
            Version = raw.Item1,
            NID = raw.Item2
         };
      }

      static List<ModuleDesc> FindNidTables(List<IWikiItem> pages)
      {
         var moduleList = new List<ModuleDesc>();

         foreach (var ipage in pages)
         {
            var page = ipage as Page;

            List<Module> modules = null;
            List<Library> libraries = null;

            var libraryFunctions = new Dictionary<string, List<Function>>();

            foreach (var ipageItem in page.Items)
            {
               switch (ipageItem.GetWikiType)
               {
                  case WikiItemTypes.Redirect:
                     break;
                  case WikiItemTypes.Section:
                     {
                        var section = ipageItem as Section;

                        var header = section.Header as SectionHeader;

                        if (header.Name == "Module")
                        {
                           foreach (var isectionItem in section.Items)
                           {
                              switch (isectionItem.GetWikiType)
                              {
                                 case WikiItemTypes.Section:
                                    {
                                       var lv3Section = isectionItem as Section;
                                       var lv3sHeader = lv3Section.Header as SectionHeader;
                                       if (lv3sHeader.Name == "Known NIDs")
                                       {
                                          foreach (var ilv3sItem in lv3Section.Items)
                                          {
                                             switch (ilv3sItem.GetWikiType)
                                             {
                                                case WikiItemTypes.Table:
                                                   {
                                                      modules = AggModuleTable((page.Header as PageHeader).Title, (lv3Section.Header as SectionHeader).Name, ilv3sItem as Table);

                                                      //foreach (var m in modules)
                                                      //   Console.WriteLine(m.ToString());
                                                   }
                                                   break;
                                             }
                                          }
                                       }
                                       else
                                       {
                                          throw new InvalidDataException("Unexpected header");
                                       }
                                    }
                                    break;
                              }
                           }
                        }
                        else if (header.Name == "Libraries")
                        {
                           foreach (var isectionItem in section.Items)
                           {
                              switch (isectionItem.GetWikiType)
                              {
                                 case WikiItemTypes.Section:
                                    {
                                       var lv3Section = isectionItem as Section;
                                       var lv3sHeader = lv3Section.Header as SectionHeader;
                                       if (lv3sHeader.Name == "Known NIDs")
                                       {
                                          foreach (var ilv3sItem in lv3Section.Items)
                                          {
                                             switch (ilv3sItem.GetWikiType)
                                             {
                                                case WikiItemTypes.Table:
                                                   {
                                                      libraries = AggLibraryTable((page.Header as PageHeader).Title, (lv3Section.Header as SectionHeader).Name, ilv3sItem as Table);

                                                      //foreach (var l in libraries)
                                                      //   Console.WriteLine(l.ToString());
                                                   }
                                                   break;
                                             }
                                          }
                                       }
                                       else
                                       {
                                          throw new InvalidDataException("Unexpected header");
                                       }
                                    }
                                    break;
                              }
                           }
                        }
                        else
                        {
                           if (libraries != null)
                           {
                              var libraryDescEntry = libraries.FirstOrDefault(e => e.Name == header.Name);
                              if (libraryDescEntry != null)
                              {
                                 //Console.WriteLine(libraryDescEntry.ToString());

                                 foreach (var isectionItem in section.Items)
                                 {
                                    switch (isectionItem.GetWikiType)
                                    {
                                       case WikiItemTypes.Section:
                                          {
                                             var lv3Section = isectionItem as Section;
                                             var lv3sHeader = lv3Section.Header as SectionHeader;

                                             //Console.WriteLine(lv3sHeader.Name);

                                             //function section items
                                             Function function = null;

                                             foreach (var ilv3sItem in lv3Section.Items)
                                             {
                                                switch (ilv3sItem.GetWikiType)
                                                {
                                                   case WikiItemTypes.Table:
                                                      {
                                                         //assign only first nid table and ignore anything else that goes after
                                                         if (function == null)
                                                         {
                                                            try
                                                            {
                                                               function = AggFunctionTable((page.Header as PageHeader).Title, (lv3Section.Header as SectionHeader).Name, ilv3sItem as Table);

                                                               function.Name = lv3sHeader.Name;

                                                               //Console.WriteLine(function.ToString());
                                                            }
                                                            catch (Exception e)
                                                            {
                                                               var prevColor = Console.ForegroundColor;
                                                               Console.ForegroundColor = ConsoleColor.Red;
                                                               Console.WriteLine(string.Format("Error: in table, skipping section {0} page {1}", (lv3Section.Header as SectionHeader).Name, (page.Header as PageHeader).Title));
                                                               Console.ForegroundColor = prevColor;
                                                            }
                                                         }
                                                      }
                                                      break;
                                                   case WikiItemTypes.Source:
                                                      {
                                                         if (function == null)
                                                         {
                                                            var prevColor = Console.ForegroundColor;
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine(string.Format("Error: skipping source without NID table in section {0} page {1}", (lv3Section.Header as SectionHeader).Name, (page.Header as PageHeader).Title));
                                                            Console.ForegroundColor = prevColor;
                                                         }
                                                         else
                                                         {
                                                            function.Source = (ilv3sItem as Source).Text.Trim();
                                                         }
                                                      }
                                                      break;
                                                }
                                             }

                                             //in some cases there can be table in section level 3
                                             //but it turns out it is some desctiption and not NID table
                                             //we need to ignore these
                                             if (function != null)
                                             {
                                                if (!libraryFunctions.ContainsKey(libraryDescEntry.Name))
                                                   libraryFunctions[libraryDescEntry.Name] = new List<Function>();

                                                libraryFunctions[libraryDescEntry.Name].Add(function);
                                             }
                                          }
                                          break;
                                    }
                                 }
                              }
                           }
                        }
                     }
                     break;
                  default:
                     throw new InvalidDataException("Wrong item");
               }
            }

            ModuleDesc md = new ModuleDesc()
            {
               Module = modules.First(),
               Libraries = libraries,
               LibraryFunctions = libraryFunctions
            };

            moduleList.Add(md);
         }

         return moduleList;
      }

      public static List<ModuleDesc> DataModelToObjectModel(List<IWikiItem> pages)
      {
         var pagesCopy = pages.ToList();
         MergeSections(pagesCopy);
         return FindNidTables(pagesCopy);
      }
   }
}
