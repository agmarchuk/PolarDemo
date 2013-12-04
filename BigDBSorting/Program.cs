using System;
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
            bp.Load2(Int32.MaxValue / 32);
            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //bp.Index();
            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            bp.IndexByKey();
            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            bp.Test3();
            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
    }
}
