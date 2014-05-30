using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using NameTable;

namespace TrueRdfViewer
{
    public class GraphOtriplet
    {
       public string subject;
        public List<KeyValuePair<int, string>> PredicatesValues;
    }
    public class GraphDtriplet
    {
        public string subject;
        public List<KeyValuePair<int, Literal>> PredicatesValues;
    }

    public static class TurtleInt
    {
        public static int BufferEntitiesForCodeMax = 1000*1000;
        // (Только для специальных целей) Это для накапливания идентификаторов собираемых сущностей:
     //   public static List<string> sarr = new List<string>();

        public static int i = 0;
        public static Dictionary<string, string> Namespaces= new Dictionary<string, string>();

        public static IEnumerable<KeyValuePair<List<GraphOtriplet>, List<GraphDtriplet>>> LoadGraph(string datafile)
        {
            int ntriples = 0;
            var gOTriplets=new List<GraphOtriplet>();
            var gDTriplets=new List<GraphDtriplet>();
            var returnPair = new KeyValuePair<List<GraphOtriplet>, List<GraphDtriplet>>(gOTriplets, gDTriplets);
            HashSet<string> entities = new HashSet<string>();
            GraphOtriplet graphOtriplet = null;
            GraphDtriplet graphDtriplet = null;
            using (var sr = new StreamReader(datafile))
            {                           
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {                 
                    if (line == "") continue;
                    if (line[0] == '@')
                    {
                        // namespace  
                        string[] parts = line.Split(' ');
                        if (parts.Length != 4 || parts[0] != "@prefix" || parts[3] != ".")
                        {
                            Console.WriteLine("Err: strange line: " + line);
                            continue;
                        }
                        string pref = parts[1];
                        string nsname = parts[2];
                        if (nsname.Length < 3 || nsname[0] != '<' || nsname[nsname.Length - 1] != '>')
                        {
                            Console.WriteLine("Err: strange nsname: " + nsname);
                            continue;
                        }
                        nsname = nsname.Substring(1, nsname.Length - 2);
                        Namespaces.Add(pref, nsname);
                    }
                    else if (line[0] != ' ')
                    {
                        if (entities.Count >= BufferEntitiesForCodeMax)
                        {
                            TripleInt.EntitiesCodeCache = TripleInt.SiCodingEntities.InsertPortion(entities);
                            entities.Clear();
                            yield return returnPair;
                            gOTriplets = new List<GraphOtriplet>();
                            gDTriplets = new List<GraphDtriplet>();
                            returnPair = new KeyValuePair<List<GraphOtriplet>, List<GraphDtriplet>>(gOTriplets, gDTriplets);
                            Console.WriteLine("cgs");
                            GC.Collect();
                            Console.WriteLine("cge");
                        }

                        var subjectStringPrefixed = line.Trim();
                        var subject = GetEntityString(subjectStringPrefixed);
                        entities.Add(subject);
                        graphOtriplet = new GraphOtriplet(){subject = subject, PredicatesValues = new List<KeyValuePair<int, string>>()};
                        gOTriplets.Add(graphOtriplet);
                        graphDtriplet = new GraphDtriplet(){subject = subject, PredicatesValues = new List<KeyValuePair<int, Literal>>()};
                        gDTriplets.Add(graphDtriplet);    
                    }
                    else
                    {
                        // Predicate and object  
                        string line1 = line.Trim();
                        int first_blank = line1.IndexOf(' ');
                        if (first_blank == -1)
                        {
                            Console.WriteLine("Err in line: " + line);
                            continue;
                        }
                        string pred_line = line1.Substring(0, first_blank);
                        int predicate;
                        var predicatetring = GetEntityString(pred_line);
                        if(!TripleInt.PredicatesCodeCache.TryGetValue(predicatetring, out predicate))
                            TripleInt.PredicatesCodeCache.Add(predicatetring, predicate=TripleInt.PredicatesCodeCache.Count);
                        string rest_line = line1.Substring(first_blank + 1).Trim();
                        // Уберем последний символ
                        rest_line = rest_line.Substring(0, rest_line.Length - 1).Trim();
                        bool isDatatype = rest_line[0] == '\"';
                        // объект может быть entity или данное, у данного может быть языковый спецификатор или тип
                        string entity = null;
                        string sdata = null;
                        string datatype = null;
                        string lang = null;
                        if (isDatatype)
                        {
                            // Последняя двойная кавычка 
                            int lastqu = rest_line.LastIndexOf('\"');
                            // Значение данных
                            sdata = rest_line.Substring(1, lastqu - 1);
                            // Языковый специализатор:
                            int dog = rest_line.LastIndexOf('@');
                            if (dog == lastqu + 1) lang = rest_line.Substring(dog + 1, rest_line.Length - dog - 1);
                            int pp = rest_line.IndexOf("^^");
                            if (pp == lastqu + 1)
                            {
                                //  Тип данных
                                string qname = rest_line.Substring(pp + 2);
                                //  тип данных может быть "префиксным" или полным
                                datatype = qname[0] == '<'
                                    ? qname.Substring(1, qname.Length - 2)
                                    : GetEntityString(qname);
                            }
                            graphDtriplet.PredicatesValues.Add(new KeyValuePair<int, Literal>(predicate,
                                LiteralStore.Literals.Write(Literal.Create(datatype, sdata, lang))));        
                        }
                        else
                        {
                            entity = rest_line[0] == '<'
                                ? rest_line.Substring(1, rest_line.Length - 2)
                                : GetEntityString(rest_line);
                            entities.Add(entity);
                            graphOtriplet.PredicatesValues.Add(new KeyValuePair<int, string>(predicate, entity));
                        }
                        if (ntriples%100000 == 0)
                            Console.Write("rt{0} ", ntriples/100000);
                        ntriples++;
                    }
                }
            }

            TripleInt.EntitiesCodeCache = TripleInt.SiCodingEntities.InsertPortion(entities);
            entities.Clear();
            yield return returnPair;
            gOTriplets = new List<GraphOtriplet>();
            gDTriplets = new List<GraphDtriplet>();
            returnPair = new KeyValuePair<List<GraphOtriplet>, List<GraphDtriplet>>(gOTriplets, gDTriplets);
            Console.WriteLine("cgs");
            GC.Collect();
            Console.WriteLine("cge");

            TripleInt.SiCodingEntities.MakeIndexed();
            
            Console.WriteLine("ntriples={0}", ntriples);
        }

       
      
        public static string GetEntityString(string line)
        {
            string subject = null; 
            int colon = line.IndexOf(':');
            if (colon == -1) { Console.WriteLine("Err in line: " + line); goto End; }
            string prefix = line.Substring(0, colon + 1);
            if (!Namespaces.ContainsKey(prefix)) { Console.WriteLine("Err in line: " + line); goto End; }
            subject = Namespaces[prefix] + line.Substring(colon + 1);
            End: 
            return subject;
        }
    }
}
