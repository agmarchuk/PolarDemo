using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using PolarDB;

namespace NameTable
{
    public class StringIntMD5Coding : IStringIntCoding
    {
        private static readonly PType Plong = new PType(PTypeEnumeration.longinteger);
                private MD5 md5 = MD5.Create();
        private PType tp_ind = new PTypeSequence(Plong);
        private PType tp_pair_longs = new PTypeSequence(
new PTypeRecord(new NamedType("check sum", new PType(PTypeEnumeration.longinteger)),   
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
                      
        public StringIntMD5Coding(string path)
        {

            niCell = path + "n_index.pac";
            pathCiCell = path + "pathCiCell.pac";
            pathMD5Index = path + "md5_index.pac"; 
          
            // Создание ячеек, предполагается, что все либо есть либо их нет и надо создавать
            if (!System.IO.File.Exists(niCell))  
                Clear();

            // Открытие ячеек в режиме работы (чтения)
            Open(true);
            Count = c_index.Root.Count();
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
        }

        public int GetCode(string name)
        {
            Open(true);
            if (Count == 0) return Int32.MinValue;
            return GetCode(name, BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(name)), 0));

        }

        private int GetCode(string name, long newD5)
        {
            if (Count == 0) return Int32.MinValue;
            var searchAll = md5_index.Root.BinarySearchAll(md5Entry =>
            {
                var existMD5 = (long) md5Entry.Field(0).Get();
                return existMD5.CompareTo(newD5);
            });
            var ncEntry = nc_cell.Root.Element(0);
            foreach (var md5Entry in searchAll)
            {
                ncEntry.offset = (long) md5Entry.Field(1).Get();
                if ((string) ncEntry.Field(1).Get() == name)
                    return (int) ncEntry.Field(0).Get();
            }
            return Int32.MinValue;
        }

        public string GetName(int code)
        {
            Open(true);
            if (Count == 0) return string.Empty;
            if (Count<= code) return string.Empty;
            PaEntry paEntry = nc_cell.Root.Element(0);
            paEntry.offset = (long) c_index.Root.Element(code).Get();
            return (string) paEntry.Field(1).Get();
        }

        public Dictionary<string, int> InsertPortion(List<string> portion)
        {
            Open(false);
            List<long> ofsets2NC = new List<long>(portion.Count);
            List<long> checkSum = new List<long>(portion.Count);
            var insertPortion = new Dictionary<string, int>();
            for (int i = 0; i < portion.Count; i++)
            {
                if(insertPortion.ContainsKey(portion[i]))
                    continue;
                var code = GetCode(portion[i]);
                if (code != Int32.MinValue)
                    insertPortion.Add(portion[i], code);
                else
                {
                    checkSum.Add(BitConverter.ToInt64(UTF8Encoding.UTF8.GetBytes(portion[i]),0));
                    ofsets2NC.Add(nc_cell.Root.AppendElement(new object[] { Count++, portion[i] }));
                }
            }

            //Count += insertPortion.Count;
            return insertPortion;
                
        }

        public void MakeIndexed()
        {
            
        }

        public long Count { get; private set; }
    }
}
