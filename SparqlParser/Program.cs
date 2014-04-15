using System;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Sparql;
using SparqlParser;
using TrueRdfViewer;

namespace ANTLR_Test
{
    class Program
    {
        private static int tripletsCount;
        static void Main(string[] args)
        {
            Func<int, int> s;
            //В качестве входного потока символов устанавливаем консольный ввод

            //var te = new StreamReader(@"..\..\Input.txt").ReadToEnd();
            // Console.WriteLine(te);
            DateTime start = DateTime.Now;
            //Parse(te);
            //);  

            //ParseXML(te).Save(@"..\..\output.xml");
            //  Console.WriteLine((DateTime.Now - start).TotalMilliseconds);
            //var f= Expression.Lambda<Func<string, double>>(Expression.Convert(Expression.Constant(null), typeof (double)),new ParameterExpression[]{Expression.Parameter(typeof(string))}).Compile();


            PolarDB.PaEntry.bufferBytes = 1*1000*1000*100;

            // ts.LoadTurtle(@"C:\deployed\dataset1M.ttl");
            int millions = 1;
            tripletsCount = millions * 1000 * 1000;
            TripleStoreInt ts =
                new TripleStoreInt(@"C:\Users\Admin\Source\Repos\PolarDemo\Databases\" + millions + @"mln\");
           //  ts.LoadTurtle(@"C:\deployed\" + millions + "M.ttl");       //30мин.
           
           

            Console.WriteLine((DateTime.Now - start).TotalMinutes);

            //   ts.LoadTurtle(@"C:\deployed\dataset1M.ttl");
            //  Parse("SELECT * {}");
          //  RunBerlinsWithConstants( ts);
            RunBerlinsParameters(ts);
            //      RunBerlins(queriesDir, ts);
            //Parse(te, ts);
        }

        private static void RunBerlinsParameters(TripleStoreInt ts)
        {
            long[] results= new long[12];
            Console.WriteLine("antrl parametered");
            int i = 0;             
            var fileInfos = new []
                {
                    @"..\..\sparql data\queries\parameters\1.rq"     ,  
                    @"..\..\sparql data\queries\parameters\2.rq"   ,
                    @"..\..\sparql data\queries\parameters\3.rq"    , 
                    @"..\..\sparql data\queries\parameters\4.rq",
                    @"..\..\sparql data\queries\parameters\5.rq" ,     
                    @"..\..\sparql data\queries\parameters\6.rq" ,
                    @"..\..\sparql data\queries\parameters\7.rq"  ,
                    @"..\..\sparql data\queries\parameters\8.rq"  ,
                    @"..\..\sparql data\queries\parameters\9.rq",
                    @"..\..\sparql data\queries\parameters\10.rq"  ,
                    @"..\..\sparql data\queries\parameters\11.rq",
                    @"..\..\sparql data\queries\parameters\12.rq"  ,
                }
                .Select(s => new FileInfo(s))
                .ToArray();
            for (int j = 0; j < 500; j++)
            {
                
                foreach (var file in fileInfos)
                {
                    var readAllText = File.ReadAllText(file.FullName);
                   readAllText = QuerySetParameter(readAllText);
                    var st = DateTime.Now;
                    var q = Parse(readAllText);


                    var resultString = q.Run(ts);
                    var totalMilliseconds = (long) (DateTime.Now - st).TotalMilliseconds;
                    // results[i++] += totalMilliseconds;
                    //File.WriteAllText(Path.ChangeExtension(file.FullName, ".txt"), resultString);
                    //.Save(Path.ChangeExtension(file.FullName,".xml"));
                }
            }
            for (int j = 0; j < 500; j++)
            {
                i = 0;
                foreach (var file in fileInfos)
                {
                    var readAllText = File.ReadAllText(file.FullName);
                    readAllText = QuerySetParameter(readAllText);
                    var st = DateTime.Now;
                    var q = Parse(readAllText);    
                     var resultString = q.Run(ts);
                    var totalMilliseconds = (DateTime.Now - st).Ticks / 10000L;
                     results[i++] += totalMilliseconds;
                   // File.WriteAllText(Path.ChangeExtension(file.FullName, ".txt"), resultString);
                    //.Save(Path.ChangeExtension(file.FullName,".xml"));
                }
            }
            Console.WriteLine(string.Join(", ", results.Select(l => l/500)));
            
        }
        private static void RunBerlinsWithConstants(TripleStoreInt ts)
        {
            long[] results = new long[12];
            Console.WriteLine("antrl with constants");
            int i = 0;
            var fileInfos = new[]
                {
                    @"..\..\sparql data\queries\with constants\1.rq"     ,  
                    @"..\..\sparql data\queries\with constants\2.rq"   ,
                    @"..\..\sparql data\queries\with constants\3.rq"    , 
                    @"..\..\sparql data\queries\with constants\4.rq",
                    @"..\..\sparql data\queries\with constants\5.rq" ,     
                    @"..\..\sparql data\queries\with constants\6.rq" ,
                    @"..\..\sparql data\queries\with constants\7.rq"  ,
                    @"..\..\sparql data\queries\with constants\8.rq"  ,
                    @"..\..\sparql data\queries\with constants\9.rq",
                    @"..\..\sparql data\queries\with constants\10.rq"  ,
                    @"..\..\sparql data\queries\with constants\11.rq",
                    @"..\..\sparql data\queries\with constants\12.rq"  ,
                }
                .Select(s => new FileInfo(s))
                .ToArray();
            for (int j = 0; j < 500; j++)
            {

                foreach (var file in fileInfos)
                {
                    var readAllText = File.ReadAllText(file.FullName);
                    readAllText = QuerySetParameter(readAllText);
                 //   var st = DateTime.Now;
                    var q = Parse(readAllText);


                    var resultString = q.Run(ts);
                    //var totalMilliseconds = (long)(DateTime.Now - st).TotalMilliseconds;
                    // results[i++] += totalMilliseconds;
                    //File.WriteAllText(Path.ChangeExtension(file.FullName, ".txt"), resultString);
                    //.Save(Path.ChangeExtension(file.FullName,".xml"));
                }
            }
            for (int j = 0; j < 500; j++)
            {
                i = 0;
                foreach (var file in fileInfos)
                {
                    var readAllText = File.ReadAllText(file.FullName);
                    readAllText = QuerySetParameter(readAllText);
                    var st = DateTime.Now;
                    var q = Parse(readAllText);
                    var resultString = q.Run(ts);
                    var totalMilliseconds = (DateTime.Now - st).Ticks / 10000L;
                    results[i++] += totalMilliseconds;
              //   File.WriteAllText(Path.ChangeExtension(file.FullName, ".txt"), resultString);
                    //.Save(Path.ChangeExtension(file.FullName,".xml"));
                }
            }
            Console.WriteLine(string.Join(", ", results));

        }

        private static Query Parse(string te)
        {
            ICharStream input = new AntlrInputStream(te);


            // Console.WriteLine(input);
            // Настраиваем лексер на этот поток
            //CalcLexer lexer = new CalcLexer(input);
            //// Создаем поток токенов на основе лексера
            //CommonTokenStream tokens = new CommonTokenStream(lexer);
            //// Создаем парсер
            //CalcParser parser = new CalcParser(tokens);
            //// И запускаем первое правило грамматики!!!
            //parser.calc();
            DateTime tm = DateTime.Now;

            var lexer = new sparql2PacLexer(input);
           
            var commonTokenStream = new CommonTokenStream(lexer);
            var sparqlParser = new sparql2PacParser(commonTokenStream);
            sparqlParser.query();
         //   Console.WriteLine((DateTime.Now-tm).TotalMilliseconds);
            return sparqlParser.q;
        }                                

        private static Random random = new Random(DateTime.Now.Millisecond);

        private static string[] words =
            File.ReadAllLines(@"..\..\sparql data\queries\parameters\titlewords.txt");
       private static string QuerySetParameter(string parameteredQuery)
       {
           var product = random.Next(1, tripletsCount / 4000);
           var reviewer = random.Next(1, tripletsCount / 69000) + 1;
           var offer = random.Next(1, tripletsCount/1700) + 1 ;
           return parameteredQuery.Replace("%ProductType%", "bsbm-inst:ProductType" + random.Next(1, tripletsCount / 66300 + 1))
               .Replace("%ProductFeature1%", "bsbm-inst:ProductFeature" + random.Next(1, tripletsCount / 21000 + 1))
               .Replace("%ProductFeature2%", "bsbm-inst:ProductFeature" + random.Next(1, tripletsCount / 20100 + 1))
               .Replace("%ProductFeature3%", "bsbm-inst:ProductFeature" + random.Next(1, tripletsCount / 21000+ 1))
               .Replace("%x%", random.Next(1, 500).ToString())
               .Replace("%y%", random.Next(1, 500).ToString())
               .Replace("%ProductXYZ%", string.Format(
                   "<http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer{0}/Product{1}>",
                   product / 50 + 1, product))
               .Replace("%word1%", words[random.Next(0, words.Length)])//
               .Replace("%currentDate%", "\"" + DateTime.Today.AddYears(-6) + "\"^^<http://www.w3.org/2001/XMLSchema#dateTime>")
               .Replace("%ReviewXYZ%",
                   string.Format(
                       "<http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromRatingSite{0}/Review{1}>",
                       reviewer / 362 + 1, reviewer))
               .Replace("%OfferXYZ%",
                   string.Format(
                       "<http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromVendor{0}/Offer{1}>",
                       offer / 1941 + 1, offer));
       }
    }
}
