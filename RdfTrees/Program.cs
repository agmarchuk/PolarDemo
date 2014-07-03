using System;
using System.Linq;
using System.Xml.Linq;
using NameTable;
using TripleIntClasses;


namespace RdfTreesNamespace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DateTime tt0 = DateTime.Now;

            string path = @"..\..\..\Databases\";
            Console.WriteLine("Start RdfTrees");
            NameSpaceStore nameSpaceStore = new NameSpaceStore(path);
            RdfTrees rtrees = new RdfTrees(path, new StringIntMD5RAMCollision(path), new PredicatesCoding(path), nameSpaceStore,   new LiteralStoreSplited(path, nameSpaceStore));
            
            //rtrees.LoadTurtle(@"D:\home\FactographDatabases\dataset\dataset1M.ttl");
            //return;

            // Разогрев
            rtrees.WarmUp();
            // Трассировка
            XElement tracing = XElement.Load(@"C:\Users\Lena\Downloads\tracing100th.xml");
            Console.WriteLine("N_tests = {0}", tracing.Elements().Count());
            tt0 = DateTime.Now;
            int ecnt = 0, ncnt = 0;
            foreach (XElement spo in tracing.Elements())
            {
                XAttribute s_att = spo.Attribute("subj");
                XAttribute p_att = spo.Attribute("pred");
                XAttribute o_att = spo.Attribute("obj");
                XAttribute r_att = spo.Attribute("res");
                string s = s_att == null? null : s_att.Value;
                string p = p_att == null ? null : p_att.Value;
                string o = o_att == null ? null : o_att.Value;
                string res = r_att == null ? null : r_att.Value;
                if (spo.Name == "spo_")
                {
                    bool r = rtrees.ChkOSubjPredObj(
                        s.GetHashCode(),
                        p.GetHashCode(),
                        o.GetHashCode());
                    if ((res == "true" && r) || (res == "false" && !r)) { ecnt++; }
                    else ncnt++;
                }
                else if (spo.Name == "spD_")
                {
                   var lit = rtrees.GetDataBySubjPred(
                        s.GetHashCode(),
                        p.GetHashCode()).FirstOrDefault();
                    if (lit == null) { ncnt++; }
                    else
                    {
                        bool isEq = false;
                        if (lit.vid == LiteralVidEnumeration.text &&
                            ((Text)lit.Value).Value == res.Substring(1, res.Length - 2)) isEq = true;
                        else isEq = lit.ToString() == res;
                        if (isEq) ecnt++; else ncnt++;
                    }
                }
                else if (spo.Name == "spO")
                {
                    var query = rtrees.GetObjBySubjPred(
                        s.GetHashCode(),
                        p.GetHashCode()).OrderBy(v => v).ToArray();
                    if (query.Count() == 0 && res == "") continue;
                    ecnt++;
                }
                else if (spo.Name == "Spo_")
                {
                    var query = rtrees.GetSubjectByObjPred(
                        o.GetHashCode(),
                        p.GetHashCode()).OrderBy(v => v).ToArray();
                    if (query.Count() == 0 && res == "") continue;
                    ecnt++;
                }
                
            }
         //   Console.WriteLine("Equal {0} Not equal {1} debug counter {2}", ecnt, ncnt, rtrees.debug_counter);
            Console.WriteLine("TOTAL: {0} мс.", (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
    }
}
