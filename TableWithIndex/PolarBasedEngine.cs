﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;

namespace TableWithIndex
{
    public class PolarBasedEngine
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
                new NamedType("obj", new PType(PTypeEnumeration.sstring)))))));

        private string path;
        private PaCell records;
        private FreeIndex id_index;
        private VectorIndex name_index;
        private VectorIndex inverse_index;

        public PolarBasedEngine(string path)
        {
            this.path = path;
            records = new PaCell(pt_records, path + "rdfrecords.pac", false);
            if (records.IsEmpty) records.Fill(new object[0]);
            id_index = new FreeIndex(path + "rdf_id", records.Root, 1);
            name_index = new VectorIndex(path + "rdf_name", new PType(PTypeEnumeration.sstring), records.Root);
            inverse_index = new VectorIndex(path + "rdf_inverse", new PType(PTypeEnumeration.sstring), records.Root);
        }
        public void Load(XElement db) 
        {
            records.Clear();
            records.Fill(new object[0]);
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
            }
            records.Flush();
        }
        public void MakeIndexes()
        {
            id_index.Load();
            name_index.Load(ent =>
                ((object[])ent.Field(3).Get())
                .Cast<object[]>()
                .Where(r3 => (string)r3[0] == ONames.p_name)
                .Select(r3 => new object[] { r3[1], ent.offset }).ToArray());
            inverse_index.Load(ent =>
                ((object[])ent.Field(4).Get())
                .Cast<object[]>()
                .Select(r2 => new object[] { r2[1], ent.offset }).ToArray());
        }
        //=========================================================
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
                        string.IsNullOrEmpty((string)v3[2])?null: new XAttribute(ONames.xmllang, v3[2]),
                        v3[1])),
                ((object[])item[4]).Cast<object[]>().Select(v2 =>
                    new XElement("direct", new XAttribute("prop", v2[0]),
                        v2[1])),
                null);
            if (addinverse)
            {
                var qu = inverse_index.GetAll(id).Select(en => en.Get()).Cast<object[]>()
                    .SelectMany(v5 => ((object[])v5[4]).Where(v2 => (string)((object[])v2)[1] == id).Select(v2 => 
                        new XElement("inverse",
                            new XAttribute("prop", ((object[])v2)[0]),
                            new XElement("record", new XAttribute("id", v5[1])))));
                res.Add(qu);
            }
            return res;
        }
        public XElement GetItemById(string id, XElement format)
        {
            PaEntry ent = id_index.GetFirst(id);
            if (ent.offset == Int64.MinValue) return null;
            object[] valu = (object[])ent.Get();
            XElement record = GetItemF5(valu, format);
            return record;
        }
        private XElement GetItemF5(object[] item, XElement format)
        {
            string id = (string)item[1];
            string type = (string)item[2];
            var tp_att = format.Attribute("type");
            if (tp_att != null && tp_att.Value != type) return null;
            object[] fields = (object[])item[3];
            object[] direct = (object[])item[4];
            
            XElement rec = new XElement("record", new XAttribute("id", id), new XAttribute("type", type));

            //string[] f_props = format.Elements("field").Select(f => f.Attribute("prop").Value).ToArray();
            //string[] d_props = format.Elements("direct").Select(f => f.Attribute("prop").Value).ToArray();
            //string[] i_props = format.Elements("inverse").Select(f => f.Attribute("prop").Value).ToArray();
            var f_props = format.Elements("field").Select(f => f.Attribute("prop").Value);
            var d_props = format.Elements("direct").Select(f => f.Attribute("prop").Value);
            var i_props = format.Elements("inverse").Select(f => f.Attribute("prop").Value);
            if (f_props.Count() > 0)
            rec.Add(fields
                .Where(v3 => f_props.Contains<string>((string)((object[])v3)[0]))
                .Select(v3 => new XElement("field", new XAttribute("prop", ((object[])v3)[0]),
                    string.IsNullOrEmpty((string)((object[])v3)[0]) ? null : new XAttribute(ONames.xmllang, ((object[])v3)[2]),
                    ((object[])v3)[1]
                    )));
            if (d_props.Count() > 0)
            rec.Add(direct
                .Where(v2 => d_props.Contains<string>((string)((object[])v2)[0]))
                .Select(v2 =>
                {
                    string prop = (string)((object[])v2)[0];
                    var fmts = format.Elements("direct")
                        .Where(d => d.Attribute("prop").Value == prop)
                        .SelectMany(d => d.Elements("record"));
                    foreach (XElement fmt in fmts)
                    {
                        XElement r = GetItemById((string)((object[])v2)[1], fmt);
                        if (r != null) return new XElement("direct", new XAttribute("prop", prop), r);
                    }
                    return null;

                }));
            if (i_props.Count() > 0)
            foreach (object[] inv_obj in inverse_index.GetAll(id).Select(ent => ent.Get()).Cast<object[]>())
            {
                // Какое именно отношение было использовано?
                var pair = ((object[])inv_obj[4]).FirstOrDefault(v2 => (string)((object[])v2)[1] == id);
                if (pair == null) continue;
                string prop = (string)((object[])pair)[0];
                if (!i_props.Contains(prop)) continue;
                string tp = (string)((object[])inv_obj)[2];
                XElement fmt = format.Elements("inverse")
                    .Where(d => d.Attribute("prop").Value == prop)
                    .SelectMany(d => d.Elements("record"))
                    .FirstOrDefault(r => { var type_att = r.Attribute("type"); return type_att == null || type_att.Value == tp; });
                if (fmt == null) continue;
                XElement xinv = GetItemF5(inv_obj, fmt);
                if (xinv == null) continue;
                rec.Add(new XElement("inverse", new XAttribute("prop", prop), xinv));
            }

            //if (f_props.Length > 0)
            //{
            //    rec.Add(fields.Where(v3 => f_props.Contains<string>((string)((object[])v3)[0]))
            //        .Select(v3 => new XElement("field", new XAttribute("prop", ((object[])v3)[0]),
            //            string.IsNullOrEmpty((string)((object[])v3)[0]) ? null : new XAttribute(ONames.xmllang, ((object[])v3)[2]),
            //            ((object[])v3)[1]
            //            )));
            //}
            //if (d_props.Length > 0)
            //{
            //    rec.Add(direct.Where(v2 => d_props.Contains<string>((string)((object[])v2)[0]))
            //        .Select(v2 => 
            //        {
            //            string prop = (string)((object[])v2)[0];
            //            var fmts = format.Elements("direct")
            //                .Where(d => d.Attribute("prop").Value == prop)
            //                .SelectMany(d => d.Elements("record"));
            //            foreach (XElement fmt in fmts)
            //            {
            //                XElement r = GetItemById((string)((object[])v2)[1], fmt);
            //                if (r != null) return new XElement("direct", new XAttribute("prop", prop), r);
            //            }
            //            return null;
                        
            //        }));
            //}
            //if (i_props.Length > 0)
            //{
            //    foreach (object[] inv_obj in inverse_index.GetAll(id).Select(ent => ent.Get()).Cast<object[]>())
            //    {
            //        // Какое именно отношение было использовано?
            //        var pair = ((object[])inv_obj[4]).FirstOrDefault(v2 => (string)((object[])v2)[1] == id);
            //        if (pair == null) continue;
            //        string prop = (string)((object[])pair)[0];
            //        if (!i_props.Contains(prop)) continue;
            //        string tp = (string)((object[])inv_obj)[2];
            //        XElement fmt = format.Elements("inverse")
            //            .Where(d => d.Attribute("prop").Value == prop)
            //            .SelectMany(d => d.Elements("record"))
            //            .FirstOrDefault(r => { var type_att = r.Attribute("type"); return type_att == null || type_att.Value == tp; });
            //        if (fmt == null) continue;
            //        XElement xinv = GetItemF5(inv_obj, fmt);
            //        if (xinv == null) continue;
            //        rec.Add(new XElement("inverse", new XAttribute("prop", prop), xinv));
            //    }
            //}
            return rec;
        }

        //=========================================================
        public void GetById(string id)
        {
            PaEntry ent = id_index.GetFirst(id);
            var val = ent.GetValue();
            //Console.WriteLine(val.Type.Interpret(val.Value));
        }

        public void MakeVector()
        {
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
            foreach (PaEntry ent in inverse_index.GetAll(id))
            {
                var val = ent.GetValue();
                Console.WriteLine(val.Type.Interpret(val.Value));
            }
        }
    }
}
