using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TableWithIndex
{
    public class PIndex
    {
        private PaCell table;
        private int field_numb;
        private PxCell icell;
        public PIndex(string ind_path, PaCell table, int field_numb)
        {
            this.table = table;
            this.field_numb = field_numb;
            PTypeSequence table_type = (PTypeSequence)table.Type;
            PType field_type = ((PTypeRecord)table_type.ElementType).Fields[field_numb].Type;
            PType tp_index = new PTypeSequence(new PTypeRecord(
                new NamedType("key", field_type),
                new NamedType("value", new PType(PTypeEnumeration.longinteger))));
            icell = new PxCell(tp_index, ind_path + "index.pxc", false);
        }
        public void Create()
        {
            icell.Clear();

            object[] ivalue = table.Root.Elements().Select(rec =>
            {
                return new object[] { ((object[])rec.Value)[field_numb], rec.Offset };
            }).ToArray();
            icell.Fill2(ivalue);
            icell.Flush();
            icell.Root.Sort(entry => (string)entry.Field(0).Get().Value);
            //icell.Root.SortComparison((e1, e2) => ((string)((object[])e1.Get().Value)[0]).CompareTo((string)((object[])e2.Get().Value)[0]));
            Console.WriteLine(icell.Root.Count());
        }
        public long SelectFirst(string id)
        {
            PxEntry candidate = icell.Root.BinarySearchFirst(entry => ((string)entry.Field(0).Get().Value).CompareTo(id));
            return (long)candidate.Field(1).Get().Value;
        }
    }
}
