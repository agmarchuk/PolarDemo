﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PolarDB;

namespace NameTable
{
    public class StringIntMD5RAMColisions : IStringIntCoding
    {
        private static readonly PType Plong = new PType(PTypeEnumeration.longinteger);
                private MD5 md5 = MD5.Create();
        private PType tp_ind = new PTypeSequence(Plong);
        private PType tp_pair_longs = new PTypeSequence(new PTypeRecord(new NamedType("check sum", new PType(PTypeEnumeration.longinteger)),   
            new NamedType("offset to nc_cell", new PType(PTypeEnumeration.longinteger))));

        private PType tp_nc = new PTypeSequence(
            new PTypeRecord(
                new NamedType("code", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring))));
                private PaCell nc_cell;

        private string niCell;
        private string pathCiCell;
        private PaCell c_index;
        private PaCell md5_index;
        private string pathMD5Index;
        private bool? openMode;
        private readonly Dictionary<long, int> codesByMd5Cache = new Dictionary<long, int>();
        private readonly Dictionary<long, int> indexOfColision = new Dictionary<long, int>();
        private static Dictionary<string, IStringIntCoding> Opend=new Dictionary<string, IStringIntCoding>();
        

        public StringIntMD5RAMColisions(string path)
        {
            IStringIntCoding existed;
            if (Opend.TryGetValue(path, out existed))
            {
                existed.Close();
                Opend.Remove(path);
            }
            niCell = path + "code name.pac";
            pathCiCell = path + "code index.pac";
            pathMD5Index = path + "md5 index.pac"; 
          
            // Создание ячеек, предполагается, что все либо есть либо их нет и надо создавать
            if (!File.Exists(niCell) || !File.Exists(pathCiCell))
                Clear();

            // Открытие ячеек в режиме работы (чтения)
            Open(true);
            Count = Convert.ToInt32( c_index.Root.Count());
            Opend.Add(path, this);
        
            ReadCodesByMd5Cache();
        }

        private void ReadCodesByMd5Cache()
        {
            codesByMd5Cache.Clear();
            int index = 0;
            foreach (object[] pair in md5_index.Root.ElementValues())
                ReadCodeByMd5((long) pair[0], index++, (long) pair[1]);
        }

        private void ReadCodeByMd5(long md5, int index, long offset)
        {
            if (codesByMd5Cache.ContainsKey(md5))
            {
                indexOfColision.Add(md5, index);
                Console.WriteLine("collsions " + indexOfColision.Count);
            }
            else
            {
                PaEntry ncEntry = nc_cell.Root.Element(0);
                ncEntry.offset = offset;
                codesByMd5Cache.Add(md5, (int) ncEntry.Field(0).Get());
            }
        }

        public void WarmUp()
        {
            foreach (var q in nc_cell.Root.ElementValues()) ;
            foreach (var q in c_index.Root.ElementValues()) ;
            foreach (var q in md5_index.Root.ElementValues()) ;
        }

        public void Open(bool readonlyMode)
        {
            if (openMode == null)
            {
                nc_cell = new PaCell(tp_nc, niCell, readonlyMode);

                c_index = new PaCell(tp_ind, pathCiCell, readonlyMode);
                md5_index = new PaCell(tp_pair_longs, pathMD5Index, readonlyMode);   
            }
            else if (openMode!=readonlyMode)
            {
                Close();
                nc_cell = new PaCell(tp_nc, niCell, readonlyMode);
                c_index = new PaCell(tp_ind, pathCiCell, readonlyMode);
                md5_index = new PaCell(tp_pair_longs, pathMD5Index, readonlyMode);  

            }
            openMode = readonlyMode;
        }

        public void Close()
        {
            nc_cell.Close();
            c_index.Close();
            md5_index.Close();
            openMode = null;
        }

        public void Clear()
        {                   
            Open(false);
            nc_cell.Clear();
            c_index.Clear();
            md5_index.Clear();
            nc_cell.Fill(new object[0]);
            c_index.Fill(new object[0]);
            md5_index.Fill(new object[0]);
            Count = 0;
            codesByMd5Cache.Clear();
        }

