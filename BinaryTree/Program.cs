using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;

namespace BinaryTree
{
    public class BTree
    {
        // Тип
        // BTree<T> = empty^none,
        //            pair^{element: T, less: BTree<T>, more: BTree<T>};
        PTypeUnion tp_btree;
        PType tp_element;
        internal static readonly object[] Empty;

        public BTree()
        {
            // Тип элемента, как и в проекте Sorting: 
            tp_element = new PTypeRecord(
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("id", new PType(PTypeEnumeration.sstring)));
            // Заполнение типа дерева. Тип рекурсивный, поэтому это делается в два выражения

            tp_btree = PTypeTree(tp_element);
        }

        static BTree()
        {
            Empty = new object[] {0, null};
        }

        private static PTypeUnion PTypeTree(PType tpElement)
        { 
           var tpBtree = new PTypeUnion();
            tpBtree.Variants = new[]
            {
                new NamedType("empty", new PType(PTypeEnumeration.none)),
                new NamedType("pair", new PTypeRecord(
                    new NamedType("element", tpElement),
                    new NamedType("less", tpBtree),
                    new NamedType("more", tpBtree),
                    //1 - слева больше, -1 - справа больше.
                    new NamedType("balance", new PType(PTypeEnumeration.integer))))
            };
            return tpBtree;
        }

