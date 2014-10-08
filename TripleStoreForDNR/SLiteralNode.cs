using System;

namespace TripleStoreForDNR
{
    public class SLiteralNode : ILiteralNode, IComparable, IEquatable<SLiteralNode>
    {
        private SGraph g;

        public IGraph Graph
        {
            get { return g; }
        }


        public NodeType NodeType
        {
            get { return NodeType.Literal; }
        }

        private long ocode;
        private readonly dynamic value;

        public long Code
        {
            get { return ocode; }
        }

        public SLiteralNode(dynamic value, string lang, Uri type, SGraph graph)
        {
            
            this.Language = lang;
            this.dataType = type;
            this.g = graph;
            if (lang != null)
            {
                if (dataType == null)
                    dataType = XmlSchema.XMLSchemaLangString;
                if (dataType != XmlSchema.XMLSchemaLangString)
                    throw new Exception();
                LiteralType = LiteralTypeEnum.langString;
            }
            if(value==null) throw new Exception();
            if(value is string)
            {
                if (type == null)
                {
                    this.value = value;
                    LiteralType = LiteralTypeEnum.@string;
                }
                else if (type.AbsoluteUri == XmlSchema.XMLSchemaInteger.AbsoluteUri)
                {
                    LiteralType=LiteralTypeEnum.@int;
                    this.value = int.Parse(value);
                }
            else if (type.AbsoluteUri == XmlSchema.XMLSchemaFloat.AbsoluteUri)
                {
                    LiteralType = LiteralTypeEnum.@float;
                    this.value = float.Parse(value);
                }
                else if (type.AbsoluteUri == XmlSchema.XMLSchemaDouble.AbsoluteUri)
                {
                    LiteralType = LiteralTypeEnum.@double;
                    this.value = double.Parse(value);
                }
                else if (type.AbsoluteUri == XmlSchema.XMLSchemaDate.AbsoluteUri)
                {
                    LiteralType = LiteralTypeEnum.@date;
                    this.value = DateTime.Parse(value);
                }
                else if (type.AbsoluteUri == XmlSchema.XMLSchemaDateTime.AbsoluteUri)
                {
                    LiteralType = LiteralTypeEnum.@dateTime;
                    this.value = DateTime.Parse(value);
                }
                else if (type.AbsoluteUri == XmlSchema.XMLSchemaBool.AbsoluteUri)
                {
                    LiteralType = LiteralTypeEnum.boolean;
                    this.value = bool.Parse(value);
                }
            
            return;}
            this.value = value;        
            
        }

        internal SLiteralNode(string sdata, SGraph g)
        {
            this.g = g;
            // Последняя двойная кавычка 
            int lastqu = sdata.LastIndexOf('\"');
            if (lastqu != -1)
            {
                // Значение данных
                 sdata = sdata.Substring(1, lastqu - 1);
            }

            // Языковый специализатор:
            int dog = sdata.LastIndexOf('@');
            string lang = "";
            if (dog == lastqu + 1) lang = sdata.Substring(dog + 1, sdata.Length - dog - 1);

            string datatype = "";
            int pp = sdata.IndexOf("^^");
            if (pp == lastqu + 1)
            {
                //  Тип данных
                string qname = sdata.Substring(pp + 2);
                //  тип данных может быть "префиксным" или полным

                datatype = g.GetEntityString(qname);
                dataType = new Uri(datatype);
            }

            long off = g.AddLiteral(ToObjects(sdata, dataType, lang));
            this.ocode = off;
        }

        internal SLiteralNode(long code, SGraph g)
        {
            this.g = g;
            this.ocode = code;
        }

        public enum LiteralTypeEnum
        {
            nil,
            @int,
            @float,
            @double,
            boolean,
            date,
            dateTime,
            @string,
            langString,
            otherType
        }

        public SLiteralNode(object @object, SGraph graph)
        {
            this.g = graph;
            var objectPresent = ((object[]) @object);
            LiteralType = (LiteralTypeEnum) (int) objectPresent[0];
            switch (LiteralType)
            {
                case LiteralTypeEnum.@int:
                    value = (int) objectPresent[1];
                    dataType = XmlSchema.XMLSchemaInteger;
                    break;
                case LiteralTypeEnum.@float:
                    value = (float) objectPresent[1];
                    dataType = XmlSchema.XMLSchemaFloat;
                    break;
                case LiteralTypeEnum.@double:
                    value = (double) objectPresent[1];
                    dataType = XmlSchema.XMLSchemaDouble;
                    break;
                case LiteralTypeEnum.boolean:
                    value = (bool) objectPresent[1];
                    dataType = XmlSchema.XMLSchemaBool;
                    break;
                case LiteralTypeEnum.date:
                    value = (DateTime.FromBinary((long) objectPresent[1]));
                    dataType = XmlSchema.XMLSchemaDate;
                    break;
                case LiteralTypeEnum.dateTime:
                    value = (DateTime.FromBinary((long) objectPresent[1]));
                    dataType = XmlSchema.XMLSchemaDateTime;
                    break;
                case LiteralTypeEnum.@langString:
                    objectPresent = (object[]) objectPresent[1];
                    value = (string) objectPresent[0];
                    Language = (string) objectPresent[1];
                    dataType = XmlSchema.XMLSchemaLangString;
                    break;
                case LiteralTypeEnum.@string:
                    value = (string) objectPresent[1];
                    dataType = XmlSchema.XMLSchemaString;
                    break;
                case LiteralTypeEnum.otherType:
                    objectPresent = (object[]) objectPresent[1];
                    value = (string) objectPresent[0];
                    dataType = new Uri((string) objectPresent[1]);
                    break;
                case LiteralTypeEnum.nil:
                default:
                    throw new NotImplementedException();
            }
        }

