using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace BigDbTest
{
    class BigPolar
    {
        System.Random rnd = new Random();
        public void PrepareToLoad()
        {
        }
        public void Index()
        {
            PaCell cell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.integer)),
                @"D:\home\dev2012\PolarDemo\Databases\bigtest.pac", false);
            cell.Root.Sort<RecInt>();
            cell.Close();
        }
        private void Load(int numb)
        {
            PaCell cell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.integer)),
                @"D:\home\dev2012\PolarDemo\Databases\bigtest.pac", false);
            cell.Clear();
            int portion = 200;
            cell.StartSerialFlow();
            cell.S();
            for (int i = 0; i < numb / portion; i++)
            {
                if (i % 10000 == 0) Console.WriteLine("{0}%", (double)i * 100.0 / (double)numb * (double)portion);
                for (int j = 0; j < portion; j++)
                {
                    int value = rnd.Next();
                    cell.V(value);
                }
            }
            cell.Se();
            cell.EndSerialFlow();
            cell.Close();
        }
        public void Test2(string condition)
        {
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
