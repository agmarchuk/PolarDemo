using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfInMemoryCopy
{
    public class SLiteralNode : ILiteralNode
    {
        private SGraph g;
        public IGraph Graph { get { return g; } }
        public NodeType NodeType { get { return NodeType.Literal; } }
        private long ocode;
        private string value;
        public long Code { get { return ocode; } }
        internal SLiteralNode(string rest_line, SGraph g)
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
                    datatype = TurtleParser.GetEntityString(g, qname);
                }
            }
        
            long off = g.AddLiteral(ToObjects(sdata,  datatype, lang));
            this.ocode = off;
        }
        internal SLiteralNode(string value, string lang)
        {
            throw new NotImplementedException();
        } 
        internal SLiteralNode(long code, SGraph g)
        {
            this.g = g;
            this.ocode = code;
        }
        public object ToObjects(string value, string datatype, string lang)
        {
            //double dv;
            //if (double.TryParse(value, out dv))
            //    return new object[] { 1, dv };
            //DateTime dtv;
            //if (DateTime.TryParse(value, out dtv))
            //    return new object[] { 3, dtv };
            //bool bv;
            //if (bool.TryParse(value, out bv))
            //    return new object[] { 4, bv };
            if (!string.IsNullOrWhiteSpace(lang))
                return new object[] { 6, new object[] { value, lang } };
            if (!string.IsNullOrWhiteSpace(datatype))
            {
                string fulllDateType = datatype;// TurtleParser.GetEntityString(Graph, datatype);
                if (fulllDateType == "http://www.w3.org/2001/XMLSchema#integer")
                    return new object[] { 1, Int32.Parse(value) };
                if (fulllDateType == "http://www.w3.org/2001/XMLSchema#float")
                    return new object[] { 2, float.Parse(value) };
                if (fulllDateType == "http://www.w3.org/2001/XMLSchema#double")
                    return new object[] { 3, double.Parse(value) };
                if (fulllDateType == "http://www.w3.org/2001/XMLSchema#boolean")
                    return new object[] { 4, bool.Parse(value) };
                if (fulllDateType == "http://www.w3.org/2001/XMLSchema#date")
                    return new object[] { 5, DateTime.Parse(value).Ticks };
                if (fulllDateType == "http://www.w3.org/2001/XMLSchema#dateTime")
                    return new object[] { 5, DateTime.Parse(value).Ticks };
             //   if (fulllDateType == "http://www.w3.org/2001/XMLSchema#string")
               //     return new object[] { 6, new object[]{ value, "en"} };

                    return new object[] { 7, new object[] { value, datatype.ToString() } };
            }
            return new object[] { 0, null };
        }

    
public Uri DataType
{
	get { throw new NotImplementedException(); }
}

public string Language
{
    get { throw new NotImplementedException(); }
}

public string Value
{
    get { throw new NotImplementedException();  }
}
}
}