        private object ToObjects(string value, Uri datatype, string lang)
        {
            if (!string.IsNullOrWhiteSpace(lang))
                return new object[] {(int) LiteralTypeEnum.@langString, new object[] {value, lang}};
            if (datatype != null)
            {
                if (datatype.AbsoluteUri == XmlSchema.XMLSchemaInteger.AbsoluteUri)
                    return new object[] {(int) LiteralTypeEnum.@int, Int32.Parse(value)};
                if (datatype.AbsoluteUri == XmlSchema.XMLSchemaFloat.AbsoluteUri)
                    return new object[] {(int) LiteralTypeEnum.@float, float.Parse(value)};
                if (datatype.AbsoluteUri == XmlSchema.XMLSchemaFloat.AbsoluteUri)
                    return new object[] {(int) LiteralTypeEnum.@double, double.Parse(value)};
                if (datatype.AbsoluteUri == XmlSchema.XMLSchemaBool.AbsoluteUri)
                    return new object[] {(int) LiteralTypeEnum.boolean, bool.Parse(value)};
                if (datatype.AbsoluteUri == XmlSchema.XMLSchemaDate.AbsoluteUri)
                    return new object[] {(int) LiteralTypeEnum.date, DateTime.Parse(value).Ticks};
                if (datatype.AbsoluteUri == XmlSchema.XMLSchemaDateTime.AbsoluteUri)
                    return new object[] {(int) LiteralTypeEnum.dateTime, DateTime.Parse(value).Ticks};
                if (datatype.AbsoluteUri == XmlSchema.XMLSchemaString.AbsoluteUri)
                    return new object[] {(int) LiteralTypeEnum.@string, value};
                if (datatype.AbsolutePath == "http://www.w3.org/2001/XMLSchema#langString")
                    throw new NotImplementedException();
                //  return new object[] { (int)LiteralType.@string, new object[] { value, "вуафгде" } };     
                return new object[] {(int) LiteralTypeEnum.otherType, new object[] {value, datatype.ToString()}};
            }
            int intValue;
            if (int.TryParse(value, out intValue))
                return new object[] {(int) LiteralTypeEnum.@int, intValue};
            float floatValue;
            if (float.TryParse(value, out floatValue))
                return new object[] {(int) LiteralTypeEnum.@float, floatValue};
            double dv;
            if (double.TryParse(value, out dv))
                return new object[] {(int) LiteralTypeEnum.@double, dv};
            bool bv;
            if (bool.TryParse(value, out bv))
                return new object[] {(int) LiteralTypeEnum.boolean, bv};
            DateTime dtv;
            if (DateTime.TryParse(value, out dtv))
                return new object[] {(int) LiteralTypeEnum.dateTime, dtv.Ticks};
            return new object[] {(int) LiteralTypeEnum.@string, value};

            //     return new object[] {0, null};
        }

        private Uri dataType;

        public Uri DataType
        {
            get { return dataType; }
        }

        public string Language { get; set; }

        public string Value
        {
            get { return value.ToString(); }
        }

        private LiteralTypeEnum LiteralType { get; set; }


        public static SLiteralNode operator +(SLiteralNode l1, SLiteralNode l2)
        {
            //todo
            return new SLiteralNode(l1.value + l2.value, null, null, l1.g);
        }

        public static SLiteralNode operator -(SLiteralNode l1, SLiteralNode l2)
        {
            //todo
            return new SLiteralNode(l1.value - l2.value, null, null, l1.g);
        }
        

        public static bool operator <(SLiteralNode l1, SLiteralNode l2)
        {
            //todo
            switch (l1.LiteralType)
            {
                case LiteralTypeEnum.dateTime:
                case LiteralTypeEnum.date:
                case LiteralTypeEnum.@int:
                case LiteralTypeEnum.@float:
                case LiteralTypeEnum.@double:
                    return l1.value < l2.value;
                case LiteralTypeEnum.boolean:
                    return l1.value == false && l2.value == true;
                case LiteralTypeEnum.@langString:
                case LiteralTypeEnum.@string:
                case LiteralTypeEnum.otherType:
                default:
                    return StringComparer.InvariantCulture.Compare(l1.value, l2.value) == -1;
            }
        }

        public static bool operator >(SLiteralNode l1, SLiteralNode l2)
        {
            //todo
            return l1.value > l2.value;
        }
        public static bool operator <=(SLiteralNode l1, SLiteralNode l2)
        {
            //todo
            return l1.value <= l2.value;
        }

        public static bool operator >=(SLiteralNode l1, SLiteralNode l2)
        {
            return l1.value >= l2.value;
        }

        public int CompareTo(object obj)
        {
            bool @equals = obj is SLiteralNode && Equals((SLiteralNode)obj);
            return @equals ? 0 : this < (SLiteralNode) (obj) ? -1 : 1;
        }


        public bool Equals(SLiteralNode other)
        {
            return value== other.value && Language==other.Language && DataType==other.DataType;
        }

        public override int GetHashCode()
        {
            int code=value.GetHashCode();
            if (Language != null)
                code ^= Language.GetHashCode();
            if (DataType != null)
                code ^= DataType.GetHashCode();
            return code;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
