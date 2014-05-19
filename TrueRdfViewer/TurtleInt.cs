using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NameTable;

namespace TrueRdfViewer
{
    public static class TurtleInt
    {
        // (Только для специальных целей) Это для накапливания идентификаторов собираемых сущностей:
        public static List<string> sarr = new List<string>();

        public static IEnumerable<TripleInt> LoadGraph(string datafile)
            //EngineVirtuoso engine, string graph, string datafile)
        {
            int ntriples = 0;
            string subject = null;
            var namespaces = new Dictionary<string, string>();
            var tripletsBuffer = new List<Tuple<string, string, object>>();    
            int bufferMax = 50*1000*1000;
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
                        if (tripletsBuffer.Count >= bufferMax)
                        {
                            foreach (var tripleInt in TripleIntsCode(tripletsBuffer))
                                yield return tripleInt;              
                            tripletsBuffer.Clear();
                        }
                        // Subject
                        line = line.Trim();
                        subject = GetEntityString(namespaces, line);
                       
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
                        string predicate = GetEntityString(namespaces, pred_line);
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
                                    : GetEntityString(namespaces, qname);
                            }
                         
                            tripletsBuffer.Add(Tuple.Create(subject, predicate, (object)
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
                                            : datatype == null || datatype == "http://www.w3.org/2001/XMLSchema#string"
                                                ? new Literal(LiteralVidEnumeration.text)
                                                {Value = new Text() {Value = sdata, Lang = lang ?? string.Empty}}
                                                : new Literal(LiteralVidEnumeration.typedObject)
                                                {Value = new TypedObject() {Value = sdata, Type = datatype}})));
                        }
                        else
                        {
                            entity = rest_line[0] == '<'
                                ? rest_line.Substring(1, rest_line.Length - 2)
                                : GetEntityString(namespaces, rest_line);
                            // (Только для специальных целей) Накапливание:
                            if (predicate == "http://www.w3.org/1999/02/22-rdf-syntax-ns#type" &&
                                entity == "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/Product")
                                sarr.Add(subject);         
                            tripletsBuffer.Add(Tuple.Create(subject, predicate, (object) entity));
                        }
                        ntriples++;
                    }
                }
            foreach (var tripleInt in TripleIntsCode(tripletsBuffer)) yield return tripleInt;
            TripleInt.SiCodingEntities.MakeIndexed();
            TripleInt.SiCodingPredicates.MakeIndexed();
            Console.WriteLine("ntriples={0}", ntriples);
        }

     //   public static Dictionary<string, int> CodingCashe=new Dictionary<string, int>();
        private static IEnumerable<TripleInt> TripleIntsCode(List<Tuple<string, string, object>> tripletsBuffer)
        {
          
           // Array.Sort(arr);
          //  var codes = arr.ToDictionary(s =>s, s=> s.GetHashCode());
            var codesEntities = TripleInt.SiCodingEntities.InsertPortion(tripletsBuffer
                                                                    .Select(tuple => tuple.Item1)
                                                                    .Concat(tripletsBuffer
                                                                        .Select(tuple => tuple.Item3)
                                                                        .Where(o => o is string)
                                                                        .Cast<string>())
                                                                    .ToArray());
            var codesPredicates = TripleInt.SiCodingPredicates.InsertPortion(tripletsBuffer.Select(tuple => tuple.Item2).ToArray());
            return tripletsBuffer.Select(tuple =>
            {
                int subject = codesEntities[tuple.Item1];
                int predicate = codesPredicates[tuple.Item2];
                var data = tuple.Item3 as Literal;
                if (data != null)
                    return (TripleInt)
                        new DTripleInt
                        {
                            subject = subject,
                            predicate = predicate,
                            data = data
                        };
                int @object = codesEntities[(string) tuple.Item3];
                    
                return new OTripleInt()
                {
                    subject = subject,
                    predicate = predicate,
                    obj =  @object
                };
            });
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
