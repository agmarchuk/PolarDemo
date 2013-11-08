using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
//using Microsoft.Office.Interop.Excel;
using PolarDB;

namespace BinaryTree
{
    public class Programm
    {

        public static void Main(string[] args)
        {
            DateTime tt0 = DateTime.Now;

            string path = @"..\..\..\Databases\";
            //"C:\home\FactographDatabases"
            Console.WriteLine("Start.");

            Func<object, PxEntry, int> edepth = (object v1, PxEntry en2) =>
            {
                string s1 = (string)(((object[])v1)[0]);
                return String.Compare(s1, (string)(en2.Field(0).Get()), StringComparison.Ordinal);
            };
            // Инициируем типы
            // Создадим фиксированную ячейку
            PTypeRecord ptElement = new PTypeRecord(
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("id", new PType(PTypeEnumeration.sstring)));
            BTree cell = new BTree(ptElement,
                edepth,
                 path + "btree.pxc", readOnly: false);
            cell.Clear();

            //// Проверим существует ли пустое значение

            // Console.WriteLine(r1.Type.Interpret(r1.Value));

            // сделаем пробное заполнение вручную
            object[] valu =
            {
                1, new object[]
                {
                    new object[] {"name1", "333L"},
                    new object[]
                    {
                        1, new object[]
                        {
                            new object[] {"name0", "444L"},
                           BTree. Empty,
                            BTree.Empty, 0
                        }
                    },
                    BTree.Empty, 1
                }
            };
            cell.Fill2(valu);
            cell.Root.UElementUnchecked(1).Field(0).Set(new object[] { "", "" });

            // проверяем содержимое
            var res = cell.Root.GetValue();
            //  Console.WriteLine(res.Type.Interpret(res.Value));


            // Пробно добавим пару элементов через метод расширения, описанный в ExtensionMethods
            cell.Clear();

            cell.Add(new object[] { "1", "333L" });
            cell.Add(new object[] { "2", "444L" });
            cell.Add(new object[] { "3", "555L" });
            cell.Add(new object[] { "4", "666L" });
            // Получается 444(333(), 555(, 666()))
            var res2 = cell.Root.GetValue();
            Console.WriteLine(res2.Type.Interpret(res2.Value));
            Console.WriteLine();
            // Повернем дерево, чтобы стало 555(444(333(),), 666())
            // Для этого, сначала выделим корневой узел 444 и узел 555
            var h444 = cell.Root.GetHead();
            var h555 = cell.Root.UElement().Field(2).GetHead();
            // Теперь 555 запишем в корень
            cell.Root.SetHead(h555);
            // Левое поддерево у нас пустое, поэтому просто перепишем этот вход
            cell.Root.UElement().Field(1).SetHead(h444);
            // а в h444 заменим правое поддерево на пустое
            cell.Root.UElement().Field(1).UElement().Field(2).Set(BTree.Empty);

            var res3 = cell.Root.GetValue();
            Console.WriteLine(res3.Type.Interpret(res3.Value));

            // Теперь попробуем загрузить реальные данные
            tt0 = DateTime.Now;
            XElement db = XElement.Load(path + "0001.xml");
            XName rdfabout = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about";
            var query = db.Elements()
                .Where(el => el.Attribute(rdfabout) != null)
                .SelectMany(el => el.Elements())
                .Where(prop => prop.Name.LocalName == "name")
                .Select(prop => new[] {(object) prop.Value, (object) prop.Parent.Attribute(rdfabout).Value})
                .ToArray();
            
            // Замерим время выборки данных из XML
            Console.WriteLine(query.Count());
            Console.WriteLine("======Count() ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

   //         var addTree = TestQueryInput(query, ptElement, edepth, path);
         
     //     BTree toBTree =TestToBTree(query, ptElement, path, edepth);
         
       //    var treeFromFill = TestBTreeFill(query, ptElement, path, edepth);
            Func<object, object, bool> elementsComparer = (o1, o2)=>(string)(((object[])o1)[0])==(string)((object[])o2)[0];
        //    Console.WriteLine("tree sequantialy add == tree fill - " + treeFromFill.Equals(addTree, elementsComparer));
         //   Console.WriteLine("tree sequantialy add == query to BTree  - " + toBTree.Equals(addTree, elementsComparer));


        //   treeFromFill.Close();
         //   addTree.Close();
         //   toBTree.Close();
        //    Console.WriteLine("======Total ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            cell.Close();
            File.Delete(path + "btree.pxc");
            //  GetOverflow(path);

        //    TestTreeOfInt(query, path);
            SimpleTreeInt(path);
        }

        private static BTree TestToBTree(IEnumerable<object[]> query, PTypeRecord ptElement, string path,
            Func<object, PxEntry, int> edepth)
        {
            var tt0 = DateTime.Now;
            var treeFromQuery = query.ToBTree(ptElement, path + "TreeFromQuery.pxc", edepth, o => ((object[]) o)[0], false);
            Console.WriteLine("tree from query createtd,duration={0}", (DateTime.Now - tt0).Ticks/10000L);
            tt0 = DateTime.Now;

            // Иcпытание на "предельные" характеристики по скорости ввода данных. Данные сортируются, а потом выстраивается в
            // оперативной памяти структурный объект, соответствующий синтаксису и семантике введенного бинарного дерева.
            // Потом объект вводится в ячейку и испытывается.
            // На моем домашнем компьютере - 130 мс.
            TestSearch(treeFromQuery, "Марчук Александр Гурьевич");
            Console.WriteLine("======TestSearch ok. duration=" + (DateTime.Now - tt0).Ticks/10000L);
            Console.WriteLine();
            return treeFromQuery;
        }

        private static BTree TestBTreeFill(IEnumerable<object[]> query, PTypeRecord ptElement, string path,
            Func<object, PxEntry, int> edepth)
        {
            PxCell elementsCell = new PxCell(new PTypeSequence(ptElement), path + "elements", false);
            elementsCell.Fill2(query);
            var tt0 = DateTime.Now;

            var treeFromQuery = new BTree(ptElement, edepth, path + "TreeFromEntree.pxc", readOnly: false);
            treeFromQuery.Fill(elementsCell.Root, o => ((object[]) o)[0], false);
            Console.WriteLine("tree fill reading entry createtd,duration={0}", (DateTime.Now - tt0).Ticks/10000L);
            tt0 = DateTime.Now;
            // Иcпытание на "предельные" характеристики по скорости ввода данных. Данные сортируются, а потом выстраивается в
            // оперативной памяти структурный объект, соответствующий синтаксису и семантике введенного бинарного дерева.
            // Потом объект вводится в ячейку и испытывается.
            // На моем домашнем компьютере - 130 мс.
            TestSearch(treeFromQuery, "Марчук Александр Гурьевич");
            Console.WriteLine("======TestSearch ok. duration=" + (DateTime.Now - tt0).Ticks/10000L);
            Console.WriteLine();
            elementsCell.Close();
            return treeFromQuery;
        }

        private static BTree TestQueryInput(IEnumerable<object[]> query, PType ptElement, Func<object, PxEntry, int> edepth, string path)
        {
            BTree cell =new BTree(ptElement, edepth, path+"add.pxc", readOnly: false);
            var tt0 = DateTime.Now;
            int count = 0;
           BTree.counter = 0;
            foreach (var pair in query)
            {
                //if (pair.name == "Марчук Александр Гурьевич") { }
                //if (pair.name == "Покрышкин Александр Иванович") { }
                cell.Add(pair);

                //if (count%1000 == 0)
                //{
                //    Console.WriteLine("c={0} BTree.counter={1}", count, BTree.counter);
                //    BTree.counter = 0;
                //}
                //count++;
            }
           Console.WriteLine("======part of BinaryTree ok. duration=" + (DateTime.Now - tt0).Ticks/10000L);
            tt0 = DateTime.Now;
            
            // Бинарный поиск на бинарном дереве
            TestSearch(cell, "Марчук Александр Гурьевич");
            Console.WriteLine("======Binary Search ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;


            // Под конец, добави еще одну пару и посмотрим появилась ли она
            TestSearch(cell, "Покрышкин Александр Иванович");
            cell.Add(new object[] { "Покрышкин Александр Иванович", "pokryshkin_ai" });
            TestSearch(cell, "Покрышкин Александр Иванович");
            Console.WriteLine("======Binary Search ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L);
            Console.WriteLine();
            return cell;
        }

        private static void TestTreeOfInt(IEnumerable<object[]> query, string path)
        {
            PType treePType = new PTypeRecord(new NamedType("id_hash", new PType(PTypeEnumeration.integer)),
                new NamedType("indexes", new PTypeSequence(new PType(PTypeEnumeration.longinteger))));
            long sample = 0;
            var tt0 = DateTime.Now;
            
            var tree = query.Select(q => (string) q[0])
                .GroupBy(q => q.GetHashCode())
                .Select(g => new object[] {g.Key, g.Select(q => sample as object).ToArray()})
                .ToBTree(treePType, path + "treeOfInt",
                    (o, entry) => (int) ((object[]) o)[0] - (int) entry.Field(0).Get(),
                    o => (int) ((object[]) o)[0], false);
            Console.WriteLine("create tree of int long pairs ok " + (DateTime.Now - tt0).TotalMilliseconds);
            var testIdHash = query.First()[0].GetHashCode();//"w20070417_5_8436".GetHashCode();
            tt0 = DateTime.Now;
            var finded = tree.BinarySearch(entry => testIdHash - (int) entry.Field(0).Get() );
            Console.WriteLine(finded.Field(0).Get() + " finded, duration=" + (DateTime.Now - tt0).TotalMilliseconds+"ms");
            Console.WriteLine();
            tree.Close();
        }

        static void SimpleTreeInt(string path)
        {
            int pointsCount = 2, pointsDistance=1000000;
            int[][] results = { new int[pointsCount], new int[pointsCount], new int[pointsCount] };
            for (int i = 1, j=0; j < pointsCount; i+=pointsDistance, j++)
            {
                if (File.Exists(path + "simple int.pac")) File.Delete(path + "simple int.pac");
                if (File.Exists(path + "simple int tree.pxc")) File.Delete(path + "simple int tree.pxc");
                if (File.Exists(path + "simple int tree add.pxc")) File.Delete(path + "simple int tree add.pxc");

                Thread.Sleep(1);
                var objects = Enumerable.Range(0, i).Cast<object>().ToArray();
                Stopwatch timer = new Stopwatch();
                timer.Start();
                var simpleIntCell = objects.ToBTree(new PType(PTypeEnumeration.integer), path + "simple int tree.pxc",
                    (o, entry) => (int) o - (int) entry.Get(), o => o, false);
                timer.Stop();
                results[0][j] = (int)timer.Elapsed.Ticks;
                Console.WriteLine("simple int tree "+i+"elements created for (ms)" + timer.Elapsed.TotalMilliseconds);
                timer.Restart();
                PaCell paCell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.integer)),
                    path + "simple int.pac", false);
                paCell.Fill(objects);
                timer.Stop();
                results[1][j] = (int)timer.Elapsed.Ticks;
                Console.WriteLine("simple int pa " + i + "elements created for (ms)" + timer.Elapsed.TotalMilliseconds);

               // линейное возрастание времени
                AddTreeAddChart(path, timer, objects, results, j);
                Console.WriteLine();
                TestGetByKey(simpleIntCell, i, paCell);
                Console.WriteLine();
                paCell.Close();
                simpleIntCell.Close();
            }
        }

