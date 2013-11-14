using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;

namespace TableWithIndex
{
    public class TwoKeys : IComparable
    {
        public string primary;
        public string secondary;
        public int CompareTo(object obj)
        {
            TwoKeys tk2 = (TwoKeys)obj;
            int cmp = primary.CompareTo(tk2.primary);
            if (cmp != 0) return cmp;
            return secondary.CompareTo(tk2.secondary);
        }
    }
    public class PolarBasedEngineFlex
    {
        private PType pt_records = new PTypeSequence(new PTypeRecord(
            new NamedType("deleted", new PType(PTypeEnumeration.boolean)),
            new NamedType("id", new PType(PTypeEnumeration.sstring)),
            new NamedType("type", new PType(PTypeEnumeration.sstring)),
            new NamedType("fields", new PTypeSequence(new PTypeRecord(
                new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                new NamedType("data", new PType(PTypeEnumeration.sstring)),
                new NamedType("lang", new PType(PTypeEnumeration.sstring)))))
            //new NamedType("direct", new PTypeSequence(new PTypeRecord(
            //    new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
            //    new NamedType("obj", new PType(PTypeEnumeration.sstring))))),
            //new NamedType("inv_beg", new PType(PTypeEnumeration.longinteger)),
            //new NamedType("inv_count", new PType(PTypeEnumeration.longinteger))
                ));
        private PType pt_triplets = new PTypeSequence(new PTypeRecord(
            new NamedType("deleted", new PType(PTypeEnumeration.boolean)),
            new NamedType("subject", new PType(PTypeEnumeration.sstring)),
            new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
            new NamedType("obj", new PType(PTypeEnumeration.sstring))
            ));
        private string path;
        private PaCell records;
        private FreeIndex id_index;
        private VectorIndex name_index;
        private PaCell triplets; // Только объектные триплеты 
        private FlexIndex direct_index;
        private FlexIndex inverse_index;

        public PolarBasedEngineFlex(string path)
        {
            this.path = path;
            records = new PaCell(pt_records, path + "rdfrecords.pac", false);
            if (records.IsEmpty) records.Fill(new object[0]);
            id_index = new FreeIndex(path + "rdf_id", records.Root, 1);
            name_index = new VectorIndex(path + "rdf_name", new PType(PTypeEnumeration.sstring), records.Root);
            triplets = new PaCell(pt_triplets, path + "rdftriplets.pac", false);
            direct_index = new FlexIndex(path + "rdf_dir", triplets.Root);
            inverse_index = new FlexIndex(path + "rdf_inv", triplets.Root);
        }
        public void Load(XElement db)
        {
            records.Clear();
            records.Fill(new object[0]);
            triplets.Clear();
            triplets.Fill(new object[0]);
            foreach (XElement el in db.Elements())
            {
                var id_att = el.Attribute(ONames.rdfabout);
                if (id_att == null) continue;
                string type = el.Name.NamespaceName + el.Name.LocalName;
                object[] fields = el.Elements()
                    .Where(sel => sel.Attribute(ONames.rdfresource) == null)
                    .Select(sel =>
                    {
                        var lang_att = sel.Attribute(ONames.xmllang);
                        return new object[] { sel.Name.NamespaceName + sel.Name.LocalName, 
                            sel.Value,
                            lang_att == null? "" : lang_att.Value };
                    }).ToArray();
                records.Root.AppendElement(new object[] { false, id_att.Value, type, fields });
                foreach (XElement xprop in el.Elements().Where(xel => xel.Attribute(ONames.rdfresource) != null))
                {
                    string prop = xprop.Name.NamespaceName + xprop.Name.LocalName;
                    string resource = xprop.Attribute(ONames.rdfresource).Value;
                    triplets.Root.AppendElement(new object[] { false, id_att.Value, prop, resource });
                }
            }
            records.Flush();
            triplets.Flush();
        }
        public void MakeIndexes()
        {
            id_index.Load();
            name_index.Load(ent =>
                ((object[])ent.Field(3).Get())
                .Cast<object[]>()
                .Where(r3 => (string)r3[0] == ONames.p_name)
                .Select(r3 => new object[] { ent.offset, r3[1] }).ToArray());
            direct_index.Load<TwoKeys>((PaEntry ent) =>
            {
                object[] rec = (object[])ent.Get();
                return new TwoKeys() { primary = (string)rec[1], secondary = (string)rec[2] };
            });
            inverse_index.Load<TwoKeys>((PaEntry ent) =>
            {
                object[] rec = (object[])ent.Get();
                return new TwoKeys() { primary = (string)rec[3], secondary = (string)rec[2] };
            });
        }
        public IEnumerable<XElement> SearchByName(string searchstring)
        {
            foreach (PaEntry ent in name_index.Search(searchstring))
            {
                object[] valu = (object[])ent.Get();
                yield return new XElement("record",
                    new XAttribute("id", valu[1]),
                    new XAttribute("type", valu[2]),
                    ((object[])valu[3]).Where(v3 => (string)((object[])v3)[0] == ONames.p_name)
                    .Select(v3 => new XElement("field", new XAttribute("prop", ((object[])v3)[0]), ((object[])v3)[1]))
                    .FirstOrDefault());
            }
        }
        public XElement GetItemByIdBasic(string id, bool addinverse)
        {
            PaEntry ent = id_index.GetFirst(id);
            object[] item = (object[])ent.Get();
            XElement res = new XElement("record", new XAttribute("id", item[1]), new XAttribute("type", item[2]),
                ((object[])item[3]).Cast<object[]>().Select(v3 =>
                    new XElement("field", new XAttribute("prop", v3[0]),
                        string.IsNullOrEmpty((string)v3[2]) ? null : new XAttribute(ONames.xmllang, v3[2]),
                        v3[1])),
                //((object[])item[4]).Cast<object[]>().Select(v2 =>
                //    new XElement("direct", new XAttribute("prop", v2[0]),
                //        v2[1])),
                null);
            // Прямые ссылки
            {
                var query = inverse_index.GetAll(ient =>
                {
                    string v = (string)ient.Field(1).Get();
                    return v.CompareTo(id);
                });
                string predicate = null;
                XElement direct = null;
                foreach (PaEntry en in query)
                {
                    var rec = (object[])en.Get();
                    string pred = (string)rec[2];
                    if (pred != predicate)
                    {
                        res.Add(direct);
                        direct = new XElement("direct", new XAttribute("prop", pred));
                        predicate = pred;
                    }
                    string idd = (string)rec[1];
                    direct.Add(new XElement("record", new XAttribute("id", idd)));
                }
                res.Add(direct);
            }
            // Обратные ссылки
            if (addinverse)
            {
                var query = inverse_index.GetAll(ient =>
                {
                    string v = (string)ient.Field(3).Get();
                    return v.CompareTo(id);
                });
                string predicate = null;
                XElement inverse = null;
                foreach (PaEntry en in query)
                {
                    var rec = (object[])en.Get();
                    string pred = (string)rec[2];
                    if (pred != predicate)
                    {
                        res.Add(inverse);
                        inverse = new XElement("inverse", new XAttribute("prop", pred));
                        predicate = pred;
                    }
                    string idd = (string)rec[1];
                    inverse.Add(new XElement("record", new XAttribute("id", idd)));
                }
                res.Add(inverse);

                //foreach (PaEntry en in query)
                //{
                //    var rec = (object[])en.Get();
                //    res.Add(new XElement("inverse", new XAttribute("prop", rec[2]),
                //        new XElement("record", new XAttribute("id", rec[1]))));
                //}
            }
            return res;
        }

