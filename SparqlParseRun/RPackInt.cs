using System;
using RDFStores;
using TripleIntClasses;

namespace SparqlParseRun
{
    public class RPackInt : ICloneable
    {
        //public bool result;
        public object[] row;
        private readonly RDFIntStoreAbstract ts;
        public RDFIntStoreAbstract StoreAbstract { get { return ts; } }

        public RPackInt(object[] row, RDFIntStoreAbstract ts)
        {
            this.row = row;
            this.ts = ts;
        }
        public string Get(object si)
        {
            if (!(si is short)) 
                return ts.EntityCoding.GetName((int)si);
            var index = (short)si;
            var literal = (row[index] as Literal);
            if (literal != null) return literal.ToString();
            else return ts.EntityCoding.GetName((int)row[index]);
        }

        public int GetE(object si)
        {
            return si is short ? (int)row[(short)si] : (int)si;
        }

        public bool Hasvalue(short si)
        {
            return
                (row[si] is int && row[si] != (object)Int32.MinValue)
                || (row[si] is Literal &&
                    ((Literal)row[si]).HasValue);

        }
        public Literal Val(short ind)
        {
            return (Literal)row[ind];
        }
        public double Vai(short ind)
        {
            Literal lit = (Literal)row[ind];
            //if (lit.vid != LiteralVidEnumeration.integer) throw new Exception("Wrong literal vid in Vai method");
            return (double)lit.Value;
        }
        public void Set(object si, object valu)
        {
            if (!(si is short)) throw new Exception("argument must be an index");
            short ind = (short)si;
            row[ind] = valu;
        }

        public RPackInt ResetDiapason(short parametersStartIndex, short parametersEndIndex)
        {
            for (short i = parametersStartIndex; i < parametersEndIndex; i++)
                Reset(i);
            return this;
        }
        public void Reset(short si)
        {
            if (row[si] == null) return;
            if (row[si] is int)
                row[si] = int.MinValue;
            else
            {
                var oldliteral = row[si] as Literal;
                if (oldliteral == null) throw new Exception(); //return;
                var newLiteral = new Literal(oldliteral.vid);
                row[si] = newLiteral;
                switch (oldliteral.vid)
                {
                    case LiteralVidEnumeration.integer:
                        newLiteral.Value = 0.0;
                        break;
                    case LiteralVidEnumeration.typedObject:
                        newLiteral.Value = new TypedObject();
                        break;
                    case LiteralVidEnumeration.text:
                        newLiteral.Value = new Text { };
                        break;
                    case LiteralVidEnumeration.date:
                        newLiteral.Value = DateTime.MinValue.ToBinary();
                        break;
                    case LiteralVidEnumeration.boolean:
                        newLiteral.Value = false;
                        break;
                    case LiteralVidEnumeration.nil:
                        break;
                }
            }
        }

        public object this[short index]
        {
            get { return row[index]; }
            set
            {
                row[index] = value;
            }
        }
        object ICloneable.Clone()
        {
            var newRow = new object[row.Length];
            for (int i = 0; i < newRow.Length; i++)
            {
                var literal = row[i] as Literal;
                if (literal != null)
                {
                    var value = literal.Value;
                    if (value is Text)
                        newRow[i] = new Literal(literal.vid) { Value = ((ICloneable)value).Clone() };
                    else if (value is TypedObject)
                        newRow[i] = new Literal(literal.vid) { Value = ((ICloneable)value).Clone() };
                    else
                        newRow[i] = new Literal(literal.vid) { Value = value };
                }
                else
                    newRow[i] = row[i];
            }
            return new RPackInt(newRow, ts);
        }
    }
}