        public static void Main(string[] args)
        {
            DateTime tt0 = DateTime.Now;

            string path = @"..\..\..\Databases\";
                         //"C:\home\FactographDatabases"
            Console.WriteLine("Start.");
            
            // Инициируем типы
            BTree tree = new BTree();
            // Создадим фиксированную ячейку
            //if (File.Exists(path + "btree.pxc")) File.Delete(path + "btree.pxc");
            PxCell cell = new PxCell(tree.tp_btree, path + "btree.pxc", false);
            cell.Clear();
        
            //// Проверим существует ли пустое значение
            var r1 = cell.Root.Get();
            Console.WriteLine(r1.Type.Interpret(r1.Value));

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
                            Empty,
                            Empty, 0
                        }
                    },
                    Empty, 1
                }
            };
            cell.Fill2(valu);
            cell.Root.UElementUnchecked(1).Field(0).Set(new object[] { "", "" });
            
            // проверяем содержимое
            var res = cell.Root.Get();
            Console.WriteLine(res.Type.Interpret(res.Value));

            // Пробно добавим пару элементов через метод расширения, описанный в ExtensionMethods
            cell.Clear();
            Func<object, PxEntry, int> edepth = (object v1, PxEntry en2) =>
                {
                    string s1 = (string)(((object[])v1)[0]);
                    return String.Compare(s1, (string)(en2.Field(0).Get().Value), StringComparison.Ordinal);
                };
            cell.Root.Add(new object[] { "bbb_name", "333L" }, edepth);
            cell.Root.Add(new object[] { "bbc_name", "444L" }, edepth);
            cell.Root.Add(new object[] { "bbd_name", "555L" }, edepth);
            cell.Root.Add(new object[] { "bbe_name", "666L" }, edepth);
            // Получается 444(333(), 555(, 666()))
            var res2 = cell.Root.Get();
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
            cell.Root.UElement().Field(1).UElement().Field(2).Set(Empty);
            
            var res3 = cell.Root.Get();
           Console.WriteLine(res3.Type.Interpret(res3.Value));
           return;

            // Теперь попробуем загрузить реальные данные
            tt0 = DateTime.Now;
            XElement db = XElement.Load(path + "0001.xml");
            XName rdfabout = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about";
            var query = db.Elements()
                .Where(el => el.Attribute(rdfabout) != null)
                .SelectMany(el => el.Elements())
                .Where(prop => prop.Name.LocalName == "name")
                .Select(prop => new { name = prop.Value, id = prop.Parent.Attribute(rdfabout).Value });
            // Замерим время выборки данных из XML
            Console.WriteLine(query.Count());
            Console.WriteLine("======Count() ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            // Еще раз
            Console.WriteLine(query.Count());
            Console.WriteLine("======Count() ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            cell.Clear();

            ExtensionMethods.counter = 0;
            var special_array = query.OrderBy(pair => pair.name)
                .Select(oe => oe)
                .ToArray();


            foreach (int ind in IndSeq(0, 8))
            {
                Console.Write("{0} ", ind);
            }
            return;
            int count = 0; // переменная для ослеживания ввода

            // Загрузка бинарного дерева с помощью сортировки и инверсного индекса            
            //cell.Clear();
            //var special_array = query.OrderBy(pair => pair.name)
            //    .Select(oe => oe)
            //    .ToArray();
            //int len = special_array.Length;
            //ExtensionMethods.counter = 0;
            //foreach (int ind in IndSeq(0, len))
            //{
            //    cell.Root.Add(new object[] { special_array[ind].name, special_array[ind].id }, edepth);
            //    if (count % 1000 == 0)
            //    {
            //        Console.WriteLine("{0} {1}", count, ExtensionMethods.counter);
            //        ExtensionMethods.counter = 0;
            //    }
            //    count++;
            //}
            //Console.WriteLine("======BinaryTree ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
           // // У меня дома получилось 17 сек. для пары {string, long} и 22 сек. для {string, string}



           // Загрузим фрагмент бинарного дерева
            cell.Clear();
            count = 0;
            ExtensionMethods.counter = 0;
            foreach (var pair in query.Take(400000).Reverse())
            {
                //if (pair.name == "Марчук Александр Гурьевич") { }
                //if (pair.name == "Покрышкин Александр Иванович") { }
                cell.Root.Add(new object[] { pair.name, "555L" }, edepth);

                if (count % 1000 == 0)
                {
                    Console.WriteLine("{0} {1}", count, ExtensionMethods.counter);
                    ExtensionMethods.counter = 0;
                }
                count++;
            }
          //  Console.WriteLine(cell.Root.Get().Type.Interpret(cell.Root.Get().Value));
            Console.WriteLine("======part of BinaryTree ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Теперь загрузим все данные, но для этого надо будет их отсортировать и подавать в специальном режиме
        
            // Еще один способ построения бинарного дерева: Сначалы мы формируем объект, потом его вводим стандартным Fill2
            var array_of_elements = query.OrderBy(pair => pair.name)
                .Select(oe => new object[] {oe.name, oe.id})
                .ToArray();
          //  object bt = BuildBinaryTreeObjectFromSortedSequence(array_of_elements, 0, array_of_elements.Length);
            //Console.WriteLine(tree.tp_btree.Interpret(bt));
           // cell.Clear();
         //   cell.Fill2(bt);
            Console.WriteLine("======Binary Tree Build ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;


            // Бинарный поиск на бинарном дереве
            string name = "Марчук Александр Гурьевич";
            TestSearch(cell, name);
            Console.WriteLine("======Binary Search ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Под конец, добави еще одну пару и посмотрим появилась ли она
            TestSearch(cell, "Покрышкин Александр Иванович");
            cell.Root.Add(new object[] { "Покрышкин Александр Иванович", "pokryshkin_ai" }, edepth);
            TestSearch(cell, "Покрышкин Александр Иванович");
            Console.WriteLine("======Total ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            cell.Close();
            File.Delete(path + "btree.pxc");
          //  GetOverflow(path);
        }

        private static void GetOverflow(string path)
        {
            var overflowCell = new PxCell(BTree.PTypeTree(new PType(PTypeEnumeration.longinteger)),
                path + "overflowFile", false);
            long c = 0;
            while (true)
            {
                if (c++%1000000 == 0)
                    Console.WriteLine(c);
                overflowCell.Root.Add(c,
                    (o, entry) =>
                    {
                        long l = (long) o - (long) entry.Get().Value;
                        if (l < Int32.MinValue)
                            return Int32.MinValue;
                        if (l > Int32.MaxValue) return Int32.MaxValue;
                        return Convert.ToInt32(l);
                    });
            }
        }

        // Построение объекта дерева бинарного поиска
        private static object BuildBinaryTreeObjectFromSortedSequence(object[] arr, int beg, int count)
        {
            int half = count / 2;
            if (half == 0)
            {
                return count == 1 ? 
                    new object[] { 1, new[] { arr[beg], new object[] { 0, null }, new object[] { 0, null } } } : 
                    new object[] { 0, null };
            }
            else
            {
                return new object[] { 1, new[] {
                    arr[beg + half],
                    BuildBinaryTreeObjectFromSortedSequence(arr, beg, half),
                    BuildBinaryTreeObjectFromSortedSequence(arr, beg + half + 1, count - half - 1)
                }};
            }

        }

        private static void TestSearch(PxCell cell, string name)
        {
            PxEntry found = cell.Root.BinarySearchInBT((PxEntry pe) =>
            {
                string s = (string)pe.Field(0).Get().Value;
                return String.Compare(name, s, StringComparison.Ordinal);
            });
            if (found.offset == long.MinValue) Console.WriteLine("Имя {0} не найдено", name);
            else
            {
                var res3 = found.Get();
                Console.WriteLine(res3.Type.Interpret(res3.Value));
            }
        }
        public static IEnumerable<int> IndSeq(int beg, int count)
        {
            int half = count / 2;
            if (half == 0)
            { // выдать индекс если count == 1
                if (count == 1) yield return beg;
            }
            else
            {
                // Сам индекс
                yield return beg + half;
                // Все индексы до
                foreach (var i in IndSeq(beg, half)) yield return i;
                // Все индексы после
                foreach (var i in IndSeq(beg + half + 1, count - half - 1)) yield return i;
            }
        }
    }
}
