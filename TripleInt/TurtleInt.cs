using System;
using System.Collections.Generic;
using System.IO;

namespace TripleIntClasses
{
    public static class TurtleInt
    {
        public static int BufferMax =30*1000 * 1000;
        
        public static IEnumerable<TripletGraph> LoadGraphs(string datafile)
            //EngineVirtuoso engine, string graph, string datafile)
        {
            int ntriples = 0;
            int nTripletsInBuffer = 0;
            string subject = null;
            var namespaces = new Dictionary<string, string>();
            
            List<TripletGraph> bufferTripletsGrpah=new List<TripletGraph>(BufferMax);
            TripletGraph currentTripletGraph = null;
            HashSet<string> entitiesStrings=new HashSet<string>();
            using (var sr = new StreamReader(datafile))
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    //if (i % 10000 == 0) { Console.Write("{0} ", i / 10000); }    
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
                        namespaces.Add(pref, nsname);
                    }
                    else if (line[0] != ' ')
                    {
                        //if (bufferTripletsGrpah.Count >= BufferMax)
                        if (nTripletsInBuffer>=BufferMax)
                        {

                            TripleInt.EntitiesCodeCache = TripleInt.SiCodingEntities.InsertPortion(entitiesStrings);
                            foreach (var tripletGraph in bufferTripletsGrpah)
                                yield return tripletGraph;
                            bufferTripletsGrpah.Clear();
                            entitiesStrings.Clear();
                            GC.Collect();
                            nTripletsInBuffer = 0;
                        }
                        // Subject
                        line = line.Trim();
                        subject = GetEntityString(namespaces, line);
                        entitiesStrings.Add(subject);
                        currentTripletGraph=new TripletGraph(){subject = subject};
                       bufferTripletsGrpah.Add(currentTripletGraph);
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
                        string predicateString = GetEntityString(namespaces, pred_line);
                        TripleInt.PredicatesCodeCache = TripleInt.SiCodingPredicates.InsertPortion(new[] {predicateString});
                        int predicate = TripleInt.PredicatesCodeCache[predicateString];
                        string rest_line = line1.Substring(first_blank + 1).Trim();
                        // ”берем последний символ
                        rest_line = rest_line.Substring(0, rest_line.Length - 1).Trim();
                        bool isDatatype = rest_line[0] == '\"';
                        // объект может быть entity или данное, у данного может быть €зыковый спецификатор или тип
                        string sdata = null;
                        string datatype = null;
                        string lang = null;
                        if (isDatatype)
                        {
                            // ѕоследн€€ двойна€ кавычка 
                            int lastqu = rest_line.LastIndexOf('\"');
                            // «начение данных
                            sdata = rest_line.Substring(1, lastqu - 1);
                            // языковый специализатор:
                            int dog = rest_line.LastIndexOf('@');
                            if (dog == lastqu + 1) lang = rest_line.Substring(dog + 1, rest_line.Length - dog - 1);
                            int pp = rest_line.IndexOf("^^");
                            if (pp == lastqu + 1)
                            {
                                //  “ип данных
                                string qname = rest_line.Substring(pp + 2);
                                //  тип данных может быть "префиксным" или полным
                                datatype = qname[0] == '<'
                                    ? qname.Substring(1, qname.Length - 2)
                                    : GetEntityString(namespaces, qname);
                            }

                            currentTripletGraph.PredicateDataValuePairs.Add(new KeyValuePair<int, Literal>(predicate, Literal.Create(datatype, sdata, lang)));
                        }
                        else
                        {
                            string obj = rest_line[0] == '<'
                                ? rest_line.Substring(1, rest_line.Length - 2)
                                : GetEntityString(namespaces, rest_line);
                            entitiesStrings.Add(obj);
                            currentTripletGraph.PredicateObjValuePairs.Add(new KeyValuePair<int, string>(predicate, obj));
                        }
                        ntriples++;
                        nTripletsInBuffer++;
                        if (ntriples % 100000 == 0) Console.Write("r{0} ", ntriples / 100000); 
                    }
                }
            TripleInt.EntitiesCodeCache = TripleInt.SiCodingEntities.InsertPortion(entitiesStrings);
            foreach (var tripletGraph in bufferTripletsGrpah)
                yield return tripletGraph;
            bufferTripletsGrpah.Clear();
            TripleInt.SiCodingEntities.MakeIndexed();
            entitiesStrings.Clear();
            GC.Collect();                
            Console.WriteLine("ntriples={0}", ntriples);
        }



        private static string GetEntityString(Dictionary<string, string> namespaces, string line)
        {
            string subject = null;
            int colon = line.IndexOf(':');
            if (colon == -1) { Console.WriteLine("Err in line: " + line); goto End; }
            string prefix = line.Substring(0, colon + 1);
            if (!namespaces.ContainsKey(prefix)) { Console.WriteLine("Err in line: " + line); goto End; }
            subject = namespaces[prefix] + line.Substring(colon + 1);
        End:
            return subject;
        }
    }
}