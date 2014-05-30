using System;
using System.Collections.Generic;
using System.Globalization;
using PolarDB;

namespace TrueRdfViewer
{
    public enum LiteralVidEnumeration { typedObject, integer, text, date, boolean, nil }

    public class LiteralStore
    {
        static PType tp_rliteral = new PTypeUnion(
                      new NamedType("void", new PType(PTypeEnumeration.none)),
                      new NamedType("integer", new PType(PTypeEnumeration.real)),
                      new NamedType("string", new PTypeRecord(
                          new NamedType("s", new PType(PTypeEnumeration.sstring)),
                          new NamedType("l", new PType(PTypeEnumeration.sstring)))),
                      new NamedType("date", new PType(PTypeEnumeration.longinteger)),
                      new NamedType("bool", new PType(PTypeEnumeration.boolean)),
                      new NamedType("typedObject", new PTypeRecord(
                          new NamedType("s", new PType(PTypeEnumeration.sstring)),
                          new NamedType("t", new PType(PTypeEnumeration.sstring)))));
        private static PType tp_data_seq = new PTypeSequence(tp_rliteral);
        public PaCell dataCell;
        private List<Literal> writeBuffer;
        public string pataCellPath;
        public static LiteralStore Literals;
        public LiteralStore(string path)
        {
            pataCellPath = path + "data.pac";              
        }

        public void Open(bool readOnlyMode)
        {
            dataCell = new PaCell(tp_data_seq, pataCellPath, readOnlyMode);
        }

        public void WarmUp()
        {
            foreach (var t in dataCell.Root.ElementValues()) ;
        }

        public Literal Read(long offset)
        {
            var paEntry = dataCell.Root.Element(0);
            paEntry.offset = offset;
            return ToLiteral((object[])paEntry.Get());
        }

        public Literal Write(Literal literal)
        {
            if (writeBuffer == null) writeBuffer = new List<Literal>();
            writeBuffer.Add(literal);
            if (writeBuffer.Count >= 1000)
                WriteBufferForce();
            return literal;
        }

        public void WriteBufferForce()
        {
            foreach (var lit in writeBuffer)
            {
                var da = ToObjects(lit);
                //в памяти сам литерал уже не хранится
                lit.Value = null;
                lit.Offset = dataCell.Root.AppendElement(da);
            }
            writeBuffer.Clear();
            dataCell.Flush();
        }

        public static object[] ToObjects(Literal lit)
        {
            object[] da;
            switch (lit.vid)
            {
                case LiteralVidEnumeration.integer:
                    da = new object[] {1, lit.Value};
                    break;
                case LiteralVidEnumeration.date:
                    da = new object[] {3, lit.Value};
                    break;
                case LiteralVidEnumeration.boolean:
                    da = new object[] {4, lit.Value};
                    break;
                case LiteralVidEnumeration.text:
                {
                    Text t = (Text) lit.Value;
                    da = new object[] {2, new object[] {t.Value, t.Lang}};
                }
                    break;
                case LiteralVidEnumeration.typedObject:
                {
                    TypedObject t = (TypedObject) lit.Value;
                    da = new object[] {5, new object[] {t.Value, t.Type}};
                }
                    break;
                default:
                    da = new object[] {0, null};
                    break;
            }
            return da;
        }

        public static Literal ToLiteral(object[] uni)
        {
            switch ((int)uni[0])
            {
                case 1:
                    return new Literal(LiteralVidEnumeration.integer) { Value = Convert.ToDouble(uni[1]) };
                case 3:
                    return new Literal(LiteralVidEnumeration.date) { Value = (long)uni[1] };
                case 4:
                    return new Literal(LiteralVidEnumeration.boolean) { Value = (bool)uni[1] };
                case 5:
                {
                    object[] txt = (object[])uni[1];
                    return new Literal(LiteralVidEnumeration.typedObject)
                    {
                        Value = new TypedObject() { Value = (string)txt[0], Type = (string)txt[1] }
                    };
                }
                case 2:
                    object[] txt1 = (object[])uni[1];
                    return new Literal(LiteralVidEnumeration.text)
                    {
                        Value = new Text() { Value = (string)txt1[0], Lang = (string)txt1[1] }
                    };
                default:
                    return new Literal(LiteralVidEnumeration.nil);
            }
        }


    }

    public class Literal
    {
        public long Offset;

        protected bool Equals(Literal other)
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

        public readonly LiteralVidEnumeration vid;

        public string GetString()
        {
            switch (vid)
            {
                case LiteralVidEnumeration.typedObject:
                    return ((TypedObject)Value).Value;
                case LiteralVidEnumeration.text:
                    return ((Text)Value).Value;
                case LiteralVidEnumeration.date:
                    return ((DateTime)Value).ToString(CultureInfo.InvariantCulture);
                case LiteralVidEnumeration.integer:
                case LiteralVidEnumeration.boolean:
                    return Value.ToString();
                case LiteralVidEnumeration.nil:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public Literal(LiteralVidEnumeration vid)
        {
            this.vid = vid;
        }

        public Literal()
        {
            
        }

        public object Value { get; set; }

        public bool HasValue
        {
            get
            {
                return Value is Double && Value == (object)double.MinValue
                       || Value is long && (long)Value == DateTime.MinValue.ToBinary()
                       || Value is Text && !string.IsNullOrEmpty(((Text)Value).Value);
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Literal)obj);
        }

        public static Literal Create(string datatype, string sdata, string lang)
        {
            return (datatype == "http://www.w3.org/2001/XMLSchema#integer" ||
                    datatype == "http://www.w3.org/2001/XMLSchema#float" ||
                    datatype == "http://www.w3.org/2001/XMLSchema#double"
                ? new Literal(LiteralVidEnumeration.integer)
                {
                    Value = double.Parse(sdata, NumberStyles.Any)
                }
                : datatype == "http://www.w3.org/2001/XMLSchema#boolean"
                    ? new Literal(LiteralVidEnumeration.date) { Value = bool.Parse(sdata) }
                    : datatype == "http://www.w3.org/2001/XMLSchema#dateTime" ||
                      datatype == "http://www.w3.org/2001/XMLSchema#date"
                        ? new Literal(LiteralVidEnumeration.date) { Value = DateTime.Parse(sdata).ToBinary() }
                        : datatype == null ||
                          datatype == "http://www.w3.org/2001/XMLSchema#string"
                            ? new Literal(LiteralVidEnumeration.text)
                            {
                                Value = new Text() { Value = sdata, Lang = lang ?? string.Empty }
                            }
                            : new Literal(LiteralVidEnumeration.typedObject) { Value = new TypedObject() { Value = sdata, Type = datatype } });
        }

    }

}