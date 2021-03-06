﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using PolarDB;

namespace SequenceIndex
{
    class AllIntUnion:PxCell
    {
        public AllIntUnion(string filePath, bool readOnly = true) :
            base(
            new PTypeRecord(
                new NamedType("plusZero", new PTypeSequence(
                    new PTypeUnion(
                    new NamedType("none", new PType(PTypeEnumeration.none)),
                    new NamedType("exists", new PType(PTypeEnumeration.longinteger))))),
                new NamedType("minus", new PTypeSequence(
                    new PTypeUnion(
                    new NamedType("none", new PType(PTypeEnumeration.none)),
                    new NamedType("exists", new PType(PTypeEnumeration.longinteger)))))
                    ),
            filePath, readOnly)
        {
        }

        private  static readonly int Max = (int)Math.Pow(2, 25);
        public void FillData(KeyValuePair<int, long>[] data)
        {
            object[] op, om;
            op = (object[])new object[Max];
            om = (object[])new object[Max];
            for (int i = 0; i < Max; i++)
            {
                op[i] = new object[] {0, null};
                om[i] = new object[] {0, null};
            }
            for (int i = 0; i < data.Length; i++)
                if (data[i].Key >= 0)
                    op[data[i].Key] = new object[] {1, data[i].Value};
                else om[-data[i].Key] = new object[] {1, data[i].Value};
            Fill2(new object[] {op, om});
        }

        public long Search(int key)
        {
            var valueUnion = Root.Field(key >= 0 ? 0 : 1).Element(Convert.ToInt64(Math.Abs(key)));
            if (valueUnion.Tag() == 0) return long.MinValue;
            return (long) valueUnion.UElementUnchecked(1).Get();
        }
    }
    class AllInt
    {
        private PxCell positive, negotive;
        public AllInt(string filePath, bool readOnly = true)
           
        {
            positive = new PxCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), filePath + "positive", readOnly);
            negotive = new PxCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), filePath + "negotive", readOnly);
        }

        private static readonly int Max = (int)Math.Pow(2, 27);

        public void FillData(KeyValuePair<int, long>[] data)
        {
            FillFile(data.Where(i => i.Key >= 0).ToArray(), positive);
            FillFile(data.Where(i => i.Key < 0).Select(i=>new KeyValuePair<int, long>(-i.Key,i.Value)).ToArray(), negotive);
        }

        private void FillFile(KeyValuePair<int, long>[] data, PxCell pxCell)
        {
            object[] op = new object[Max];
            for (int i = 0; i < Max; i++)
            {
                op[i] = long.MinValue;
            }
            for (int i = 0; i < data.Length; i++)
                op[data[i].Key] = data[i].Value;
            pxCell.Fill2(op);
        }

        public long Search(int key)
        {
            var valueUnion = (key >= 0 ? positive : negotive).Root.Element(Convert.ToInt64(Math.Abs(key)));
            return (long)valueUnion.Get();
        }
    }

    class HashIndex
    {
       static PType testTableType = new PTypeSequence(new PTypeRecord(new NamedType("id", new PType(PTypeEnumeration.sstring)), new NamedType("some", new PType(PTypeEnumeration.sstring))));
       static PType hashesType = new PTypeSequence(new PTypeRecord(new NamedType("hash", new PType(PTypeEnumeration.integer)), new NamedType("offset", new PType(PTypeEnumeration.longinteger))));
       static PType indexesType = new PTypeSequence(new PTypeRecord(new NamedType("offset4hash",new PType(PTypeEnumeration.longinteger)),new NamedType("count", new PType(PTypeEnumeration.integer))));
        private PaCell testTable, hashes, indexes;

        public HashIndex(string dirPath)
        {

            
            this.testTable = new PaCell(testTableType, dirPath + "/test_table.pac",false);
            hashes = new PaCell(testTableType, dirPath + "/test_table.pac", false);
            indexes = new PaCell(testTableType, dirPath + "/test_table.pac", false);
            testTable.Fill(new object[0]);
            hashes.Fill(new object[0]);
            indexes.Fill(new object[0]);
            long offset = testTable.Root.AppendElement(new object[] {"id", "some"});
            KeyValuePair<long,int> offset4HashCount = GetOffsetAndCount4Hash("id");
            if (offset4HashCount.Value == 0) // new key's hash
            {
                
            }
            else
            {
                
            }
            long offset4hash = hashes.Root.AppendElement(null);
        }

        public void Fill(KeyValuePair<object,long>[] keysOffsets)
        {
            indexes.Fill(new object[0]);
            foreach (var key in 
                keysOffsets.GroupBy(key => key.Key.GetHashCode() > 0 ? key.Key.GetHashCode() : -key.Key.GetHashCode()).OrderBy(g => g.Key))
            {
                
                
            }
        }

        public void CreateIndexes()
        {
            
        }
        public KeyValuePair<long, int> GetOffsetAndCount4Hash(string key)
        {
            int hash = key.GetHashCode();
            if (hash < 0) hash = -hash;
            PaEntry index = indexes.Root.Element(Convert.ToInt64(hash));
            var count = Convert.ToInt32(index.Field(1).Get());
            if (count == 0) return new KeyValuePair<long, int>(long.MinValue, 0);
            return new KeyValuePair<long, int>(Convert.ToInt64(index.Field(0).Get()), count);
        }
        //[DllImport("kernel32.dll", SetLastError = true)]
        //public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX buffer);
    }
}
