using System;
using System.Diagnostics;
using System.Linq;
using BigDbTest;
using PolarDB;

namespace BigDBSorting
{
    class Program
    {
        // Поверка "разогрева" WarmUp
        static void Main(string[] args)
        {
            string path = @"..\..\..\Databases\";
            DateTime tt0 = DateTime.Now;
            Random rnd = new Random();
            PaCell icell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.integer)), path + "icell.pax", false);

            bool toload = false;
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
