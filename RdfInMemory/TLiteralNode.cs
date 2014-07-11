using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfInMemory
{
    public class TLiteralNode : ILiteralNode
    {
        private TGraph g;
        public IGraph Graph { get { return g; } }
        public NodeType NodeType { get { return RdfInMemory.NodeType.Literal; } }
        private long ocode;
        public long Code { get { return ocode; } }
        internal TLiteralNode(string rest_line, TGraph g)
        {
            this.g = g;
            // Последняя двойная кавычка 
            int lastqu = rest_line.LastIndexOf('\"');

            // Значение данных
            var sdata = rest_line.Substring(1, lastqu - 1);

            // Языковый специализатор:
            int dog = rest_line.LastIndexOf('@');
            string lang = "";
            if (dog == lastqu + 1) lang = rest_line.Substring(dog + 1, rest_line.Length - dog - 1);

            string datatype = "";
            int pp = rest_line.IndexOf("^^");
            if (pp == lastqu + 1)
            {
                //  Тип данных
                string qname = rest_line.Substring(pp + 2);
                //  тип данных может быть "префиксным" или полным
                if (qname[0] == '<')
                {
                    datatype = qname.Substring(1, qname.Length - 2);
                }
                else
                {
                    datatype = TTurtleParser.GetEntityString(g, qname);
                }
            }
            Literal lit =
                datatype == "http://www.w3.org/2001/XMLSchema#integer" ?
                    new Literal() { Vid = LiteralVidEnumeration.integer, Value = int.Parse(sdata) } :
                (datatype == "http://www.w3.org/2001/XMLSchema#date" ?
                    new Literal() { Vid = LiteralVidEnumeration.date, Value = DateTime.Parse(sdata).ToBinary() } :
                (new Literal() { Vid = LiteralVidEnumeration.text, Value = new Text() { Value = sdata, Lang = "en" } }));
            long off = g.AddLiteral(lit);
            this.ocode = off;
        }
        internal TLiteralNode(long code, TGraph g)
        {
            this.g = g;
            this.ocode = code;
        }
        private object pvalue = null;
        private void GetPValue()
        {
            pvalue = g.GetLiteralPValue(ocode);
        }
        public Uri DataType { get { return null; } }
        public string Language { get { return null; } }
        public string Value { get { return "literalvalue"; } }
    }
}
