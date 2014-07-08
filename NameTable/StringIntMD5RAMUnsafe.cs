using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PolarDB;

namespace NameTable
{
    public class StringIntMD5RAMUnsafe : IStringIntCoding
    {
        private static readonly PType Plong = new PType(PTypeEnumeration.longinteger);
        private MD5 md5 = MD5.Create();
        private PType tp_ind = new PTypeSequence(Plong);
        private PType tp_pair_longs = new PTypeSequence(
            new PTypeRecord(
                new NamedType("check sum", new PType(PTypeEnumeration.longinteger)),
                new NamedType("offset to nc_cell", new PType(PTypeEnumeration.longinteger))));
        private PType tp_md5_code = new PTypeSequence(
            new PTypeRecord(
                new NamedType("check sum", new PType(PTypeEnumeration.longinteger)),
                new NamedType("code", new PType(PTypeEnumeration.integer))));
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
        private readonly Dictionary<long, int> codeByMd5 = new Dictionary<long, int>();    
        private static Dictionary<string, IStringIntCoding> Opend = new Dictionary<string, IStringIntCoding>();


        public StringIntMD5RAMUnsafe(string path)
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
            Count = Convert.ToInt32(c_index.Root.Count());
            Opend.Add(path, this);

            ReadCodesByMd5();
              Console.WriteLine("ENTITIES COUNT " + codeByMd5.Count);
        }

        private void ReadCodesByMd5()
        {
            codeByMd5.Clear();

            foreach (object[] pair in md5_index.Root.ElementValues())
                codeByMd5.Add((long) pair[0], (int) pair[1]);
        }

        void WriteOffsetsByMD5()
        {
            md5_index.Clear();
            //Array.Sort(codeByMd5.ToArray());
            md5_index.Fill(codeByMd5.Select(pair => new object[] { pair.Key, pair.Value }).ToArray());
            md5_index.Flush();
        }   

     
        public void WarmUp()
        {
            foreach (var q in nc_cell.Root.ElementValues()) ;
            foreach (var q in c_index.Root.ElementValues()) ;
            foreach (var q in md5_index.Root.ElementValues()) ;
        }

        public int InsertOne(string newString)
        {
            var newMD5 = BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(newString)), 0);
            int code = 0;
            if (!codeByMd5.TryGetValue(newMD5, out code))
            {
                nc_cell.Root.AppendElement(new object[] { code = Count++, newString });
                codeByMd5.Add(newMD5, code);
            }
            
            return code;
        }

        public void Open(bool readonlyMode)
        {
            if (openMode == readonlyMode) return;

            if (openMode != null) Close();

            nc_cell = new PaCell(tp_nc, niCell, readonlyMode);  
            c_index = new PaCell(tp_ind, pathCiCell, readonlyMode);
            md5_index = new PaCell(tp_md5_code, pathMD5Index, readonlyMode);

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
            codeByMd5.Clear();
      
        }

        public int GetCode(string name)
        {
            Open(true);
            if (Count == 0) return Int32.MinValue;
            long newMd5 = BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(name)), 0);
            if (Count == 0) return Int32.MinValue;     
            
            int code;
            return !codeByMd5.TryGetValue(newMd5, out code) ? Int32.MinValue : code;   
        }



        public string GetName(int code)
        {
            Open(true);
            if (Count == 0) return string.Empty;
            if (code == int.MinValue) return string.Empty;
            if (Count <= code) return string.Empty;
            PaEntry paEntry = nc_cell.Root.Element(0);
            paEntry.offset = (long)c_index.Root.Element(code).Get();
            return (string)paEntry.Field(1).Get();
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
         //   Console.Write("c_warm ");
            //  List<long> ofsets2NC = new List<long>(portion.Count);
          //  List<long> checkSumList = new List<long>(portion.Count);
            var insertPortion = new Dictionary<string, int>(portion.Count);
            foreach (var newString in portion)
                insertPortion.Add(newString, InsertOne(newString));

            nc_cell.Flush();
            Console.Write("c_nc ");

            return insertPortion;
        }
        public void MakeIndexed()
        {
            Open(false);  

            c_index.Clear();
            var offsets = new object[Count];
            int i = 0;
            nc_cell.Flush();
            foreach (PaEntry entry in nc_cell.Root.Elements())
                offsets[i++] = entry.offset;
            c_index.Fill(offsets);
             c_index.Flush();
            
            WriteOffsetsByMD5();      
            Open(true);
        }

        public int Count { get; private set; }

    }
}
