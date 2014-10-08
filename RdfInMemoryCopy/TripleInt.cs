using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfInMemory
{
    public abstract class TripleInt
    {
        public int subject, predicate;
        public static int Code(string s) { return s.GetHashCode(); }
        public static string Decode(int e) { return "noname" + e; }
        public static IEnumerable<TripleInt> LoadGraph(string datafile)
        {
            int ntriples = 0;
            string subject = null;
            Dictionary<string, string> namespaces = new Dictionary<string, string>();
            System.IO.StreamReader sr = new System.IO.StreamReader(datafile);
            int count = 2000000000;
            for (int i = 0; i < count; i++)
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
                    namespaces.Add(pref, nsname);
                }
                else if (line[0] != ' ')
                { // Subject
                    line = line.Trim();
                    subject = GetEntityString(namespaces, line);
                    if (subject == null) continue;
                }
                else
                { // Predicate and object
                    string line1 = line.Trim();
                    int first_blank = line1.IndexOf(' ');
                    if (first_blank == -1) { Console.WriteLine("Err in line: " + line); continue; }
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
                            if (qname[0] == '<')
                            {
                                datatype = qname.Substring(1, qname.Length - 2);
                            }
                            else
                            {
                                datatype = GetEntityString(namespaces, qname);
                            }
                        }
                        yield return new DTripleInt()
                        {
                            subject = TripleInt.Code(subject),
                            predicate = TripleInt.Code(predicate),
                            data = // d
                                datatype == "http://www.w3.org/2001/XMLSchema#integer" ?
                                    new Literal() { Vid = LiteralVidEnumeration.integer, Value = int.Parse(sdata) } :
                                (datatype == "http://www.w3.org/2001/XMLSchema#date" ?
                                    new Literal() { Vid = LiteralVidEnumeration.date, Value = DateTime.Parse(sdata).ToBinary() } :
                                (new Literal() { Vid = LiteralVidEnumeration.text, Value = new Text() { Value = sdata, Lang = "en" } }))

                        };
                    }
                    else
                    { // entity
                        entity = rest_line[0] == '<' ? rest_line.Substring(1, rest_line.Length - 2) : GetEntityString(namespaces, rest_line);

                        yield return new OTripleInt()
                        {
                            subject = TripleInt.Code(subject),
                            predicate = TripleInt.Code(predicate),
                            obj = TripleInt.Code(entity)
                        };
                    }
                    ntriples++;
                }
            }
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
    public class OTripleInt : TripleInt { public int obj; }
    public class DTripleInt : TripleInt { public Literal data; }
    public enum LiteralVidEnumeration { unknown, integer, text, date }
    public class Literal     : ICloneable
    {
        public LiteralVidEnumeration Vid;
        public object Value;
        public Literal() { }
        public Literal(object[] pair)
        {
            switch ((int)pair[0])
            {
                case 0: Vid = LiteralVidEnumeration.unknown; break;
                case 1: Vid = LiteralVidEnumeration.integer;
                    Value = pair[1];
                    break;
                case 2: Vid = LiteralVidEnumeration.text;
                    object[] text_pair = (object[])pair[1];
                    Value = new Text() { Value = (string)text_pair[0], Lang = (string)text_pair[1] };
                    break;
                case 3: Vid = LiteralVidEnumeration.date;
                    Value = pair[1]; // Надо как-то по-другому
                    break;
                default: throw new Exception("Err: 20901");
            }
        }
        public override string ToString()
        {
            switch (Vid)
            {
                case LiteralVidEnumeration.text:
                    {
                        Text txt = (Text)Value;
                        return "\"" + txt.Value + "\"@" + txt.Lang;
                    }
                default: return Value.ToString();
            }
        }

        public object Clone()
        {
            return Value is ICloneable
                ? new Literal() {Value = ((ICloneable) Value).Clone(), Vid = Vid}
                : new Literal() {Value = Value, Vid = Vid};
        }
    }
    public class Text { public string Value, Lang; }

    public class SubjPredInt : IComparable
    {
        public int subj, pred;
        public int CompareTo(object sp)
        {
            int cmp = subj.CompareTo(((SubjPredInt)sp).subj);
            if (cmp != 0) return cmp;
            return pred.CompareTo(((SubjPredInt)sp).pred);
        }
    }
    public class SubjPredObjInt : IComparable
    {
        public int subj, pred, obj;
        public SubjPredObjInt() { }
        public SubjPredObjInt(object pobj)
        {
            object[] rec = (object[])pobj;
            subj = (int)rec[0];
            pred = (int)rec[1];
            obj = (int)rec[2];
        }
        public int CompareTo(object sp)
        {
            SubjPredObjInt target = (SubjPredObjInt)sp;
            int cmp = subj.CompareTo(target.subj);
            if (cmp != 0) return cmp;
            cmp = pred.CompareTo(target.pred);
            if (cmp != 0) return cmp;
            return obj.CompareTo(target.obj);
        }
    }
    public class SPComparer : IComparer<SubjPredInt>
    {
        public int Compare(SubjPredInt x, SubjPredInt y)
        {
            return x.CompareTo(y);
        }
    }
    public class SPOComparer : IComparer<SubjPredObjInt>
    {
        public int Compare(SubjPredObjInt x, SubjPredObjInt y)
        {
            return x.CompareTo(y);
        }
    }
}
