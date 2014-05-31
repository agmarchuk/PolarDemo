using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;

namespace NameTable
{
    public class StringIntCoding :IStringIntCoding
    {
        private string originalCell;
        private string sourceCell;
        private string niCell;
        private string ciCell;

        private PaCell nc_cell;
        private PaCell n_index;
        private PaCell c_index;

        private PType tp_ind = new PTypeSequence(new PType(PTypeEnumeration.longinteger));
        private PType tp_nc = new PTypeSequence(new PTypeRecord(
                new NamedType("code", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring))));

        private bool? mode;

        public StringIntCoding(string path)
        {
            originalCell = path + "original_nt.pac";
            sourceCell = path + "source_nt.pac";
            niCell = path + "n_index.pac";
            ciCell = path + "c_index.pac";
            // Создание ячеек, предполагается, что все либо есть либо их нет и надо создавать
            if (!System.IO.File.Exists(originalCell))
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
            Open(true);
        }
        public void Open(bool readOnlyMode)
        {
            if (mode == readOnlyMode) return;
            if (mode != null)
                Close();
            nc_cell = new PaCell(tp_nc, originalCell, readOnlyMode);
            n_index = new PaCell(tp_ind, niCell, readOnlyMode);
            c_index = new PaCell(tp_ind, ciCell, readOnlyMode);
            mode = readOnlyMode;
        }
        public void Close()
        {
            nc_cell.Close();
            n_index.Close();
            c_index.Close();
            mode = null;
        }
        public void Clear()
        {
            Open(false);
            nc_cell.Clear();
            n_index.Clear();
            c_index.Clear();
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
            Open(false);
            //DateTime tt0 = DateTime.Now;
            string[] ssa = sorted_arr;
            if (ssa.Length == 0) return new Dictionary<string, int>();

            this.Close();
            // Подготовим основную ячейку для работы
            if (System.IO.File.Exists(sourceCell)) System.IO.File.Delete(sourceCell);
            System.IO.File.Move(originalCell, sourceCell);
            //if (!System.IO.File.Exists(tmpCell))
            //{
            //    PaCell tmp = new PaCell(tp_nc, tmpCell, false);
            //    tmp.Fill(new object[0]);
            //    tmp.Close();
            //}
            //System.IO.File.Move(tmpCell, originalCell);

            PaCell source = new PaCell(tp_nc, sourceCell);
            PaCell target = new PaCell(tp_nc, originalCell, false);
            //if (!target.IsEmpty) target.Clear();
            target.Fill(new object[0]);

            int ssa_ind = 0;
            bool ssa_notempty = true;
            string ssa_current = ssa_notempty ? ssa[ssa_ind] : null;
            ssa_ind++;

            // Для накопления пар  
            List<KeyValuePair<string, int>> accumulator = new List<KeyValuePair<string, int>>(ssa.Length);

            // Очередной (новый) код (индекс)
            int code_new = 0;
            if (!source.IsEmpty)
            {
                code_new = (int)source.Root.Count();
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
                            target.Root.AppendElement(v);
                            code_new++;
                            accumulator.Add(new KeyValuePair<string, int>((string)v[1], (int)v[0]));
                        }
                        else
                        { // используется существующий код
                            accumulator.Add(new KeyValuePair<string, int>((string)val[1], (int)val[0]));
                        }
                        if (ssa_ind < ssa.Length)
                            ssa_current = ssa[ssa_ind++]; //ssa.ElementAt<string>(ssa_ind);
                        else
                            ssa_notempty = false;
                    }
                    target.Root.AppendElement(val); // переписывается тот же объект
                }
            }
            // В массиве ssa могут остаться элементы, их надо просто добавить
            if (ssa_notempty)
            {
                do
                {
                    object[] v = new object[] { code_new, ssa_current };
                    target.Root.AppendElement(v);
                    code_new++;
                    accumulator.Add(new KeyValuePair<string, int>((string)v[1], (int)v[0]));
                    if (ssa_ind < ssa.Length) ssa_current = ssa[ssa_ind];
                    ssa_ind++;
                }
                while (ssa_ind <= ssa.Length);
            }

            target.Close();
            source.Close();
            System.IO.File.Delete(sourceCell);
            this.Open(true); // парный к this.Close() оператор
            // Финальный аккорд: формирование и выдача словаря
              Dictionary<string, int> dic = new Dictionary<string, int>();
            foreach (var keyValuePair in accumulator.Where(keyValuePair => !dic.ContainsKey(keyValuePair.Key)))
            {
                dic.Add(keyValuePair.Key, keyValuePair.Value);
            }
            //Console.WriteLine("Слияние ok (" + ssa.Length + "). duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            return dic;
        }

        public Dictionary<string, int> InsertPortion(HashSet<string> entities)
        {
            return InsertPortion(entities.ToArray());
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
            Open(true);
        }
        public int Count{ get { return Convert.ToInt32(c_index.Root.Count()); }}
        public void WarmUp()
        {
            foreach (var q in nc_cell.Root.ElementValues()) ;
            foreach (var q in c_index.Root.ElementValues()) ;
            foreach (var q in n_index.Root.ElementValues()) ; 
        }
    }
}