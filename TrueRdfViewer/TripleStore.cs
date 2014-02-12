using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;
using sema2012m;

namespace TrueRdfViewer
{
    public class TripleStore
    {
        private string path;
        private PType tp_otriple_seq;
        private PType tp_dtriple_seq;
        private PaCell otriples;
        private PaCell dtriples;
        private FlexIndexView<SubjPredObj> spo_o_index = null;
        private FlexIndexView<SubjPred> sp_d_index = null;
        private FlexIndexView<SubjPred> op_o_index = null;

        public TripleStore(string path)
        {
            this.path = path;
            InitTypes();
            otriples = new PaCell(tp_otriple_seq, path + "otriples.pac", false);
            dtriples = new PaCell(tp_dtriple_seq, path + "dtriples.pac", false);
            if (!otriples.IsEmpty)
            {
                OpenCreateIndexes();
            }
        }

        private void OpenCreateIndexes()
        {
            spo_o_index = new FlexIndexView<SubjPredObj>(path + "spo_o_index", otriples.Root,
                ent => new SubjPredObj() { subj = (string)ent.Field(0).Get(), pred = (string)ent.Field(1).Get(), obj = (string)ent.Field(2).Get() });
            sp_d_index = new FlexIndexView<SubjPred>(path + "subject_d_index", dtriples.Root,
                ent => new SubjPred() { subj = (string)ent.Field(0).Get(), pred = (string)ent.Field(1).Get() });
            op_o_index = new FlexIndexView<SubjPred>(path + "obj_o_index", otriples.Root,
                ent => new SubjPred() { subj = (string)ent.Field(2).Get(), pred = (string)ent.Field(1).Get() });
        }

        public void LoadTurtle(string filepath)
        {
            otriples.Clear();
            otriples.Fill(new object[0]);
            dtriples.Clear();
            dtriples.Fill(new object[0]);
            int i = 0;
            foreach (var triple in Turtle.LoadGraph(filepath))
            {
                if (i % 10000 == 0) Console.Write("{0} ", i / 10000);
                i++;
                if (triple is OTriple)
                {
                    var tr = (OTriple)triple;
                    //Console.WriteLine(tr.obj);
                    otriples.Root.AppendElement(new object[] { tr.sublect, tr.predicate, tr.obj });
                }
                else
                {
                    var tr = (DTriple)triple;
                    //Console.WriteLine("{0} {1}", tr.data.vid, tr.data.value);
                    dtriples.Root.AppendElement(new object[] { tr.sublect, tr.predicate, 
                        new object[] { 2, new object[] { tr.data.value, "en" } } });
                }
            }
            Console.WriteLine();
            otriples.Flush();
            dtriples.Flush();
            // Индексирование
            if (spo_o_index == null)
            {
                OpenCreateIndexes();
            }
            spo_o_index.Load(null);
            sp_d_index.Load(null);
            op_o_index.Load(null);
        }
        public void LoadXML(string filepath)
        {
            otriples.Clear();
            otriples.Fill(new object[0]);
            dtriples.Clear();
            dtriples.Fill(new object[0]);
            XElement db = XElement.Load(filepath);
            foreach (XElement elem in db.Elements())
            {
                var id_att = elem.Attribute(ONames.rdfabout);
                if (id_att == null) continue;
                string id = id_att.Value;
                string typ = elem.Name.NamespaceName + elem.Name.LocalName;
                otriples.Root.AppendElement(
                    new object[] { id, ONames.rdfnsstring + "type", typ } );
                foreach (XElement el in elem.Elements())
                {
                    string prop = el.Name.NamespaceName + el.Name.LocalName;
                    var resource_att = el.Attribute(ONames.rdfresource);
                    if (resource_att != null)
                    { // Объектная ссылка
                        otriples.Root.AppendElement(
                            new object[] { id, prop, resource_att.Value } );
                    }
                    else
                    { // Поле данных
                        var lang_att = el.Attribute(ONames.xmllang);
                        string lang = lang_att == null ? "" : lang_att.Value;
                        dtriples.Root.AppendElement(
                            new object[] { id, prop, 
                                new object[] { 2, new object[] { el.Value, lang } } } );
                    }
                }
            }
            otriples.Flush();
            dtriples.Flush();
            // Индексирование
            if (!otriples.IsEmpty)
            {
                OpenCreateIndexes();
            }
            spo_o_index.Load(null);
            sp_d_index.Load(null);
            op_o_index.Load(null);
        }

        private Scale2 scale = null;
        public void CreateScale()
        {
            scale = new Scale2(26);
            foreach (object[] tr in otriples.Root.ElementValues())
            {
                string subj = (string)tr[0];
                string pred = (string)tr[1];
                string obj = (string)tr[2];
                int code = scale.Code(subj, pred, obj);
                int twobits = scale[code];
                if (twobits > 1) continue; // Уже "плохо"
                scale[code] = twobits + 1;
            }
        }
        public void ShowScale()
        {
            int c = scale.Count();
            int c0 = 0, c1 = 0, c2 = 0, cerr = 0; 
            for (int i=0; i<c; i++)
            {
                int tb = scale[i];
                if (tb == 0) c0++;
                else if (tb == 1) c1++;
                else if (tb == 2) c2++;
                else cerr++;
            }
            Console.WriteLine("{0} {1} {2} {3} err: {4}", c, c0, c1, c2, cerr);
        }

