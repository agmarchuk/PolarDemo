using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfInMemory
{
    public class TurtleParser
    {
        public void Load(IGraph g, string datafile)
        {
            g.Clear();
            g.NamespaceMap.Clear(); // Возможно, это действие входит в предыдущее
            int ntriples = 0;
            string subject = null;
            //Dictionary<string, string> namespaces = new Dictionary<string, string>();
            System.IO.StreamReader sr = new System.IO.StreamReader(datafile);
            for (int i = 0; ; i++)
            {
                string line = sr.ReadLine();
                //if (i % 10000 == 0) { Console.Write("{0} ", i / 10000); }
                if (line == null) break;
                if (line == "") continue;
                if (line[0] == '@')
                { // namespace
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
                    //namespaces.Add(pref, nsname);
                    g.NamespaceMap.AddNamespace(pref, new Uri(nsname));
                }
                else if (line[0] != ' ')
                { // Subject
                    line = line.Trim();
                    subject = GetEntityString(g, line);
                    if (subject == null) continue;
                }
                else
                { // Predicate and object
                    string line1 = line.Trim();
                    int first_blank = line1.IndexOf(' ');
                    if (first_blank == -1) { Console.WriteLine("Err in line: " + line); continue; }
                    string pred_line = line1.Substring(0, first_blank);
                    if (pred_line == "a") pred_line = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
                    string predicate = GetEntityString(g, pred_line);
                    string rest_line = line1.Substring(first_blank + 1).Trim();
                    // Уберем последний символ (точку)
                    rest_line = rest_line.Substring(0, rest_line.Length - 1).Trim();
                    bool isDatatype = rest_line[0] == '\"';
                    // объект может быть entity или данное, у данного может быть языковый спецификатор или тип
                    if (isDatatype)
                    {
                        g.Assert(new Triple(g.CreateUriNode(subject),
                            g.CreateUriNode(predicate), g.CreateLiteralNode(rest_line))); 
                    }
                    else
                    { // entity
                        string entity = rest_line[0] == '<' ? rest_line.Substring(1, rest_line.Length - 2) : GetEntityString(g, rest_line);
                        g.Assert(new Triple(g.CreateUriNode(subject),
                            g.CreateUriNode(predicate), g.CreateUriNode(entity)));
                    }
                    ntriples++;
                }
            }

        }
        private static string GetEntityString(IGraph g, string line)
        {
            string subject = null;
            int colon = line.IndexOf(':');
            if (colon == -1) { Console.WriteLine("Err in line: " + line); goto End; }
            string prefix = line.Substring(0, colon + 1);
            if (!g.NamespaceMap.HasNamespace(prefix)) { Console.WriteLine("Err in line: " + line); goto End; }
            subject = g.NamespaceMap.GetNamespaceUri(prefix).OriginalString + line.Substring(colon + 1);
        End:
            return subject;
        }
    }
}
