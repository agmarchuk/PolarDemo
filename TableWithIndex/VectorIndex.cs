using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TableWithIndex
{
    public class VectorIndex
    {
        private PaEntry table;
        private string indexName;
        private PType keyType;
        private PType tp_intern;
        private PaCell intern_cell;
        private FreeIndex key_index;

        public VectorIndex(string indexName, PType keyType, PaEntry table)
        {
            this.indexName = indexName;
            this.keyType = keyType;
            this.table = table;
            tp_intern = new PTypeSequence(new PTypeRecord(
                new NamedType("deleted", new PType(PTypeEnumeration.boolean)),
                new NamedType("outoffset", new PType(PTypeEnumeration.longinteger)),
                new NamedType("key", keyType)));
            intern_cell = new PaCell(tp_intern, indexName + "_vind.pac", false);
            if (intern_cell.IsEmpty) intern_cell.Fill(new object[0]);
            key_index = new FreeIndex(indexName + "_v", intern_cell.Root, 2);
        }
        public void Load(Func<PaEntry, object[][]> genPairs)
        {
            intern_cell.Clear();
            intern_cell.Fill(new object[0]);
            foreach (PaEntry rec in table.Elements())
            {
                object[][] pairs = genPairs(rec);
                foreach (object[] pair in pairs)
                {
                    intern_cell.Root.AppendElement(new object[] { false, pair[0], pair[1] });
                }
            }
            intern_cell.Flush();
            key_index.Load();
        }
        public IEnumerable<PaEntry> Search(string sample)
        {
            PaEntry entry_table = table.Element(0);
            return key_index.SearchAll(sample).Select(ent => 
            {
                object[] v3 = (object[])ent.Get();
                entry_table.offset = (long)v3[1];
                return entry_table;
            });
        }
        public IEnumerable<PaEntry> GetAll(object key)
        {
            PaEntry entry_table = table.Element(0);
            var query = key_index.GetAll(key).Select(ent =>
                {
                    object[] v3 = (object[])ent.Get();
                    entry_table.offset = (long)v3[1];
                    return entry_table;
                });
            return query;
        }
    }
}