        private static void AddTreeAddChart(string path, Stopwatch timer, object[] objects, int[][] results, int j)
        {
            timer.Restart();
            var cell = new BTree(new PType(PTypeEnumeration.integer),
                (o, entry) => (int) o - (int) entry.Get(), path + "simple int tree add.pxc", false);
            foreach (var pair in objects)
                cell.Add(pair);
            timer.Stop();
            Console.WriteLine("simple int tree " + objects.Length + "elements created by add per one for (ms)" + timer.Elapsed.TotalMilliseconds);

            results[2][j] = (int) timer.Elapsed.Ticks;
            cell.Close();
        }

        private static void TestGetByKey(BTree tree, int max, PaCell paCell)
        {
            Stopwatch timer=new Stopwatch();
            for (int i = 0; i < 3; i++)
            {
                if(i==0)
                    Console.WriteLine("first search");
                int tested = new Random(DateTime.Now.Millisecond).Next(max-1);
                timer.Restart();
                var res=tree.BinarySearch(entry => tested - (int)entry.Get());
                timer.Stop();
                Console.WriteLine("search in "+max+" elements TREE find "+tested+" for (ticks)"+timer.Elapsed.Ticks);
                timer.Restart();
                var res1 = paCell.Root.BinarySearchFirst(entry => (int)entry.Get() - tested);
                timer.Stop();
                Console.WriteLine("binary search in "+max+" elements PA CELL find " + tested + " for (ticks)" + timer.Elapsed.Ticks);
                Console.WriteLine();
            }
        }

