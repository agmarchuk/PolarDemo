using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PolarDB;

namespace NameTable
{
    public class StringIntMD5RAMCollision : IStringIntCoding
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
                new NamedType("offset", new PType(PTypeEnumeration.longinteger))));
        private PType tp_nc = new PTypeSequence(
            new PTypeRecord(
                new NamedType("code", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring))));
        private PaCell nc_cell;

        private string niCell;
        private string pathCiCell;
        private string pathCOllisionsCell;
        private PaCell collisionsCell;
        private PaCell c_index;
        private PaCell md5_index;
        private string pathMD5Index;
        private bool? openMode;
        private readonly Dictionary<long, long> offsetByMd5 = new Dictionary<long, long>();
        private readonly Dictionary<long, List<long>> collisionsByMD5 = new Dictionary<long, List<long>>();
        private static Dictionary<string, IStringIntCoding> Opend = new Dictionary<string, IStringIntCoding>();


        public StringIntMD5RAMCollision(string path)
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
            pathCOllisionsCell = path + "collisionsCell.pac";
            // Создание ячеек, предполагается, что все либо есть либо их нет и надо создавать
            if (!File.Exists(niCell) || !File.Exists(pathCiCell))
                Clear();

            // Открытие ячеек в режиме работы (чтения)
            Open(true);
            Count = Convert.ToInt32(c_index.Root.Count());
            Opend.Add(path, this);

            ReadOffsetsByMd5();
            ReadCollisionsByMD5();
            Console.WriteLine("collisionsByMD5.Count " + collisionsByMD5.Count);
            Console.WriteLine("ENTITIES COUNT " + offsetByMd5.Count);
        }

        private void ReadOffsetsByMd5()
        {
            offsetByMd5.Clear();

            foreach (    object[] pair in md5_index.Root.ElementValues())
            {
                offsetByMd5.Add((long)pair[0], (long)pair[1]);
            }
        }

        void WriteOffsetsByMD5()
        {
            md5_index.Clear();
            //Array.Sort(codeByMd5.ToArray());
            md5_index.Fill(offsetByMd5.Select(pair => new object[] { pair.Key, pair.Value }).ToArray());
            md5_index.Flush();
        }


        private void ReadCollisionsByMD5()
        {
            collisionsByMD5.Clear();
            if (collisionsCell.Root.Count() == 0) return;
            long md5 = (long)((object[])collisionsCell.Root.ElementValues(0, 1).First())[0];
            var offsets = new List<long>();
            foreach (object[] md5_offset in collisionsCell.Root.ElementValues())
            {
                if ((long)md5_offset[0] != md5)
                {
                    collisionsByMD5.Add(md5, offsets);
                    md5 = (long)md5_offset[0];
                    offsets = new List<long>();
                }
                offsets.Add((long)md5_offset[1]);
            }
        }


        void WriteCollisions()
        {
            collisionsCell.Clear();
            collisionsCell.Fill(new object[0]);
            foreach (KeyValuePair<long, List<long>> pair in collisionsByMD5)
                foreach (long offset in pair.Value)
                    collisionsCell.Root.AppendElement(new object[] { pair.Key, offset });
            collisionsCell.Flush();
        }

        public void WarmUp()
        {
            foreach (var q in nc_cell.Root.ElementValues()) ;
            foreach (var q in c_index.Root.ElementValues()) ;
            foreach (var q in md5_index.Root.ElementValues()) ;
        }

        public int InsertOne(string entity)
        {

            var newMD5 = BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(entity)), 0);
            int code = 0;

            List<long> offsets;
            if (collisionsByMD5.TryGetValue(newMD5, out offsets))
            {
                PaEntry ncEntry = nc_cell.Root.Element(0);
                bool newCollision = true;
                foreach (int offset in offsets)
                {
                    ncEntry.offset = offset;
                    var code_name = (object[])ncEntry.Get();
                    if ((string)code_name[1] != entity) continue;
                    code = (int)code_name[0];
                    newCollision = false;
                    break;
                }
                if (newCollision)
                {
                    //checkSumList.Add(newMD5);
                    long newOffset = nc_cell.Root.AppendElement(new object[] { code = Count++, entity });
                    // ofsets2NC.Add(newOffset);
                    offsetByMd5.Add(newMD5, newOffset);
                }
            }
            else
            {
                long offsetOnCodeName = 0;
                if (offsetByMd5.TryGetValue(newMD5, out offsetOnCodeName))
                {
                    //может появилась коллизия
                    PaEntry ncEntry = nc_cell.Root.Element(0);
                    ncEntry.offset = offsetOnCodeName;
                    string existsName = (string)((object[])ncEntry.Get())[1];
                    if (existsName == entity)
                        code = (int)((object[])ncEntry.Get())[0];
                    else
                    {
                        //новая коллизия, добавляем строку
                        collisionsByMD5.Add(newMD5, offsets = new List<long>());
                        offsets.Add(offsetOnCodeName);
                        long newOffset = nc_cell.Root.AppendElement(new object[] { code = Count++, entity });
                        offsets.Add(newOffset);
                    }
                }
                else
                {
                    long newOffset = nc_cell.Root.AppendElement(new object[] { code = Count++, entity });
                    offsetByMd5.Add(newMD5, newOffset);
                }
            }
            return code;
        }

        public void Open(bool readonlyMode)
        {
            if (openMode == readonlyMode) return;

            if (openMode != null) Close();

            nc_cell = new PaCell(tp_nc, niCell, readonlyMode);
            collisionsCell = new PaCell(tp_pair_longs, pathCOllisionsCell, readonlyMode);
            c_index = new PaCell(tp_ind, pathCiCell, readonlyMode);
            md5_index = new PaCell(tp_md5_code, pathMD5Index, readonlyMode);

            openMode = readonlyMode;
        }

        public void Close()
        {
            nc_cell.Close();
            c_index.Close();
            md5_index.Close();
            collisionsCell.Close();
            openMode = null;
        }

        public void Clear()
        {
            Open(false);
            nc_cell.Clear();
            c_index.Clear();
            md5_index.Clear();
            collisionsCell.Clear();
            collisionsCell.Fill(new object[0]);
            nc_cell.Fill(new object[0]);
            c_index.Fill(new object[0]);
            md5_index.Fill(new object[0]);
            Count = 0;
            offsetByMd5.Clear();
            collisionsByMD5.Clear();
        }

        public int GetCode(string name)
        {
            Open(true);
            if (Count == 0) return Int32.MinValue;
            long newMd5 = BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(name)), 0);
            if (Count == 0) return Int32.MinValue;

            PaEntry ncEntry = nc_cell.Root.Element(0);
            List<long> offsets;
            object[] code_name;
            if (collisionsByMD5.TryGetValue(newMd5, out offsets))
            {
                foreach (long offset in offsets)
                {
                    ncEntry.offset = offset;
                    code_name = (object[])ncEntry.Get();
                    if ((string)code_name[1] == name)
                        return (int)code_name[0];
                }
                return int.MinValue;
            }
            long offsetUni;
            if (!offsetByMd5.TryGetValue(newMd5, out offsetUni)) return Int32.MinValue;
            ncEntry.offset = offsetUni;
            code_name = (object[])ncEntry.Get();
            return (string)code_name[1] == name ? (int)code_name[0] : Int32.MinValue;

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
            Console.Write("c_warm ");
            //  List<long> ofsets2NC = new List<long>(portion.Count);
          //  List<long> checkSumList = new List<long>(portion.Count);
            var insertPortion = new Dictionary<string, int>(portion.Count);
            foreach (var newString in portion)
            {
                insertPortion.Add(newString, InsertOne(newString));
            }

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
            foreach (PaEntry entry in nc_cell.Root.Elements())
                offsets[i++] = entry.offset;
            c_index.Fill(offsets);
             c_index.Flush();
            
            WriteOffsetsByMD5();
            WriteCollisions();
            Open(true);
        }

        public int Count { get; private set; }

    }
}
