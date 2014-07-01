using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PolarDB;
using TripleIntClasses;

namespace NameTable
{
    public class StringIntEncoded : IStringIntCoding
    {
        private static readonly PType Plong = new PType(PTypeEnumeration.longinteger);
              
        private PType tp_ind = new PTypeSequence(Plong);

        private PType encodedCellPType=new PTypeSequence(new PTypeSequence(new PType(PTypeEnumeration.@byte)));

        private Dictionary<Bytes2Longs, int> coding=new Dictionary<Bytes2Longs, int>();
        
        private string pathCiCell;
        private PaCell c_index;
        private string encodedCellPath;
        private PaCell encodedCell;
            private bool? openMode;
        private static Dictionary<string, IStringIntCoding> Opend=new Dictionary<string, IStringIntCoding>();
        PaEntry ncEntry;
        private readonly StaticFreqEncoding staticFreqEncoding;
        public StringIntEncoded(string path)
        {
            IStringIntCoding existed;
            if (Opend.TryGetValue(path, out existed))
            {
                existed.Close();
                Opend.Remove(path);
            }
            encodedCellPath = path + "encoded.pac";
            pathCiCell = path + "c_index.pac";
            
            // Создание ячеек, предполагается, что все либо есть либо их нет и надо создавать
            if (!File.Exists(encodedCellPath) || !File.Exists(pathCiCell))
                Clear();

            // Открытие ячеек в режиме работы (чтения)
            Open(true);
            Count = Convert.ToInt32( c_index.Root.Count());
            Opend.Add(path, this);
          
            staticFreqEncoding = new StaticFreqEncoding();
            coding=new Dictionary<Bytes2Longs, int>();
            if (Count > 0)
                foreach (object[] q in encodedCell.Root.ElementValues())
                {
                    byte[] bytes = q.Cast<byte>().ToArray();
                    Bytes2Longs bytes2Longs = new Bytes2Longs(bytes);
                   
                    coding.Add(bytes2Longs, coding.Count);
                 
                }
        }

     
        public void WarmUp()
        {
            foreach (var q in encodedCell.Root.ElementValues()) ;
            foreach (var q in c_index.Root.ElementValues()) ;          
        }

        public void Open(bool readonlyMode)
        {
            if (openMode == null)
            {
                encodedCell = new PaCell(encodedCellPType, encodedCellPath, readonlyMode);

                c_index = new PaCell(tp_ind, pathCiCell, readonlyMode);
            }
            else if (openMode!=readonlyMode)
            {
                Close();
                encodedCell = new PaCell(encodedCellPType, encodedCellPath, readonlyMode);
                c_index = new PaCell(tp_ind, pathCiCell, readonlyMode);
              
            }
            openMode = readonlyMode;
        }

        public void Close()
        {
            encodedCell.Close();
            c_index.Close();
       
            openMode = null;
        }

        public void Clear()
        {                   
            Open(false);
            encodedCell.Clear();
            c_index.Clear();

            encodedCell.Fill(new object[0]);
            c_index.Fill(new object[0]);
        
            Count = 0;
            coding.Clear();
        }

        public int GetCode(string name)
        {
            Open(true);
            if (Count == 0) return Int32.MinValue;
            bool hasNew;
            var longs=new Bytes2Longs(staticFreqEncoding.Encode(name, out hasNew));    
            if (hasNew) return int.MinValue;
            return coding[longs];
        }
   

        public string GetName(int code)
        {
            Open(true);
            if (Count == 0) return string.Empty;
            if ( code==int.MinValue) return string.Empty;
            if (Count <= code) return string.Empty;     
 
            ncEntry = encodedCell.Root.Element(0);    
            ncEntry.offset = (long)c_index.Root.Element(code).Get();
            return staticFreqEncoding.Decode(((object[])ncEntry.Get()).Cast<byte>().ToArray());
        }

    
        public Dictionary<string, int> InsertPortion(string[] portion)
        {
            return InsertPortion(new HashSet<string>(portion));
        }
        public Dictionary<string, int> InsertPortion(HashSet<string> portion)
        {
            Console.Write("c0 ");
            
            var insertPortion = new Dictionary<string, int>(portion.Count);
                foreach (var newString in portion)
                {
                    var code = InsertOne(newString);
                    insertPortion.Add(newString, code);
                }
            Console.WriteLine("c end");
            return insertPortion;     
        }

        public int InsertOne(string newString)
        {
            bool hasNew;
            byte[] encode = staticFreqEncoding.Encode(newString, out hasNew);

            var checkSum = new Bytes2Longs(encode);

            int code;
            if (hasNew || !coding.TryGetValue(checkSum, out code) || code == Int32.MinValue)
                coding.Add(checkSum, code = coding.Count);
            return code;
        }


        public void MakeIndexed()
        {
           Open(false); 
            c_index.Clear();      
            int i = 0;


            var offsets = new object[coding.Count];
            foreach (var v in coding.Keys)
                offsets[i++] =encodedCell.Root.AppendElement(v.ToBytes().Cast<object>().ToArray());
        
                
            c_index.Fill(offsets);
            Open(true);
        }

        public int Count { get; private set; }
      
    }
}
