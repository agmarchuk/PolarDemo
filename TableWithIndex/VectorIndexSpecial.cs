﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TableWithIndex
{
    public class VectorIndexSpecial
    {
        private PaEntry table;
        private string indexName;
        private PType tp_intern;
        private PaCell intern_cell;
        private FreeIndex key_index;

        public VectorIndexSpecial(string indexName, PaEntry table)
        {
            this.indexName = indexName;
            this.table = table;
            tp_intern = new PTypeSequence(new PTypeRecord(
                new NamedType("deleted", new PType(PTypeEnumeration.boolean)),
                new NamedType("sourceoffset", new PType(PTypeEnumeration.longinteger)),
                new NamedType("key", new PType(PTypeEnumeration.sstring)),
                new NamedType("predicate", new PType(PTypeEnumeration.sstring))));
            intern_cell = new PaCell(tp_intern, indexName + "_i.pac", false);
            if (intern_cell.IsEmpty) intern_cell.Fill(new object[0]);
            key_index = new FreeIndex(indexName + "_v", intern_cell.Root, 2);
        }
        public void Load(Func<PaEntry, object[][]> genTriples)
        {
            intern_cell.Clear();
            intern_cell.Fill(new object[0]);
            foreach (PaEntry rec in table.Elements())
            {
                object[][] triples = genTriples(rec);
                foreach (object[] v3 in triples)
                {
                    intern_cell.Root.AppendElement(new object[] { false, v3[0], v3[1], v3[2] });
                }
            }
            intern_cell.Flush();
            key_index.Load();
        }
        public IEnumerable<PaEntry> Search(string sample, string predicate)
        {
            PaEntry entry_table = table.Element(0);
            return key_index.SearchAll(sample).Select(ent =>
            {
                object[] v4 = (object[])ent.Get();
                if ((string)v4[3] != predicate)
                    entry_table.offset = Int64.MinValue;
                else
                    entry_table.offset = (long)v4[1];
                return entry_table;
            })
            .Where(ent => ent.offset != Int64.MinValue);
        }

        public struct entry_string_pair { public PaEntry entr; public string stri; };
        
        public IEnumerable<entry_string_pair> GetAll(string id)
        {
            PaEntry entry_table = table.Element(0);
            var query = key_index.GetAll(id).Select(ent =>
            {
                object[] v4 = (object[])ent.Get();
                entry_table.offset = (long)v4[1];
                return new entry_string_pair() { entr = entry_table, stri = (string)v4[3] };
            });
            return query;
        }
    }
}
