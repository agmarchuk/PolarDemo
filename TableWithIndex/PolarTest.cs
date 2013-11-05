using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;

namespace TableWithIndex
{
    public class PolarTest
    {
        string path;
        private PType tp_seq;
        private PaCell cell;
        public PolarTest(string path)
        {
            this.path = path;
            tp_seq = new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.sstring)),
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("fd", new PType(PTypeEnumeration.sstring)),
                new NamedType("deleted", new PType(PTypeEnumeration.boolean))
                ));
            this.cell = new PaCell(tp_seq, path + @"bigtest.pac", false);
            this.index = new PIndex(path, cell, 0);
        }
        public void Load(IEnumerable<XElement> db_flow)
        {
            cell.Clear();
            cell.Fill(new object[0]);
            foreach (XElement element in db_flow)
            {
                var fd_el = element.Element(ONames.tag_fromdate);

                string id = element.Attribute(ONames.rdfabout).Value;
                string name = element.Element(ONames.tag_name).Value;
                string fd = fd_el == null ? "" : fd_el.Value;
                cell.Root.AppendElement(new object[] { id, name, fd, false });
            }
            cell.Flush();
            Console.WriteLine("Число записей: " + cell.Root.Count());
        }
        private PIndex index;
        public void CreateIndex()
        {
            this.index.Create();
        }
        public void SelectById(string id)
        {
            long offset = index.SelectFirst(id);
            //Console.WriteLine("offset=" + offset);
            PaEntry entry = cell.Root.Element(0);
            entry.offset = offset;

            PValue pv = entry.Get();
            Console.WriteLine("record=" + pv.Type.Interpret(pv.Value));
        }

    }
}
