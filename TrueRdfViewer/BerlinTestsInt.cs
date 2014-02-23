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
        private static int E(string spo) { return TripleInt.Code(spo); }
        private static OVal rdftype = new OVal() { vid = OValEnumeration.obj, entity = E(rdf + "type") };
        private static OVal rdfslabel = new OVal() { vid = OValEnumeration.obj, entity = E(rdfs + "label") };

        public static IEnumerable<OValRowInt> Berlin1(TripleStoreInt ts)
        {
            short _product = 0, _bsbm_productFeature = 1, _bsbm_inst_ProductFeature19 = 2, _bsbm_inst_ProductFeature8 = 3;
            short _rdftype = 4, _bsbm_inst_ProductType1 = 5, _bsbm_ProductPropertyNumeric1 = 6;
            short _value1 = 7, _label = 8, _rdfslabel = 9;

            OVal[] row = new OVal[] {
                new OVal() { vid = OValEnumeration.obj }, // _product
                new OVal() { vid = OValEnumeration.obj, entity = E(bsbm + "productFeature") },
                new OVal() { vid = OValEnumeration.obj, entity = E(bsbm_inst + "ProductFeature19") },
                new OVal() { vid = OValEnumeration.obj, entity = E(bsbm_inst + "ProductFeature8") },
                rdftype,
                new OVal() { vid = OValEnumeration.obj, entity = E(bsbm_inst + "ProductType1") },
                new OVal() { vid = OValEnumeration.obj, entity = E(bsbm + "productPropertyNumeric1") },
                new OVal() { vid = OValEnumeration.val }, // _value1
                new OVal() { vid = OValEnumeration.val }, // _label
                rdfslabel
            };
            OValRowInt ovr = new OValRowInt(ts, row);
            var quer = Enumerable.Repeat<OValRowInt>(ovr, 1)
                ._Spo(_product, _bsbm_productFeature, _bsbm_inst_ProductFeature19)
                //._spo(_product, _bsbm_productFeature, _bsbm_inst_ProductFeature8)
                //._spo(_product, _rdftype, _bsbm_inst_ProductType1)
                //._spD(_product, _bsbm_ProductPropertyNumeric1, _value1)
                //.Where(ovalrow => (int)ovalrow.row[_value1].lit.value > 1000)
                //._spD(_product, _rdfslabel, _label)
                ;
            return quer;
        }
        //
        public static IEnumerable<RPackInt> Query1(TripleStoreInt ts)
        {
            object[] row = new object[3];
            short _produc = 0, _value1 = 1, _label = 2;
            var quer = Enumerable.Repeat<RPackInt>(new RPackInt(row, ts), 1)
                .Spo(_produc, E(bsbm + "productFeature"), E(bsbm_inst + "ProductFeature19"))
                //.spo(_produc, E(bsbm + "productFeature"), E(bsbm_inst + "ProductFeature8"))
                //.spo(_produc, E(rdf + "type"), E(bsbm_inst + "ProductType1"))
                //.spD(_produc, E(bsbm + "productPropertyNumeric1"), _value1)
                //.Where(pack => pack.Vai(_value1) > 1000)
                //.spD(_produc, E(rdfs + "label"), _label)
                ;
            return quer;
        }
        // Вариант первого теста
        public static IEnumerable<RPackInt> Query1_1(TripleStoreInt ts)
        {
            object[] row = new object[3];
            short _produc = 0, _value1 = 1, _label = 2;
            var quer = Enumerable.Repeat<RPackInt>(new RPackInt(row, ts), 1)
                .Spo(_produc, E(bsbm + "productFeature"), E(bsbm_inst + "ProductFeature17"))
                //.spo(_produc, E(bsbm + "productFeature"), E(bsbm_inst + "ProductFeature7"))
                //.spo(_produc, E(rdf + "type"), E(bsbm_inst + "ProductType1"))
                //.spD(_produc, E(bsbm + "productPropertyNumeric1"), _value1)
                //.Where(pack => pack.Vai(_value1) > 10)
                //.spD(_produc, E(rdfs + "label"), _label)
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
                .spD(E(bsbm_inst + "ProductType1"), E(rdfs + "label"), _label)
                .spD(E(bsbm_inst + "ProductType1"), E(rdfs + "comment"), _comment)
                .spD(E(bsbm_inst + "ProductType1"), E(bsbm + "producer"), _p)
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
                .Spo(_product, E(bsbm + "productFeature"), E(bsbm_inst + "ProductFeature1"))
                .spD(_product, E(rdfs + "label"), _label)
                .spo(_product, E(rdf + "type"), E(bsbm + "Product"))
                .spD(_product, E(bsbm + "productPropertyNumeric1"), _p1)
                .Where(pack => pack.Vai(_p1) > 1)
                .spD(_product, E(bsbm + "productPropertyNumeric3"), _p3)
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
                .Spo(_product, E(bsbm + "productFeature"), E(bsbm_inst + "ProductFeature1"))
                .spD(_product, E(rdfs + "label"), _label)
                .spo(_product, E(rdf + "type"), E(bsbm + "Product"))
                .spD(_product, E(bsbm + "productPropertyNumeric1"), _p1)
                .Where(pack => pack.Vai(_p1) > 500)
                .spD(_product, E(bsbm + "productPropertyNumeric3"), _p3)
                .Where(pack => pack.Vai(_p3) < 1000)
                ;
            return quer;
        }
        public static IEnumerable<RPackInt> Query5(TripleStoreInt ts)
        {
            string dataFromProducer1 = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer1/";
            object[] row = new object[7];
            short _product = 0, _productLabel = 1;
            short _prodFeature = 2, _origProperty1 = 3, _simProperty1 = 4, _origProperty2 = 5, _simProperty2 = 6;
            var quer = Enumerable.Repeat<RPackInt>(new RPackInt(row, ts), 1)
                .spO(E(dataFromProducer1 + "Product12"), E(bsbm + "productFeature"), _prodFeature)
                .Spo(_product, E(bsbm + "productFeature"), _prodFeature)
                .Where(pack => E(dataFromProducer1 + "Product12") != pack.GetE(_product))
                //.spD(_product, E(rdfs + "label"), _productLabel)
                .spD(E(dataFromProducer1 + "Product12"), E(bsbm + "productPropertyNumeric1"), _origProperty1) 
                .spD(_product, E(bsbm + "productPropertyNumeric1"), _simProperty1)
                .Where(pack => pack.Vai(_simProperty1) < (pack.Vai(_origProperty1) + 120) && pack.Vai(_simProperty1) > (pack.Vai(_origProperty1) - 120))
                .spD(E(dataFromProducer1 + "Product12"), E(bsbm + "productPropertyNumeric2"), _origProperty2)
                .spD(_product, E(bsbm + "productPropertyNumeric2"), _simProperty2)
                .Where(pack => pack.Vai(_simProperty2) < (pack.Vai(_origProperty2) + 170) && pack.Vai(_simProperty2) > (pack.Vai(_origProperty2) - 170))
                .spD(_product, E(rdfs + "label"), _productLabel) // переставлено
                ;
            return quer;
        }
        public static IEnumerable<RPackInt> Query6(TripleStoreInt ts)
        {
            System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex("^s");
            object[] row = new object[2];
            short _product = 0, _label = 1;
            var quer = Enumerable.Repeat<RPackInt>(new RPackInt(row, ts), 1)
                .Spo(_product, E(rdf + "type"), E(bsbm + "Product"))
                .spD(_product, E(rdfs + "label"), _label)
                .Where(pack => rx.IsMatch(((Text)pack.Val(_label).value).s))
                ;
            return quer;
        }
    }
}
