using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;

namespace TableWithIndex
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start");
            string path = @"..\..\..\Databases\";
            string[] ids = new[]
            {
                "svet_100616111408_10844",
                "pavl_100531115859_2020",
                "piu_200809051791",
                "pavl_100531115859_6952",
                "svet_100616111408_10864",
                "w20090506_svetlana_5727",
                "piu_200809051742",
                "p0013313",
                "p0011098",
                "svet_100616111408_14354"
            };
            XElement formats = XElement.Load(path + "ApplicationProfile.xml").Element("formats");
            bool toload = true;
            XElement db = null;
            IEnumerable<XElement> query = Enumerable.Empty<XElement>();
            if (toload)
            {
                //db = XElement.Load(@"D:\home\dev2012\tm3.xml");
                db = XElement.Load(@"..\..\..\Databases\0001.xml");
                query = db.Elements()
                    .Where(el => el.Attribute(ONames.rdfabout) != null && el.Element(ONames.tag_name) != null);
            }


            DateTime tt0 = DateTime.Now;

            string variant = "rdfengineflex";
            //string variant = "rdfengine";
            //string variant = "bigintset";
            //string variant = "freeindex";
            //string variant = "semiindex";
            if (variant == "sql")
            {
                var test = new SQLTest(@"Data Source=(LocalDB)\v11.0;AttachDbFilename=D:\home\FactographDatabases\test20131025.mdf;Integrated Security=True;Connect Timeout=30");
                test.PrepareToLoad();
                Console.WriteLine("PrepareToLoad ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                test.Load(query);
                Console.WriteLine("Load ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                test.Index1();
                Console.WriteLine("Index1 ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                
                
                test.SelectById("w20070417_5_8436");
                Console.WriteLine("Первый Select ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                foreach (string id in ids) test.SelectById(id);
                Console.WriteLine("SelectById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //test.SelectById("pavl_100531115859_6952");
                //Console.WriteLine("SelectById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                test.Count();
            }
            else if (variant == "polartest")
            {
                var test2 = new PolarTest(@"..\..\..\Databases\");
                //test2.Load(query);
                //Console.WriteLine("Load ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //test2.CreateIndex();
                //Console.WriteLine("Index ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                test2.SelectById("w20070417_5_8436");
                Console.WriteLine("Index ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                foreach (string id in ids) test2.SelectById(id);
                Console.WriteLine("SelectById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            else if (variant == "semiindex")
            {
                Console.WriteLine("SemiIndex Strat. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                SITest sit = new SITest(path);
                sit.Load(query);
                Console.WriteLine("Load ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                var rec = sit.GetById("w20070417_5_8436");
                Console.WriteLine("GetById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //Console.WriteLine(rec.Type.Interpret(rec.Value));
                foreach (string id in ids) sit.GetById(id);
                Console.WriteLine("10 GetById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                sit.Search("марчук");
                Console.WriteLine("Search ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            else if (variant == "freeindex")
            {
                Console.WriteLine("FreeIndex Start. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                FITest fit = new FITest(path);
                fit.Load(query);
                Console.WriteLine("Load ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                fit.CreateIndexes();
                Console.WriteLine("Indexes ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                var rec = fit.GetById("w20070417_5_8436");
                Console.WriteLine("GetById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //Console.WriteLine(rec.Type.Interpret(rec.Value));
                foreach (string id in ids) fit.GetById(id);
                Console.WriteLine("10 GetById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                foreach (PValue v in fit.Search("марчук"))
                {
                    Console.WriteLine(v.Type.Interpret(v.Value));
                }
                Console.WriteLine("Search ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            else if (variant == "bigintset")
            {
                Console.WriteLine("BigIntSet Start. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                PaCell iset_cell = new PaCell(new PTypeSequence(new PTypeRecord(
                    new NamedType("deleted", new PType(PTypeEnumeration.boolean)),
                    new NamedType("value", new PType(PTypeEnumeration.integer)))), path + "intset.pac", false);
                
                // Загрузка таблицы
                System.Random rnd = new Random();
                iset_cell.Clear();
                iset_cell.Fill(new object[0]);
                for (int i = 0; i < 1000000; i++)
                {
                    iset_cell.Root.AppendElement(new object[] { false, rnd.Next() });
                }
                int special = 7777777; // Это для поиска
                for (int i = 0; i < 11; i++) iset_cell.Root.AppendElement(new object[] { false, special + i * 10000000 });
                iset_cell.Flush();
                Console.WriteLine("Load ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                // подключение индекса
                FreeIndex iset_index = new FreeIndex(path + "intset_index", iset_cell.Root, 1);

                // Создание индекса
                iset_index.Load();
                Console.WriteLine("Index ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                // Поиск
                int sample = 7777777;
                Find(iset_index, sample);
                Console.WriteLine("Search ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                for (int i = 1; i < 11; i++) Find( iset_index, sample + i * 10000000 );
                Console.WriteLine("10 Search ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            else if (variant == "rdfengine")
            {
                Console.WriteLine("RdfEngine start");
                PolarBasedEngineSpecial engine = new PolarBasedEngineSpecial(path);
                
                //toload = false;
                if (toload)
                {
                    engine.Load(db);
                    Console.WriteLine("Load ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                    engine.MakeIndexes();
                    Console.WriteLine("Indexes ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                }
                tt0 = DateTime.Now;
                var val = engine.GetById("w20070417_5_8436");
                Console.WriteLine(val.Type.Interpret(val.Value));
                Console.WriteLine("GetById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                foreach (string id in ids) engine.GetById(id);
                Console.WriteLine("10 GetById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                foreach (XElement res in engine.SearchByName("Марчук"))
                {
                    //Console.WriteLine(res.ToString());
                }
                Console.WriteLine("Search ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //engine.GetInverse("w20070417_5_8436");
                engine.GetById("w20070417_5_8436");
                Console.WriteLine("GetById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //var xres = engine.GetItemByIdBasic("w20070417_5_8436", true); // Это я
                var xres = engine.GetItemByIdBasic("w20071030_1_20927", true); // Это Андрей
                Console.WriteLine("GetItemByIdBasic ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                Console.WriteLine(xres.ToString());
                
                XElement format = formats.Elements("record").First(r => r.Attribute("type").Value == "http://fogid.net/o/person");
                xres = engine.GetItemById("w20070417_5_8436", format);
                Console.WriteLine("GetItemById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                Console.WriteLine(xres.Elements().Count());
                foreach (string id in ids) engine.GetItemById(id, format);
                Console.WriteLine("10 GetItemById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            else if (variant == "rdfengineflex")
            {
                Console.WriteLine("RdfEngineFlex start");
                tt0 = DateTime.Now;
                PolarBasedEngineFlex engine = new PolarBasedEngineFlex(path);

                toload = false;
                if (toload)
                {
                    engine.Load(db);
                    Console.WriteLine("Load ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                    engine.MakeIndexes();
                    Console.WriteLine("Indexes ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                }
                var val = engine.GetById("w20070417_5_8436");
                //Console.WriteLine(val.Type.Interpret(val.Value));
                Console.WriteLine("GetById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                foreach (string id in ids) engine.GetById(id);
                Console.WriteLine("10 GetById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                foreach (XElement res in engine.SearchByName("Марчук"))
                {
                    //Console.WriteLine(res.ToString());
                }
                Console.WriteLine("Search ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                //engine.GetInverse("w20070417_5_8436");
                //engine.GetInverse("w20071030_1_20927");
                //Console.WriteLine("GetInverse ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                //var xres = engine.GetItemByIdBasic("w20070417_5_8436", true); // Это я
                var xres = engine.GetItemByIdBasic("piu_200809051508", true); // Это Г.И.
                //var xres = engine.GetItemByIdBasic("w20071030_1_20927", true); // Это Андрей
                Console.WriteLine("GetItemByIdBasic ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //Console.WriteLine(xres.ToString());

                XElement format = formats.Elements("record").First(r => r.Attribute("type").Value == "http://fogid.net/o/person");
                tt0 = DateTime.Now;
                //xres = engine.GetItemById("w20070417_5_8436", format);
                xres = engine.GetItemById("piu_200809051508", format);
                Console.WriteLine("GetItemById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                Console.WriteLine(xres.ToString());
            }
        }

        private static void Find(FreeIndex iset_index, int sample)
        {
            var qu = iset_index.GetFirst(ent =>
            {
                int v = (int)ent.Get();
                return v.CompareTo(sample);
            });
            if (qu.offset == Int64.MinValue) Console.WriteLine("value not found");
            else
            {
                var val = qu.GetValue();
                Console.WriteLine(val.Type.Interpret(val.Value));
            }
        }
    }
}
