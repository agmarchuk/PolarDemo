using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace TrueRdfViewer
{
    public static class TurtleInt
    {
        public static int BufferEntitiesForCodeMax = 1000*1000;
        // (Только для специальных целей) Это для накапливания идентификаторов собираемых сущностей:
     //   public static List<string> sarr = new List<string>();

        public static int i = 0;
        public static Dictionary<string, string> _namespaces= new Dictionary<string, string>();

        public static IEnumerable<TripleInt> LoadGraph(string datafile)
        {
            int ntriples = 0;
            int subject = 0;
            Dictionary<string, int> entitiesCodes;
            Dictionary<string, int> predicatesCodes;    
           
             {
                
                bool isLast = false;
                while (!isLast)
                {
                    offsetEndReadForLoadTriplets = offsetEndReadForGetCode;
                    using (var srForCode = new StreamReader(datafile))
                    {
                        srForCode.BaseStream.Position = offsetEndReadForGetCode;
                        isLast = ReadGraphCreateNameTable(srForCode, out entitiesCodes, out predicatesCodes);
                        
                    }                                        

                    using (var sr = new StreamReader(datafile))
                    {
                        sr.BaseStream.Position = offsetEndReadForLoadTriplets;
                        while (sr.BaseStream.Position < offsetEndReadForGetCode)
                    {                   
                        string line = sr.ReadLine();
                        if (line == "") continue;
                        if (line[0] == '@')
                        {
                            // namespace already readed      
                        }
                        else if (line[0] != ' ')
                            subject = entitiesCodes[GetEntityString(line.Trim())];
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
                            int predicate = predicatesCodes[GetEntityString(pred_line)];
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

                                yield return new DTripleInt(subject, predicate,
                                    (datatype == "http://www.w3.org/2001/XMLSchema#integer" ||
                                     datatype == "http://www.w3.org/2001/XMLSchema#float" ||
                                     datatype == "http://www.w3.org/2001/XMLSchema#double"
                                        ? new Literal(LiteralVidEnumeration.integer)
                                        {
                                            Value = double.Parse(sdata, NumberStyles.Any)
                                        }
                                        : datatype == "http://www.w3.org/2001/XMLSchema#boolean"
                                            ? new Literal(LiteralVidEnumeration.date) {Value = bool.Parse(sdata)}
                                            : datatype == "http://www.w3.org/2001/XMLSchema#dateTime" ||
                                              datatype == "http://www.w3.org/2001/XMLSchema#date"
                                                ? new Literal(LiteralVidEnumeration.date)
                                                {Value = DateTime.Parse(sdata).ToBinary()}
                                                : datatype == null ||
                                                  datatype == "http://www.w3.org/2001/XMLSchema#string"
                                                    ? new Literal(LiteralVidEnumeration.text)
                                                    {Value = new Text() {Value = sdata, Lang = lang ?? string.Empty}}
                                                    : new Literal(LiteralVidEnumeration.typedObject)
                                                    {Value = new TypedObject() {Value = sdata, Type = datatype}}));
                                ;
                            }
                            else
                            {
                                entity = rest_line[0] == '<'
                                    ? rest_line.Substring(1, rest_line.Length - 2)
                                    : GetEntityString(rest_line);
                                yield return
                                    new OTripleInt()
                                    {
                                        subject = subject,
                                        predicate = predicate,
                                        obj = entitiesCodes[entity]
                                    };
                            }
                            if (ntriples%100000 == 0)
                                Console.Write("rt{0} ", ntriples/100000);
                            ntriples++;
                        }
                    }
                    }
                }
            }

            TripleInt.SiCodingEntities.MakeIndexed();
            TripleInt.SiCodingPredicates.MakeIndexed();
            Console.WriteLine("ntriples={0}", ntriples);
        }

        private static long offsetEndReadForGetCode = 0;
        private static long offsetEndReadForLoadTriplets = 0;
        private static int ntriplesForCode;

        public static bool ReadGraphCreateNameTable(StreamReader sr, out Dictionary<string, int> entitiesPortion, out Dictionary<string, int> predicatesPortion)
        {   
           
            string subject = null;
            HashSet<string> predicates = new HashSet<string>();
            HashSet<string> entities = new HashSet<string>();
            //sr.BaseStream.Position = offsetEndReadForGetCode; должны совпадать
            Console.Write("start read for code {0} ", ntriplesForCode);
            string line=null;
            while ((line = sr.ReadLine())!=null)
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
                    _namespaces.Add(pref, nsname);
                }
                else if (line[0] != ' ')
                {
                    
                    if (entities.Count >= BufferEntitiesForCodeMax)
                        break;
                    // Subject
                    line = line.Trim();
                    subject = GetEntityString(line);
                    entities.Add(subject);
                    
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
                    predicates.Add(GetEntityString(pred_line));
                    string rest_line = line1.Substring(first_blank + 1).Trim();
                    // Уберем последний символ
                    rest_line = rest_line.Substring(0, rest_line.Length - 1).Trim();
                    bool isDatatype = rest_line[0] == '\"';
                    if (!isDatatype)
                        entities.Add(rest_line[0] == '<'
                            ? rest_line.Substring(1, rest_line.Length - 2)
                            : GetEntityString(rest_line));
                    ntriplesForCode++;
                    if (ntriplesForCode%100000 == 0)
                        Console.Write("rc{0} ", ntriplesForCode/100000);      
                }
                offsetEndReadForGetCode = sr.BaseStream.Position;
            }
            Console.WriteLine("code  start{0} ", ntriplesForCode);           
            entitiesPortion = TripleInt.SiCodingEntities.InsertPortion(entities);
            predicatesPortion = TripleInt.SiCodingPredicates.InsertPortion(predicates);
            Console.WriteLine("code end");
            return line == null;
        }

        private static string GetEntityString(string line)
        {
            string subject = null; 
            int colon = line.IndexOf(':');
            if (colon == -1) { Console.WriteLine("Err in line: " + line); goto End; }
            string prefix = line.Substring(0, colon + 1);
            if (!_namespaces.ContainsKey(prefix)) { Console.WriteLine("Err in line: " + line); goto End; }
            subject = _namespaces[prefix] + line.Substring(colon + 1);
            End: 
            return subject;
        }
    }
}
