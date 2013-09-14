using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;

namespace SerialFlow
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello!");
            string path = @"..\..\..\Databases\";
            // Мы будем XML-файл 0001.xml превращать в последовательность специально организованных записей
            PType tp_seqrec = new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.sstring)),
                new NamedType("type", new PType(PTypeEnumeration.sstring)),
                new NamedType("fields", new PTypeSequence(new PTypeRecord(
                    new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                    new NamedType("data", new PType(PTypeEnumeration.sstring)),
                    new NamedType("lang", new PType(PTypeEnumeration.sstring))))),
                new NamedType("direct", new PTypeSequence(new PTypeRecord(
                    new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                    new NamedType("obj", new PType(PTypeEnumeration.sstring)))))));
            XElement db = XElement.Load(path + "0001.xml");
            XName rdfabout = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about";
            XName rdfresource = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource";
            XName xmllang = "{http://www.w3.org/XML/1998/namespace}lang";
            //string rdftypestring = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
            var query = db.Elements()
                .Where(el => el.Attribute(rdfabout) != null);
            // Для проверки, сделаем объект
            object[] db_obj = query
                .Select(el => new object[] {
                    el.Attribute(rdfabout).Value, 
                    el.Name.NamespaceName + el.Name.LocalName,
                    el.Elements()
                        .Where(ee => ee.Attribute(rdfresource) == null)
                        .Select(ee => new object[] {
                            ee.Name.NamespaceName + ee.Name.LocalName, 
                            ee.Value, 
                            ee.Attribute(xmllang) == null? "": ee.Attribute(xmllang).Value}).ToArray(),
                    el.Elements()
                        .Where(ee => ee.Attribute(rdfresource) != null)
                        .Select(ee => new object[] {
                            ee.Name.NamespaceName + ee.Name.LocalName, 
                            ee.Attribute(rdfresource).Value}).ToArray()
                }).ToArray();
            Console.WriteLine(db_obj.Length);
            DateTime tt0 = DateTime.Now;
            // Создадим ячейку
            PaCell cell = new PaCell(tp_seqrec, path + "records_fromObject.pac", false);
            cell.Clear();
            cell.Fill(db_obj);
            cell.Close();
            Console.WriteLine("======Fill ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Теперь будем вводить через поток
            cell = new PaCell(tp_seqrec, path + "records_fromFlow.pac", false);
            cell.Clear();
            ISerialFlow input = cell;
            input.StartSerialFlow();
            input.S();
            foreach (var rec in db_obj)
            {
                input.V(rec);
            }
            input.Se();
            input.EndSerialFlow();
            cell.Close();
            Console.WriteLine("======Fill flow ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Будем мельчить поток (у меня получилось почти 8 сек.)
            cell = new PaCell(tp_seqrec, path + "records_fromFlow.pac", false);
            cell.Clear();
            input = cell;
            input.StartSerialFlow();
            input.S();
            foreach (object[] rec in db_obj)
            {
                input.R();
                input.V(rec[0]);
                input.V(rec[1]);
                //input.V(rec[2]);
                {
                    input.S();
                    foreach (object[] fields in (object[])rec[2])
                    {
                        input.R();
                        input.V(fields[0]);
                        input.V(fields[1]);
                        input.V(fields[2]);
                        input.Re();
                    }
                    input.Se();
                }
                //input.V(rec[3]);
                {
                    input.S();
                    foreach (object[] directs in (object[])rec[3])
                    {
                        input.R();
                        input.V(directs[0]);
                        input.V(directs[1]);
                        input.Re();
                    }
                    input.Se();
                }
                input.Re();
            }
            input.Se();
            input.EndSerialFlow();
            cell.Close();
            Console.WriteLine("======Fill small flow ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Теперь предыдущий мелкий поток пропустим через буфер (у меня получилось почти 8 сек.)
            cell = new PaCell(tp_seqrec, path + "records_fromFlow.pac", false);
            cell.Clear();
            input = new SerialBuffer(cell, 3);
            input.StartSerialFlow();
            input.S();
            foreach (object[] rec in db_obj)
            {
                input.R();
                input.V(rec[0]);
                input.V(rec[1]);
                //input.V(rec[2]);
                {
                    input.S();
                    foreach (object[] fields in (object[])rec[2])
                    {
                        input.R();
                        input.V(fields[0]);
                        input.V(fields[1]);
                        input.V(fields[2]);
                        input.Re();
                    }
                    input.Se();
                }
                //input.V(rec[3]);
                {
                    input.S();
                    foreach (object[] directs in (object[])rec[3])
                    {
                        input.R();
                        input.V(directs[0]);
                        input.V(directs[1]);
                        input.Re();
                    }
                    input.Se();
                }
                input.Re();
            }
            input.Se();
            input.EndSerialFlow();
            cell.Close();
            Console.WriteLine("======Fill bufferred small flow ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
    }
}
