using System;
using System.Collections.Generic;

namespace TripleIntClasses
{
    public class IRI
    {
        //public string prefixed;
       // public string Namespace;
        public int NamespaceCode;
        public string ShortName;
        public static readonly Dictionary<string, string> namespacesByPrefix = new Dictionary<string, string>();
        public string Coded { get { return  NamespaceCode + ShortName; } }
        public IRI(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            if (line[0]=='<')
            {
                var fullName =  line.Substring(1, line.Length - 2);
                FromFullName(fullName);
            }
            else//prefixed
            {
                int colon = line.IndexOf(':');
                if (colon == -1)
                {
                    Console.WriteLine("Err in line: " + line);
                    return;
                }
                string prefix = line.Substring(0, colon + 1);
                if (!namespacesByPrefix.ContainsKey(prefix)) 
                { Console.WriteLine("Err in line: " + line);   return;}
                var @namespace = namespacesByPrefix[prefix];
                NamespaceCode = TripleInt.GetNamespaceCode(@namespace);
              //  Namespace = @namespace;
                ShortName= line.Substring(colon + 1);
            }                             
        }

        public IRI(string @namespace, string shortName)
        {
            NamespaceCode = TripleInt.GetNamespaceCode(@namespace);
            ShortName = shortName;             
          //  Namespace = @namespace;
        }

        private void FromFullName(string fullName)
        {
            var lastIndexOfSplitSlash = fullName.LastIndexOf('/');
            var lastIndexOfSplitSharp = fullName.LastIndexOf('#');
            int lastIndexOfSplit = Math.Max(lastIndexOfSplitSharp, lastIndexOfSplitSlash);
            if (lastIndexOfSplit == -1)
            {
                Console.WriteLine("error split namespace " + fullName);
                return;
            }
            ShortName = fullName.Substring(lastIndexOfSplit + 1);
            var @namespace = fullName.Substring(0, lastIndexOfSplit);
           // Namespace = @namespace;
            NamespaceCode = TripleInt.GetNamespaceCode(@namespace);

        }
    }
}