using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HenkakuWikiAgg
{
   class WikiDataDumper
   {
      static string NormalizeNid(string nid)
      {
         if (nid.StartsWith("0x") || nid.StartsWith("0X"))
         {
            var num = nid.Substring(2).ToUpper();
            return string.Format("0x{0}", num);
         }
         else
         {
            //sometimes there is a text instead of nid
            try
            {
               int.Parse(nid, NumberStyles.HexNumber);

               var num = nid.ToUpper();
               return string.Format("0x{0}", num);
            }
            catch (Exception e)
            {
               return nid;
            }
         }
      }

      public static void DumpModuleDescriptionsAsYaml(List<ModuleDesc> moduleList)
      {
         Console.WriteLine("modules:");

         foreach (var module in moduleList)
         {
            Console.WriteLine(string.Format("  {0}", module.Module.Name));
            Console.WriteLine(string.Format("    nid: {0}", NormalizeNid(module.Module.NID)));
            Console.WriteLine("    libraries:");

            foreach (var library in module.Libraries)
            {
               Console.WriteLine(string.Format("      {0}", library.Name));
               Console.WriteLine("      functions:");

               if (module.LibraryFunctions.ContainsKey(library.Name))
               {
                  foreach (var function in module.LibraryFunctions[library.Name])
                  {
                     Console.WriteLine(string.Format("        {0}: {1}", function.Name, NormalizeNid(function.NID)));

                     //if(function.Source != null)
                     //   Console.WriteLine(function.Source);
                  }
               }

               Console.WriteLine(string.Format("      kernel:{0}", "?"));
               Console.WriteLine(string.Format("      nid:{0}", NormalizeNid(library.NID)));
            }
         }
      }

      public static void DumpModuleDescriptionsAsCpp(List<ModuleDesc> moduleList)
      {
         foreach (var module in moduleList)
         {
            Console.WriteLine(string.Format("// #################### {0} ####################\n", module.Module.Name));

            foreach (var library in module.Libraries)
            {
               Console.WriteLine(string.Format("// -------------------- {0} --------------------\n", library.Name));

               if (module.LibraryFunctions.ContainsKey(library.Name))
               {
                  foreach (var function in module.LibraryFunctions[library.Name])
                  {
                     if (function.Source != null)
                     {
                        Console.WriteLine(string.Format("// {0}\n", function.Name));

                        Console.WriteLine(function.Source);

                        Console.WriteLine();
                     }
                  }
               }
            }
         }
      }
   }
}
