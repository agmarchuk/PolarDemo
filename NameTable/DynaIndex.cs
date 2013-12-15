using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace NameTable
{
    public class DynaIndex<Tkey>
    {
        private string path;
        private string index_name;
        private PaCell index;
        private SortedList<Tkey, long> list;
        private Func<object, Tkey> keyProducer;
        public DynaIndex(string path, string index_name, Func<object, Tkey> offKeyProducer)
        {
            this.path = path;
            this.index_name = index_name;
            this.keyProducer = offKeyProducer;
            index = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)),
                path + index_name + ".pac", false);
            if (index.IsEmpty)
            {
                index.Fill(new object[0]);
            }
            list = new SortedList<Tkey, long>();
        }
        // Слияние двух индексов
        public void Flush()
        {
            //long number1 = list.LongCount();
            //long number2 = index.Root.Count();

            long number1 = index.Root.Count();
            long number2 = list.LongCount();

            // Если ничего не накопилось, ничего не делаем
            //if (list.Count == 0) return;
            if (number2 == 0) return;

            //// Текущий индекс превращаем в другую ячеку
            //index.Close();
            //System.IO.File.Copy(path + index_name + ".pac", path + "tmp_dindex.pac", true);
            //// Откроем и очистим ячейку
            //index = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)),
            //    path + index_name + ".pac", false);
            //index.Clear();
            //// Вначале поставим содержимое list
            //index.Fill(list.Select(pair => pair.Value).Cast<object>().ToArray());
            //// теперь добавим то, что было
            //PaCell tmp_cell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)),
            //    path + "tmp_dindex.pac");

            //foreach (PaEntry ent in tmp_cell.Root.Elements())
            //{
            //    index.Root.AppendElement(ent.Get());
            //}
            //index.Flush();
            //tmp_cell.Close();
            foreach (long off in list.Select(pair => pair.Value))
            {
                index.Root.AppendElement(off);
            }
            index.Flush();
            // Сортировка слиянием
            index.Root.MergeUpByKey<Tkey>(0, number1, number2, keyProducer, null);
            list = new SortedList<Tkey, long>();
        }
        public void Close()
        {
            this.Flush();
            index.Close();
        }
        // Добавление элемента индекса
        public void Add(Tkey key, long value)
        {
            list.Add(key, value);
            if (list.LongCount() > 10000) Flush();
        }
        public bool Exists(Tkey key)
        {
            if (list.ContainsKey(key)) return true;
            if (index.Root.Count() == 0) return false;
            var qu = index.Root.BinarySearchFirst(entry => ((IComparable)keyProducer(entry.Get())).CompareTo(key));
            if (qu.IsEmpty) return false;
            return true;
        }
        public long GetFirst(Tkey key)
        {
            long value;
            if (list.TryGetValue(key, out value)) return value;
            if (index.Root.Count() == 0) return Int64.MinValue;
            var qu = index.Root.BinarySearchFirst(entry => ((IComparable)keyProducer(entry.Get())).CompareTo(key));
            if (qu.IsEmpty) return Int64.MinValue;
            //return qu.offset;
            return (long)qu.Get();
        }
        // Выдает суммарное число эелментов в индексе
        public long Count()
        {
            return list.LongCount() + index.Root.Count();
        }
        public IEnumerable<Tkey> Keys()
        {
            return list.Keys.Concat(index.Root.Elements().Select(ent => keyProducer(ent.Get())));
        }
    }
}
