using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using SparqlParser;
using TrueRdfViewer;

namespace ANTLR_Test
{
    class Program
    {
        private static int Millions = 1;

        private static void Main(string[] args)
        {
            PolarDB.PaEntry.bufferBytes = 1*1000*1000*1000;

            Millions = 1;         
          
          //   Test();
         
            Millions = 10;
               
          
          Test();
              
            Millions = 100;     // TestCoding();
           //    Test();
        }

        

        private static void Test()
        {
            Console.WriteLine(Millions);

           TripleStoreInt ts = new TripleStoreInt(@"C:\Users\Admin\Source\Repos\PolarDemo\Databases\" + Millions + @"mln\");
            //  TripleStoreInt ts = new TripleStoreInt(@"C:\Users\Admin\Source\Repos\PolarDemo\Databases\undecoded\" + Millions + @"mln\");

             bool load = false;
            //      bool load = true;
            using (StreamWriter wr = new StreamWriter(@"..\..\output.txt", true))
                wr.WriteLine("millions " + Millions);
            DateTime start = DateTime.Now;
            if (load)
            {
                ts.LoadTurtle(@"C:\deployed\" + Millions + "M.ttl"); //30мин.          


            }
            else
            {
               //     RunBerlinsWithConstants( ts);
               RunBerlinsParameters(ts);      
            }
            var spent = (DateTime.Now - start).Ticks/10000;
            using (StreamWriter wr = new StreamWriter(@"..\..\output.txt", true))   
                wr.WriteLine("total " + spent + " мс.");
        }

        private static void RunBerlinsParameters(TripleStoreInt ts)
        {
           
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
            var paramvaluesFilePath = string.Format(@"..\..\sparql data\queries\parameters\param values for{0} m.txt", Millions);
            //using (StreamWriter streamQueryParameters = new StreamWriter(paramvaluesFilePath)) 
            //    for (int j = 0; j < 1000; j++)
            //        foreach (var file in fileInfos.Select(info => File.ReadAllText(info.FullName)))
            //            QueryWriteParameters(file, streamQueryParameters, ts);


            using (StreamReader streamQueryParameters = new StreamReader(paramvaluesFilePath))
            {
                for (int j = 0; j < 500; j++)
                    fileInfos.Select(file => QueryReadParameters(File.ReadAllText(file.FullName),
                        streamQueryParameters))
                        .Select(queryReadParameters => Parse(queryReadParameters).Run(ts))
                        .ToArray();

                SubTestRun(ts, fileInfos, streamQueryParameters, 500);
            }
        }

        private static void SubTestRun(TripleStoreInt ts, FileInfo[] fileInfos,  StreamReader streamQueryParameters, int i1)
        {
            int i; 
            long[] results = new long[12];
            double[] minimums = Enumerable.Repeat(double.MaxValue, 12).ToArray();
            double[] maximums = new double[12];
            double maxMemoryUsage = 0;
            long[] totalparseMS=new long[12];
            long[] totalrun = new long[12];
            for (int j = 0; j < i1; j++)
            {
                i = 0;
                
                foreach (var file in fileInfos)
                {
                    var readAllText = File.ReadAllText(file.FullName);
                    readAllText = QueryReadParameters(readAllText, streamQueryParameters);

                    var st = DateTime.Now;
                    var q = Parse(readAllText);
                    totalparseMS[i] += (DateTime.Now - st).Ticks / 10000L;
                    var st1 = DateTime.Now;
                    var resultString = q.Run(ts);
                    var totalMilliseconds = (DateTime.Now - st).Ticks/10000L;
                    totalrun[i] += (DateTime.Now - st1).Ticks / 10000L;

                    var memoryUsage = GC.GetTotalMemory(false);
                    if (memoryUsage > maxMemoryUsage)
                        maxMemoryUsage = memoryUsage;
                    if (minimums[i] > totalMilliseconds)
                        minimums[i] = totalMilliseconds;
                    if (maximums[i] < totalMilliseconds)
                        maximums[i] = totalMilliseconds;
                    results[i++] += totalMilliseconds;
                    //  File.WriteAllText(Path.ChangeExtension(file.FullName, ".txt"), resultString);
                    //.Save(Path.ChangeExtension(file.FullName,".xml"));
                }
            }

            using (StreamWriter r = new StreamWriter(@"..\..\output.txt", true))
            {
                r.WriteLine("milions " + Millions);
                r.WriteLine("max memory usage " + maxMemoryUsage);
                r.WriteLine("average " +     string.Join(", ", results.Select(l => l == 0 ? "inf" : (500*1000/l).ToString())));
                r.WriteLine("minimums " + string.Join(", ", minimums));
                r.WriteLine("minimums " + string.Join(", ", minimums));
                r.WriteLine("maximums " + string.Join(", ", maximums));
                r.WriteLine("total parse " + string.Join(", ", totalparseMS));
                r.WriteLine("total run " + string.Join(", ", totalrun));
                r.WriteLine("countCodingUsages {0} totalMillisecondsCodingUsages {1}", TripleInt.CodeCache.Count, TripleInt.totalMilisecondsCodingUsages);
                r.WriteLine("EWT count" + EntitiesMemoryHashTable.count);
                r.WriteLine("EWT total search" + EntitiesMemoryHashTable.total);
                r.WriteLine("EWT max search" + EntitiesMemoryHashTable.max);
                r.WriteLine("EWT total range" + EntitiesMemoryHashTable.totalRange);
                r.WriteLine("EWT max range" + EntitiesMemoryHashTable.maxRange);
                r.WriteLine("EWT average search" + EntitiesMemoryHashTable.total / EntitiesMemoryHashTable.count);
                r.WriteLine("EWT average range" + EntitiesMemoryHashTable.totalRange / EntitiesMemoryHashTable.count);
                TripleInt.CodeCache.Clear();
                TripleInt.totalMilisecondsCodingUsages = 0;
            }
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
            for (int j = 0; j < 0; j++)
            {

                foreach (var file in fileInfos)
                {
                    var readAllText = File.ReadAllText(file.FullName);
                    
                 //   var st = DateTime.Now;
                    var q = Parse(readAllText);


                    var resultString = q.Run(ts);
                    //var totalMilliseconds = (long)(DateTime.Now - st).TotalMilliseconds;
                    // results[i++] += totalMilliseconds;
                    File.WriteAllText(Path.ChangeExtension(file.FullName, ".txt"), resultString);
                    //.Save(Path.ChangeExtension(file.FullName,".xml"));
                }
            }
            for (int j = 0; j < 1; j++)
            {
                i = 0;
                foreach (var file in fileInfos)
                {
                    var readAllText = File.ReadAllText(file.FullName);
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
            using (StreamWriter r = new StreamWriter(@"..\..\output.txt", true))
            {
                r.WriteLine("milions " + Millions);          
                r.WriteLine("countCodingUsages {0} totalMillisecondsCodingUsages {1}", TripleInt.CodeCache.Count, TripleInt.totalMilisecondsCodingUsages);
            }

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

        private static void QueryWriteParameters(string parameteredQuery, StreamWriter output, TripleStoreInt ts)
        {         
            var productsCodes = ts.GetSubjectByObjPred(
                TripleInt.Code("http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/Product"),
                TripleInt.Code("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
            var codes = productsCodes as int[] ?? productsCodes.ToArray();
            int productCount = codes.Count();
            var product = TripleInt.Decode(codes.ElementAt(random.Next(0, productCount)));
                //Millions == 1000 ? 2855260 : Millions == 100 ? 284826 : Millions == 10 ? 284826 : 2785;
            int productFeatureCount =
                Millions == 1000 ? 478840 : Millions == 100 ? 47884 : Millions == 10 ? 47450 : 4745;
             int productTypesCount =Millions == 1000 ? 20110 : Millions == 100 ? 2011 : Millions == 10 ? 1510 : 151;  
            //var review = random.Next(1, productCount*10);
            ////var product = random.Next(1, productCount);
            ////var productProducer = product/ProductsPerProducer + 1; 
            //var offer = random.Next(1, productCount*OffersPerProduct);
            //var vendor = offer/OffersPerVendor + 1;
             var offersCodes = ts.GetSubjectByObjPred(
               TripleInt.Code("http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/Offer"),
               TripleInt.Code("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
             codes = offersCodes as int[] ?? offersCodes.ToArray();
            var offer = TripleInt.Decode(codes[random.Next(0, codes.Length)]);
            var reviewsCodes = ts.GetSubjectByObjPred(
           TripleInt.Code("http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/Review"),
           TripleInt.Code("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
            codes = reviewsCodes as int[] ?? reviewsCodes.ToArray();
            var review = TripleInt.Decode(codes[random.Next(0, codes.Length)]);
            if (parameteredQuery.Contains("%ProductType%"))
                output.WriteLine("bsbm-inst:ProductType" + random.Next(1, productTypesCount));
            if (parameteredQuery.Contains("%ProductFeature1%"))
                output.WriteLine("bsbm-inst:ProductFeature" + random.Next(1, productFeatureCount));
            if (parameteredQuery.Contains("%ProductFeature2%"))
                output.WriteLine("bsbm-inst:ProductFeature" + random.Next(1, productFeatureCount));
            if (parameteredQuery.Contains("%ProductFeature3%"))
                output.WriteLine("bsbm-inst:ProductFeature" + random.Next(1, productFeatureCount));
            if (parameteredQuery.Contains("%x%")) output.WriteLine(random.Next(1, 500).ToString());
            if (parameteredQuery.Contains("%y%")) output.WriteLine(random.Next(1, 500).ToString());
            if (parameteredQuery.Contains("%ProductXYZ%"))
                output.WriteLine(product);//"<http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer{0}/Product{1}>",productProducer, product);
            if (parameteredQuery.Contains("%word1%")) output.WriteLine(words[random.Next(0, words.Length)]);
            if (parameteredQuery.Contains("%currentDate%"))
                output.WriteLine("\"" + DateTime.Today.AddYears(-6) + "\"^^<http://www.w3.org/2001/XMLSchema#dateTime>");
            if (parameteredQuery.Contains("%ReviewXYZ%"))
                output.WriteLine(review);//"<http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromRatingSite{0}/Review{1}>",review/10000 + 1, review);
            if (parameteredQuery.Contains("%OfferXYZ%"))
                output.WriteLine(offer);
                    //"<http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromVendor{0}/Offer{1}>", vendor, offer);
        }

        private static string QueryReadParameters(string parameteredQuery, StreamReader input)
        {
            if(parameteredQuery.Contains("%ProductType%"))
                parameteredQuery = parameteredQuery.Replace("%ProductType%", input.ReadLine());
            if (parameteredQuery.Contains("%ProductFeature1%"))
                parameteredQuery = parameteredQuery.Replace("%ProductFeature1%", input.ReadLine());
            if (parameteredQuery.Contains("%ProductFeature2%"))
                parameteredQuery = parameteredQuery.Replace("%ProductFeature2%", input.ReadLine());
            if (parameteredQuery.Contains("%ProductFeature3%"))
                parameteredQuery = parameteredQuery.Replace("%ProductFeature3%", input.ReadLine());
            if (parameteredQuery.Contains("%x%"))
                parameteredQuery = parameteredQuery.Replace("%x%", input.ReadLine());
            if (parameteredQuery.Contains("%y%"))
                parameteredQuery = parameteredQuery.Replace("%y%", input.ReadLine());
            if (parameteredQuery.Contains("%ProductXYZ%"))
                parameteredQuery = parameteredQuery.Replace("%ProductXYZ%", "<" + input.ReadLine()+">");
            if (parameteredQuery.Contains("%word1%"))
                parameteredQuery = parameteredQuery.Replace("%word1%", input.ReadLine());
            if (parameteredQuery.Contains("%currentDate%"))
                parameteredQuery = parameteredQuery.Replace("%currentDate%", input.ReadLine());
            if (parameteredQuery.Contains("%ReviewXYZ%"))
                parameteredQuery = parameteredQuery.Replace("%ReviewXYZ%", "<"+input.ReadLine()+">");
            if (parameteredQuery.Contains("%OfferXYZ%"))
                parameteredQuery = parameteredQuery.Replace("%OfferXYZ%", "<"+input.ReadLine()+">");    
            return parameteredQuery;
        }
        private static void TestParametersValuesCopies()
        {
            var paramvaluesFilePath = string.Format(@"..\..\sparql data\queries\parameters\param values for{0} m.txt", Millions);
            HashSet<String> parameteersValues = new HashSet<string>();
            int copies = 0;
            using (StreamReader streamQueryParameters = new StreamReader(paramvaluesFilePath))
            {
                while (!streamQueryParameters.EndOfStream)
                {
                    var value = streamQueryParameters.ReadLine();
                    if (parameteersValues.Contains(value)) copies++;
                    else parameteersValues.Add(value);
                }
            }
            Console.WriteLine(parameteersValues.Count);
            Console.WriteLine(copies);
        }
        private static void TestCoding()
        {
            var paramvaluesFilePath = string.Format(@"..\..\sparql data\queries\parameters\param values for{0} m.txt", Millions);
            TripleStoreInt ts = new TripleStoreInt(@"C:\Users\Admin\Source\Repos\PolarDemo\Databases\" + Millions + @"mln\");
            int copies = 0;
            long max=0;
            long total=0;
            int i = 0, imax=0;
            using (StreamReader streamQueryParameters = new StreamReader(paramvaluesFilePath))
            {
                while (!streamQueryParameters.EndOfStream)
                {
                    var value = streamQueryParameters.ReadLine();
                    var st = DateTime.Now;
                    var coede = TripleInt.SiCoding.GetCode(value);
                    long timeSpan = (DateTime.Now-st).Ticks/10000;
                    total += timeSpan;
                    if (timeSpan > max)
                    {
                        max = timeSpan;
                        imax = i;
                    }
                    i++;
                }
            }
            Console.WriteLine(max);
            Console.WriteLine(imax);
            Console.WriteLine(total);
            Console.WriteLine(i);
        }
    }
}
