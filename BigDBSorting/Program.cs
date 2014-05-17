using System;
using System.Diagnostics;
using System.Linq;
using BigDbTest;
using PolarDB;

namespace BigDBSorting
{
    class Program
    {
        // Проверка индексирования строкового столбца
        static void Main(string[] args)
        {
            Console.WriteLine("Start");
            string path = @"..\..\..\Databases\";
            DateTime tt0 = DateTime.Now;
            Random rnd = new Random(3333);

            // Две ячейки: строковый столбец и индексный столбец
            PaCell scell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.sstring)), path + "scell.pac", false);
            PaCell icell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), path + "icell.pac", false);

            // Заполнение
            bool toload = false;
            int nvalues = 10000000;
            if (toload)
            {
                scell.Clear();
                scell.Fill(new object[0]);
                icell.Clear();
                icell.Fill(new object[0]);
                for (int i = 0; i < nvalues; i++)
                {
                    if ((i + 1) % 100000 == 0) Console.Write("{0} ", (i + 1) / 100000); 
                    long off = scell.Root.AppendElement("http://verylongdomaintotestprefix.ru/qwerty/" + rnd.Next().ToString());
                    icell.Root.AppendElement(off);
                }
                Console.WriteLine();
                scell.Flush();
                icell.Flush();
                Console.WriteLine("Load ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                PaEntry.bufferBytes = 200000000;

                var ptr = scell.Root.Element(0);
                icell.Root.SortByKey<string>((object v) =>
                {
                    ptr.offset = (long)v;
                    return (string)ptr.Get();
                });
                Console.WriteLine("Sort ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }

            rnd = new Random(3333);
            int[] rand_arr = new int[nvalues];
            for (int i = 0; i < nvalues; i++)
            {
                rand_arr[i] = rnd.Next();
            }

            // Проверка бинарного поиска
            rnd = new Random(3333);
            tt0 = DateTime.Now;
            int ntests = 1000;
            int found = 0;
            for (int i = 0; i < ntests; i++)
            {
                string sample = "http://verylongdomaintotestprefix.ru/qwerty/" + rand_arr[rnd.Next(nvalues - 1)].ToString();
                PaEntry entity = scell.Root.Element(0);
                PaEntry qq = icell.Root.BinarySearchFirst(en =>
                    {
                        long off = (long)en.Get();
                        entity.offset = off;
                        //return sample.CompareTo((string)entity.Get());
                        return ((string)entity.Get()).CompareTo(sample);
                    });
                if (!qq.IsEmpty) found++;
            }
            Console.WriteLine("Found {0}. Duration={1}", found, (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
        // Поверка "разогрева" WarmUp
        static void Main1(string[] args)
        {
            string path = @"..\..\..\Databases\";
            //string path = @"G:\Home\Databases\";
            DateTime tt0 = DateTime.Now;
            Random rnd = new Random();
            PaCell icell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.integer)), path + "icell.pac", false);

            bool toload = true;
            int nvalues = 100000000;
            if (toload)
            {
                icell.Clear();
                icell.Fill(new object[0]);
                for (int i = 0; i < nvalues; i++)
                {
                    icell.Root.AppendElement(rnd.Next());
                }
                icell.Flush();
                Console.WriteLine("Load2 ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                PaEntry.bufferBytes = 200000000;
                icell.Root.SortByKey<int>(ob => (int)ob); ;
                Console.WriteLine("Sort ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            
            //foreach (var v in icell.Root.ElementValues()) ;
            //Console.WriteLine("WarmUp ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            
            for (int i = 0; i < 1000; i++)
            {
                int r = rnd.Next(nvalues - 1);
                int v = (int)icell.Root.Element(r).Get();
            }
            Console.WriteLine("ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
        static void Main0(string[] args)
        {
            string path = @"..\..\..\Databases\";
            DateTime tt0 = DateTime.Now;
            BigPolar bp = new BigPolar(path);

            bool toload = true;
            if (toload)
            {
                bp.Load2(100000000);
                Console.WriteLine("Load2 ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                bp.IndexByKey();
                Console.WriteLine("IndexByKey ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }

            Console.WriteLine("N records: " + bp.Count());
            Random rnd = new Random();
            int[] arr1000 = Enumerable.Range(0, 1000).Select(i => rnd.Next()).ToArray();
            tt0 = DateTime.Now;
            Stopwatch timer=new Stopwatch(); 
            timer.Start();
            bp.Test4(new int[] { 100, 100000000, 10, 4000000, 108, 90000000, 50000000, 1, int.MaxValue / 16 - 1, 303 });
            timer.Stop();
            Console.WriteLine("Test5 ok. duration=" + timer.Elapsed.Ticks );
            Console.WriteLine("standard duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            // Разогрев
            int v = 0;
            foreach (var e in bp.Cell.Root.Elements()) { v = (int)e.Get(); }
            Console.WriteLine("Worming... standard duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            timer.Restart();
            //bp.Test4(new int[] { 99999999, 88888888, 77777777, 66666666, 55555555, 44444444, 33333333, 22222222, 11111111, 3 });
            //bp.Test4(new int[] { 2039530, 923720000, -921354321, 1445454545, -55555555, 828282828, 1140000000, -22222222, 1030303030, -777777777 });
            bp.Test4(arr1000);
            timer.Stop();
            Console.WriteLine("Test5 ok. duration=" + timer.Elapsed.Ticks);
            Console.WriteLine("standard duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //bp.TestSort(0, 10);
            
        }
    }
}
