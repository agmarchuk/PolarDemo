using System;
using RdfInMemory;
using RDFStores;
using TripleIntClasses;
using Literal = TripleIntClasses.Literal;
using LiteralVidEnumeration = TripleIntClasses.LiteralVidEnumeration;
using Text = TripleIntClasses.Text;

namespace SparqlParseRun
{
    public class QueryNodesSet : ICloneable
    {
        //public bool result;
        public INode[] row;
        private readonly RDFIntStoreAbstract ts;

        public QueryNodesSet(INode[] row)
        {
            this.row = row;
        
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
     

        public QueryNodesSet ResetDiapason(short parametersStartIndex, short parametersEndIndex)
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

        public INode this[short index]
        {
            get { return row[index]; }
            set
            {
                row[index] = value;
            }
        }
        object ICloneable.Clone()
        {
            var newRow = new INode[row.Length];
            for (int i = 0; i < newRow.Length; i++)
            {
                if (row[i] != null)
                    newRow[i] = (INode) row[i].Clone();
            }
            return new QueryNodesSet(newRow);
        }
    }
}