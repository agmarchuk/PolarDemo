using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TableWithIndex
{
    public class SemiIndex
    {
        private PaCell acell;
        private PxCell xcell;
        public SemiIndex(string indexName, PType field_type)
        {
            PType tp_index = new PTypeSequence(new PTypeRecord(
                new NamedType("key", field_type),
                new NamedType("value", new PType(PTypeEnumeration.longinteger)),
                new NamedType("deleted", new PType(PTypeEnumeration.boolean))));
            acell = new PaCell(tp_index, indexName + ".pac", false);
            xcell = new PxCell(tp_index, indexName + ".pxc", false);
        }
        public void Load(IEnumerable<object[]> rec_flow)
        {
            acell.Clear();
            acell.Fill(new object[0]);
            foreach (object[] rec in rec_flow)
            {
                acell.Root.AppendElement(new object[] { rec[0], rec[1], false });
            }
            acell.Flush();
            xcell.Clear();
            xcell.Fill2(acell.Root.Get());
            xcell.Flush();
            xcell.Root.Sort((PxEntry entry) => (string)entry.Field(0).Get()); // Это надо привести в соответствие с типом ключа
        }
        public long FindFirst(object sample)
        {
            foreach (PxEntry found in xcell.Root.BinarySearchAll((PxEntry entry) => ((string)entry.Field(0).Get()).CompareTo(sample)))
            {
                if ((bool)found.Field(2).Get() == false) return (long)found.Field(1).Get(); 
            }
            return Int64.MinValue;
        }
        public IEnumerable<long> FindAll(Func<object, int> compare)
        {
            foreach (PxEntry found in xcell.Root.BinarySearchAll((PxEntry entry) => compare((string)entry.Field(0).Get())))
            {
                if ((bool)found.Field(2).Get() == false) yield return (long)found.Field(1).Get();
            }
        }

        public void Delete(object key, long value)
        {
            foreach (PxEntry found in xcell.Root.BinarySearchAll((PxEntry entry) => ((string)entry.Field(0).Get()).CompareTo(key)))
            {
                if ((bool)found.Field(2).Get() == false &&
                    (long)found.Field(1).Get() == value)
                {
                    found.Field(2).Set(true);
                    return;
                }
            }
        }
    }
}
