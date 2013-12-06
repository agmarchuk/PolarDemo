﻿using System;
using System.Linq;
using PolarDB;

namespace BigDbTest
{
    class BigPolar
    {
        private string cellPath;
        private PaCell cell;
        public BigPolar(string path)
        {
            this.cellPath = path + @"bigtest.pac";
            cell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.integer)), cellPath, false);
        }
        System.Random rnd = new Random();
        public void PrepareToLoad()
        {
        }
        public void Index()
        {
            cell.Root.Sort<RecInt>();
            //cell.Root.SortByKey<RecInt>(o => (int)o);
            cell.Flush();
        }
        public void Load(int numb)
        {
            cell.Clear();
            int portion = 200;
            cell.StartSerialFlow();
            cell.S();
            for (int i = 0; i < numb / portion; i++)
            {
                if (i % 1000 == 0) Console.WriteLine("{0}%", (double)i * 100.0 / (double)numb * (double)portion);
                for (int j = 0; j < portion; j++)
                {
                    int value = i * portion + j; // rnd.Next();
                    cell.V(value);
                }
            }
            cell.Se();
            cell.EndSerialFlow();
            cell.Flush();
        }
        // Проверка метода AppendElement
        public void Load2(int numb)
        {
            cell.Clear();
            int portion = 200;
            cell.Fill(new object[0]);
            for (int i = 0; i < numb / portion; i++)
            {
                if (i % 1000 == 0) Console.WriteLine("{0}%", (double)i * 100.0 / (double)numb * (double)portion);
                for (int j = 0; j < portion; j++)
                {
                    int value = i * portion + j; // rnd.Next();
                    cell.Root.AppendElement(value);
                }
            }
            cell.Flush();
        }
        public void Test2(string condition)
        {
            //var qu = cell.Root.
        }
        public void Test3()
        {
            int cnt = 0;
            foreach (var x in cell.Root.Elements())
            {
                if ((int)x.Get() % 2 == 0) cnt++;
            }
            Console.WriteLine("Test3 result=" + cnt);
        }
        public void IndexByKey()
        {
            cell.Root.SortByKey(o => (int)o);
            cell.Flush();
        }

        internal void Test4(int[] samples)
        {
            foreach (var se in samples)
            {
               cell.Root.BinarySearchFirst(entry =>
                {
                    int e = (int) entry.Get();
                    return e == se ? 0 : e > se ? 1 : -1;
                });
            }
        }

        public long Count() { return cell.Root.Count(); }

        public void TestSort(int start, int stop)
        {
            for (int i = start; i < stop; i++)
            {
                Console.WriteLine(cell.Root.Element(i).Get());
            }
        }
    }
    // Это нужно для сортировки записей, см. ранее
    internal struct RecInt : IBRW, IComparable
    {
        internal int ientity;
        internal RecInt(int ient)
        {
            this.ientity = ient;
        }
        public IBRW BRead(System.IO.BinaryReader br)
        {
            ientity = br.ReadInt32();
            return new RecInt(ientity);
        }
        public void BWrite(System.IO.BinaryWriter bw)
        {
            bw.Write(ientity);
        }
        public int CompareTo(object v2)
        {
            RecInt y = (RecInt)v2;
            int c = this.ientity.CompareTo(y.ientity); return c;
        }
    }
}
