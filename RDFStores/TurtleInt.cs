using System;
using System.Collections.Generic;
using System.IO;
using NameTable;
using PolarDB;
using RDFStores;

namespace TripleIntClasses
{
    public static class TurtleInt
    {
        public static int BufferMax = 30 * 1000 * 1000;
        public static void LoadByGraphsBuffer(string filepath, PaCell otriples, PaCell dtriplets, RDFIntStoreAbstract rdfIntStore)
        {
            //Directory.Delete(path);               
            //Directory.CreateDirectory(path);

            otriples.Clear();
            otriples.Fill(new object[0]);


            dtriplets.Clear();
            dtriplets.Fill(new object[0]);
            int i = 0;
            //Entity e = new Entity();
            rdfIntStore.Clear();      
            foreach (var tripletGrpah in LoadGraphs(filepath, rdfIntStore))
            {
                if (i % 100000 == 0) Console.Write("w{0} ", i / 100000);
                i += tripletGrpah.PredicateDataValuePairs.Count + tripletGrpah.PredicateObjValuePairs.Count;
                var subject = rdfIntStore.EntityCoding.GetCode(tripletGrpah.subject);

                foreach (var predicateObjValuePair in tripletGrpah.PredicateObjValuePairs)
                    otriples.Root.AppendElement(new object[]
                    {
                        subject,
                        predicateObjValuePair.Key,
                       rdfIntStore.EntityCoding.GetCode(predicateObjValuePair.Value)
                    }); 

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
            rdfIntStore.NameSpaceStore.Flush();
            rdfIntStore.LiteralStore.Flush();
        }
        private static IEnumerable<TripletGraph> LoadGraphs(string datafile, RDFIntStoreAbstract rdfIntStore)
        {
            int ntriples = 0;
            int nTripletsInBuffer = 0;
            string subject = null;       

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
                        rdfIntStore.NameSpaceStore.AddPrefix(pref, nsname);
                    }
                    else if (line[0] != ' ')
                    {
                        //if (bufferTripletsGrpah.Count >= BufferMax)
                        if (nTripletsInBuffer >= BufferMax)
                        {

                             rdfIntStore.EntityCoding.InsertPortion(entitiesStrings);
                            foreach (var tripletGraph in bufferTripletsGrpah)
                                yield return tripletGraph;
                            bufferTripletsGrpah.Clear();
                            entitiesStrings.Clear();
                            GC.Collect();
                            nTripletsInBuffer = 0;
                        }
                        // Subject
                        line = line.Trim();
                        subject = rdfIntStore.NameSpaceStore.GetShortFromFullOrPrefixed(line);
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
                        // Óáåðåì ïîñëåäíèé ñèìâîë
                        rest_line = rest_line.Substring(0, rest_line.Length - 1).Trim();
                        bool isDatatype = rest_line[0] == '\"';

                        string pred_line = line1.Substring(0, first_blank);
                        string predicateString = rdfIntStore.NameSpaceStore.GetShortFromFullOrPrefixed(pred_line);


                        // îáúåêò ìîæåò áûòü entity èëè äàííîå, ó äàííîãî ìîæåò áûòü ÿçûêîâûé ñïåöèôèêàòîð èëè òèï
                        string sdata = null;
                        string datatype = null;
                        string lang = null;
                        if (isDatatype)
                        {
                            // Ïîñëåäíÿÿ äâîéíàÿ êàâû÷êà 
                            int lastqu = rest_line.LastIndexOf('\"');
                            // Çíà÷åíèå äàííûõ
                            sdata = rest_line.Substring(1, lastqu - 1);
                            // ßçûêîâûé ñïåöèàëèçàòîð:
                            int dog = rest_line.LastIndexOf('@');
                            if (dog == lastqu + 1) lang = rest_line.Substring(dog + 1, rest_line.Length - dog - 1);
                            int pp = rest_line.IndexOf("^^");
                            if (pp == lastqu + 1)
                            {
                                //  Òèï äàííûõ
                                string qname = rest_line.Substring(pp + 2);
                                //  òèï äàííûõ ìîæåò áûòü "ïðåôèêñíûì" èëè ïîëíûì
                                //datatype = qname[0] == '<'
                                //    ? qname.Substring(1, qname.Length - 2)
                                //    : GetEntityString(namespaces, qname);
                                datatype = rdfIntStore.NameSpaceStore.GetShortFromFullOrPrefixed(qname);
                            }

                            Literal literal = rdfIntStore.LiteralStore.Create(datatype, sdata, lang);
                          rdfIntStore.PredicatesCoding.Insert(predicateString, literal.vid);
                            currentTripletGraph.PredicateDataValuePairs.Add(
                                new KeyValuePair<int, Literal>(rdfIntStore.PredicatesCoding[predicateString],
                                    rdfIntStore.LiteralStore.Write(literal)));
                        }
                        else
                        {
                           rdfIntStore.PredicatesCoding.Insert(predicateString, null);
                            string obj = rdfIntStore.NameSpaceStore.GetShortFromFullOrPrefixed(rest_line);
                            entitiesStrings.Add(obj);
                            currentTripletGraph.PredicateObjValuePairs.Add(new KeyValuePair<int, string>(rdfIntStore.PredicatesCoding[predicateString], obj));
                        }
                        ntriples++;
                        nTripletsInBuffer++;
                        if (ntriples % 100000 == 0) Console.Write("r{0} ", ntriples / 100000);
                    }
                }
          rdfIntStore.EntityCoding.InsertPortion(entitiesStrings);
            foreach (var tripletGraph in bufferTripletsGrpah)
                yield return tripletGraph;
            bufferTripletsGrpah.Clear();
           rdfIntStore.MakeIndexed();
            
