using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PolarDB;

namespace NameTable
{
    public class StringIntCoding
    {
        private string path;
        private string originalCell;
        private string tmpCell;
        private string sourceCell;
        private string resultCell;
        private string niCell;
        private string ciCell;
        
        private PaCell nc_cell;
        private PaCell n_index;
        private PaCell c_index;

        private PType tp_ind = new PTypeSequence(new PType(PTypeEnumeration.longinteger));
        private PType tp_nc = new PTypeSequence(new PTypeRecord(
                new NamedType("code", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring))));

        public StringIntCoding(string path)
        {
            this.path = path;
            originalCell = path + "original_nt.pac";
            tmpCell = path + "tmp_nt.pac";
            sourceCell = path + "source_nt.pac";
            niCell = path + "n_index.pac";
            ciCell = path + "c_index.pac";
            resultCell = path + "result_nt.pac";
            // Создание ячеек, предполагается, что все либо есть либо их нет и надо создавать
            if ( ! System.IO.File.Exists(originalCell))
            {
                nc_cell = new PaCell(tp_nc, originalCell, false);
                nc_cell.Fill(new object[0]);
                nc_cell.Close();
                n_index = new PaCell(tp_ind, niCell, false);
                n_index.Fill(new object[0]);
                n_index.Close();
                c_index = new PaCell(tp_ind, ciCell, false);
                c_index.Fill(new object[0]);
                c_index.Close();
            }
            // Открытие ячеек в режиме работы (чтения)
            Open();
        }
        public void Open()
        {
            nc_cell = new PaCell(tp_nc, originalCell);
            n_index = new PaCell(tp_ind, niCell);
            c_index = new PaCell(tp_ind, ciCell);
        }
        public void Close()
        {
            nc_cell.Close();
            n_index.Close();
            c_index.Close();
        }
        public int GetCode(string name)
        {
            if (string.IsNullOrEmpty(name) || n_index.Root.Count() == 0) return Int32.MinValue;
            PaEntry nc_entry = nc_cell.Root.Element(0);
            var qu = n_index.Root.BinarySearchFirst(ent =>
            {
                nc_entry.offset = (long)ent.Get();
                return ((string)nc_entry.Field(1).Get()).CompareTo(name);
            });
            if (qu.IsEmpty) return Int32.MinValue;
            nc_entry.offset = (long)qu.Get();
            return (int)nc_entry.Field(0).Get();
        }
        public string GetName(int code)
        {
            long cnt = c_index.Root.Count();
            if (code < 0 || code >= c_index.Root.Count()) return null;
            PaEntry nc_entry = nc_cell.Root.Element(0);
            var qu = c_index.Root.BinarySearchFirst(ent =>
            {
                nc_entry.offset = (long)ent.Get();
                return ((int)nc_entry.Field(0).Get()).CompareTo(code);
            });
            if (qu.IsEmpty) return null;
            nc_entry.offset = (long)qu.Get();
            return (string)nc_entry.Field(1).Get();
        }
        public Dictionary<string, int> InsertPortion(string[] sorted_arr) //(IEnumerable<string> portion)
        {
            DateTime tt0 = DateTime.Now;
            ////SortedSet<string> ss = new SortedSet<string>(new string[] { "test" }); // new SortedSet<string>(portion);
            //string[] arr = portion.ToArray();
            ////Console.WriteLine("Before SortedSet (" + arr.Length + "). duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            ////SortedSet<string> ss = new SortedSet<string>(portion);
            //Array.Sort<string>(arr);
            //string current = null;
            //int nunique = 0;
            //foreach (string s in arr) if (s != current) { current = s; nunique++; }
            //List<string> ss = new List<string>(nunique);
            //current = null;
            //foreach (string s in arr)
            //{
            //    if (s != current)
            //    {
            //        current = s;
            //        ss.Add(current);
            //    }
            //}
            //string[] ssa = ss.ToArray();
            //ss = null;
            string[] ssa = sorted_arr;
            //string[] ssa = portion.ToArray();
            if (ssa.Length == 0) return new Dictionary<string, int>();
            //Console.WriteLine("Sort and compress ok ("+ ssa.Length +"). duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            this.Close();
            // Подготовим основную ячейку для работы
            System.IO.File.Move(originalCell, sourceCell);
            if (!System.IO.File.Exists(tmpCell))
            {
                PaCell tmp = new PaCell(tp_nc, tmpCell, false);
                tmp.Fill(new object[0]);
                tmp.Close();
            }
            System.IO.File.Move(tmpCell, originalCell);

            PaCell source = new PaCell(tp_nc, sourceCell);
            PaCell target = new PaCell(tp_nc, originalCell, false);
            if (!target.IsEmpty) target.Clear();
            target.Fill(new object[0]);

            //Console.WriteLine("подготовка ячеек ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            int ssa_ind = 0;
            bool ssa_notempty = true;
            string ssa_current = ssa_notempty ? ssa[ssa_ind] : null;
            ssa_ind++;
            
            // Для накопления пар  
            List<KeyValuePair<string, int>> accumulator = new List<KeyValuePair<string, int>>(ssa.Length);

            // Очередной (новый) код (индекс)
            //long nn = source.Root.Count();
            int code_new = (int)source.Root.Count();
            foreach (object[] val in source.Root.ElementValues())
            {
                // Пропускаю элементы из нового потока, которые меньше текущего сканированного элемента 
                string s = (string)val[1];
                int cmp = 0;
                while (ssa_notempty && (cmp = ssa_current.CompareTo(s)) <= 0)
                {
                    if (cmp < 0)
                    { // добавляется новый код
                        object[] v = new object[] { code_new, ssa_current };
                        long off = target.Root.AppendElement(v); // При равенстве, новый элемент игнорируется
                        code_new++;
                        accumulator.Add(new KeyValuePair<string, int>((string)v[1], (int)v[0]));
                    }
                    else
                    { // используется существующий код
                        accumulator.Add(new KeyValuePair<string, int>((string)val[1], (int)val[0]));
                    }
                    if (ssa_ind < ssa.Length)
                    {
                        ssa_current = ssa[ssa_ind]; //ssa.ElementAt<string>(ssa_ind);
                        ssa_ind++;
                    }
                    else
                    {
                        ssa_notempty = false;
                    }
                }
                target.Root.AppendElement(val); // переписывается тот же объект
            }
            // В массиве ssa могут остаться элементы, их надо просто добавить
            if (ssa_notempty) 
            {
                do
                {
                    object[] v = new object[] { code_new, ssa_current };
                    long off = target.Root.AppendElement(v); // При равенстве, новый элемент игнорируется
                    code_new++;
                    accumulator.Add(new KeyValuePair<string, int>((string)v[1], (int)v[0]));
                    if (ssa_ind < ssa.Length) ssa_current = ssa[ssa_ind];
                    ssa_ind++;
                }
                while (ssa_ind <= ssa.Length);
            }
            
            target.Close();
            source.Close();
            System.IO.File.Move(sourceCell, tmpCell);
            this.Open(); // парный к this.Close() оператор
            // Финальный аккорд: формирование и выдача словаря
            //Dictionary<string, int> dic = accumulator.ToDictionary(
            //    (object[] pair) => (string)pair[1],
            //    (object[] pair) => (int)pair[0]);
            Dictionary<string, int> dic = new Dictionary<string, int>();
            //Console.WriteLine("Слияние ok (" + ssa.Length + "). duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            return dic;
        }
        public void MakeIndexed()
        {
            // Подготовим индексы для заполнения
            n_index.Close();
            c_index.Close();
            n_index = new PaCell(tp_ind, niCell, false);
            n_index.Clear();
            n_index.Fill(new object[0]);
            c_index = new PaCell(tp_ind, ciCell, false);
            c_index.Clear();
            c_index.Fill(new object[0]);
            foreach (PaEntry ent in nc_cell.Root.Elements())
            {
                long off = ent.offset;
                n_index.Root.AppendElement(off);
                c_index.Root.AppendElement(off);
            }
            n_index.Flush();
            c_index.Flush();

            // Индекс n_index отсортирован по построению. Надо сортировать c_index
            PaEntry nc_entry = nc_cell.Root.Element(0);
            c_index.Root.SortByKey(obj =>
            {
                nc_entry.offset = (long)obj;
                return nc_entry.Field(0).Get();
            });
        }
        public long Count() { return c_index.Root.Count(); } 
    }
}
