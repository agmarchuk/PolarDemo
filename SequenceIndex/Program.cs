using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SequenceIndex
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"..\..\..\Databases\";
            var seq = new AllInt(path + "seq1", false);
            seq.FillData(new Dictionary<int, long>() { { 10, 10 }, { 100, 100 }, { -10, -10 }, { 100000, 100000 } }.ToArray());
            //  var seq = new DexTree(path + "dt", false);
            //  seq.Fill(Enumerable.Range(0,1000000).Select(i=>new KeyValuePair<int,long>(i,(long)i)).ToArray());

            Stopwatch s = new Stopwatch(); s.Start();
            long resчапр = seq.Search(10);
            s.Stop();
            Console.WriteLine(resчапр + " " + s.Elapsed.Ticks);
            s.Restart();
            long варапр = seq.Search(100000);
            s.Stop();
            Console.WriteLine(варапр + " " + s.Elapsed.Ticks);           
        }
    }
}
