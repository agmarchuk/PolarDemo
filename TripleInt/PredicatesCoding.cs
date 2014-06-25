using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PolarDB;
using TripleIntClasses;

namespace NameTable
{
    public class PredicatesCoding 
    {

        private PType tp_nc = new PTypeSequence(
            new PTypeRecord(
                new NamedType("code", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("literal type", new PType(PTypeEnumeration.integer))));
        private PaCell nc_cell;

        private bool? openMode;
        private readonly Dictionary<string, int> codeByString = new Dictionary<string, int>();
        private string[] stringByCode;
        public LiteralVidEnumeration?[] LiteralVid;
        private string niCell;

        public PredicatesCoding(string path)
        {

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            niCell = path + "/n_index.pac";

            // Создание ячеек, предполагается, что все либо есть либо их нет и надо создавать
            if (!File.Exists(niCell))
                Clear();

            // Открытие ячеек в режиме работы (чтения)
            var stringByCodeList = new List<string>();
            var LiteralVidList = new List<int>();
            Open(true);
            foreach (object[] code_name in nc_cell.Root.ElementValues())
            {   
                codeByString.Add((string) code_name[1], (int) code_name[0]);
                stringByCodeList.Add((string)code_name[1]);
                LiteralVidList.Add((int) code_name[2]);
            }
            stringByCode = stringByCodeList.ToArray();
            LiteralVid = LiteralVidList.Select(i => i==-1 ? null : new LiteralVidEnumeration?((LiteralVidEnumeration)i)).ToArray();
            Count = stringByCode.Length;
          Close();
        }              

        public void WarmUp()
        {
            //foreach (var q in nc_cell.Root.ElementValues()) ;
           
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
            LiteralVid=new LiteralVidEnumeration?[0];
            
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


        public Dictionary<string, int> InsertPortion(KeyValuePair<string, LiteralVidEnumeration?>[] portion)
        {
            Open(false);
            List<string> stringByCodeList = new List<string>(stringByCode);
            List<LiteralVidEnumeration?> literalsTypes=new List<LiteralVidEnumeration?>(LiteralVid);
            var insertPortion = new Dictionary<string, int>();
            for (int i = 0; i < portion.Length; i++)
                if (!insertPortion.ContainsKey(portion[i].Key))
                {
                    var code = GetCode(portion[i].Key);
                    if (code == Int32.MinValue)
                    {
                        codeByString.Add(portion[i].Key, code = Count++);
                        nc_cell.Root.AppendElement(new object[] { code, portion[i].Key, (object) portion[i].Value ?? -1 });
                        stringByCodeList.Add(portion[i].Key);
                        literalsTypes.Add(portion[i].Value);
                    }
                    else
                    {
                        if (LiteralVid[code] != portion[i].Value) throw new Exception("literal types different in same predicate " + portion[i]);
                    }
                    insertPortion.Add(portion[i].Key, code);
                }
            nc_cell.Flush();
            stringByCode = stringByCodeList.ToArray();
            LiteralVid = literalsTypes.ToArray();
            return insertPortion;
        }

        public void MakeIndexed()
        {         
            Close();
        }

        public int Count { get; private set; }
   
    }
}
