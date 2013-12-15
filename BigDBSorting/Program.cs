using System;
using System.Diagnostics;
using BigDbTest;

namespace BigDBSorting
{
    class Program
    {
        static void Main(string[] args)
        {
            string fb_path = @"F:\FactographData\freebase-rdf-2013-02-10-00-00.nt2";
            //System.IO.FileStream fs = new System.IO.FileStream(fb_path, System.IO.FileMode.Open);
            System.IO.StreamReader sr = new System.IO.StreamReader(fb_path);
            System.IO.StreamWriter sw = new System.IO.StreamWriter(@"F:\FactographData\freebase.nt2");
            for (int i = 0; i < 100000; i++)
            {
                string line = sr.ReadLine();
                sw.WriteLine(line);
            }
            sw.Close();
            sr.Close();
            return;
            
            string path = @"..\..\..\Databases\";
            //string path = @"C:\Users\Marchuk\Polar\";
            DateTime tt0 = DateTime.Now;
            BigPolar bp = new BigPolar(path);

            bp.Load2(1000000000);
            Console.WriteLine("Load2 ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //bp.Index();
            //Console.WriteLine("Index ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            bp.IndexByKey();
            Console.WriteLine("IndexByKey ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

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
