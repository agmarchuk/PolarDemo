using System.Collections.Generic;
using System.Linq;
using TripleIntClasses;

namespace TrueRdfViewer
{
    public class BerlinTestsInt
    {
        public static string rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        public static string rdfs = "http://www.w3.org/2000/01/rdf-schema#";
        public static string dc = "http://purl.org/dc/elements/1.1/";
        public static string bsbm = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/";
        public static string bsbm_inst = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/";
        
        //TODO repase E to EP
        private static int E(string so) { return _ts.EntityCoding.GetCode(so); }
        private static int EP(string p) { return _ts.PredicatesCoding.GetCode(p); }
        private static OVal rdftype = new OVal() { vid = OValEnumeration.obj, entity = EP(rdf + "type") };
        private static OVal rdfslabel = new OVal() { vid = OValEnumeration.obj, entity = EP(rdfs + "label") };

        public static IEnumerable<OValRowInt> Berlin1(TripleStoreInt ts)
        {
            _ts = ts;
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
                ._spo(_product, _bsbm_productFeature, _bsbm_inst_ProductFeature8)
                ._spo(_product, _rdftype, _bsbm_inst_ProductType1)
                ._spD(_product, _bsbm_ProductPropertyNumeric1, _value1)
                .Where(ovalrow => (int)ovalrow.row[_value1].lit.Value > 1000)
                ._spD(_product, _rdfslabel, _label)
                ;
            return quer;
        }
        // Концовка теста не доделана
        public static IEnumerable<OValRowInt> Berlin3(TripleStoreInt ts)
        {
            short _product = 0, _label = 1;
            short _p1 = 2, _p3 = 3;
            short _bsbm_productFeature = 4, _bsbm_inst_ProductFeature1 = 5, _bsbm_Product = 6;
            short _rdftype = 7, _bsbm_ProductPropertyNumeric1 = 8, _bsbm_ProductPropertyNumeric3 = 9, _rdfslabel = 10;

            OVal[] row = new OVal[] {
                new OVal() { vid = OValEnumeration.obj }, // _product
                new OVal() { vid = OValEnumeration.val }, // _label
                new OVal() { vid = OValEnumeration.val }, // _p1
                new OVal() { vid = OValEnumeration.val }, // _p2
                new OVal() { vid = OValEnumeration.obj, entity = E(bsbm + "productFeature") },
                new OVal() { vid = OValEnumeration.obj, entity = E(bsbm_inst + "ProductFeature1") },
                new OVal() { vid = OValEnumeration.obj, entity = E(bsbm + "Product") },
                rdftype,
                new OVal() { vid = OValEnumeration.obj, entity = E(bsbm + "productPropertyNumeric1") },
                new OVal() { vid = OValEnumeration.obj, entity = E(bsbm + "productPropertyNumeric3") },
                rdfslabel,
            };

            OValRowInt ovr = new OValRowInt(ts, row);
            var quer = Enumerable.Repeat<OValRowInt>(ovr, 1)
                ._Spo(_product, _bsbm_productFeature, _bsbm_inst_ProductFeature1)
                //._spD(_product, _rdfslabel, _label)
                //._spo(_product, _rdftype, _bsbm_Product)
                ._spD(_product, _bsbm_ProductPropertyNumeric1, _p1)
                //.Where(ovalrow => (int)ovalrow.row[_p1].lit.value > 1)
                //._spD(_product, _bsbm_ProductPropertyNumeric3, _p3)
                //.Where(ovalrow => (int)ovalrow.row[_p1].lit.value < 100000)
                //._spD(_product, _rdfslabel, _label)
                ;
            return quer;
        }
        public static IEnumerable<OValRowInt> Berlin6(TripleStoreInt ts)
        {
            short _product = 0, _label = 1;
            short _bsbm_Product = 2;
            short _rdftype = 3, _rdfslabel = 4;

            OVal[] row = new OVal[] {
                new OVal() { vid = OValEnumeration.obj }, // _product
                new OVal() { vid = OValEnumeration.val }, // _label
                new OVal() { vid = OValEnumeration.obj, entity = E(bsbm + "Product") },
                rdftype,
                rdfslabel,
            };
            System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex("^s");

            OValRowInt ovr = new OValRowInt(ts, row);
            var quer = Enumerable.Repeat<OValRowInt>(ovr, 1)
                ._Spo(_product, _rdftype, _bsbm_Product)
                ._spD(_product, _rdfslabel, _label)
                .Where(ovalrow => rx.IsMatch( ((Text)ovalrow.row[_label].lit.Value).Value ))
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
                .spo(_produc, E(bsbm + "productFeature"), E(bsbm_inst + "ProductFeature8"))
                .spo(_produc, E(rdf + "type"), E(bsbm_inst + "ProductType1"))
                .spD(_produc, E(bsbm + "productPropertyNumeric1"), _value1)
                .Where(pack => pack.Vai(_value1) > 1000)
                .spD(_produc, E(rdfs + "label"), _label)
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
                .spO(E(bsbm_inst + "ProductType1"), E(bsbm + "producer"), _p)
                ;
            return quer;
        }
        public static IEnumerable<RPackInt> Query2param(TripleStoreInt ts, string sprod)
        {
            object[] row = new object[13];
            short _label = 0, _comment = 1, _producer = 2, _productFeature = 3,
                _propertyTextual1 = 4, _propertyTextual2 = 5, _propertyTextual3 = 5,
                _propertyNumeric1 = 6, _propertyNumeric2 = 7,
                _propertyTextual4 = 8, _propertyTextual5 = 9,
                _prolertyNumeric4 = 10;
            short _p = 11, _f = 12;
            var quer = Enumerable.Repeat<RPackInt>(new RPackInt(row, ts), 1)
                .spD(E(sprod), E(rdfs + "label"), _label)
                .spD(E(sprod), E(rdfs + "comment"), _comment)
                .spO(E(sprod), E(bsbm + "producer"), _p)

                .spD(_p, E(rdfs + "label"), _producer)
                .spo(E(sprod), E(dc + "publisher"), _p)
                .spO(E(sprod), E(bsbm + "productFeature"), _f)
                .spD(_f, E(rdfs + "label"), _productFeature)
                
                // Следующая группа может быть переставлена из конца в середину, будет быстрее
                .spD(E(sprod), E(bsbm + "productPropertyTextual1"), _propertyTextual1)
                .spD(E(sprod), E(bsbm + "productPropertyTextual2"), _propertyTextual2)
                .spD(E(sprod), E(bsbm + "productPropertyTextual3"), _propertyTextual3)
                .spD(E(sprod), E(bsbm + "productPropertyNumeric1"), _propertyNumeric1)
                .spD(E(sprod), E(bsbm + "productPropertyNumeric2"), _propertyNumeric2)
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
        public static IEnumerable<RPackInt> Query5parameter(TripleStoreInt ts, string sprod)
        {
            string dataFromProducer1 = sprod;
            object[] row = new object[7];
            short _product = 0, _productLabel = 1;
            short _prodFeature = 2, _origProperty1 = 3, _simProperty1 = 4, _origProperty2 = 5, _simProperty2 = 6;
            var quer = Enumerable.Repeat<RPackInt>(new RPackInt(row, ts), 1)
                .spO(E(dataFromProducer1), E(bsbm + "productFeature"), _prodFeature)
                .Spo(_product, E(bsbm + "productFeature"), _prodFeature)
                .Where(pack => E(dataFromProducer1) != pack.GetE(_product))
                .spD(E(dataFromProducer1), E(bsbm + "productPropertyNumeric1"), _origProperty1)
                .spD(_product, E(bsbm + "productPropertyNumeric1"), _simProperty1)
                .Where(pack => pack.Vai(_simProperty1) < (pack.Vai(_origProperty1) + 120) && pack.Vai(_simProperty1) > (pack.Vai(_origProperty1) - 120))
                .spD(E(dataFromProducer1), E(bsbm + "productPropertyNumeric2"), _origProperty2)
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
                .Where(pack => rx.IsMatch(((Text)pack.Val(_label).Value).Value))
                ;
            return quer;
        }

        public static string[] sarr = new string[] {
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer1/Product1", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer1/Product21", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer1/Product41", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer2/Product61", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer2/Product81", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer3/Product101", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer3/Product121", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer4/Product141", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer4/Product161", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer5/Product181", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer5/Product201", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer6/Product221", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer6/Product241", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer6/Product261", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer7/Product281", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer7/Product301", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer7/Product321", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer8/Product341", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer8/Product361", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer8/Product381", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer9/Product401", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer9/Product421", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer9/Product441", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer10/Product461", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer11/Product481", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer11/Product501", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer11/Product521", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer12/Product541", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer12/Product561", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer12/Product581", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer13/Product601", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer13/Product621", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer14/Product641", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer14/Product661", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer14/Product681", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer15/Product701", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer15/Product721", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer16/Product741", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer16/Product761", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer16/Product781", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer17/Product801", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer17/Product821", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer18/Product841", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer19/Product861", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer19/Product881", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer20/Product901", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer20/Product921", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer20/Product941", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer21/Product961", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer21/Product981", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer22/Product1001", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer22/Product1021", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer22/Product1041", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer23/Product1061", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer23/Product1081", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer23/Product1101", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer24/Product1121", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer25/Product1141", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer25/Product1161", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer25/Product1181", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer26/Product1201", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer26/Product1221", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer26/Product1241", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer27/Product1261", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer27/Product1281", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer28/Product1301", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer29/Product1321", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer29/Product1341", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer30/Product1361", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer31/Product1381", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer31/Product1401", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer32/Product1421", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer32/Product1441", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer32/Product1461", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer32/Product1481", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer33/Product1501", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer33/Product1521", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer33/Product1541", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer35/Product1561", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer35/Product1581", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer36/Product1601", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer36/Product1621", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer37/Product1641", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer37/Product1661", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer37/Product1681", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer37/Product1701", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer38/Product1721", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer38/Product1741", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer39/Product1761", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer39/Product1781", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer39/Product1801", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer39/Product1821", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer40/Product1841", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer40/Product1861", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer41/Product1881", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer41/Product1901", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer41/Product1921", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer41/Product1941", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer42/Product1961", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer42/Product1981", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer42/Product2001", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer42/Product2021", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer43/Product2041", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer43/Product2061", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer44/Product2081", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer45/Product2101", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer45/Product2121", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer45/Product2141", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer46/Product2161", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer47/Product2181", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer47/Product2201", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer47/Product2221", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer48/Product2241", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer48/Product2261", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer49/Product2281", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer49/Product2301", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer50/Product2321", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer50/Product2341", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer50/Product2361", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer50/Product2381", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer51/Product2401", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer51/Product2421", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer51/Product2441", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer52/Product2461", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer53/Product2481", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer53/Product2501", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer54/Product2521", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer54/Product2541", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer54/Product2561", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer55/Product2581", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer55/Product2601", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer56/Product2621", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer57/Product2641", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer58/Product2661", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer58/Product2681", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer58/Product2701", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer59/Product2721", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer59/Product2741", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer59/Product2761", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer60/Product2781", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer61/Product2801", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer61/Product2821", 
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/dataFromProducer61/Product2841", 
};

        private static TripleStoreInt _ts;
    }
}
