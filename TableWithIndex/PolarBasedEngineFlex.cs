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
                new NamedType("lang", new PType(PTypeEnumeration.sstring))))),
            new NamedType("direct", new PTypeSequence(new PTypeRecord(
                new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                new NamedType("obj", new PType(PTypeEnumeration.sstring)))))
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
        private FlexIndex id_index;
        private Dictionary<string, PaEntry> item_dic = null;
        bool toload_dic = true;
        private VectorIndex name_index;
        private PaCell triplets; // Только объектные триплеты 
        private FlexIndex direct_index;
        private FlexIndex inverse_index;

        public PolarBasedEngineFlex(string path)
        {
            this.path = path;
            records = new PaCell(pt_records, path + "rdfrecords.pac", false);
            if (records.IsEmpty) records.Fill(new object[0]);
            //id_index = new FreeIndex(path + "rdf_id", records.Root, 1);
            id_index = new FlexIndex(path + "rdf_id", records.Root);
            name_index = new VectorIndex(path + "rdf_name", new PType(PTypeEnumeration.sstring), records.Root);
            triplets = new PaCell(pt_triplets, path + "rdftriplets.pac", false);
            if (triplets.IsEmpty) triplets.Fill(new object[0]);
            direct_index = new FlexIndex(path + "rdf_dir", triplets.Root);
            inverse_index = new FlexIndex(path + "rdf_inv", triplets.Root);
            if (toload_dic) item_dic = id_index.GetAll().ToDictionary(ent => (string)ent.Field(1).Get());
            //if (toload_dic) item_dic = records.Root.Elements().ToDictionary(ent => (string)ent.Field(1).Get());
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
                object[] direct = el.Elements()
                    .Where(sel => sel.Attribute(ONames.rdfresource) != null)
                    .Select(sel =>
                    {
                        return new object[] { sel.Name.NamespaceName + sel.Name.LocalName, 
                            sel.Attribute(ONames.rdfresource).Value };
                    }).ToArray();
                records.Root.AppendElement(new object[] { false, id_att.Value, type, fields, direct });
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
            id_index.Load(ent => ent.Field(1).Get());
            if (toload_dic) item_dic = id_index.GetAll().ToDictionary(ent => (string)ent.Field(1).Get());
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
            PaEntry ent;
            if (item_dic == null) ent = id_index.GetFirst(en => ((IComparable)en.Field(1).Get()).CompareTo(id));
            else { if (!item_dic.TryGetValue(id, out ent)) ent = new PaEntry(null, Int64.MinValue, null); }

            object[] item = (object[])ent.Get();
            XElement res = new XElement("record", new XAttribute("id", item[1]), new XAttribute("type", item[2]),
                ((object[])item[3]).Cast<object[]>().Select(v3 =>
                    new XElement("field", new XAttribute("prop", v3[0]),
                        string.IsNullOrEmpty((string)v3[2]) ? null : new XAttribute(ONames.xmllang, v3[2]),
                        v3[1])),
                ((object[])item[4]).Cast<object[]>().Select(v2 =>
                    new XElement("direct", new XAttribute("prop", v2[0]),
                        v2[1])),
                null);
            // Прямые ссылки
            //{
            //    var query = direct_index.GetAll(ient =>
            //    {
            //        string v = (string)ient.Field(1).Get();
            //        return v.CompareTo(id);
            //    });
            //    string predicate = null;
            //    XElement direct = null;
            //    foreach (PaEntry en in query)
            //    {
            //        var rec = (object[])en.Get();
            //        string pred = (string)rec[2];
            //        if (pred != predicate)
            //        {
            //            res.Add(direct);
            //            direct = new XElement("direct", new XAttribute("prop", pred));
            //            predicate = pred;
            //        }
            //        string idd = (string)rec[3];
            //        direct.Add(new XElement("record", new XAttribute("id", idd)));
            //    }
            //    res.Add(direct);
            //}
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
            }
            return res;
        }

        public XElement GetItemById(string id, XElement format)
        {
            PaEntry ent;
            if (item_dic == null) ent = id_index.GetFirst(en => ((IComparable)en.Field(1).Get()).CompareTo(id));
            else { if (!item_dic.TryGetValue(id, out ent)) ent = new PaEntry(null, Int64.MinValue, null); }
            if (ent.offset == Int64.MinValue) return null;

            object[] item = (object[])ent.Get();
            var type_att = format.Attribute("type");
            if (type_att != null && type_att.Value != (string)item[2]) return null;
            return GetItemById(item, format);
        }
        private XElement GetItemById(object[] item, XElement format)
        {
            object[] fields = (object[])item[3];
            object[] direct = (object[])item[4];
            string id = (string)item[1];
            XElement record = new XElement("record", new XAttribute("id", item[1]), new XAttribute("type", item[2]));

            var f_props = format.Elements("field").Select(f => f.Attribute("prop").Value);
            record.Add(f_props
                .SelectMany(prop => fields.Cast<object[]>()
                    .Where(v3 => (string)v3[0] == prop)
                    .Select(v3 => new XElement("field", new XAttribute("prop", v3[0]),
                        string.IsNullOrEmpty((string)v3[2]) ? null : new XAttribute(ONames.xmllang, ((object[])v3)[2]),
                        v3[1]))));
            // Прямые ссылки
            record.Add(format.Elements("direct")
                .Select(di =>
                {
                    string prop = di.Attribute("prop").Value;
                    XElement xdirect = new XElement("direct", new XAttribute("prop", prop),
                        direct.Cast<object[]>()
                        .Where(v2 => (string)v2[0] == prop)
                        .Select(v2 => 
                        {
                            string idd = (string)v2[1];
                            var xrec = di.Elements("record")
                            .Select(r => GetItemById(idd, r))
                            .FirstOrDefault(x => x != null);
                            return xrec;
                        }));
                    return xdirect.IsEmpty ? null : xdirect;
                }));
            //record.Add(format.Elements("direct")
            //    .Select(fd =>
            //    {
            //        string prop = fd.Attribute("prop").Value;
            //        XElement xdirect = new XElement("direct", new XAttribute("prop", prop),
            //            direct_index.GetAll(tent =>
            //            {
            //                object[] triplet = (object[])tent.Get();
            //                string subj = (string)triplet[1];
            //                string pred = (string)triplet[2];
            //                int cmp = subj.CompareTo(id);
            //                return cmp != 0 ? cmp : pred.CompareTo(prop);
            //            })
            //            .Select(tent =>
            //            {
            //                string obj_id = (string)tent.Field(3).Get();
            //                var xrec = fd.Elements("record")
            //                .Select(r => GetItemById(obj_id, r))
            //                .FirstOrDefault();
            //                return xrec;
            //            }));
            //        return xdirect.IsEmpty ? null : xdirect;
            //    }));
            record.Add(format.Elements("inverse")
                .Select(fi =>
                {
                    string prop = fi.Attribute("prop").Value;
                    XElement xinverse = new XElement("inverse", new XAttribute("prop", prop),
                        inverse_index.GetAll(tent =>
                        {
                            object[] triplet = (object[])tent.Get();
                            string subj = (string)triplet[3];
                            string pred = (string)triplet[2];
                            int cmp = subj.CompareTo(id);
                            return cmp != 0 ? cmp : pred.CompareTo(prop);
                        })
                        .Select(tent =>
                        {
                            string subject_id = (string)tent.Field(1).Get();
                            var xrec = fi.Elements("record")
                            .Select(r => GetItemById(subject_id, r))
                            .FirstOrDefault();
                            return xrec;
                        }));
                    return xinverse.IsEmpty ? null : xinverse;
                }));
            return record;
        }


        //========================================================= для тестирования, не для использования
        public PValue GetById(string id)
        {
            PaEntry ent = item_dic == null ? id_index.GetFirst(en => ((IComparable)en.Field(1).Get()).CompareTo(id))
                : item_dic[id];
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
