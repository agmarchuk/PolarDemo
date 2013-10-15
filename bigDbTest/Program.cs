using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace BigDbTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string path = @"C:\home\dev2012\PolarDemo\";
            Console.WriteLine("Hello!");
            DateTime tt0 = DateTime.Now;
            bool sql = true;
            if (sql)
            {
                //BigSQL bs = new BigSQL(@"Data Source=(LocalDB)\v11.0;AttachDbFilename="+path+"Databases/test20131006.mdf;Integrated Security=True;Connect Timeout=30");
                BigSQL bs = new BigSQL(@"Data Source=(LocalDB)\v11.0;AttachDbFilename=C:\home\dev2012\bigDbTest\test20131014.mdf;Integrated Security=True;Connect Timeout=30");

                bs.PrepareToLoad();
                bs.Load(10000000);
                Console.WriteLine("Load ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //return;

                bs.Index();
                Console.WriteLine("Index ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                //bs.Test2("randcol>10000 AND randcol<12000");
                //Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                bs.Test2("randcol>777777770 AND randcol<777777780");
                Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                bs.Test2("randcol=777777777");
                Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                bs.Test3();
                Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            else
            {
                BigPolar bp = new BigPolar(path);
                bp.Load2(1000000);
                Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                return;
                //bp.Index();
                //Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                bp.Test3();
                Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                bp.Test3();
                Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
        }
    }
}
