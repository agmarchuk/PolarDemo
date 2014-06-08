using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfTrees
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string path = @"..\..\..\Databases\";
            Console.WriteLine("Start RdfTrees");
            RdfTrees rtrees = new RdfTrees(path);
            rtrees.LoadTurtle(@"D:\home\FactographDatabases\dataset\dataset1M.ttl");
        }
    }
}
