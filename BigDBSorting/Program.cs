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
            DateTime tt0 = DateTime.Now;
            BigPolar bp = new BigPolar(path);
            bp.Load2((int)int.MaxValue/16);
            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //bp.Index();
            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            bp.IndexByKey();
            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            Stopwatch timer=new Stopwatch(); timer.Start();
            bp.Test5();
            timer.Stop();
            Console.WriteLine("duration=" + timer.Elapsed.Ticks ); tt0 = DateTime.Now;
        }
    }
}