            entitiesStrings.Clear();
            GC.Collect();
            Console.WriteLine("ntriples={0}", ntriples);
        }                 

        public static void LoadTriplets(string filepath, PaCell otriples, PaCell dtriplets, RDFIntStoreAbstract rdf)
        {
            //Directory.Delete(path);               
            //Directory.CreateDirectory(path);


            otriples.Clear();
            otriples.Fill(new object[0]);


            dtriplets.Clear();
            dtriplets.Fill(new object[0]);
            int i = 0;
            //Entity e = new Entity();
            rdf.Clear();
         

            foreach (var triplet in LoadTriplets(filepath, rdf))
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
        }       
       
        private static IEnumerable<TripleInt> LoadTriplets(string datafile, RDFIntStoreAbstract rdf)
        {
            int ntriples = 0;
            int nTripletsInBuffer = 0;
            string subject = null;
            int subjectCode = 0;         
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
                       rdf.NameSpaceStore.namespacesByPrefix.Add(pref, nsname);
                        string ns = nsname;
                        //   @namespace = @namespace.ToLower();
                        if (ns[ns.Length-1] == '/' || ns[ns.Length-1] == '\\' ||
                            ns[ns.Length-1] == '#')
                            ns = ns.Substring(0, ns.Length - 1);
                        int code;
                        if (!rdf.NameSpaceStore.Codes.TryGetValue(ns, out code))
                        {
                            rdf.NameSpaceStore.Codes.Add(ns, code = rdf.NameSpaceStore.Codes.Count);
                            rdf.NameSpaceStore.NameSpaceStrings.Add(ns);
                        }
                        int temp = code;  
                    }
                    else if (line[0] != ' ')
                    {
                        // Subject
                        line = line.Trim();
                        subject = rdf.NameSpaceStore.GetShortFromFullOrPrefixed(line);
                        subjectCode = rdf.EntityCoding.InsertOne(subject);
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
                        // Уберем последний символ
                        rest_line = rest_line.Substring(0, rest_line.Length - 1).Trim();
                        bool isDatatype = rest_line[0] == '\"';

                        string pred_line = line1.Substring(0, first_blank);
                        string predicateString = rdf.NameSpaceStore.GetShortFromFullOrPrefixed(pred_line);


                        // объект может быть entity или данное, у данного может быть языковый спецификатор или тип
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
                                datatype = rdf.NameSpaceStore.GetShortFromFullOrPrefixed(qname);
                            }

                            Literal literal = rdf.LiteralStore.Create(datatype, sdata, lang);
                            rdf.PredicatesCoding.Insert(predicateString, literal.vid);
                            yield return new DTripleInt(subjectCode, rdf.PredicatesCoding[predicateString],
                             rdf.LiteralStore.Write(literal));
                        }
                        else
                        {
                            rdf.PredicatesCoding.Insert(predicateString, null);
                            string obj = rdf.NameSpaceStore.GetShortFromFullOrPrefixed(rest_line);
                            yield return
                                new OTripleInt(subjectCode, rdf.PredicatesCoding[predicateString],
                                    rdf.EntityCoding.InsertOne(obj));
                        }
                        ntriples++;
                        nTripletsInBuffer++;
                        if (ntriples % 100000 == 0) Console.Write("r{0} ", ntriples / 100000);
                    }
                }
            
            rdf.MakeIndexed();
            GC.Collect();
            Console.WriteLine("ntriples={0}", ntriples);
        }
    }
}