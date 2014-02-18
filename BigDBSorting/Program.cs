using System;
using System.Diagnostics;
using BigDbTest;

namespace BigDBSorting
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"..\..\..\Databases\";
            //string path = @"C:\Users\Marchuk\Polar\";
            DateTime tt0 = DateTime.Now;
            BigPolar bp = new BigPolar(path);

            bool toload = false;
            if (toload)
            {
                bp.Load2(100000000);
                Console.WriteLine("Load2 ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                bp.IndexByKey();
                Console.WriteLine("IndexByKey ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }

            Console.WriteLine("N records: " + bp.Count());

            Stopwatch timer=new Stopwatch(); 
            timer.Start();
            bp.Test4(new int[] { 100, 100000000, 10, 4000000, 108, 90000000, 50000000, 1, int.MaxValue / 16 - 1, 303 });
            timer.Stop();
            Console.WriteLine("Test5 ok. duration=" + timer.Elapsed.Ticks ); 
            timer.Restart();
            bp.Test4(new int[] { 99999999, 88888888, 77777777, 66666666, 55555555, 44444444, 33333333, 22222222, 11111111, 3 });
            timer.Stop();
            Console.WriteLine("Test5 ok. duration=" + timer.Elapsed.Ticks); 
          //bp.TestSort(0, 10);
            
        }
    }
}
