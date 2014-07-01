using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TripleIntClasses
{
   public class NameSpaceStore
   {
       public readonly Dictionary<string, int> Codes;
       public readonly List<string> NameSpaceStrings;
       private PaCell stringsCell;
       private PxCell entitiesByNamespaceCell;
       private string path;

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
       }

       public void Clear()
       {
           stringsCell.Clear();
           stringsCell.Fill(new object[0]);    
           Codes.Clear();
           NameSpaceStrings.Clear();
           InitConstants();
       }

       public static IRI @type;
       public static void InitConstants()
       {
           Literal.integer = new IRI("<http://www.w3.org/2001/XMLSchema#integer>");
           Literal.@double = new IRI("<http://www.w3.org/2001/XMLSchema#double>");
           Literal.@float = new IRI("<http://www.w3.org/2001/XMLSchema#float>");
           Literal.boolean = new IRI("<http://www.w3.org/2001/XMLSchema#boolean>");
           Literal.date = new IRI("<http://www.w3.org/2001/XMLSchema#date>");
           Literal.@string = new IRI("<http://www.w3.org/2001/XMLSchema#string>");
           Literal.dateTime = new IRI("<http://www.w3.org/2001/XMLSchema#dateTime>");
           @type = new IRI("<http://www.w3.org/1999/02/22-rdf-syntax-ns#type>");
       }

       public void Flush()
       {
           stringsCell.Clear();
           //NameSpaceStrings.Clear();
           //NameSpaceStrings.AddRange( Codes.Keys);    
           stringsCell.Fill(NameSpaceStrings.Cast<object>().ToArray()); 
       }

       public static string SplitCodeNameSpace(string line)
       {
           return new IRI(line).Coded;
       }
   }


}
