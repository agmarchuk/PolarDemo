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
        private string originalCell;
        private string tmpCell;
        private string sourceCell;
        private string niCell;
        private string ciCell;
        private string checkSumsCell;
        
        private PaCell nc_cell;
        private PaCell n_index;
        private PaCell c_index;
        private PaCell checkSums_index;

        private PType tp_ind = new PTypeSequence(Plong);
        //private PType tp_ind_checksum = new PTypeSequence(new PTypeRecord(new NamedType("offset", new PType(PTypeEnumeration.longinteger)), new NamedType("cheksum", new PType(PTypeEnumeration.longinteger))));
        private PType tp_nc = new PTypeSequence(
            new PTypeRecord(
                new NamedType("code", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring))));

        private MD5 md5 = MD5.Create();
        private static readonly PType Plong = new PType(PTypeEnumeration.longinteger);

        public StringIntCoding(string path)
        {
            originalCell = path + "original_nt.pac";
            tmpCell = path + "tmp_nt.pac";
            sourceCell = path + "source_nt.pac";
            niCell = path + "n_index.pac";
            ciCell = path + "c_index.pac";
            checkSumsCell = path + "checkSums_index.pac";
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
                checkSums_index = new PaCell(tp_ind, checkSumsCell, false);
                checkSums_index.Fill(new object[0]);
                checkSums_index.Close();
            }
            // Открытие ячеек в режиме работы (чтения)
            Open();
        }
        public void Open()
        {
            //TODO: надо разобраться с readOnly модой 
            nc_cell = new PaCell(tp_nc, originalCell, false);
            n_index = new PaCell(tp_ind, niCell, false);
            c_index = new PaCell(tp_ind, ciCell, false);
            checkSums_index = new PaCell(tp_ind, checkSumsCell, false);
        }
        public void Close()
        {
            nc_cell.Close();
            n_index.Close();
            c_index.Close();
            checkSums_index.Close();
        }
        public void Clear()
        {
            nc_cell.Clear();
            n_index.Clear();
            c_index.Clear();
            checkSums_index.Clear();
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
            var diapason = checkSums_index.Root.BinarySearchDiapason(ent =>
            {
                var value =  (long) ent.Get();
                return value.CompareTo(newcheckSum);
            });
            //if (!qu.Any()) return Int32.MinValue;

            foreach (long offset in n_index.Root.ElementValues(diapason.start, diapason.numb))
            {
                nc_entry.offset =  offset;
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
            System.IO.File.Move(niCell, tmpCell);
           
          
            PaCell source = new PaCell(tp_nc, sourceCell);

            PaCell target = new PaCell(tp_nc, originalCell, false);
       
            checkSums_index = new PaCell(tp_ind, checkSumsCell, false);

                   
         
            int ssa_ind = 0;
            bool ssa_notempty = true;
            
            // Для накопления пар  
            var accumulator = new List<KeyValuePair<string, int>>(ssa.Length);

            // Очередной (новый) код (индекс)
            int code_new = 0;           
           
            target.Fill(new object[0]);
   
            if (!source.IsEmpty)
            {
                code_new = (int) source.Root.Count();
                int existsHashesIndex = 0;
                var existHashes = checkSums_index.Root.ElementValues().Cast<long>().ToArray();
                checkSums_index.Clear();

                //if (!target.IsEmpty) target.Clear();

                checkSums_index.Fill(new object[0]);
                foreach (object[] val in source.Root.ElementValues())
                {
                    // Пропускаю элементы из нового потока, которые меньше текущего сканированного элемента 
                    int cmp = 0;

                    var existsCheckSum = existHashes[existsHashesIndex]; // (long) val[2];
                    long offset;
                    while (ssa_notempty && (cmp = hashes_arr[ssa_ind].CompareTo(existsCheckSum)) <= 0)
                    {
                        if (cmp < 0)
                        {
                            // добавляется новый код
                            offset = target.Root.AppendElement(new object[] {code_new, ssa[ssa_ind]});
                        
                            checkSums_index.Root.AppendElement(hashes_arr[ssa_ind]);
                            accumulator.Add(new KeyValuePair<string, int>(ssa[ssa_ind], code_new++));
                            ssa_ind++;
                        }
                        else // используется существующий код
                            accumulator.Add(new KeyValuePair<string, int>((string) val[1], (int) val[0]));
                        ssa_ind++;
                        if (ssa_ind == ssa.Length)
                            ssa_notempty = false;
                    }
                    offset = target.Root.AppendElement(val); // переписывается тот же объект
                    checkSums_index.Root.AppendElement(existsCheckSum);
           
                }
            }
            else checkSums_index.Fill(new object[0]);
            // В массиве ssa могут остаться элементы, их надо просто добавить
            if (ssa_notempty)
                do
                {
                    var offset = target.Root.AppendElement(new object[] {code_new, ssa[ssa_ind]});
            
                    checkSums_index.Root.AppendElement( hashes_arr[ssa_ind] );
                    accumulator.Add(new KeyValuePair<string, int>(ssa[ssa_ind], code_new++));
                    ssa_ind++;
                } while (ssa_ind < ssa.Length);

           
            target.Close();      
            source.Close();     
            checkSums_index.Close();
         
            this.Open(); // парный к this.Close() оператор
            // Финальный аккорд: формирование и выдача словаря
              Dictionary<string, int> dic = new Dictionary<string, int>();
            //Console.WriteLine("Слияние ok (" + ssa.Length + "). duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            foreach (var keyValuePair in accumulator.Where(keyValuePair => !dic.ContainsKey(keyValuePair.Key)))
                dic.Add(keyValuePair.Key, keyValuePair.Value);

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
            var offsets = new object[nc_cell.Root.Count()];
            if (nc_cell.IsEmpty) return;
            
            foreach (PaEntry ent in nc_cell.Root.Elements())
            {
                long offset = ent.offset;
                offsets[(int) ent.Field(0).Get()] = offset;
                n_index.Root.AppendElement(offset);
            }
            c_index.Fill(offsets);

            var nc_Entry = nc_cell.Root.Element(0);
            object[] objects = checkSums_index.Root.ElementValues().ToArray();
            for (int i = 1; i < objects.Length; i++)
            {

                long o1 = (long)objects[i-1];
                long o2 = (long)objects[i];
                //nc_Entry.offset = (long)offsets[i - 1];
                //object o1 = nc_Entry.Field(0).Get();
                if (o1 > o2) throw new Exception();
            }
            if (objects.Length != offsets.Length) throw new Exception();
            if (nc_cell.Root.Count() != offsets.Length) throw new Exception();
            if (c_index.Root.Count() != offsets.Length) throw new Exception();
            // n_index.Root.AppendElement(off);
               // c_index.Root.AppendElement(off);
          //  n_index.Flush();
            c_index.Flush();

            // Индекс n_index отсортирован по построению. Надо сортировать c_index
            //PaEntry nc_entry = nc_cell.Root.Element(0);
            //c_index.Root.SortByKey(obj =>
            //{
            //    nc_entry.offset = (long)obj;
            //    return nc_entry.Field(0).Get();
            //});
        }
        public long Count() { return c_index.Root.Count(); } 
    }
}
