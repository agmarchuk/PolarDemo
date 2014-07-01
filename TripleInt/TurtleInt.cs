using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NameTable;
using PolarDB;
using TrueRdfViewer;

namespace TripleIntClasses
{
    public static partial class TurtleInt
    {
        public static int BufferMax =30*1000 * 1000;
        public static void LoadTriplets(string filepath, ref PaCell otriples, ref PaCell dtriplets)
        {
            //Directory.Delete(path);               
            //Directory.CreateDirectory(path);


            otriples.Clear();
            otriples.Fill(new object[0]);


            dtriplets.Clear();
            dtriplets.Fill(new object[0]);
            int i = 0;
            //Entity e = new Entity();
            TripleInt.SiCodingEntities.Clear();
            TripleInt.PredicatesCoding.Clear();
            TripleInt.EntitiesCodeCache.Clear();
            
            LiteralStoreSplited.Literals.Clear();

            foreach (var tripletGrpah in TurtleInt.LoadGraphs(filepath))
            {
                if (i % 100000 == 0) Console.Write("w{0} ", i / 100000);
                i += tripletGrpah.PredicateDataValuePairs.Count + tripletGrpah.PredicateObjValuePairs.Count;
                var subject = TripleInt.EntitiesCodeCache[tripletGrpah.subject];

                foreach (var predicateObjValuePair in tripletGrpah.PredicateObjValuePairs)
                    otriples.Root.AppendElement(new object[]
                    {
                        subject,
                        predicateObjValuePair.Key,
                        TripleInt.EntitiesCodeCache[predicateObjValuePair.Value]
                    });
                //foreach (var predicateDataValuePair in tripletGrpah.PredicateDataValuePairs)
                //    LiteralStore.Literals.Write(predicateDataValuePair.Value);
                LiteralStoreSplited.Literals.WriteBufferForce();

                foreach (var predicateDataValuePair in tripletGrpah.PredicateDataValuePairs)
                    dtriplets.Root.AppendElement(new object[]
                    {
                        subject,
                        predicateDataValuePair.Key,
                        predicateDataValuePair.Value.Offset
                    });
            }

            otriples.Flush();
            dtriplets.Flush();
        //    LiteralStoreSplited.Literals.Compress(dtriplets);

        }
        private static IEnumerable<TripletGraph> LoadGraphs(string datafile)
        {
            int ntriples = 0;
            int nTripletsInBuffer = 0;
            string subject = null;
            var namespaces = new Dictionary<string, string>();

            List<TripletGraph> bufferTripletsGrpah = new List<TripletGraph>(BufferMax);
            TripletGraph currentTripletGraph = null;
            HashSet<string> entitiesStrings = new HashSet<string>();
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
                        if (nTripletsInBuffer >= BufferMax)
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
                        currentTripletGraph = new TripletGraph() { subject = subject };
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
                  
                        string rest_line = line1.Substring(first_blank + 1).Trim();
                        // ”берем последний символ
                        rest_line = rest_line.Substring(0, rest_line.Length - 1).Trim();
                        bool isDatatype = rest_line[0] == '\"';

                        string pred_line = line1.Substring(0, first_blank);
                        string predicateString = GetEntityString(namespaces, pred_line);
                      

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

                            Literal literal = Literal.Create(datatype, sdata, lang);
                            TripleInt.PredicatesCoding.Insert(predicateString, literal.vid);
                            currentTripletGraph.PredicateDataValuePairs.Add(
                                new KeyValuePair<int, Literal>(TripleInt.PredicatesCoding[predicateString],
                                    LiteralStoreSplited.Literals.Write(literal)));
                        }
                        else
                        {
                             TripleInt.PredicatesCoding.Insert(predicateString,null);
                            string obj = rest_line[0] == '<'
                                ? rest_line.Substring(1, rest_line.Length - 2)
                                : GetEntityString(namespaces, rest_line);
                            entitiesStrings.Add(obj);
                            currentTripletGraph.PredicateObjValuePairs.Add(new KeyValuePair<int, string>(TripleInt.PredicatesCoding[predicateString], obj));
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



        public static void LoadTriplets(string filepath, PaCell otriples, PaCell dtriplets)
        {
            //Directory.Delete(path);               
            //Directory.CreateDirectory(path);


            otriples.Clear();
            otriples.Fill(new object[0]);


            dtriplets.Clear();
            dtriplets.Fill(new object[0]);
            int i = 0;
            //Entity e = new Entity();
            TripleInt.SiCodingEntities.Clear();
            TripleInt.PredicatesCoding.Clear();
            TripleInt.EntitiesCodeCache.Clear();
            TripleInt.nameSpaceStore.Clear();
            IRI.namespacesByPrefix.Clear();
            TripleInt.nameSpaceStore.Clear();
            LiteralStoreSplited.Literals.Clear();

            foreach (var triplet in Buffer(LoadTriplets(filepath)))
            {
                if (i%100000 == 0) Console.Write("w{0} ", i/100000);
                i++;
                if (triplet is OTripleInt)
                {
                    var oTripleInt = triplet as OTripleInt;
                    otriples.Root.AppendElement(new object[]
                    {
                        oTripleInt.subject,
                        oTripleInt.predicate,
                        oTripleInt.obj
                    });
                }
                else
                {
                    var dtr = triplet as DTripleInt;     
                    dtriplets.Root.AppendElement(new object[]
                    {
                        dtr.subject,
                        dtr.predicate,
                        dtr.data.Offset
                    });
                }
            }

            otriples.Flush();
            dtriplets.Flush();
            //    LiteralStoreSplited.Literals.Compress(dtriplets);

        }

        private static IEnumerable<T> Buffer<T>(IEnumerable<T> triplets)
        {
            var buffer = new List<T>();

            foreach (var triplet in triplets)
                if (buffer.Count < BufferMax)
                    buffer.Add(triplet);
                else
                {
                    LiteralStoreSplited.Literals.WriteBufferForce();
                    foreach (var tripleInt in buffer)
                        yield return tripleInt;
                    buffer.Clear();
                }
            LiteralStoreSplited.Literals.WriteBufferForce();
            foreach (var tripleInt in buffer)
                yield return tripleInt;
        }

        private static IEnumerable<TripleInt> LoadTriplets(string datafile)
        {
            
            int ntriples = 0;
            int nTripletsInBuffer = 0;
            string subject = null;
            int subjectCode = 0;
            StringIntEncoded stringIntEncoded = ((StringIntEncoded)TripleInt.SiCodingEntities);
       
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
                        IRI.namespacesByPrefix.Add(pref, nsname);
                        TripleInt.GetNamespaceCode(nsname);  
                    }
                    else if (line[0] != ' ')
                    {
                        // Subject
                        line = line.Trim();
                        subject = new IRI(line).Coded;
                        subjectCode = stringIntEncoded.InsertOne(subject);
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

                        string rest_line = line1.Substring(first_blank + 1).Trim();
                        // ”берем последний символ
                        rest_line = rest_line.Substring(0, rest_line.Length - 1).Trim();
                        bool isDatatype = rest_line[0] == '\"';

                        string pred_line = line1.Substring(0, first_blank);
                        string predicateString = new IRI(pred_line).Coded;


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
                                datatype = NameSpaceStore.SplitCodeNameSpace(qname);
                            }

                            Literal literal = Literal.Create(datatype, sdata, lang);
                          TripleInt.PredicatesCoding.Insert(predicateString, literal.vid);
                            yield return new DTripleInt(subjectCode, TripleInt.PredicatesCoding[predicateString],
                                LiteralStoreSplited.Literals.Write(literal));
                        }
                        else
                        {
                            TripleInt.PredicatesCoding.Insert(predicateString, null);
                            string obj = NameSpaceStore.SplitCodeNameSpace(rest_line);
                            yield return
                                new OTripleInt(subjectCode, TripleInt.PredicatesCoding[predicateString],
                                    stringIntEncoded.InsertOne(obj));
                        }
                        ntriples++;
                        nTripletsInBuffer++;
                        if (ntriples % 100000 == 0) Console.Write("r{0} ", ntriples / 100000);
                    }
                }
            
            TripleInt.SiCodingEntities.MakeIndexed();
            GC.Collect();
            Console.WriteLine("ntriples={0}", ntriples);
            TripleInt.nameSpaceStore.Flush();
            Console.WriteLine("writed namespaces ");
        }
    }
}