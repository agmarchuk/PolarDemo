using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;
namespace TableWithIndex
{
    public class FITest
    {
        private PaCell cell;
        private string path;
        private PaCell index_cell;
        public FITest(string path)
        {
            this.path = path;
            PType tp = new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.sstring)),
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("fd", new PType(PTypeEnumeration.sstring)),
                new NamedType("deleted", new PType(PTypeEnumeration.boolean))));

            cell = new PaCell(tp, path + "twi.pac", false);
            index_cell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), path + "id_twi.pac", false);
        }
        public void Load(IEnumerable<XElement> element_flow)
        {
            cell.Clear();
            index_cell.Clear();
            cell.Fill(new object[0]);
            index_cell.Fill(new object[0]);
            foreach (XElement element in element_flow)
            {
                var fd_el = element.Element(ONames.tag_fromdate);

                string id = element.Attribute(ONames.rdfabout).Value;
                string name = element.Element(ONames.tag_name).Value;
                string fd = fd_el == null ? "" : fd_el.Value;
                long offset = cell.Root.AppendElement(new object[] { id, name, fd, false });
                index_cell.Root.AppendElement(offset);
            }
            cell.Flush();
            index_cell.Flush();
            // Попробую сортировать index_cell специальным образом: значение (long) используется как offset ячейки и там прочитывается нулевое поле
            var ptr = cell.Root.Element(0);
            index_cell.Root.SortByKey<string>((object v) =>
            {
                ptr.offset = (long)v;
                return (string)ptr.Field(0).Get().Value;
            });
        }
        public PValue GetById(string id)
        {
            PaEntry entry = cell.Root.Element(0);
            PaEntry index_entry = index_cell.Root.BinarySearchFirst(ent =>
            {
                long off = (long)ent.Get().Value;
                entry.offset = off;
                return 0 - id.CompareTo((string)entry.Field(0).Get().Value);
            });
            if (index_entry.offset == Int64.MinValue) return new PValue(null, Int64.MinValue, null);
            long cell_offset = (long)index_entry.Get().Value;
            entry.offset = cell_offset;
            var rec = entry.Get();
            //Console.WriteLine(rec.Type.Interpret(rec.Value));
            return rec;
        }
        public void Search(string ss)
        {
        }
    }
}
