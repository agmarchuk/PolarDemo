using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PolarDB;

namespace NameTable
{
    public class StringIntRAMDIctionary : IStringIntCoding
    {

        private PType tp_nc = new PTypeSequence(
            new PTypeRecord(
                new NamedType("code", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring))));
        private PaCell nc_cell;

        private bool? openMode;
        private readonly Dictionary<string, int> codeByString = new Dictionary<string, int>();
        private string[] stringByCode;
        private string niCell;

        public StringIntRAMDIctionary(string path)
        {

            niCell = path + "/n_index.pac";

            // Создание ячеек, предполагается, что все либо есть либо их нет и надо создавать
            if (!File.Exists(niCell))
                Clear();

            // Открытие ячеек в режиме работы (чтения)
            var stringByCodeList = new List<string>();
            Open(true);
            foreach (object[] code_name in nc_cell.Root.ElementValues())
            {   
                codeByString.Add((string) code_name[1], (int) code_name[0]);
                stringByCodeList.Add((string)code_name[1]);
            }
            stringByCode = stringByCodeList.ToArray();
            Count = stringByCode.Length;
          Close();
        }

        public StringIntRAMDIctionary(string path, Dictionary<string, int> ReWrite)      
        {
            niCell = path + "n_index.pac";

            // Создание ячеек, предполагается, что все либо есть либо их нет и надо создавать
          
                Clear();

            foreach (var str_code in ReWrite)
            {
                nc_cell.Root.AppendElement(new object[]
                {
                    str_code.Value, str_code.Key
                });
            }
            nc_cell.Flush();
            // Открытие ячеек в режиме работы (чтения)
            codeByString = ReWrite;
            stringByCode = ReWrite.Select(pair => pair.Key).ToArray();
            Count = stringByCode.Length;
            Close();
        }

        public void WarmUp()
        {
            //foreach (var q in nc_cell.Root.ElementValues()) ;
           
        }

        public int InsertOne(string entity)
        {
            throw new NotImplementedException();
        }

        public void Open(bool readonlyMode)
        {
            if (openMode == null)
            {
                nc_cell = new PaCell(tp_nc, niCell, readonlyMode);

            }
            else if (openMode != readonlyMode)
            {
                Close();
                nc_cell = new PaCell(tp_nc, niCell, readonlyMode);
            }
            openMode = readonlyMode;
        }

        public void Close()
        {
            nc_cell.Close();
            
            openMode = null;
        }

        public void Clear()
        {
            Open(false);
            nc_cell.Clear();
    
            nc_cell.Fill(new object[0]);
       
            Count = 0;
            codeByString.Clear();
            stringByCode = new string[0];
            
        }

        public int GetCode(string name)
        {
            int code;
            if (Count == 0 || !codeByString.TryGetValue(name, out code)) return Int32.MinValue;
            return code;
        }


        public string GetName(int code)
        {

            if (Count == 0) return string.Empty;
            if (code == int.MinValue) return string.Empty;
            if (Count <= code) return string.Empty;
            return stringByCode[code];
        }


        public Dictionary<string, int> InsertPortion(string[] portion)
        {
            Open(false);
            List<string> stringByCodeList = new List<string>();
            stringByCodeList.AddRange(stringByCode);
            var insertPortion = new Dictionary<string, int>();
            for (int i = 0; i < portion.Length; i++)
                if (!insertPortion.ContainsKey(portion[i]))
                {
                    var code = GetCode(portion[i]);
                    if (code == Int32.MinValue)
                    {
                        codeByString.Add(portion[i], code = Count++);
                        nc_cell.Root.AppendElement(new object[] { code, portion[i] });
                        stringByCodeList.Add(portion[i]);
                    }
                    insertPortion.Add(portion[i], code);
                }
            nc_cell.Flush();
            stringByCode = stringByCodeList.ToArray();
            return insertPortion;
        }

        public void MakeIndexed()
        {         
            Close();
        }

        public int Count { get; private set; }
        public Dictionary<string, int> InsertPortion(HashSet<string> entities)
        {
            return InsertPortion(entities.ToArray());
        }
    }
}
