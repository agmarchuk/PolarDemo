using System;
using System.Collections.Generic;
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
        //            pair^{element: T, count: longinteger, less: BTree<T>, more: BTree<T>};
        PTypeUnion tp_btree;
        PType tp_element;
        public BTree()
        {
            // Тип элемента, как и в проекте Sorting: 
            //tp_element = new PTypeRecord(
            //    new NamedType("name", new PType(PTypeEnumeration.sstring)),
            //    new NamedType("rec_off", new PType(PTypeEnumeration.longinteger)));
            tp_element = new PTypeRecord(
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("id", new PType(PTypeEnumeration.sstring)));
            //tp_element = new PTypeRecord(
            //    new NamedType("name", new PTypeFString(32)),
            //    new NamedType("rec_off", new PType(PTypeEnumeration.longinteger)));
            // Заполнение типа дерева. Тип рекурсивный, поэтому это делается в два выражения
            tp_btree = new PTypeUnion();
            tp_btree.Variants = new NamedType[] {
                new NamedType("empty", new PType(PTypeEnumeration.none)),
                new NamedType("pair", new PTypeRecord(
                    new NamedType("element", tp_element),
                    new NamedType("count", new PType(PTypeEnumeration.longinteger)),
                    new NamedType("less", tp_btree),
                    new NamedType("more", tp_btree)))
            };
        }
        public static void Main(string[] args)
        {
            DateTime tt0 = DateTime.Now;

            string path = @"D:\home\FactographDatabases\PolarDemo\";
            Console.WriteLine("Start.");
            // Инициируем типы
            BTree tree = new BTree();
            // Создадим фиксированную ячейку
            PxCell cell = new PxCell(tree.tp_btree, path + "btree.pxc", false);
            cell.Clear();

            //// Проверим существует ли пустое значение
            //var r1 = cell.Root.Get();
            //Console.WriteLine(r1.Type.Interpret(r1.Value));

            // сделаем пробное заполнение вручную
            object[] empty = new object[] { 0, null };
            object[] valu =
                new object[] { 1, new object[] { new object[] {"name1", "333L"}, 2L, 
                    new object[] { 1, new object[] { new object[] {"name0", "444L"}, 1L, empty, empty}}, 
                    empty } };
            cell.Fill2(valu);
            // проверяем содержимое
            var res = cell.Root.Get();
            Console.WriteLine(res.Type.Interpret(res.Value));

            // Пробно добавим пару элементов через метод расширения, описанный в ExtensionMethods
            cell.Clear();
            Comparison<object> compare = (object v1, object v2) =>
                {
                    string s1 = (string)(((object[])v1)[0]);
                    return s1.CompareTo((string)(((object[])v2)[0]));
                };
            cell.Root.Add(new object[] { "bbb_name", "333L" }, compare);
            cell.Root.Add(new object[] { "bbc_name", "444L" }, compare);
            
            var res2 = cell.Root.Get();
            Console.WriteLine(res2.Type.Interpret(res.Value));

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
            
            // Загрузим фрагмент бинарного дерева
            cell.Clear();
            int count = 0;
            ExtensionMethods.counter = 0;
            foreach (var pair in query.Take(5001).Reverse())
            {
                cell.Root.Add(new object[] { pair.name, "555L" }, compare);
                if (count % 1000 == 0)
                {
                    Console.WriteLine("{0} {1}", count, ExtensionMethods.counter);
                    ExtensionMethods.counter = 0;
                }
                count++;
            }
            Console.WriteLine("======part of BinaryTree ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Теперь загрузим все данные, но для этого надо будет их отсортировать и подавать в специальном режиме
            cell.Clear();
            ExtensionMethods.counter = 0;
            var special_array = query.OrderBy(pair => pair.name)
                .Select(oe => oe)
                .ToArray();
            int len = special_array.Length;

            foreach (int ind in IndSeq(0, 8))
            {
                Console.WriteLine(ind);
            }
            //return;
            count = 0;
            ExtensionMethods.counter = 0;
            foreach (int ind in IndSeq(0, len))
            {
                cell.Root.Add(new object[] { special_array[ind].name, special_array[ind].id }, compare);
                if (count % 1000 == 0)
                {
                    Console.WriteLine("{0} {1}", count, ExtensionMethods.counter);
                    ExtensionMethods.counter = 0;
                }
                count++;
            }
            Console.WriteLine("======BinaryTree ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            // У меня дома получилось 17 сек. для пары {string, long} и 22 сек. для {string, string}

            // Бинарный поиск на бинарном дереве
            string name = "Марчук Александр Гурьевич";
            TestSearch(cell, name);
            Console.WriteLine("======Binary Search ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Под конец, добави еще одну пару и посмотрим появилась ли она
            TestSearch(cell, "Покрышкин Александр Иванович");
            cell.Root.Add(new object[] { "Покрышкин Александр Иванович", "pokryshkin_ai" }, compare);
            TestSearch(cell, "Покрышкин Александр Иванович");
            Console.WriteLine("======Total ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }

        private static void TestSearch(PxCell cell, string name)
        {
            PxEntry found = cell.Root.BinarySearchInBT((PxEntry pe) =>
            {
                string s = (string)pe.Field(0).Get().Value;
                return name.CompareTo(s);
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