        private static void GetOverflow(string path, Func<object, PxEntry, int> edapth)
        {
            var overflowCell = new BTree(new PType(PTypeEnumeration.longinteger), edapth, path + "overflowFile", false);
            long c = 0;
            while (true) 
            {
                if (c++ % 1000000 == 0)
                    Console.WriteLine(c);
                overflowCell.Add(c);
            }
        }
        /// <summary>
        /// Отображает грфик в EXEL, но не сохраняет его. 
        /// </summary>
        /// <param name="xy">корневой массив-линий, листовой точек. Точки должны отличться на одну постоянноую величину</param>
//        static void Draw(int[][] xy)
//        {
//            Application application = new Application(){Visible = true};
//            var workbooks = application.Workbooks;
//            var wordBook = workbooks.Open(new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.Parent.FullName+"/chart.xls");
//            var sheet = (_Worksheet) wordBook.ActiveSheet;
//var chart =(_Chart)wordBook.Charts.Add();
//            chart.Name = "sdfs";
//            Thread.CurrentThread.CurrentCulture=new CultureInfo("en-US");
//            sheet.ClearArrows();
//            for (int j = 0; j < xy.Length; j++)
//                for (int i = 0; i < xy[0].Length; i++)
//            {
//                {
//                    sheet.Cells[i + 1, j + 1] = xy[j][i].ToString(CultureInfo.InvariantCulture);
//                }
//            }

//            chart.ChartWizard(sheet.Range["A1", "G" + xy[0].Length], XlChartType.xlLine);
//            //System.Runtime.InteropServices.Marshal.ReleaseComObject(chart);
//            //System.Runtime.InteropServices.Marshal.ReleaseComObject(sheet);
//            //wordBook.Close(false);
//            //System.Runtime.InteropServices.Marshal.ReleaseComObject(wordBook);
//            //System.Runtime.InteropServices.Marshal.ReleaseComObject(workbooks);
//            //System.Runtime.InteropServices.Marshal.ReleaseComObject(application);
//        }
 
      

        private static void TestSearch(BTree cell, string name)
        {
            PxEntry found = cell.BinarySearch(pe =>
            {
                string s = (string)pe.Field(0).Get();
                return String.Compare(name, s, StringComparison.Ordinal);
            });
            if (found.offset == long.MinValue) Console.WriteLine("Имя {0} не найдено", name);
            else
            {
                var res3 = found.GetValue();
                Console.WriteLine(res3.Type.Interpret(res3.Value));
            }
        } 
        // Построение объекта дерева бинарного поиска
        //public static IEnumerable<int> IndSeq(int beg, int count)
        //{
        //    int half = count / 2;
        //    if (half == 0)
        //    { // выдать индекс если count == 1
        //        if (count == 1) yield return beg;
        //    }
        //    else
        //    {
        //        // Сам индекс
        //        yield return beg + half;
        //        // Все индексы до
        //        foreach (var i in IndSeq(beg, half)) yield return i;
        //        // Все индексы после
        //        foreach (var i in IndSeq(beg + half + 1, count - half - 1)) yield return i;
        //    }
        //}
    }
}