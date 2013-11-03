using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using PolarDB;

namespace BinaryTree
{
   public class Programm{

        public static void Main(string[] args)
        {
            DateTime tt0 = DateTime.Now;

            string path = @"..\..\..\Databases\";
                         //"C:\home\FactographDatabases"
            Console.WriteLine("Start.");

            Func<object, PxEntry, int> edepth = (object v1, PxEntry en2) =>
            {
                string s1 = (string)(((object[])v1)[0]);
                return String.Compare(s1, (string)(en2.Field(0).Get().Value), StringComparison.Ordinal);
            };
            // Инициируем типы
            // Создадим фиксированную ячейку
            BTree cell = new  BTree(new PTypeRecord(
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("id", new PType(PTypeEnumeration.sstring))),
                edepth,
                 path + "btree.pxc", false);
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
            var res = cell.Root.Get();
          //  Console.WriteLine(res.Type.Interpret(res.Value));


            // Пробно добавим пару элементов через метод расширения, описанный в ExtensionMethods
            cell.Clear();

            cell.Add(new object[] { "1", "333L" });
            cell.Add(new object[] { "2", "444L" });
            cell.Add(new object[] { "3", "555L" });
            cell.Add(new object[] { "4", "666L" });
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
            cell.Root.UElement().Field(1).UElement().Field(2).Set(BTree.Empty);

            var res3 = cell.Root.Get();
            Console.WriteLine(res3.Type.Interpret(res3.Value));
            
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
            
            cell.Clear();

            int count = 0; // Счетчик, используемый в отслеживании тестирований

            // Иcпытание на "предельные" характеристики по скорости ввода данных. Данные сортируются, а потом выстраивается в
            // оперативной памяти структурный объект, соответствующий синтаксису и семантике введенного бинарного дерева.
            // Потом объект вводится в ячейку и испытывается.
            object[][] special_array = query.OrderBy(pair => pair.name)
                .Select(oe => new object[] { oe.name, oe.id })
                .ToArray();
            int len = special_array.Length;
            object[] special_value = ToTreeObject(special_array, 0, len);
            Console.WriteLine("======TreeObject ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            // На моем домашнем компьютере - 350 мс.
            cell.Root.Set(special_value);
            Console.WriteLine("======BinaryTree ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            // На моем домашнем компьютере - 130 мс.
            TestSearch(cell, "Марчук Александр Гурьевич");
            Console.WriteLine("======TestSearch ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;


           // Основное испытание
            cell.Clear();
            count = 0;
            BTree.counter = 0;
            foreach (var pair in query)
            {
                //if (pair.name == "Марчук Александр Гурьевич") { }
                //if (pair.name == "Покрышкин Александр Иванович") { }
                cell.Add(new object[] { pair.name, "555L" });

                if (count % 1000 == 0)
                {
                    Console.WriteLine("{0} {1}", count, BTree.counter);
                    BTree.counter = 0;
                }
                count++;
            }
          //  Console.WriteLine(cell.Root.Get().Type.Interpret(cell.Root.Get().Value));
            Console.WriteLine("======part of BinaryTree ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Теперь загрузим все данные, но для этого надо будет их отсортировать и подавать в специальном режиме
        
            // Еще один способ построения бинарного дерева: Сначалы мы формируем объект, потом его вводим стандартным Fill2
            //var array_of_elements = query.OrderBy(pair => pair.name)
            //    .Select(oe => new object[] {oe.name, oe.id})
            //    .ToArray();
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
            cell.Add(new object[] { "Покрышкин Александр Иванович", "pokryshkin_ai" });
            TestSearch(cell, "Покрышкин Александр Иванович");
            Console.WriteLine("======Total ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            cell.Close();
            File.Delete(path + "btree.pxc");
          //  GetOverflow(path);
        }

        private static object[] ToTreeObject(object[][] special_array, int beg, int len)
        {
            if (len == 0) return BTree.Empty;
            else if (len == 1)
                return new object[] {1, new object[] { // запись
                    new object[] { special_array[beg][0], special_array[beg][1] }, // значение
                    BTree.Empty,
                    BTree.Empty,
                    0}};
            else
            {
                int half = len / 2;
                return new object[] {1, new object[] { // запись
                    new object[] { special_array[beg + half][0], special_array[beg + half][1] }, // значение
                    ToTreeObject(special_array, beg, half),
                    ToTreeObject(special_array, beg + half + 1, len - half - 1),
                    0}};
            }
        }

        private static void GetOverflow(string path, Func<object, PxEntry, int> edapth)
        {
            var overflowCell = new BTree(new PType(PTypeEnumeration.longinteger), edapth,  path + "overflowFile", false);
            long c = 0;
            while (true)
            {
                if (c++%1000000 == 0)
                    Console.WriteLine(c);
                overflowCell.Add(c);
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

        private static void TestSearch(BTree cell, string name)
        {
            PxEntry found = cell.BinarySearch(pe =>
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
