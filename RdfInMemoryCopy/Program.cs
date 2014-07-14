using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfInMemoryCopy
{
    class Program
    {
        static void Main(string[] args)
        {

            SGraph graph = new SGraph(@"../../../Databases/");
            
            //new TurtleParser().LoadTriplets(graph, @"D:\home\FactographDatabases\dataset\dataset1M.ttl");
        }

    }
}
