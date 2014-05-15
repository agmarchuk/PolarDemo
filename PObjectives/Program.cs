using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace PObjectives
{
    public class Program
    {
        public static void Main(string[] args)
        {
            PType tp_person = new PTypeRecord(
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("age", new PType(PTypeEnumeration.integer)),
                new NamedType("reside", new PType(PTypeEnumeration.integer)));
            string path = @"..\..\..\Databases\";
            Console.WriteLine("Start");
            //создание или открытие базы данных
            Database db = new Database(path + @"po_db\");

            //// Фаза 1: добавление коллекции
            //db.CreateCollection("persons", tp_person);

            ////Фаза 1
            //Collection persons = db.Collection("persons");
            //var p1 = persons.CreateElement(new object[] { "Иванов", 33, 88 });
            //var p2 = persons.CreateElement(new object[] { "Петров", 34, 89 });
            //var p3 = persons.CreateElement(new object[] { "Сидоров", 35, 90 });

            //Фаза 2
            foreach (var element in db.Collection("persons").Elements())
            {
                var v = element.Get();
                Console.WriteLine("Element: {0}", tp_person.Interpret(v));
            }


            Console.WriteLine("ok.");
        }
    }
}
