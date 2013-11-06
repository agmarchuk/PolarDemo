using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;
namespace TableWithIndex
{
    public class SITest
    {
        private PaCell cell;
        private string path;
        private SemiIndex si;
        private SemiIndex ni;
        public SITest(string path)
        {
            this.path = path;
            PType tp = new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.sstring)),
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("fd", new PType(PTypeEnumeration.sstring)),
                new NamedType("deleted", new PType(PTypeEnumeration.boolean))));
            cell = new PaCell(tp, path + "twi.pac", false);
            si = new SemiIndex(path + "ind_id", new PType(PTypeEnumeration.sstring));
            ni = new SemiIndex(path + "ind_name", new PType(PTypeEnumeration.sstring));
        }
        public void Load(IEnumerable<XElement> element_flow) 
        {
            cell.Clear();
            cell.Fill(new object[0]);
            foreach (XElement element in element_flow)
            {
                var fd_el = element.Element(ONames.tag_fromdate);

                string id = element.Attribute(ONames.rdfabout).Value;
                string name = element.Element(ONames.tag_name).Value;
                string fd = fd_el == null ? "" : fd_el.Value;
                cell.Root.AppendElement(new object[] { id, name, fd, false });
            }
            cell.Flush();

            si.Load(cell.Root.Elements().Select(r4 =>
            {
                object[] o4 = (object[])r4.Value;
                return new object[] { o4[0], r4.Offset };
            }));
            ni.Load(cell.Root.Elements().Select(r4 =>
            {
                object[] o4 = (object[])r4.Value;
                return new object[] { ((string)o4[1]).ToLower(), r4.Offset };
            }));
        }
        public PValue GetById(string id)
        {
            long offset = si.FindFirst(id);
            PaEntry entry = cell.Root.Element(0);
            entry.offset = offset;
            var rec = entry.Get();
            //Console.WriteLine(rec.Type.Interpret(rec.Value));
            return rec;
        }
        public void Search(string ss)
        {
            ss = ss.ToLower();
            int n = ss.Length;
            foreach (var off in ni.FindAll(obj => {
                if (((string)obj).StartsWith(ss)) return 0;
                return ((string)obj).CompareTo(ss); 
            }))
            {
                PaEntry entry = cell.Root.Element(0);
                entry.offset = off;
                var rec = entry.Get();

                //Console.WriteLine("VAlu=" + rec.Type.Interpret(rec.Value));
            }
        }
    }
}
