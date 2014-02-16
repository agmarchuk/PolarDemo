using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueRdfViewer
{
    public class BerlinTests
    {
        public static string rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        public static string rdfs = "http://www.w3.org/2000/01/rdf-schema#";
        public static string dc = "http://purl.org/dc/elements/1.1/";
        public static string bsbm = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/";
        public static string bsbm_inst = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/";
        //
        public static IEnumerable<RPack> Query1(TripleStore ts)
        {
            object[] row = new object[3]; 
            int _produc = 0, _value1 = 1, _label = 2;
            var quer = Enumerable.Repeat<RPack>(new RPack(row, ts), 1)
                .Spo(_produc, bsbm + "productFeature", bsbm_inst + "ProductFeature19")
                .spo(_produc, bsbm + "productFeature", bsbm_inst + "ProductFeature8")
                .spo(_produc, rdf + "type", bsbm_inst + "ProductType1")
                .spD(_produc, bsbm + "productPropertyNumeric1", _value1)
                .Where(pack => pack.Vai(_value1) > 10)
                .spD(_produc, rdfs + "label", _label)
                ;
            return quer;
        }
        // Не выдает результатов, поэтому не доделан
        public static IEnumerable<RPack> Query2(TripleStore ts)
        {
            object[] row = new object[13];
            int _label = 0, _comment = 1, _producer = 2, _productFeature = 3,
                _propertyTextual1 = 4, _propertyTextual2 = 5, _propertyTextual3 = 5,
                _propertyNumeric1 = 6, _propertyNumeric2 = 7,
                _propertyTextual4 = 8, _propertyTextual5 = 9,
                _prolertyNumeric4 = 10;
            int _p = 11, f = 12;
            var quer = Enumerable.Repeat<RPack>(new RPack(row, ts), 1)
                .spD(bsbm_inst + "ProductType1", rdfs + "label", _label)
                .spD(bsbm_inst + "ProductType1", rdfs + "comment", _comment)
                .spD(bsbm_inst + "ProductType1", bsbm + "producer", _p)
                ;
            return quer;
        }
        // Концовка теста не доделана
        public static IEnumerable<RPack> Query3(TripleStore ts)
        {
            object[] row = new object[5];
            int _product = 0, _label = 1;
            int _p1 = 2, _p3 = 3, _testVar = 4;
            var quer = Enumerable.Repeat<RPack>(new RPack(row, ts), 1)
                .Spo(_product, bsbm + "productFeature", bsbm_inst + "ProductFeature1")
                .spD(_product, rdfs + "label", _label)
                .spo(_product, rdf + "type", bsbm + "Product")
                .spD(_product, bsbm + "productPropertyNumeric1", _p1)
                .Where(pack => pack.Vai(_p1) > 1)
                .spD(_product, bsbm + "productPropertyNumeric3", _p3)
                .Where(pack => pack.Vai(_p3) < 100000)
                ;
            return quer;
        }
        public static IEnumerable<RPack> Query6(TripleStore ts)
        {
            System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex("^s");
            object[] row = new object[2];
            int _product = 0, _label = 1;
            var quer = Enumerable.Repeat<RPack>(new RPack(row, ts), 1)
                .Spo(_product, rdf + "type", bsbm + "Product")
                .spD(_product, rdfs + "label", _label)
                //.Where(pack => ((Text)pack.Val(_label).value).s == "merer")
                .Where(pack => rx.IsMatch(((Text)pack.Val(_label).value).s))
                ;
            return quer;
        }
    }
}
