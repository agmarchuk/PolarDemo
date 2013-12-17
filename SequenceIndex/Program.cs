using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PolarDB;

namespace SequenceIndex
{
    class Program
    {
        private static void Main(string[] args)
        {
            
            string path = @"..\..\..\Databases\";
           // File.Delete(path+"teststrings.pac");
            PaCell testCell=new PaCell(new PTypeSequence(new PType(PTypeEnumeration.sstring)), path+"teststrings.pac", false );
            if (true)
            {
                testCell.Fill(new object[0]);
                for (long i = 0; i < (long) 1000*1000*1000; i++)
                {
                    var s = Guid.NewGuid().ToString();
                    testCell.Root.AppendElement(s);
                }

                testCell.Flush();
            }
            string testString = (string)testCell.Root.Elements().Last().Get();
            HashIndex<string> index = new HashIndex<string>(path, testCell, entry => (string)entry.Get());
            Stopwatch stopwatch=new Stopwatch();
            stopwatch.Start();
            index.Load();
            stopwatch.Stop();
            using (StreamWriter log = new StreamWriter("../../log.txt"))
                log.WriteLine("load " + (double) stopwatch.Elapsed.Ticks/10000);
            stopwatch.Restart();
            var res = index.GetFirst(testString);
           stopwatch.Stop();
            if(res.offset==long.MinValue) throw new Exception("fail");
            using (StreamWriter log = new StreamWriter("../../log.txt"))
                log.WriteLine("find " + res.Get() + Environment.NewLine + (double) stopwatch.Elapsed.Ticks/10000);
        }
    }
}
