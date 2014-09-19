using System;
using System.IO;

namespace TripleStoreForDNR
{
    public class TurtleParser
    {
        public void LoadTriplets(IGraph graph, string datafile)
        {
            graph.Clear();


            int ntriples = 0;
            int nTripletsInBuffer = 0;
            INode subject = null;
            using (var sr = new StreamReader(datafile))
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
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
                        graph.NamespaceMap.AddNamespace(pref, new Uri(nsname));
                    }
                    else if (line[0] != ' ')
                    {
                        // Subject
                        line = line.Trim();
                        subject = graph.CreateUriNode(line);
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
                        INode predicate = graph.CreateUriNode(pred_line == "a" ? "<http://www.w3.org/1999/02/22-rdf-syntax-ns#type>" : pred_line);

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

                                datatype = "<" + qname + ">"; // rdf.NameSpaceStore.GetShortFromFullOrPrefixed(qname);
                            }

                            ILiteralNode literal = graph.CreateLiteralNode(rest_line);

                            graph.Assert(new Triple(subject, predicate, literal));
                        }
                        else
                        {
                            graph.Assert(new Triple(subject, predicate, graph.CreateUriNode(rest_line)));
                        }
                        ntriples++;
                        nTripletsInBuffer++;
                        if (ntriples % 100000 == 0) Console.Write("r{0} ", ntriples / 100000);
                    }
                }

            graph.Build();

            Console.WriteLine("ntriples={0}", ntriples);
        }


    }
}
