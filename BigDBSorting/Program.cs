using System;
using System.Diagnostics;
using System.Linq;
using BigDbTest;
using PolarDB;

namespace BigDBSorting
{
    class Program
    {
        // Проверка эффективности работы последовательностей фиксированного формата
        static void Main(string[] args)
        {
            string path = @"..\..\..\Databases\";
            DateTime tt0 = DateTime.Now;
            Random rnd = new Random(7777777);
            PaCell icell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.integer)), path + "icell.pac", false);
            PxCell xcell = new PxCell(new PTypeSequence(new PType(PTypeEnumeration.integer)), path + "xcell.pac", false);

            bool toload = false;
            int nvalues = 10000000;
            if (toload)
            {
                icell.Clear();
                icell.Fill(new object[0]);
                for (int i = 0; i < nvalues; i++)
                {
                    icell.Root.AppendElement(rnd.Next());
                }
                icell.Flush();
                Console.WriteLine("Load1 ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                PaEntry.bufferBytes = 200000000;
                icell.Root.SortByKey<int>(ob => (int)ob); ;
                Console.WriteLine("Sort ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            bool toload2 = false;
            if (toload2)
            {
                xcell.Clear();
                //xcell.Root.SetRepeat(nvalues);
                //for (int i = 0; i < nvalues; i++)
                //{
                //    xcell.Root.Element(i).Set(icell.Root.Element(i).Get());
                //}
                //xcell.Flush();
                //xcell.Fill(icell.Root.Get());
                xcell.Root.Set(icell.Root.Get());
                Console.WriteLine("Load2 ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                xcell.Flush();
            }

            //foreach (var v in icell.Root.ElementValues()) ;
            //var ooo = xcell.Root.Get();
            foreach (var xent in xcell.Root.Elements()) { var xxx = xent.Get(); }
            Console.WriteLine("WarmUp ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            
            rnd = new Random(7777777);
            int start = nvalues / 2;
            for (int i = 0; i < start; i++) { int r0 = rnd.Next(); }
            for (int i = start; i < start + 10000; i++)
            {
                int r = rnd.Next(nvalues - 1);
                int v = (int)icell.Root.Element(r).Get();
                //int v = (int)xcell.Root.Element(r).Get();
            }
            Console.WriteLine("ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            rnd = new Random(7777777);
            for (int i = 0; i < start; i++) { int r0 = rnd.Next(); } // Пропустили половину чисел
            int found = 0;
            for (int i = start; i < start + 10000; i++)
            {
                int r = rnd.Next();
                PaEntry entry = icell.Root.BinarySearchFirst(en => ((int)en.Get()).CompareTo(r));
                //PxEntry entry = xcell.Root.BinarySearchFirst(en => ((int)en.Get()).CompareTo(r));
                if (entry.IsEmpty) continue;
                int v = (int)entry.Get();
                //int v = (int)xcell.Root.Element(r).Get();
                found++;
            }
            Console.WriteLine("found {0} ok. duration={1}",found , (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
        // Проверка таблицы с индексами
        static void Main4(string[] args)
        {
            Console.WriteLine("Start");
            string path = @"..\..\..\Databases\";
            DateTime tt0 = DateTime.Now;
            Random rnd = new Random(3333);

            // Тип для таблицы
            PType tp_tab = new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.integer)),
                new NamedType("date", new PType(PTypeEnumeration.longinteger)),
                new NamedType("name", new PType(PTypeEnumeration.sstring))
                ));

            // Таблица
            PaCell table = new PaCell(tp_tab, path + "table.pac", false);
            // Индексы
            PaCell id_index = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), path + "id_index.pac", false);
            PaCell dt_index = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), path + "dt_index.pac", false);
            PaCell nm_index = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), path + "nm_index.pac", false);

            // Заполнение
            bool toload = false;
            int nvalues = 10000000;
            System.Collections.Generic.List<int> times = new System.Collections.Generic.List<int>();
            if (toload)
            {
                table.Clear();
                table.Fill(new object[0]);
                id_index.Clear();
                id_index.Fill(new object[0]);
                dt_index.Clear();
                dt_index.Fill(new object[0]);
                nm_index.Clear();
                nm_index.Fill(new object[0]);
                for (int i = 0; i < nvalues; i++)
                {
                    int r1 = rnd.Next();
                    int r2 = rnd.Next();
                    int r3 = rnd.Next();
                    if ((i + 1) % 100000 == 0)
                    {
                        Console.Write("{0} ", (i + 1) / 100000);
                    }
                    object[] record = new object[] { r1, (long)r2, "Иванов" + r3 };
                    long off = table.Root.AppendElement(record);
                    id_index.Root.AppendElement(off);
                    dt_index.Root.AppendElement(off);
                    nm_index.Root.AppendElement(off);
                }
                Console.WriteLine();
                table.Flush();
                id_index.Flush();
                dt_index.Flush();
                nm_index.Flush();
                Console.WriteLine("Load ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                PaEntry.bufferBytes = 200000000;

                var ptr = table.Root.Element(0);
                id_index.Root.SortByKey<int>((object v) =>
                {
                    ptr.offset = (long)v;
                    return (int)ptr.Field(0).Get();
                });
                Console.WriteLine("id ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                dt_index.Root.SortByKey<long>((object v) =>
                {
                    ptr.offset = (long)v;
                    return (long)ptr.Field(1).Get();
                });
                Console.WriteLine("date index ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                nm_index.Root.SortByKey<string>((object v) =>
                {
                    ptr.offset = (long)v;
                    return (string)ptr.Field(2).Get();
                });
                Console.WriteLine("Sort ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }

            // Бинарный поиск
            rnd = new Random(3333);
            for (int i = 0; i < nvalues; i++)
            {
                int r1 = rnd.Next();
                int r2 = rnd.Next();
                int r3 = rnd.Next();
                if ((i + 1) % 100000 == 0) times.Add(r2);
            }
            long min = 199900000, max = 200000000;

            var tab_entry = table.Root.Element(0);
            var query = dt_index.Root.BinarySearchAll(entry =>
            {
                tab_entry.offset = (long)entry.Get();
                long time = (long)tab_entry.Field(1).Get();
                return time < min ? -1 : (time > max ? 1 : 0);
                //return time.CompareTo((long)times[10]);
            });
            Console.WriteLine("Binary search found {0}. duration={1}",
                query.Count(), (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            int cnt = 0;
            foreach (var tm in times)
            {
                var query2 = dt_index.Root.BinarySearchAll(entry =>
                {
                    tab_entry.offset = (long)entry.Get();
                    long time = (long)tab_entry.Field(1).Get();
                    return time.CompareTo((long)tm);
                });
                cnt += query2.Count();
            }
            Console.WriteLine("For {0} binary searches: duration={1}",
                times.Count,
                (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

        }

        // Проверка работы с данными, существенно превышающими размер оперативной памяти
        static void Main3(string[] args)
        {
            Console.WriteLine("Start");
            //string path = @"..\..\..\Databases\"; //HDD
            string path = @"G:\Home\Databases\"; //SSD
            DateTime tt0 = DateTime.Now;
            Random rnd = new Random(3333);

            // Две ячейки: строковый столбец и индексный столбец
            PaCell scell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.sstring)), path + "scell.pac", false);
            PaCell icell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), path + "icell.pac", false);

            // Заполнение или разогрев
            bool toload = false;
            int nvalues = 100000000;
            if (toload)
            {
                scell.Clear();
                scell.Fill(new object[0]);
                icell.Clear();
                icell.Fill(new object[0]);
                for (int i = 0; i < nvalues; i++)
                {
                    if ((i + 1) % 100000 == 0) Console.Write("{0} ", (i + 1) / 100000);
                    // длина идентификатора более 100 байтов
                    long off = scell.Root.AppendElement("http://verylongdomain012345678901234567890123456789012345678901234567890123456789totestprefix.ru/qwerty/" + rnd.Next().ToString());
                    icell.Root.AppendElement(off);
                }
                Console.WriteLine();
                scell.Flush();
                icell.Flush();
                Console.WriteLine("Load ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            else
            {
                //foreach (var q in icell.Root.ElementValues()) ;
                //Console.WriteLine("Warming-up ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //foreach (var q in scell.Root.ElementValues()) ;
                //Console.WriteLine("Warming-up ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }

            // Случайные выборки. По rnd выбирается номер рядка, прочитывается offset из индекса, читается строка из scell
            int portion = 100000;
            for (int j = 0; j < 10; j++)
            {
                tt0 = DateTime.Now;
                PaEntry entry = scell.Root.Element(0);
                for (int i = 0; i < portion; i++)
                {
                    int ind = rnd.Next(nvalues - 1);
                    long off = (long)icell.Root.Element(ind).Get();
                    entry.offset = off;
                    string s = (string)entry.Get();
                }
                Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
        }

        // Проверка индексирования строкового столбца
        static void Main2(string[] args)
        {
            Console.WriteLine("Start");
            string path = @"..\..\..\Databases\";
            DateTime tt0 = DateTime.Now;
            Random rnd = new Random(3333);

            // Две ячейки: строковый столбец и индексный столбец
            PaCell scell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.sstring)), path + "scell.pac", false);
            PaCell icell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), path + "icell.pac", false);

            // Заполнение
            bool toload = false;
            int nvalues = 10000000;
            if (toload)
            {
                scell.Clear();
                scell.Fill(new object[0]);
                icell.Clear();
                icell.Fill(new object[0]);
                for (int i = 0; i < nvalues; i++)
                {
                    if ((i + 1) % 100000 == 0) Console.Write("{0} ", (i + 1) / 100000);
                    long off = scell.Root.AppendElement("http://verylongdomaintotestprefix.ru/qwerty/" + rnd.Next().ToString());
                    icell.Root.AppendElement(off);
                }
                Console.WriteLine();
                scell.Flush();
                icell.Flush();
                Console.WriteLine("Load ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                PaEntry.bufferBytes = 200000000;

                var ptr = scell.Root.Element(0);
                icell.Root.SortByKey<string>((object v) =>
                {
                    ptr.offset = (long)v;
                    return (string)ptr.Get();
                });
                Console.WriteLine("Sort ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            else
            {
                //foreach (var q in icell.Root.ElementValues()) ;
                //Console.WriteLine("Warming-up ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //foreach (var q in scell.Root.ElementValues()) ;
                //Console.WriteLine("Warming-up ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }

            rnd = new Random(3333);
            int[] rand_arr = new int[nvalues];
            for (int i = 0; i < nvalues; i++)
            {
                rand_arr[i] = rnd.Next();
            }

            // Проверка бинарного поиска
            rnd = new Random();
            tt0 = DateTime.Now;
            int ntests = 1000;
            int found = 0;
            for (int i = 0; i < ntests; i++)
            {
                string sample = "http://verylongdomaintotestprefix.ru/qwerty/" + rand_arr[rnd.Next(nvalues - 1)].ToString();
                PaEntry entity = scell.Root.Element(0);
                PaEntry qq = icell.Root.BinarySearchFirst(en =>
                    {
                        long off = (long)en.Get();
                        entity.offset = off;
                        //return sample.CompareTo((string)entity.Get());
                        return ((string)entity.Get()).CompareTo(sample);
                    });
                if (!qq.IsEmpty) found++;
                //if ((i + 1) % 100000 == 0) 
                //    Console.Write("{0} ", i + 1);
            }
            Console.WriteLine("Found {0}. Duration={1}", found, (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
        // Поверка "разогрева" WarmUp
        static void Main1(string[] args)
        {
            string path = @"..\..\..\Databases\";
            //string path = @"G:\Home\Databases\";
            DateTime tt0 = DateTime.Now;
            Random rnd = new Random();
            PaCell icell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.integer)), path + "icell.pac", false);

            bool toload = true;
            int nvalues = 100000000;
            if (toload)
            {
                icell.Clear();
                icell.Fill(new object[0]);
                for (int i = 0; i < nvalues; i++)
                {
                    icell.Root.AppendElement(rnd.Next());
                }
                icell.Flush();
                Console.WriteLine("Load2 ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                PaEntry.bufferBytes = 200000000;
                icell.Root.SortByKey<int>(ob => (int)ob); ;
                Console.WriteLine("Sort ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            
            //foreach (var v in icell.Root.ElementValues()) ;
            //Console.WriteLine("WarmUp ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            
            for (int i = 0; i < 1000; i++)
            {
                int r = rnd.Next(nvalues - 1);
                int v = (int)icell.Root.Element(r).Get();
            }
            Console.WriteLine("ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
        static void Main0(string[] args)
        {
            string path = @"..\..\..\Databases\";
            DateTime tt0 = DateTime.Now;
            BigPolar bp = new BigPolar(path);

            bool toload = true;
            if (toload)
            {
                bp.Load2(100000000);
                Console.WriteLine("Load2 ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                bp.IndexByKey();
                Console.WriteLine("IndexByKey ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }

            Console.WriteLine("N records: " + bp.Count());
            Random rnd = new Random();
            int[] arr1000 = Enumerable.Range(0, 1000).Select(i => rnd.Next()).ToArray();
            tt0 = DateTime.Now;
            Stopwatch timer=new Stopwatch(); 
            timer.Start();
            bp.Test4(new int[] { 100, 100000000, 10, 4000000, 108, 90000000, 50000000, 1, int.MaxValue / 16 - 1, 303 });
            timer.Stop();
            Console.WriteLine("Test5 ok. duration=" + timer.Elapsed.Ticks );
            Console.WriteLine("standard duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            // Разогрев
            int v = 0;
            foreach (var e in bp.Cell.Root.Elements()) { v = (int)e.Get(); }
            Console.WriteLine("Worming... standard duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            timer.Restart();
            //bp.Test4(new int[] { 99999999, 88888888, 77777777, 66666666, 55555555, 44444444, 33333333, 22222222, 11111111, 3 });
            //bp.Test4(new int[] { 2039530, 923720000, -921354321, 1445454545, -55555555, 828282828, 1140000000, -22222222, 1030303030, -777777777 });
            bp.Test4(arr1000);
            timer.Stop();
            Console.WriteLine("Test5 ok. duration=" + timer.Elapsed.Ticks);
            Console.WriteLine("standard duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //bp.TestSort(0, 10);
            
        }
    }
}
