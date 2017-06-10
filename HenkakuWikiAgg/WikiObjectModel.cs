using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HenkakuWikiAgg
{
   class Module
   {
      public string Name { get; set; }
      public string Version { get; set; }
      public string NID { get; set; }

      public override string ToString()
      {
         return string.Format("{0} {1} {2}", Name, Version, NID);
      }
   }

   class Library
   {
      public string Name { get; set; }
      public string Version { get; set; }
      public string NID { get; set; }

      public override string ToString()
      {
         return string.Format("{0} {1} {2}", Name, Version, NID);
      }
   }

   class Function
   {
      public string Name { get; set; }
      public string Version { get; set; }
      public string NID { get; set; }

      public string Source { get; set; }

      public override string ToString()
      {
         return string.Format("{0} {1} {2}", Name, Version, NID);
      }
   }

   class ModuleDesc
   {
      public Module Module { get; set; }
      public List<Library> Libraries { get; set; }
      public Dictionary<string, List<Function>> LibraryFunctions { get; set; }
   }
}
