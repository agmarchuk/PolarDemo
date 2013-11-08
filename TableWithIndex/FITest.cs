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
        private PaCell cell_table;
        private string path;
        FreeIndex id_index = null;
        FreeIndex name_index = null;
        int id_field = 1;
        int name_field = 2;
        public FITest(string path)
        {
            this.path = path;
            PType tp = new PTypeSequence(new PTypeRecord(
                new NamedType("deleted", new PType(PTypeEnumeration.boolean)),
                new NamedType("id", new PType(PTypeEnumeration.sstring)),
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("fd", new PType(PTypeEnumeration.sstring))
                ));

            cell_table = new PaCell(tp, path + "twi.pac", false);
            //index_cell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), path + "id_twi.pac", false);
            if (!cell_table.IsEmpty)
            {
                id_index = new FreeIndex(path + "twi_id_index", cell_table.Root, id_field);
                name_index = new FreeIndex(path + "twi_name_index", cell_table.Root, name_field);
            }
        }
        public void Load(IEnumerable<XElement> element_flow)
        {
            cell_table.Clear();
            cell_table.Fill(new object[0]);
            foreach (XElement element in element_flow)
            {
                var fd_el = element.Element(ONames.tag_fromdate);

                string id = element.Attribute(ONames.rdfabout).Value;
                string name = element.Element(ONames.tag_name).Value;
                string fd = fd_el == null ? "" : fd_el.Value;
                long off = cell_table.Root.AppendElement(new object[] { false, id, name, fd });
            }
            cell_table.Flush();
        }
        public void CreateIndexes()
        {
            if (id_index == null)
            {
                id_index = new FreeIndex(path + "twi_id_index", cell_table.Root, id_field);
            }
            if (name_index == null)
            {
                name_index = new FreeIndex(path + "twi_name_index", cell_table.Root, name_field);
            }
            id_index.Load();
            name_index.Load();
        }
        public PValue GetById(string id)
        {
            return id_index.GetById(id);
        }
        public IEnumerable<PValue> Search(string sample)
        {
            return name_index.SearchAll(sample).Select(ent => ent.GetValue());
        }
    }
}
