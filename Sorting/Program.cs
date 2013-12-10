using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;

namespace Sorting
{
    public class Program
    {
        // Из XML базы данных, выбирается множество RDF-дуг (DatatypeProperty) с предикатом http://fogid.net/o/name и формируется 
        // последовательность пар (записей) имя-сущности - идентификатор сущности. Задача заключается в том, чтобы по частичному
        // имени, определить множество идентификаторов сущностей, для которых имеется похожее имя

        public static void Main(string[] args)
        {
            string path = @"..\..\..\Databases\";

            // Совсем простой тест
            PaCell cell_simple = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.integer)), path + "simple.pac", false);
            object[] arr = { 99, 98, 97, 1, 2, 3, 9, 8, 7, 6, 5, 4, 0 };
            cell_simple.Clear();
            cell_simple.Fill(arr);
            cell_simple.Flush();

            cell_simple.Root.SortByKey<int>(i_obj => (int)i_obj);

            Console.WriteLine("count=" + cell_simple.Root.Count());
            Console.WriteLine(cell_simple.Type.Interpret(cell_simple.Root.Get()));
            cell_simple.Close();
            return;

            PType tp_seq = new PTypeSequence(new PTypeRecord(
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("id", new PType(PTypeEnumeration.sstring))));
            
            DateTime tt0 = DateTime.Now;
            Console.WriteLine("Start");

            PaCell cella = new PaCell(tp_seq, path + "cella.pac", false);
            cella.Clear();
            
            // Заполним ячейку данными
            XElement db = XElement.Load(path + "0001.xml");
            cella.StartSerialFlow();
            cella.S();
            foreach (XElement rec in db.Elements())
            {
                XAttribute about_att = rec.Attribute(sema2012m.ONames.rdfabout);
                if (about_att == null) continue;
                foreach (XElement prop in rec.Elements().Where(pr => pr.Name.LocalName == "name"))
                {
                    cella.V(new object[] { prop.Value, about_att.Value });
                }
            }
            cella.Se();
            cella.EndSerialFlow();
            // Проверим, что данные прочитались (должно получится 40361 пар имя-идентификатор)
            Console.WriteLine(cella.Root.Count());

            // Надо перевести данные в фиксированный формат
            PxCell cell_seqnameid = new PxCell(tp_seq, path + "seqnameid.pxc", false);
            
            // очистим и перекинем данные
            cell_seqnameid.Clear();
            cell_seqnameid.Fill2(cella.Root.Get());

            Console.WriteLine("======Fill ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            //// Теперь сортируем пары по первому (нулевому) полю
            //cell_seqnameid.Root.SortComparison((e1, e2) =>
            //{
            //    string s1 = (string)e1.Field(0).Get();
            //    string s2 = (string)e2.Field(0).Get();
            //    return s1.CompareTo(s2);
            //});
            //Console.WriteLine("======Sort ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Сортируем по-другому
            cell_seqnameid.Root.Sort(e =>
            {
                return (string)e.Field(0).Get();
            });
            Console.WriteLine("======Sort2 ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Посмотрим первые 100
            var qu = cell_seqnameid.Root.Elements().Skip(100).Take(10);
            foreach (var c in qu)
            {
                var v = c.GetValue();
                Console.WriteLine(v.Type.Interpret(v.Value));
            }
            Console.WriteLine("======First 10 after 100. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // поищем чего-нибудь
            string name = "Марчук Александр Гурьевич";
            var found = cell_seqnameid.Root.BinarySearchFirst(e =>
            {
                string nm = (string)e.Field(0).Get();
                return nm.CompareTo(name);
            });
            var f = found.GetValue();
            Console.WriteLine(f.Type.Interpret(f.Value));
            Console.WriteLine("======BinarySearchFirst. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // поищем  по-другому
            string name2 = "марчук";
            var found2 = cell_seqnameid.Root.BinarySearchFirst(e =>
            {
                string nm = ((string)e.Field(0).Get()).ToLower();
                if (nm.StartsWith(name2)) return 0;
                return nm.CompareTo(name);
            });
            var f2 = found.GetValue();
            Console.WriteLine(f2.Type.Interpret(f2.Value));
            Console.WriteLine("======BinarySearchFirst variant 2. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Поиск всех, удовлетворяющих условию
            string name3 = "белинский";
            var found3 = cell_seqnameid.Root.BinarySearchAll(e =>
            {
                string nm = ((string)e.Field(0).Get()).ToLower();
                if (nm.StartsWith(name3)) return 0;
                return nm.CompareTo(name3);
            });
            foreach (var ff in found3)
            {
                var f3 = ff.GetValue();
                Console.WriteLine(f3.Type.Interpret(f3.Value));
            }
            Console.WriteLine("======BinarySearchAll ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Проверка "вручную" правильности поиска всех
            var query = cell_seqnameid.Root.Elements();
            foreach (var rec in query)
            {
                object[] value = (object[])rec.Get();
                string nam = ((string)value[0]).ToLower();
                if (nam.StartsWith(name3))
                {
                    Console.WriteLine("{0} {1}", value[0], value[1]);
                }
            }

            Console.WriteLine("======Fin. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            cella.Close();
            cell_seqnameid.Close();
            System.IO.File.Delete(path + "cella.pac");
            System.IO.File.Delete(path + "seqnameid.pxc");
        }
    }
}
