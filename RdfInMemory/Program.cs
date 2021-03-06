﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;

namespace RdfInMemory
{
    public class Program
    {
        public static void Main0(string[] args)
        {
            DateTime tt0 = DateTime.Now;

            string path = @"..\..\..\Databases\";
            Console.WriteLine("Start RdfInMemory");
            RdfGraph graph = new RdfGraph(path);
            Console.WriteLine("Graph ok. duration={0}", (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            
            // Загрузка
            //graph.LoadTurtle(@"D:\home\FactographDatabases\dataset\dataset10M.ttl");
            //return;
            
            // Разогрев
            //graph.WarmUp();
            // Трассировка
            XElement tracing = XElement.Load(@"D:\Users\Marchuk\Downloads\tracing100th.xml");
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
                if (spo.Name == "spo")
                {
                    bool r = graph.ChkSubjPredObj(
                        s.GetHashCode(),
                        p.GetHashCode(),
                        o.GetHashCode());
                    if ((res == "true" && r) || (res == "false" && !r)) { ecnt++; }
                    else ncnt++;
                }
                else if (spo.Name == "spD")
                {
                    IEnumerable<long> codes = graph.GetDataCodeBySubjPred(
                        s.GetHashCode(),
                        p.GetHashCode());
                    Literal lit = null;
                    // Несколько экзотичный способ получения FirstOrDefault()
                    foreach (var litcode in codes)
                    {
                        lit = graph.DecodeDataCode(litcode);
                        break;
                    }
                    if (lit == null) { ncnt++; }
                    else
                    {
                        bool isEq = false;
                        if (lit.Vid == LiteralVidEnumeration.text &&
                            ((Text)lit.Value).Value == res.Substring(1, res.Length - 2)) isEq = true;
                        else isEq = lit.ToString() == res;
                        if (isEq) ecnt++; else ncnt++;
                    }
                }
                else if (spo.Name == "spO")
                {
                    var query = graph.GetObjBySubjPred(
                        s.GetHashCode(),
                        p.GetHashCode()).OrderBy(v => v).ToArray();
                    if (query.Count() == 0 && res == "") continue;
                    ecnt++;
                }
                else if (spo.Name == "Spo")
                {
                    var query = graph.GetSubjectByObjPred(
                        o.GetHashCode(),
                        p.GetHashCode()).OrderBy(v => v).ToArray();
                    if (query.Count() == 0 && res == "") continue;
                    ecnt++;
                }
                
            }
            Console.WriteLine("TOTAL: {0} мс. ecnt={1} ncnt={2}", (DateTime.Now - tt0).Ticks / 10000L, ecnt, ncnt); tt0 = DateTime.Now;
            //Console.ReadKey();
        }
    }
}
