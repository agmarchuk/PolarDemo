﻿using System;
using System.Linq;
using System.Xml.Linq;
using PolarDB;


namespace FirstSteps
{
    public class Program
    {
        public static PType seqtriplets;
        public static void InitTypes()
        {
            seqtriplets = new PTypeSequence(
                new PTypeUnion(
                    new NamedType("empty", new PType(PTypeEnumeration.none)),
                    new NamedType("op",
                        new PTypeRecord(
                            new NamedType("subject", new PType(PTypeEnumeration.sstring)),
                            new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                            new NamedType("obj", new PType(PTypeEnumeration.sstring)))),
                    new NamedType("dp",
                        new PTypeRecord(
                            new NamedType("subject", new PType(PTypeEnumeration.sstring)),
                            new NamedType("predicate", new PType(PTypeEnumeration.sstring)),
                            new NamedType("data", new PType(PTypeEnumeration.sstring)),
                            new NamedType("lang", new PType(PTypeEnumeration.sstring))))));
        }

        static void Main(string[] args)
        {
            string path = @"..\..\..\Databases\";
            InitTypes();
            DateTime tt0 = DateTime.Now;

            // Проверка объекта
            object[] testdb = new object[] {
                new object[] { 1, new object[] {"a", "b", "c"}},
                new object[] { 1, new object[] {"a1", "b1", "c1"}},
                new object[] { 2, new object[] {"da", "db", "dc", "lang"}}
            };
            Console.WriteLine(seqtriplets.Interpret(testdb));

            // Создание ячейки плавающего формата
            string testpacfilename = path + "test.pac";
            if (System.IO.File.Exists(testpacfilename)) { System.IO.File.Delete(testpacfilename); }
            PaCell cell = new PaCell(seqtriplets, testpacfilename, false); // false - чтобы заполнять
            // Заполнение ячейки данными из объекта
            cell.Fill(testdb);
            // Проверка того, что имеется в ячейке
            var cell_pvalue = cell.Root.GetValue();
            Console.WriteLine(cell_pvalue.Type.Interpret(cell_pvalue.Value));

            PTypeUnion tp_u = new PTypeUnion();
            tp_u.Variants = new[] {
                new NamedType("empty", new PType(PTypeEnumeration.none)),
                new NamedType("node", new PTypeRecord(
                    new NamedType("f0", new PType(PTypeEnumeration.boolean)),
                    new NamedType("f1", new PTypeSequence(tp_u))))
            };
            object[] vv = new object[] { 1,
                new object[] { 
                    true, 
                    new object[] {
                        new object[] { 1,
                            new object[] { 
                                false, 
                                new object[0]
                            }
                        }
                    }}};
            PxCell xcell = new PxCell(tp_u, path + "xcell.pxc", false);
            xcell.Fill(vv);
            PxEntry e1 = xcell.Root.UElement().Field(1);
            PxEntry e2 = e1.Element(0);
            var v = e2.GetValue();
            Console.WriteLine(v.Type.Interpret(v.Value));

            return;

            //cell.Clear();
            //cell.Fill(testtriplets); // проверка на то, что при неочищенной ячейке, записать в нее нельзя
            //cell.Close();
            //cell.Clear();
            //cell.Fill(testtriplets); // проверка на то, что при очищении, записать можно

            //// Проверка серийного буфера, в него загружаются данные из XML-файла, в ячейку ничего не помещается 
            //// Этот тест, для начала, можно пропустить.
            //tt0 = DateTime.Now;
            //SerialBuffer buff = new SerialBuffer(new SerialFlowReceiverStub(seqtriplets));
            //TestSerialInput(buff, path);
            //Console.WriteLine("Число элементов в объекте:" + ((object[])buff.Result).LongLength);
            //Console.WriteLine("Forming buffer ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Проверка ввода из серийного скобочного потока для ячейки свободного формата
            // В данном случае, поток порождается при сканировании XML-документа
            tt0 = DateTime.Now;
            cell.Clear();
            TestSerialInput(cell, path);
            Console.WriteLine("Число элементов в объекте:" + cell.Root.Count());
            Console.WriteLine("Serial input ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            cell.Close(); // Ячейка закрыта, теперь ее нельзя использовать

            // Проверка создания ячейки в режиме чтения
            PaCell cell2pac = new PaCell(seqtriplets, testpacfilename);
            long cnt2 = cell2pac.Root.Count();
            var pval2 = cell2pac.Root.Element(100000).GetValue();
            Console.WriteLine("cnt2=" + cnt2 + " Element(100000).Get()=" + pval2.Type.Interpret(pval2.Value));
            Console.WriteLine("ReadObly cell ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Создание ячейки фиксированного формата
            xcell.Clear();
            xcell = new PxCell(seqtriplets, path + "test.pxc", false);
            var pv = cell2pac.Root.Get();
            tt0 = DateTime.Now;
            xcell.Fill2(pv); // Плохой метод, заменю на хороший
            Console.WriteLine("xcell Fill ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Проверка наполнения
            PxEntry rxt = xcell.Root;
            var ele = rxt.Element(400000).GetValue();
            Console.WriteLine(ele.Type.Interpret(ele.Value));
            Console.WriteLine("ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

        }

        private static void TestSerialInput(ISerialFlow sflow, string path)
        {
            XElement db = XElement.Load(path + @"0001.xml");
            var query = db.Elements().Where(el => el.Attribute(sema2012m.ONames.rdfabout) != null);
            sflow.StartSerialFlow();
            sflow.S();
            foreach (var xelement in query)
            {
                string about = xelement.Attribute(sema2012m.ONames.rdfabout).Value;
                sflow.V(new object[] { 1, new object[] { about, sema2012m.ONames.rdftypestring, xelement.Name.NamespaceName + xelement.Name.LocalName } });
                foreach (var prop in xelement.Elements())
                {
                    string resource_ent = prop.Name.NamespaceName + prop.Name.LocalName;
                    XAttribute resource = prop.Attribute(sema2012m.ONames.rdfresource);
                    if (resource != null)
                    {
                        sflow.V(new object[] { 1, new object[] { about, resource_ent, resource.Value } });
                    }
                    else
                    {
                        sflow.V(new object[] { 2, new object[] { about, resource_ent, prop.Value, "" } });
                    }
                }
            }
            sflow.Se();
            sflow.EndSerialFlow();
        }
    }
}
