using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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

        private PType tp_ind = new PTypeSequence(Plong);
        private PType tp_ind_checksum = new PTypeSequence(new PTypeRecord(new NamedType("offset", new PType(PTypeEnumeration.longinteger)), new NamedType("cheksum", new PType(PTypeEnumeration.longinteger))));
        private PType tp_nc = new PTypeSequence(
            new PTypeRecord(
                new NamedType("code", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring))));

        private MD5 md5 = MD5.Create();
        private static readonly PType Plong = new PType(PTypeEnumeration.longinteger);

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
                n_index = new PaCell(tp_ind_checksum, niCell, false);
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
            //TODO: надо разобраться с readOnly модой 
            nc_cell = new PaCell(tp_nc, originalCell, false);
            n_index = new PaCell(tp_ind_checksum, niCell, false);
            c_index = new PaCell(tp_ind, ciCell, false);
        }
        public void Close()
        {
            nc_cell.Close();
            n_index.Close();
            c_index.Close();
        }
        public void Clear()
        {
            nc_cell.Clear();
            n_index.Clear();
            c_index.Clear();
        }
        public int GetCode(string name)
        {
            if (string.IsNullOrEmpty(name) || n_index.Root.Count() == 0) return Int32.MinValue;
            PaEntry nc_entry = nc_cell.Root.Element(0);
            var newcheckSum = BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(name)), 0);

            //проверка первого
            //if(((long)nc_entry.Field(2).Get()).CompareTo(newcheckSum)<=0)
            //    return Int32.MinValue;
            ////проверка последнего
            //nc_entry.offset = (long)n_index.Root.Element(n_index.Root.Count()).Get();
            //if (((long)nc_entry.Field(2).Get()).CompareTo(newcheckSum) > 0)
            //    return Int32.MinValue;
            var qu = n_index.Root.BinarySearchAll(ent =>
            {
                object[] o = (object[]) ent.Get();
                long value = (long) o[1];
                return value.CompareTo(newcheckSum);
            });
            //if (!qu.Any()) return Int32.MinValue;
            foreach (object[] o in qu.Select(entry => entry.Get()))
            {
                nc_entry.offset = (long) o[0];
                var o1 = (object[]) nc_entry.Get();
                if ((string) o1[1] == name)
                    return (int) o1[0];
            }
            //var test = n_index.Root.ElementValues().Cast<object[]>().ToArray();//.Select(ent =>
            //{
            //  //return (object[])ent;
            //    //nc_entry.offset = (long)o[0];
            ////    return ((long)nc_entry.Field(2).Get());
            //}
            //Console.WriteLine(test[0][0]);
            //for (int i = 1; i < test.Length; i++)
            //{
            //    var l1 = test[i];
            //    var l2 = test[i - 1];
            //    if ((long)l1[1] < (long)l2[1])
            //    {
            //        Console.WriteLine("hujikolhjk");
            //    }
            //    if ((long)l1[1] == newcheckSum || l2[1] == newcheckSum)
            //    {
            //        Console.WriteLine("agerweff");
            //    }
            //}
            return Int32.MinValue;
        }

        public string GetName(int code)
        {
            long cnt = c_index.Root.Count();
            if (code < 0 || code >= c_index.Root.Count()) return null;
            PaEntry nc_entry = nc_cell.Root.Element(0);
           nc_entry.offset = (long)c_index.Root.Element(code).Get();
            //var qu = c_index.Root.BinarySearchFirst(ent =>
            //{
            //    nc_entry.offset = (long)ent.Get();
            //    return ((int)nc_entry.Field(0).Get()).CompareTo(code);
            //});
            //if (qu.IsEmpty) return null;
            //nc_entry.offset = (long)qu.Get();
            return (string)nc_entry.Field(1).Get();
        }
        public Dictionary<string, int> InsertPortion(string[] portion)  //   (string[] sorted_arr) 
        {
            //DateTime tt0 = DateTime.Now;
            var hashes_arr= portion.Select(s =>BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(s)), 0)).ToArray();
            Array.Sort(hashes_arr, portion);
            var ssa = portion;
            if (ssa.Length == 0) return new Dictionary<string, int>();

            this.Close();
            // Подготовим основную ячейку для работы
            if (System.IO.File.Exists(sourceCell)) System.IO.File.Delete(sourceCell);
            System.IO.File.Move(originalCell, sourceCell);
            if (System.IO.File.Exists(tmpCell)) System.IO.File.Delete(tmpCell);
         //   n_index.Close();
            System.IO.File.Move(niCell, tmpCell);          

            //if (!System.IO.File.Exists(tmpCell))
            //{
            //    PaCell tmp = new PaCell(tp_nc, tmpCell, false);
            //    tmp.Fill(new object[0]);
            //    tmp.Close();
            //}
            //System.IO.File.Move(tmpCell, originalCell);

            PaCell source = new PaCell(tp_nc, sourceCell);
            PaCell tmp_n_index = new PaCell(tp_ind_checksum, tmpCell);
          PaCell n_index_copy = new PaCell(tp_ind_checksum, niCell, false);
            PaCell target = new PaCell(tp_nc, originalCell, false);
            
            //if (!target.IsEmpty) target.Clear();
            target.Fill(new object[0]);
            n_index_copy.Fill(new object[0]);

            int ssa_ind = 0;
            bool ssa_notempty = true;
            
            // Для накопления пар  
            var accumulator = new List<KeyValuePair<string, int>>(ssa.Length);

            // Очередной (новый) код (индекс)
            int code_new = 0;
            if (!source.IsEmpty)
            {
                code_new = (int)source.Root.Count();
                int existsHashesIndex = 0;
                var existHashes = tmp_n_index.Root.ElementValues().Cast<object[]>().Select(o => o[1]).ToArray();
                foreach (object[] val in source.Root.ElementValues())
                {
                    // Пропускаю элементы из нового потока, которые меньше текущего сканированного элемента 
                    int cmp = 0;
               
                    var existsCheckSum =existHashes[existsHashesIndex++];// (long) val[2];
                    long offset;
                    while (ssa_notempty && (cmp = hashes_arr[ssa_ind].CompareTo(existsCheckSum)) <= 0)
                    {
                        if (cmp < 0)
                        {
                            // добавляется новый код
                            offset = target.Root.AppendElement(new object[] {code_new, ssa[ssa_ind]});
                            n_index_copy.Root.AppendElement(new object[] { offset, hashes_arr[ssa_ind] });
                            accumulator.Add(new KeyValuePair<string, int>(ssa[ssa_ind], code_new++));      
                        }
                        else // используется существующий код
                            accumulator.Add(new KeyValuePair<string, int>((string) val[1], (int) val[0]));
                        ssa_ind++;
                        if (ssa_ind == ssa.Length)
                            ssa_notempty = false;
                    }
                    offset = target.Root.AppendElement(val); // переписывается тот же объект
                    n_index_copy.Root.AppendElement(new object[] { offset, existsCheckSum });
                }
            }
            // В массиве ssa могут остаться элементы, их надо просто добавить
            if (ssa_notempty)
                do
                {
                    var offset = target.Root.AppendElement(new object[] {code_new, ssa[ssa_ind]});
                    n_index_copy.Root.AppendElement(new object[] {offset, hashes_arr[ssa_ind]});
                    accumulator.Add(new KeyValuePair<string, int>(ssa[ssa_ind], code_new++));
                    ssa_ind++;
                } while (ssa_ind < ssa.Length);

            n_index_copy.Close(); 
            target.Close();      
            source.Close();
            tmp_n_index.Close();
            System.IO.File.Delete(tmpCell);
            System.IO.File.Delete(sourceCell);    
            this.Open(); // парный к this.Close() оператор
            // Финальный аккорд: формирование и выдача словаря
            //  Dictionary<string, int> dic = new Dictionary<string, int>();
            //Console.WriteLine("Слияние ok (" + ssa.Length + "). duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            Dictionary<string, int> insertPortion = accumulator.ToDictionary(pair => pair.Key,   pair => pair.Value);
            return insertPortion;
        }
        public void MakeIndexed()
        {
            // Подготовим индексы для заполнения
          //  n_index.Close();
            c_index.Close();
            //n_index = new PaCell(tp_ind, niCell, false);
            //n_index.Clear();
            //n_index.Fill(new object[0]);
            c_index = new PaCell(tp_ind, ciCell, false);
            c_index.Clear();
            c_index.Fill(new object[0]);
            if (nc_cell.IsEmpty) return;
            foreach (PaEntry ent in nc_cell.Root.Elements())
            {
                long off = ent.offset;
               // n_index.Root.AppendElement(off);
                c_index.Root.AppendElement(off);
            }
          //  n_index.Flush();
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
