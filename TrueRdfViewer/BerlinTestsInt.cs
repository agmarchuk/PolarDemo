using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueRdfViewer
{
    public class BerlinTestsInt
    {
        public static string rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        public static string rdfs = "http://www.w3.org/2000/01/rdf-schema#";
        public static string dc = "http://purl.org/dc/elements/1.1/";
        public static string bsbm = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/";
        public static string bsbm_inst = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/";
        private static int P(string spo) { return TripleInt.Code(spo); }
        //
        public static IEnumerable<RPackInt> Query1(TripleStoreInt ts)
        {
            object[] row = new object[3];
            short _produc = 0, _value1 = 1, _label = 2;
            var quer = Enumerable.Repeat<RPackInt>(new RPackInt(row, ts), 1)
                .Spo(_produc, P(bsbm + "productFeature"), P(bsbm_inst + "ProductFeature19"))
                .spo(_produc, P(bsbm + "productFeature"), P(bsbm_inst + "ProductFeature8"))
                .spo(_produc, P(rdf + "type"), P(bsbm_inst + "ProductType1"))
                .spD(_produc, P(bsbm + "productPropertyNumeric1"), _value1)
                .Where(pack => pack.Vai(_value1) > 10)
                .spD(_produc, P(rdfs + "label"), _label)
                ;
            return quer;
        }
        // Вариант первого теста
        public static IEnumerable<RPackInt> Query1_1(TripleStoreInt ts)
        {
            object[] row = new object[3];
            short _produc = 0, _value1 = 1, _label = 2;
            var quer = Enumerable.Repeat<RPackInt>(new RPackInt(row, ts), 1)
                .Spo(_produc, bsbm + "productFeature", bsbm_inst + "ProductFeature17")
                .spo(_produc, bsbm + "productFeature", bsbm_inst + "ProductFeature7")
                .spo(_produc, rdf + "type", bsbm_inst + "ProductType1")
                .spD(_produc, bsbm + "productPropertyNumeric1", _value1)
                .Where(pack => pack.Vai(_value1) > 10)
                .spD(_produc, rdfs + "label", _label)
                ;
            return quer;
        }
        // Тестовый запрос для экспериментов
        public static IEnumerable<RPackInt> Query0(TripleStoreInt ts)
        {
            object[] row = new object[3];
            short _produc = 0, _value1 = 1, _label = 2;
            var quer = Enumerable.Repeat<RPackInt>(new RPackInt(row, ts), 1)
                .Spo(_produc, bsbm + "productFeature", bsbm_inst + "ProductFeature19")
                //.spo(_produc, bsbm + "productFeature", bsbm_inst + "ProductFeature8")
                //.spo(_produc, rdf + "type", bsbm_inst + "ProductType1")
                //.spD(_produc, bsbm + "productPropertyNumeric1", _value1)
                //.Where(pack => pack.Vai(_value1) > 10)
                //.spD(_produc, rdfs + "label", _label)
                ;
            return quer;
        }
        // Не выдает результатов, поэтому не доделан
        public static IEnumerable<RPackInt> Query2(TripleStoreInt ts)
        {
            object[] row = new object[13];
            short _label = 0, _comment = 1, _producer = 2, _productFeature = 3,
                _propertyTextual1 = 4, _propertyTextual2 = 5, _propertyTextual3 = 5,
                _propertyNumeric1 = 6, _propertyNumeric2 = 7,
                _propertyTextual4 = 8, _propertyTextual5 = 9,
                _prolertyNumeric4 = 10;
            short _p = 11, f = 12;
            var quer = Enumerable.Repeat<RPackInt>(new RPackInt(row, ts), 1)
                .spD(P(bsbm_inst + "ProductType1"), P(rdfs + "label"), _label)
                .spD(P(bsbm_inst + "ProductType1"), P(rdfs + "comment"), _comment)
                .spD(P(bsbm_inst + "ProductType1"), P(bsbm + "producer"), _p)
                ;
            return quer;
        }
        // Концовка теста не доделана
        public static IEnumerable<RPackInt> Query3(TripleStoreInt ts)
        {
            object[] row = new object[5];
            short _product = 0, _label = 1;
            short _p1 = 2, _p3 = 3, _testVar = 4;
            var quer = Enumerable.Repeat<RPackInt>(new RPackInt(row, ts), 1)
                .Spo(_product, P(bsbm + "productFeature"), P(bsbm_inst + "ProductFeature1"))
                .spD(_product, P(rdfs + "label"), _label)
                .spo(_product, P(rdf + "type"), P(bsbm + "Product"))
                .spD(_product, P(bsbm + "productPropertyNumeric1"), _p1)
                .Where(pack => pack.Vai(_p1) > 1)
                .spD(_product, P(bsbm + "productPropertyNumeric3"), _p3)
                .Where(pack => pack.Vai(_p3) < 100000)
                ;
            return quer;
        }
        // Вариант третьего теста
        public static IEnumerable<RPackInt> Query3_1(TripleStoreInt ts)
        {
            object[] row = new object[5];
            short _product = 0, _label = 1;
            short _p1 = 2, _p3 = 3, _testVar = 4;
            var quer = Enumerable.Repeat<RPackInt>(new RPackInt(row, ts), 1)
                .Spo(_product, bsbm + "productFeature", bsbm_inst + "ProductFeature1")
                .spD(_product, rdfs + "label", _label)
                .spo(_product, rdf + "type", bsbm + "Product")
                .spD(_product, bsbm + "productPropertyNumeric1", _p1)
                .Where(pack => pack.Vai(_p1) > 500)
                .spD(_product, bsbm + "productPropertyNumeric3", _p3)
                .Where(pack => pack.Vai(_p3) < 1000)
                ;
            return quer;
        }
        public static IEnumerable<RPackInt> Query6(TripleStoreInt ts)
        {
            System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex("^s");
            object[] row = new object[2];
            short _product = 0, _label = 1;
            var quer = Enumerable.Repeat<RPackInt>(new RPackInt(row, ts), 1)
                .Spo(_product, P(rdf + "type"), P(bsbm + "Product"))
                .spD(_product, P(rdfs + "label"), _label)
                //.Where(pack => ((Text)pack.Val(_label).value).s == "merer")
                .Where(pack => rx.IsMatch(((Text)pack.Val(_label).value).s))
                ;
            return quer;
        }
    }
}
