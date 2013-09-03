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
            string path = @"D:\home\FactographDatabases\PolarDemo\";

            PType tp_seq = new PTypeSequence(new PTypeRecord(
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("id", new PType(PTypeEnumeration.sstring))));
            
            DateTime tt0 = DateTime.Now;
            Console.WriteLine("Start");

            PaCell cella = new PaCell(tp_seq, path + "cella.pac", false);
            
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
            cell_seqnameid.Fill2(cella.Root.Get().Value);
            // Теперь сортируем пары по первому (нулевому) полю


            Console.WriteLine("Fin. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
    }
}