        public XElement GetItemById(string id, XElement format)
        {
            PaEntry ent = id_index.GetFirst(id);
            if (ent.offset == Int64.MinValue) return null;
            object[] item = (object[])ent.Get();
            var type_att = format.Attribute("type");
            if (type_att != null && type_att.Value != (string)item[2]) return null;

            object[] fields = (object[])item[3];
            XElement record = new XElement("record", new XAttribute("id", item[1]), new XAttribute("type", item[2]));

            var f_props = format.Elements("field").Select(f => f.Attribute("prop").Value);
            //var d_props = format.Elements("direct").Select(f => f.Attribute("prop").Value);
            //var i_props = format.Elements("inverse").Select(f => f.Attribute("prop").Value);
            //if (f_props.Count() > 0)
            //    record.Add(fields
            //        //.Where(v3 => f_props.Contains<string>((string)((object[])v3)[0]))
            //        .Select(v3 => new XElement("field", new XAttribute("prop", ((object[])v3)[0]),
            //            string.IsNullOrEmpty((string)((object[])v3)[0]) ? null : new XAttribute(ONames.xmllang, ((object[])v3)[2]),
            //            ((object[])v3)[1]
            //            )));
            //if (d_props.Count() > 0)
            //{
            //}
            record.Add(f_props
                .SelectMany(prop => fields.Cast<object[]>()
                    .Where(v3 => (string)v3[0] == prop)
                    .Select(v3 => new XElement("field", new XAttribute("prop", v3[0]),
                        string.IsNullOrEmpty((string)v3[2]) ? null : new XAttribute(ONames.xmllang, ((object[])v3)[2]),
                        v3[1]))));
            record.Add(format.Elements("direct")
                .Select(fd => 
                {
                    string prop = fd.Attribute("prop").Value;
                    return new XElement("direct", new XAttribute(fd.Attribute("prop")),
                    direct_index.GetAll(tent => 
                    {
                        object[] triplet = (object[])tent.Get();
                        string subj = (string)triplet[1];
                        string pred = (string)triplet[2];
                        int cmp = subj.CompareTo(id);
                        return cmp != 0? cmp : pred.CompareTo(prop);
                    }));
                }));
            record.Add(format.Elements("inverse")
                .Select(fi =>
                {
                    string prop = fi.Attribute("prop").Value;
                    return new XElement("inverse", new XAttribute("prop", prop),
                    inverse_index.GetAll(tent =>
                    {
                        object[] triplet = (object[])tent.Get();
                        string subj = (string)triplet[3];
                        string pred = (string)triplet[2];
                        int cmp = subj.CompareTo(id);
                        return cmp != 0 ? cmp : pred.CompareTo(prop);
                    })
                    .Count()
                    );
                }));
            return record;
        }


        //========================================================= для тестирования, не для использования
        public PValue GetById(string id)
        {
            PaEntry ent = id_index.GetFirst(id);
            return ent.GetValue();
        }

        public void Search(string ss)
        {
            foreach (PaEntry ent in name_index.Search(ss))
            {
                var val = ent.GetValue();
                Console.WriteLine(val.Type.Interpret(val.Value));
            }
        }
        public void GetInverse(string id)
        {
            var query = inverse_index.GetAll(ient => 
            {
                string v = (string)ient.Field(3).Get();
                return v.CompareTo(id);
            });
            foreach (PaEntry ent in query)
            {
                var val = ent.GetValue();
                Console.WriteLine(val.Type.Interpret(val.Value));
            }
        }
    }
}
