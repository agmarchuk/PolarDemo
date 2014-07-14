using System;
using System.Globalization;
using RdfInMemory;

namespace TripleIntClasses
{
    public class Literal : INode
    {     
        public long Offset;      
        public LiteralVidEnumeration vid;

        private bool Equals(Literal other)
        {
            return vid == other.vid && Equals(Value, other.Value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)vid * 397) ^ (Value != null ? Value.GetHashCode() : 0);
            }
        }


        public string GetString()
        {
            switch (vid)
            {
                case LiteralVidEnumeration.typedObject:
                    return ((TypedObject)Value).Value;
                case LiteralVidEnumeration.text:
                    return ((Text)Value).Value;
                case LiteralVidEnumeration.date:
                    return DateTime.FromBinary((long) Value).ToString(CultureInfo.InvariantCulture);
                case LiteralVidEnumeration.integer:
                case LiteralVidEnumeration.boolean:
                    return Value.ToString();
                case LiteralVidEnumeration.nil:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public Literal(LiteralVidEnumeration vid, IGraph graph)
        {
            this.vid = vid;
            Graph = graph;
        }

        public object Value { get; set; }

        public bool HasValue
        {
            get
            {
                return Value is Double && Value == (object)Double.MinValue
                       || Value is long && (long)Value == DateTime.MinValue.ToBinary()
                       || Value is Text && !String.IsNullOrEmpty(((Text)Value).Value);
            }
        }

        public override string ToString()
        {
            return GetString();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Literal)obj);
        }      

        public static object[] ToObjects(Literal lit)
        {
            object[] da;
            switch (lit.vid)
            {
                case LiteralVidEnumeration.integer:
                    da = new object[] { 1, lit.Value };
                    break;
                case LiteralVidEnumeration.date:
                    da = new object[] { 3, lit.Value };
                    break;
                case LiteralVidEnumeration.boolean:
                    da = new object[] { 4, lit.Value };
                    break;
                case LiteralVidEnumeration.text:
                {
                    Text t = (Text)lit.Value;
                    da = new object[] { 2, new object[] { t.Value, t.Lang } };
                }
                    break;
                case LiteralVidEnumeration.typedObject:
                {
                    TypedObject t = (TypedObject)lit.Value;
                    da = new object[] { 5, new object[] { t.Value, t.Type } };
                }
                    break;
                default:
                    da = new object[] { 0, null };
                    break;
            }
            return da;
        }

        public static Literal ToLiteral(object[] uni, IGraph graph)
        {
            switch ((int)uni[0])
            {
                case 1:
                    return new Literal(LiteralVidEnumeration.integer, graph) { Value = Convert.ToDouble(uni[1]) };
                case 3:
                    return new Literal(LiteralVidEnumeration.date, graph) { Value = (long)uni[1] };
                case 4:
                    return new Literal(LiteralVidEnumeration.boolean, graph) { Value = (bool)uni[1] };
                case 5:
                {
                    object[] txt = (object[])uni[1];
                    return new Literal(LiteralVidEnumeration.typedObject, graph)
                    {
                        Value = new TypedObject() { Value = (string)txt[0], Type = (string)txt[1] }
                    };
                }
                case 2:
                    object[] txt1 = (object[])uni[1];
                    return new Literal(LiteralVidEnumeration.text, graph)
                    {
                        Value = new Text() { Value = (string)txt1[0], Lang = (string)txt1[1] }
                    };
                default:
                    return new Literal(LiteralVidEnumeration.nil, graph);
            }
        }

        public NodeType NodeType { get{return NodeType.Literal;} }
        public IGraph Graph { get; private set; }
    }

}