        public int GetCode(string name)
        {
            Open(true);
            if (Count == 0) return Int32.MinValue;
            return GetCode(name, BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(name)), 0));

        }

        private int GetCode(string name, long newMD5)
        {
            if (Count == 0) return Int32.MinValue;
              
                 int indexMD5Row;
                 PaEntry ncEntry = nc_cell.Root.Element(0);
            if (indexOfColision.TryGetValue(newMD5, out indexMD5Row))
                for (;; indexMD5Row++)
                {
                    var md5_offet = (object[]) md5_index.Root.Element(indexMD5Row).Get();
                    if ((long) md5_offet[0] != newMD5) return Int32.MinValue;
                    ncEntry.offset = (long) md5_offet[1];
                    if ((string) ncEntry.Field(1).Get() == name)
                        return (int) ncEntry.Field(0).Get();
                }
            int code;
            return !codesByMd5Cache.TryGetValue(newMD5, out code) ? Int32.MinValue : code;


            //for (;;index++)
            //{
            //    var md5_offet=(object[])md5_index.Root.Element(index).Get();
            //    if ((long)md5_offet[0] != newD5) return Int32.MinValue;
            //    ncEntry.offset = (long) md5_offet[1];
            //    if ((string)ncEntry.Field(1).Get() == name)
            //        return (int)ncEntry.Field(0).Get();
            //}
            //return Int32.MinValue;
        }

        public string GetName(int code)
        {
            Open(true);
            if (Count == 0) return string.Empty;
            if ( code==int.MinValue) return string.Empty;
            if (Count <= code) return string.Empty;
            PaEntry paEntry = nc_cell.Root.Element(0);
            paEntry.offset = (long) c_index.Root.Element(code).Get();
            return (string) paEntry.Field(1).Get();
        }

    
        public Dictionary<string, int> InsertPortion(string[] portion)
        {
            return InsertPortion(new HashSet<string>(portion));
        }
        public Dictionary<string, int> InsertPortion(HashSet<string> portion)
        {
            Console.Write("c0 ");
           // foreach (var t in md5_index.Root.ElementValues()) ;
          //  foreach (var q in nc_cell.Root.ElementValues()) ; //14гб
            Console.Write("c_warm ");
           List<long> ofsets2NC = new List<long>(portion.Count);
            List<long> checkSumList = new List<long>(portion.Count);
            var insertPortion = new Dictionary<string, int>(portion.Count);
                foreach (var newString in portion)
                {
                    var checkSum = BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(newString)), 0);
                    var code = GetCode(newString, checkSum);
                    if (code == Int32.MinValue)
                    {
                        checkSumList.Add(checkSum);
                        ofsets2NC.Add(nc_cell.Root.AppendElement(new object[] { code = Count++, newString }));
                        codesByMd5Cache.Add(checkSum, code);
                    }
                    insertPortion.Add(newString, code);
                }
            nc_cell.Flush();
            Console.Write("c_nc ");
            var offsetsNC = ofsets2NC.ToArray();
            var checkSums = checkSumList.ToArray();
            Array.Sort(checkSums, offsetsNC);
            Console.Write("c_s" );
            int portionIndex = 0;
            int md5_count = 0;
            if (md5_index.Root.Count() > 0)
            {
                Close();
                string tmp = pathMD5Index + ".tmp";
                if (File.Exists(tmp)) File.Delete(tmp);

                File.Move(pathMD5Index, tmp);
                Open(false);
                codesByMd5Cache.Clear();
                md5_index.Clear();
                md5_index.Fill(new object[0]);
                var tmpCell = new PaCell(tp_pair_longs, tmp);

                const int max = 500 * 1000 * 1000;
                long count = tmpCell.Root.Count();
                for (int i = 0; i < (int)(count / max) + 1; i++)
                    foreach (object[] existingPair in tmpCell.Root.ElementValues(i * max, Math.Min(max, count - i * max)).ToArray())
                    {
                        var existsCheckSum = (long) existingPair[0];
                        for (;
                            portionIndex < offsetsNC.Length
                            && checkSums[portionIndex] <= existsCheckSum;
                            portionIndex++)
                        {
                            md5_index.Root.AppendElement(new object[] { checkSums[portionIndex], offsetsNC[portionIndex] });
                            //ReadCodeByMd5(checkSums[portionIndex], md5_count++, offsetsNC[portionIndex]);
                        }
                        md5_index.Root.AppendElement(existingPair);
                        //ReadCodeByMd5(existsCheckSum, md5_count++, (long)existingPair[1]);
                    }
                tmpCell.Close();
                File.Delete(tmp);
            }
            for (; portionIndex < checkSums.Length; portionIndex++)
            {
                md5_index.Root.AppendElement(new object[] { checkSums[portionIndex], offsetsNC[portionIndex] });
                //ReadCodeByMd5(checkSums[portionIndex], md5_count++, offsetsNC[portionIndex]);
            }
            md5_index.Flush();

            Console.Write("c_md5 ");


           // ReadOffsetsByMd5Cache();
          
            //Count += insertPortion.Count;
            return insertPortion;     
        }
        public void MakeIndexed()
        {
           Open(false); 
            c_index.Clear();
            var offsets = new object[Count];
            int i = 0;
            foreach (PaEntry entry in nc_cell.Root.Elements())
                offsets[i++] = entry.offset;
            c_index.Fill(offsets);
            Open(true);
        }

        public int Count { get; private set; }
      
    }
}
