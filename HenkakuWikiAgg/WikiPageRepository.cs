using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace HenkakuWikiAgg
{
   class WikiPageRepository
   {
      static Dictionary<int, string> ListToDict(List<string> linesRaw)
      {
         var lines = new Dictionary<int, string>();
         for (int i = 0; i < linesRaw.Count(); i++)
         {
            lines.Add(i, linesRaw[i]);
         }
         return lines;
      }

      static void PrintTable(Table table)
      {
         if (table.TableData.Count == 0)
            return;

         //Console.WriteLine(string.Join(" | ", table.First().Select(e=>e.Key)));

         foreach (var row in table.TableData)
         {
            //Console.WriteLine(string.Join(" | ", row.Select(e => e.Value)));
         }
      }

      static Source ParseSource(List<string> sourceData)
      {
         var text = string.Join("\n", sourceData);
         var idx = text.IndexOf(">");
         text = text.Substring(idx + 1);
         idx = text.IndexOf("</");
         text = text.Substring(0, idx);
         return new Source() { Text = text };
      }

      //parses table
      static Table ParseTable(List<string> tableData)
      {
         //remove table open
         if (tableData.First().StartsWith("{| class"))
            tableData.RemoveAt(0);

         //remove table close
         if (tableData.Last() == "|}")
            tableData.Remove("|}");

         var text = string.Join("", tableData).Trim();

         var rowsRaw = Regex.Split(text, @"\|\-").Select(e => e.Trim()).ToList();

         var columnNames = new List<string>();

         var styledHeader = rowsRaw.Where(e => e.StartsWith("| style=")).FirstOrDefault();
         if (styledHeader == null)
         {
            var header = rowsRaw.Where(e => e.StartsWith("!")).First().TrimStart('!').Trim();

            if (header.IndexOf("!!") < 0)
            {
               if (header.IndexOf("!") < 0)
               {
                  if (header.IndexOf("|") < 0)
                  {
                     if (header.IndexOf("|") < 0)
                     {
                        var prevColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(string.Format("Error: Invalid table header in {0}", header));
                        Console.ForegroundColor = prevColor;

                        return new Table() { TableData = new List<Dictionary<string, string>>() };
                     }
                     else
                     {
                        columnNames = Regex.Split(header, @"\|").Select(e => e.Trim()).ToList();
                     }
                  }
                  else
                  {
                     columnNames = Regex.Split(header, @"\|\|").Select(e => e.Trim()).ToList();
                  }
               }
               else
               {
                  columnNames = Regex.Split(header, @"\!").Select(e => e.Trim()).ToList();
               }
            }
            else
            {
               columnNames = Regex.Split(header, @"\!\!").Select(e => e.Trim()).ToList();
            }

            rowsRaw.RemoveAt(0); //remove column names
         }
         else
         {
            var header = rowsRaw.Where(e => e.StartsWith("| style=")).First().Substring(8).Trim();
            var columnNamesRaw = Regex.Split(header, @"style\=").Select(e => e.Trim()).ToList();
            columnNames = columnNamesRaw.Select(e => e.Split('|').ElementAt(1).Trim().Trim('\'').Trim()).ToList();

            rowsRaw.RemoveAt(0); //remove column names
         }

         var rows = rowsRaw.Where(e => e.StartsWith("|")).Select(e => e.TrimStart('|').Trim()).ToList();
         rows = rows.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();

         var columnData = rows.Select(r =>
         {
            if (r.IndexOf("||") < 0)
            {
               if (r.IndexOf("|") < 0)
               {
                  if (columnNames.Count > 1)
                  {
                     var prevColor = Console.ForegroundColor;
                     Console.ForegroundColor = ConsoleColor.Red;
                     Console.WriteLine(string.Format("Error: Invalid table row in {0}", r));
                     Console.ForegroundColor = prevColor;

                     return new List<string>() { r.Trim() }; //invalid delimiter
                  }
                  else
                  {
                     return new List<string>() { r.Trim() }; //single item in row
                  }
               }
               else
               {
                  return Regex.Split(r, @"\|").Select(e => e.Trim()).ToList();
               }
            }
            else
            {
               return Regex.Split(r, @"\|\|").Select(e => e.Trim()).ToList();
            }
         }
            ).ToList();

         var transRows = new List<Dictionary<string, string>>();
         for (int j = 0; j < columnData.Count; j++)
         {
            var row = new Dictionary<string, string>();
            for (int i = 0; i < columnNames.Count; i++)
            {
               if (i < columnData[j].Count)
               {
                  row[columnNames[i]] = columnData[j][i];
               }
               else
               {
                  var prevColor = Console.ForegroundColor;
                  Console.ForegroundColor = ConsoleColor.Red;
                  Console.WriteLine(string.Format("Error: Missing column {0} in {1}", columnNames[i], string.Join(" | ", columnData[j])));
                  Console.ForegroundColor = prevColor;

                  row[columnNames[i]] = string.Empty;
               }
            }
            transRows.Add(row);
         }

         return new Table() { TableData = transRows };
      }

      static SectionHeader ParseHeader(string header)
      {
         int level = 0;

         for (int i = 0; i < header.Length; i++)
         {
            if (header[i] != '=')
               break;
            level++;
         }

         var part = header.Substring(level);
         var nameRaw = part.Substring(0, part.IndexOf("="));
         var name = nameRaw.Trim();
         return new SectionHeader() { Level = level, Name = name };
      }

      static Category ParseCategory(string category)
      {
         return new Category() { Text = category };
      }

      static WikiFile ParseFile(string file)
      {
         return new WikiFile() { Text = file };
      }

      static Curly ParseCurly(List<string> curly)
      {
         var text = string.Join("\n", curly);
         return new Curly() { Text = text };
      }

      static PlainText ParseText(string text)
      {
         return new PlainText() { Text = text };
      }

      static ListItem ParseListItem(string text)
      {
         return new ListItem() { Text = text };
      }

      static List<string> KnownWords = new List<string>()
      {
         #region
         "The", "Version", 
         "Applications",
         "Most",
         "Since",
         "Perhaps",
         "Syscall",
         "See",
         "It",
         "Finally,",
         "This",
         "If",
         "Comments",
         "Conditionals",
         "Example:",
         "Known",
         "Any",
         "During",
         "Upon",
         "In",
         "First,",
         "Then",
         "There",
         "Key",
         "Emmc",
         "These",
         "Each",
         "A",
         "Parses",
         "Set",
         "Decrypt",
         "Decrypts",
         "Used",
         "Verify",
         "Get",
         "Check",
         "Introduced",
         "Returns",
         "Seems",
         "Encrypt",
         "Return",
         "check",
         "Supported",
         "Original",
         "Result",
         "New",
         "Request:",
         "Response:",
         "Communication",
         "To",
         "Load",
         "Suspend",
         "Kill",
         "Force",
         "Unused",
         "When",
         "Command",
         "Once",
         "Possible",
         "ARM",
         "At",
         "Next",
         "Game",
         "Card",
         "CMD56",
         "Second",
         "Part1:",
         "Part2:",
         "Initialization",
         "Re-encrypt",
         "Unused.",
         "For",
         "TODO:",
         "Registration",
         "More",
         "According",
         "Another",
         "As",
         "Memory",
         "After",
         "An",
         "Flags",
         "Below",
         "On",
         "HENkaku",
         "For",
         "Molecule",
         "Sri",
         "Buffer:",
         "SHA256",
         "Unfortunately,",
         "Systemdata",
         "Preinst",
         "Debug",
         "Package",
         "Some",
         "Individual",
         "Devices",
         "Inside",
         "PVF",
         "SCE",
         "Header",
         "Repeating",
         "Sections",
         "Offset",
         "Relocations",
         "Segment",
         "Symbol",
         "Address",
         "P",
         "S",
         "Loads",
         "Example",
         "Though",
         "derived",
         "Installs",
         "Where",
         "System",
         "User",
         "Secure",
         "SMC",
         "IRQs",
         "FIQs",
         "should",
         "returns",
         "First",
         "Third",
         "Forth",
         "Numeric",
         "Aliases",
         "Implementation",
         "if",
         "Typically",
         "However",
         "operation",
         "calls",
         "Call",
         "Derived",
         "Implementation",
         "Pid",
         "thread",
         "Vfs",
         "Here",
         "arguments",
         "Enables",
         "Note",
         "Calls",
         "Obtains",
         "Here's",
         "Note",
         "was",
         "I",
         "Related",
         "Find",
         "Search",
         "PS",
         "Is",
         "Gets",
         "Deletes",
         "Updates",
         "Same",
         "Clears",
         "Called",
         "Note:",
         "Priority",
         "this",
         "it",
         "these",
         "some",
         "there",
         "SceSdstor",
         "Based",
         "Looks",
         "All",
         "Unrestricted",
         "Permission",
         "Changes",
         "Retrieves",
         "sm_comm_context",
         "f00d_resp",
         "gc_param",
         "Layout",
         "or",
         "Disables",
         "Restores",
         "Crypto",
         "Libraries",
         "adds",
         "Provides",
         "While",
         "Curiously,",
         "Will",
         "By",
         "Ideally,",
         "Because",
         "Update",
         "Bits",
         "Taken",
         "Title",
         "There's",
         "might",
         "reads",
         "processes",
         "then",
         "bad",
         "they",
         "Fixed",
         "By",
         "dlmalloc,",
         "Only",
         "Before",
         "Pass",
         "[[Memory]]",
         "[[SceSblGcAuthMgr|SceSblGcAuthMgr]]",
         "<references/>",
         "[[SceSysmem]]",
         "Buffer"
         #endregion
      };

      //filter normal text
      static bool IsKnownTextStart(string text)
      {
         int idx = text.IndexOf(" ");
         if (idx < 0)
         {
            return KnownWords.Contains(text);
         }
         else
            return KnownWords.Contains(text.Substring(0, idx));
      }

      //parses section of the page
      static Section ParseSection(List<string> sectionData, bool NoHeader)
      {
         var sectionObj = new Section();

         if (NoHeader)
            sectionData.Insert(0, "== No Header ==");

         var header = ParseHeader(sectionData.First());
         sectionObj.Header = header;

         //Console.WriteLine(string.Format("section:{0} {1}", header.Level, header.Name));

         var section = ListToDict(sectionData);

         for (int i = 1; i < section.Count(); i++)
         {
            if (string.IsNullOrWhiteSpace(section[i]))
               continue;

            //beginning of table
            if (section[i].StartsWith("{| class=\"wikitable\"") ||
                section[i].StartsWith("{| class=\'wikitable\'") ||
                section[i].StartsWith("{| class=\"wikitable sortable\""))
            {
               int tableStart = i;
               int tableEnd = 0;

               for (int j = i + 1; j < section.Count(); j++, i++)
               {
                  //end of table
                  if (section[j].StartsWith("|}"))
                  {
                     tableEnd = j;
                     j++; i++;
                     break;
                  }
               }

               var tableData = section.Where(e => e.Key >= tableStart && e.Key <= tableEnd).Select(e => e.Value).ToList();
               var tableRows = ParseTable(tableData);
               sectionObj.Items.Add(tableRows);

               if (tableRows.TableData.Count > 0)
               {
                  PrintTable(tableRows);
               }
               else
               {
                  var prevColor = Console.ForegroundColor;
                  Console.ForegroundColor = ConsoleColor.Red;
                  Console.WriteLine(string.Format("Error: Empty table {0}", header));
                  Console.ForegroundColor = prevColor;
               }
            }
            //beginning of source
            else if (section[i].StartsWith("<source "))
            {
               int sourceStart = i;
               int sourceEnd = 0;

               for (int j = i; j < section.Count(); j++, i++)
               {
                  //end of table
                  if (section[j].EndsWith("/source>"))
                  {
                     sourceEnd = j;
                     j++; i++;
                     break;
                  }
               }

               var sourceData = section.Where(e => e.Key >= sourceStart && e.Key <= sourceEnd).Select(e => e.Value).ToList();
               var source = ParseSource(sourceData);
               sectionObj.Items.Add(source);
               //Console.WriteLine(source.Text);
            }
            else if (section[i].StartsWith("[[Category:"))
            {
               var cat = ParseCategory(section[i]);
               sectionObj.Items.Add(cat);
            }
            else if (section[i].StartsWith("[[File:"))
            {
               var file = ParseFile(section[i]);
               sectionObj.Items.Add(file);
            }
            else if (section[i].StartsWith("{{"))
            {
               int curlyStart = i;
               int curlyEnd = 0;

               for (int j = i; j < section.Count(); j++, i++)
               {
                  //end of table
                  if (section[j].EndsWith("}}"))
                  {
                     curlyEnd = j;
                     j++; i++;
                     break;
                  }
               }

               var curlyData = section.Where(e => e.Key >= curlyStart && e.Key <= curlyEnd).Select(e => e.Value).ToList();
               var curly = ParseCurly(curlyData);
               sectionObj.Items.Add(curly);
            }
            else if (section[i].StartsWith("*") || section[i].StartsWith("#"))
            {
               var li = ParseListItem(section[i]);
               sectionObj.Items.Add(li);
            }
            else
            {
               if (IsKnownTextStart(section[i]))
               {
                  var txt = ParseText(section[i]);
                  sectionObj.Items.Add(txt);
               }
               else
               {
                  var prevColor = Console.ForegroundColor;
                  Console.ForegroundColor = ConsoleColor.Red;
                  Console.WriteLine(string.Format("Error: Found unknown content at: {0} {1}", i, section[i]));
                  Console.ForegroundColor = prevColor;

                  var uc = new Unclassified() { Text = section[i] };
                  sectionObj.Items.Add(uc);
               }
            }
         }

         return sectionObj;
      }

      static Redirect ParseRedirect(string redirect)
      {
         return new Redirect() { Direction = redirect };
      }

      //parses specific page
      static Page ParseWikiText(string pageTitle, string wikiText)
      {
         var pageObj = new Page();

         var linesRaw = wikiText.Split('\n').Select(e => e.Trim('\r')).ToList();

         var lines = ListToDict(linesRaw);

         var sectionIndexes = new List<int>();

         foreach (var line in lines)
         {
            if (line.Value.StartsWith("="))
               sectionIndexes.Add(line.Key);
         }

         sectionIndexes.Add(lines.Count());

         if (sectionIndexes.Count == 1)
         {
            var text = lines.First().Value;
            if (text.StartsWith("#REDIRECT"))
            {
               var rd = ParseRedirect(text);
               pageObj.Items.Add(rd);

               return pageObj;
            }
            else
            {
               var prevColor = Console.ForegroundColor;
               Console.ForegroundColor = ConsoleColor.Red;
               Console.WriteLine(string.Format("Error: Section layout is missing {0}", pageTitle));
               Console.ForegroundColor = prevColor;

               var sec = ParseSection(lines.Select(e => e.Value).ToList(), true);
               pageObj.Items.Add(sec);

               return pageObj;
            }
         }

         for (int i = 0; i < sectionIndexes.Count() - 1; i++)
         {
            var sectionLines = lines.Where(e => e.Key >= sectionIndexes[i] && e.Key < sectionIndexes[i + 1]).Select(e => e.Value).ToList();

            var sec = ParseSection(sectionLines, false);
            pageObj.Items.Add(sec);
         }

         return pageObj;
      }

      static List<IWikiItem> ParsePageLines(List<string> lines)
      {
         var pageObjList = new List<IWikiItem>();

         var json = string.Join("", lines.Select(e => e.Replace("\\n", "\r\n")).ToArray());

         dynamic root = null;
         try
         {
            root = JObject.Parse(json);
         }
         catch (Exception e)
         {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(string.Format("Error: Failed to parse json {0}", json.Substring(0, 100)));
            Console.ForegroundColor = prevColor;

            return pageObjList;
         }

         foreach (var page in root.query.pages)
         {
            var name = page.Name.ToString();
            var title = page.Value.title.ToString();
            Console.WriteLine(string.Format("page id: {0}", name));
            Console.WriteLine(string.Format("page title: {0}", title));

            var phObj = new PageHeader() { Id = int.Parse(name), Title = title };

            var revisions = page.Value.revisions;

            if (revisions == null)
            {
               var prevColor = Console.ForegroundColor;
               Console.ForegroundColor = ConsoleColor.Red;
               Console.WriteLine(string.Format("Error: Revision is missing for page {0}", title));
               Console.ForegroundColor = prevColor;

               continue;
            }

            foreach (var rev in revisions)
            {
               foreach (var item in rev)
               {
                  if (item.Name == "*")
                  {
                     var pageObj = ParseWikiText(title, item.Value.Value);
                     pageObj.Header = phObj;
                     pageObjList.Add(pageObj);
                  }
                  else
                  {
                     //Console.WriteLine(string.Format("{0}:{1}", item.Name, item.Value));
                  }
               }
            }
         }

         return pageObjList;
      }

      static Dictionary<int, string> ParsePageListLines(List<string> lines)
      {
         var json = string.Join("", lines.Select(e => e.Replace("\\n", "\r\n")).ToArray());

         dynamic root = JObject.Parse(json);

         var pages = new Dictionary<int, string>();

         foreach (var page in root.query.allpages)
         {
            var pageid = page.pageid.ToString();
            var title = page.title = page.title.ToString();

            //Console.WriteLine(string.Format("page id: {0}", pageid));
            //Console.WriteLine(string.Format("page title: {0}", title));
            pages[int.Parse(pageid)] = title;
         }

         return pages;
      }

      static List<string> ExecuteUrl(string url)
      {
         var request = (HttpWebRequest)WebRequest.Create(url);
         var response = (HttpWebResponse)request.GetResponse();
         var objStream = response.GetResponseStream();

         var lines = new List<string>();

         using (StreamReader objReader = new StreamReader(objStream))
         {
            var line = string.Empty;

            while ((line = objReader.ReadLine()) != null)
               lines.Add(line);
         }

         return lines;
      }

      static List<string> GetPageLines(string pageName)
      {
         string url = string.Format("https://wiki.henkaku.xyz/vita/api.php?action=query&format=json&prop=revisions&titles={0}&rvprop=content", pageName);

         return ExecuteUrl(url);
      }

      static List<string> GetListOfPages(int limit)
      {
         string url = string.Format("https://wiki.henkaku.xyz/vita/api.php?action=query&format=json&list=allpages&aplimit={0}", limit);

         return ExecuteUrl(url);
      }

      static List<string> ExcludePageNames = new List<string>()
      {
         #region
         "Boot Sequence",
         "F00D Processor",
         "Game Card",
         "Id.dat",
         "Kernel",
         "Kernel Loader",
         "Main Page/Navigation",
         "Molecule",
         "PCH-1XXX",
         "PSVIMG",
         "PVF",
         "Physical Memory",
         "SELF",
         "SceKernelBootimage",
         "Keys",
         "Main Page",
         "Main Page/Header",
         "Main Page/News",
         "Module:Arguments",
         "SCE",
         "Syscon Update",
         "Sysroot",
         "Title Updates",
         "Ux0:id.dat",
         "Vita RPC",
         "Vulnerabilities",
         "EHCI",
         "Updater",
         "Accessory Port",
         "Act.dat",
         "Boot Chain",
         "Boot ROM",
         "CXD5315GG",
         "Clocks",
         "Concurrency",
         "Cortex A9",
         "Debugger Interface",
         "Dmac5",
         "EMMC",
         "Error codes",
         "F00D Commands",
         "File Management",
         "GPIO Registers",
         "I2C",
         "I2C Registers",
         "Interrupts",
         "Keystone",
         "Libraries",
         "LiveArea",
         "Main Processor",
         "Memory Card",
         "NPXS10000",
         "NPXS10002",
         "NPXS10003",
         "NPXS10004",
         "NPXS10005",
         "NPXS10006",
         "NPXS10008",
         "NPXS10009",
         "NPXS10010",
         "NPXS10012",
         "NPXS10014",
         "NPXS10015",
         "NPXS10017",
         "NPXS10021",
         "NPXS10023",
         "NPXS10025",
         "NPXS10026",
         "NPXS10027",
         "NPXS10028",
         "NPXS10031",
         "NPXS10037",
         "NPXS10074",
         "NPXS10081",
         "PSM",
         "PSN",
         "PSP Emulator",
         "PSVMD",
         "PUP",
         "Packages",
         "Pervasive",
         "Registry",
         "SELF Loading",
         "SLB2",
         "SMC",
         "SPI Registers",
         "SVC",
         "Secure Bootloader",
         "Sealedkey",
         "Secure World",
         "Service Calls",
         "TrustZone",
         "Virtual Memory",
         "VitaInjector",
         "Web Browser",
         "Venezia",
         "ARZL",
         "Applications",
         "GPU",
         "Modules",
         "Partitions",
         "SCECAF",
         "Security",
         "Suspend",
         "Syscalls",
         "System Software",
         
         //not module pages
         "SceCuiSetUpper",
         "SceKblForKernel",
         "ScePervasiveBaseClk",
         "ScePsp2BootConfig",
         "ScePsp2Swu",
         "SceSblUpdateMgr",
         "SceStoreBrowser",
         "SceSysStateMgr",
         #endregion
      };

      static void ExcludePages(Dictionary<int, string> pages)
      {
         foreach (var name in ExcludePageNames)
         {
            pages.Remove(pages.First(e => e.Value == name).Key);
         }
      }

      public static List<IWikiItem> GetPages()
      {
         var pageList = GetListOfPages(1000);
         var pages = ParsePageListLines(pageList);

         //exclude pages that not needed but cause some warnings with txt content
         ExcludePages(pages);

         //Console.WriteLine("---------");

         var pageObjects = new List<IWikiItem>();

         foreach (var page in pages)
         {
            var lines = GetPageLines(page.Value);
            var pos = ParsePageLines(lines);
            pageObjects.AddRange(pos);
         }

         return pageObjects;
      }

      public static List<IWikiItem> GetPage(string pageName)
      {
         var pageObjects = new List<IWikiItem>();

         var lines = GetPageLines(pageName);
         var pos = ParsePageLines(lines);
         pageObjects.AddRange(pos);

         return pageObjects;
      }
   }
}
