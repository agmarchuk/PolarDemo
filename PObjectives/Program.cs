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
        public static void Main1(string[] args)
        {
            PType tp_person = new PTypeRecord(
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("age", new PType(PTypeEnumeration.integer)),
                new NamedType("reside", new PType(PTypeEnumeration.integer)));
            PType tp_city = new PTypeRecord(
                new NamedType("name", new PType(PTypeEnumeration.sstring)));
            string path = @"..\..\..\Databases\";
            Console.WriteLine("Start");
            //создание или открытие базы данных
            Database db = new Database(path + @"po_db\");

            // Фаза 1: добавление коллекций
            //db.CreateCollection("persons", tp_person);
            //db.CreateCollection("cities", tp_city);

            //Фаза 1
            Collection persons = db.Collection("persons");
            Collection cities = db.Collection("cities");

            //var c1 = cities.CreateElement(new object[] { "Новосибирск" });
            //var c2 = cities.CreateElement(new object[] { "Бердск" });
            //var p1 = persons.CreateElement(new object[] { "Иванов", 33, -1 });
            //var p2 = persons.CreateElement(new object[] { "Петров", 34, c1.Id });
            //var p3 = persons.CreateElement(new object[] { "Сидоров", 35, c2.Id });

            //var c3 = cities.CreateElement(new object[] { "Искитим" });
            //var p1 = persons.Element(0);
            //persons.UpdateElement(p1, new object[] { "Иванов Иван Иванович", 33, c3.Id });
            
            //Фаза 2
            foreach (var element in db.Collection("persons").Elements())
            {
                var v = element.Get();
                Console.WriteLine("Element: {0}", tp_person.Interpret(v));
            }

            //// Другой способ
            //Person pers = persons.Element<Person>(1);
            //Console.WriteLine("{0} {1}", pers.Name, pers.Age);
            //City cty = cities.Element<City>(1);
            //Console.WriteLine("{0} ", cty.Name);

            Person pers = persons.Element<Person>(1);
            Console.WriteLine("Element: {0}", tp_person.Interpret(pers.Get()));
            Console.WriteLine("{0} {1}", pers.Name, pers.Age);
            City cty = pers.City;
            Console.WriteLine("{0} ", cty.Name);

            //// Изменение
            //pers.Age = 35;
            //pers.Name = "Петренко";

            Console.WriteLine("ok.");
        }
    }
}