        public IEnumerable<string> GetSubjectByObjPred(string obj, string pred)
        {
            return op_o_index.GetAll(ent => 
            {
                string ob = (string)ent.Field(2).Get();
                int cmp = ob.CompareTo(obj);
                if (cmp != 0) return cmp;
                string pr = (string)ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => (string)en.Field(0).Get());
        }
        public IEnumerable<string> GetObjBySubjPred(string subj, string pred)
        {
            return spo_o_index.GetAll(ent =>
            {
                string su = (string)ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                string pr = (string)ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => (string)en.Field(2).Get());
        }
        public IEnumerable<Literal> GetDataBySubjPred(string subj, string pred)
        {
            return sp_d_index.GetAll(ent =>
            {
                string su = (string)ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                string pr = (string)ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => 
                {
                    object[] uni = (object[])en.Field(2).Get();
                    Literal lit = new Literal();
                    int vid = (int)uni[0];
                    if (vid == 1) { lit.vid = LiteralVidEnumeration.integer; lit.value = (int)uni[1]; }
                    if (vid == 3) { lit.vid = LiteralVidEnumeration.date; lit.value = (long)uni[1]; }
                    else if (vid == 2)
                    {
                        lit.vid = LiteralVidEnumeration.text;
                        object[] txt = (object[])uni[1];
                        lit.value = new Text() { s = (string)txt[0], l = (string)txt[1] };
                    }
                    return lit;
                });
        }
        public bool ChkOSubjPredObj(string subj, string pred, string obj)
        {
            if (scale != null)
            {
                int tb = scale[scale.Code(subj, pred, obj)];
                if (tb == 0) return false;
                else if (tb == 1) return true;
                // else надо считаль длинно, см. далее
            }
            return !spo_o_index.GetFirst(ent =>
            {
                string su = (string)ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                string pr = (string)ent.Field(1).Get();
                cmp = pr.CompareTo(pred);
                if (cmp != 0) return cmp;
                string ob = (string)ent.Field(2).Get();
                return ob.CompareTo(obj);
            }).IsEmpty;
        }
        public XElement GetItem(string subject)
        {
            if (otriples.Root.Count() == 0) return null;
            if (dtriples.Root.Count() == 0) return null;
            XElement res = new XElement("record", new XAttribute("id", subject));
            //PaEntry dent = dtriples.Root.Element(0);
            //foreach (var ent in subject_d_index.GetAllByKey(subject))
            foreach (var ent in sp_d_index.GetAll(en => ((string)en.Field(0).Get()).CompareTo(subject)))
            {
                object[] tr = (object[])ent.Get();
                string predicate = (string)tr[1];
                object[] literal = (object[])tr[2];
                res.Add(new XElement("field", new XAttribute("prop", predicate),
                    ((object[])literal[1])[0]));
            }
            string type = null;
            //PaEntry oent = otriples.Root.Element(0);
            //foreach (var ent in subject_o_index.GetAllByKey(subject))
            foreach (var ent in spo_o_index.GetAll(en => ((string)en.Field(0).Get()).CompareTo(subject)))
            {
                object[] tr = (object[])ent.Get();
                string predicate = (string)tr[1];
                string obj = (string)tr[2];
                if (predicate == ONames.rdftypestring)
                {
                    type = obj;
                }
                else
                {
                    res.Add(new XElement("direct", new XAttribute("prop", predicate),
                        new XElement("record", new XAttribute("id", obj))));
                }
            }
            if (type != null) res.Add(new XAttribute("type", type));
            // Обратные ссылки
            //foreach (var ent in obj_o_index.GetAllByKey(subject))
            foreach (var ent in op_o_index.GetAll(en => ((string)en.Field(2).Get()).CompareTo(subject)))
            {
                object[] tr = (object[])ent.Get();
                string subj = (string)tr[0];
                string predicate = (string)tr[1];
                res.Add(new XElement("inverse", new XAttribute("prop", predicate),
                    subj));
            }

            return res;
        }

        private void InitTypes()
        {
            PType tp_rliteral = new PTypeUnion(
                new NamedType("void", new PType(PTypeEnumeration.none)),
                new NamedType("integer", new PType(PTypeEnumeration.integer)),
                new NamedType("string", new PTypeRecord(
                    new NamedType("s", new PType(PTypeEnumeration.sstring)),
                    new NamedType("l", new PType(PTypeEnumeration.sstring)))),
                new NamedType("date", new PType(PTypeEnumeration.longinteger)));
            tp_otriple_seq = new PTypeSequence(new PTypeRecord(
                    new NamedType("subject", new PType(PTypeEnumeration.sstring)),
                    new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                    new NamedType("object", new PType(PTypeEnumeration.sstring))));
            tp_dtriple_seq = new PTypeSequence(new PTypeRecord(
                    new NamedType("subject", new PType(PTypeEnumeration.sstring)),
                    new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                    new NamedType("data", tp_rliteral)));
        }
    }
}
