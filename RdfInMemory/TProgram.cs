using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfInMemory
{
    public class TProgram
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start");
            string path = @"..\..\..\Databases\";
            Graph gra = new Graph(path);
            TurtleParser parser = new TurtleParser();
            parser.Load(gra, @"D:\home\FactographDatabases\dataset\dataset1M.ttl");
        }
    }
}
