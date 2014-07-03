using System;
using System.Collections.Generic;

using System.Linq;

using System.Text.RegularExpressions;

using PolarDB;

namespace TripleIntClasses
{
   public class NameSpaceStore
   {
       public readonly Dictionary<string, int> Codes;
       public readonly List<string> NameSpaceStrings;
       private readonly PaCell stringsCell;
       private PxCell entitiesByNamespaceCell;
       private string path;
       public readonly Dictionary<string, string> namespacesByPrefix = new Dictionary<string, string>();

       public NameSpaceStore(string path)
       {
           this.path = path;
           stringsCell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.sstring)), path + "namespaces.pac", false);
           //entitiesByNamespaceCell= new PxCell(new PTypeSequence(new PTypeSequence(PTypeEnumeration)), path + "namespaces.pac", false);
           if(stringsCell.IsEmpty)
               stringsCell.Fill(new object[0]);
           NameSpaceStrings = new List<string>();
           Codes=new Dictionary<string, int>();
           foreach (string nsString in stringsCell.Root.ElementValues())
           {
               Codes.Add(nsString, Codes.Count);
               NameSpaceStrings.Add(nsString);
           }
           @type = GetShortFromFullOrPrefixed("<http://www.w3.org/1999/02/22-rdf-syntax-ns#type>");
           int t;
           if(Codes.TryGetValue(@"http://downlode.org/rdf/iso-3166/countries#", out t))
               Console.WriteLine(t);
       }

       public void Clear()
       {
           stringsCell.Clear();
           stringsCell.Fill(new object[0]);    
           Codes.Clear();
           NameSpaceStrings.Clear();
           namespacesByPrefix.Clear();
           @type = GetShortFromFullOrPrefixed("<http://www.w3.org/1999/02/22-rdf-syntax-ns#type>");
       
       }

       public string @type;

      

       public void Flush()
       {
           stringsCell.Clear();
           //NameSpaceStrings.Clear();
           //NameSpaceStrings.AddRange( Codes.Keys);    
           stringsCell.Fill(NameSpaceStrings.Cast<object>().ToArray()); 
       }   

       public string GetShortFromFullOrPrefixed(string line)
       {
           if (string.IsNullOrWhiteSpace(line)) return line;
           if (line[0] == '<')
           {
               var fullName = line.Substring(1, line.Length - 2);
               return FromFullName(fullName);
           }
           else//prefixed
           {
               int colon = line.IndexOf(':');
               if (colon == -1)
                   throw new Exception("Err split line to ns: " + line);
               string prefix = line.Substring(0, colon + 1);
               if (!namespacesByPrefix.ContainsKey(prefix))
                   throw new Exception("Err split line to ns: " + line);
               var ns = namespacesByPrefix[prefix];
               var shortName = line.Substring(colon + 1);
               return FromSplitted(ns, shortName);
           }
       }

       public string FromSplitted(string ns, string shortName)
       {
           return CombineNsCodeShortName(CodeNamspace(ns), shortName);
       }

       public string FromFullName(string fullName)
       {
           var lastIndexOfSplitSlash = fullName.LastIndexOf('/');
           var lastIndexOfSplitSharp = fullName.LastIndexOf('#');
           int lastIndexOfSplit = Math.Max(lastIndexOfSplitSharp, lastIndexOfSplitSlash);
           if (lastIndexOfSplit == -1)
           {
               Console.WriteLine("error split namespace " + fullName);
               throw new Exception("error split namespace");
           }

           var shortName = fullName.Substring(lastIndexOfSplit + 1);

           string ns = fullName.Substring(0, lastIndexOfSplit + 1);

           return CombineNsCodeShortName( CodeNamspace(ns) , shortName);
       }

       public string CombineNsCodeShortName(int code, string shortName)
       {
           return code + shortName;
       }
       public string DecodeNsShortName(string nsCodeShortName)
       {
           if (string.IsNullOrWhiteSpace(nsCodeShortName)) return nsCodeShortName;
           int nsCode = -1;
           var shortName = new Regex("^[0-9]+").Replace(nsCodeShortName,match =>
           {
               nsCode = Convert.ToInt32(match.Value);
               return string.Empty;
           });
           if(nsCode<0 || nsCode>NameSpaceStrings.Count)    throw new Exception("unexceptable namespace code "+ nsCodeShortName);
           return NameSpaceStrings[nsCode] +shortName;
       }

       public int CodeNamspace(string @namespace)
       {
           string ns = @namespace;
           //   @namespace = @namespace.ToLower();
           if (ns[ns.Length - 1] != '/' && // ns[ns.Length-1] != '\\' &&
               ns[ns.Length - 1] != '#')
               ns = ns + "/";
           int code;
           if (!Codes.TryGetValue(ns, out code))
           {
               Codes.Add(ns, code = Codes.Count);
               NameSpaceStrings.Add(ns);
           }
           return code;
       }

       public void AddPrefix(string pref, string nsname)
       {
           string exists;
           if (namespacesByPrefix.TryGetValue(pref, out exists))
           {
               if (exists != nsname)
                   throw new Exception("namespace prefix duplicated " + pref + " " + nsname + " " + exists);
           }
           else namespacesByPrefix.Add(pref, nsname);
           CodeNamspace(nsname);
       }
   }
}
