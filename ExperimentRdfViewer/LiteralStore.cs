using System.Collections.Generic;
using PolarDB;
using TripleIntClasses;

namespace TrueRdfViewer
{
    public class LiteralStore
    {
        public static PType tp_rliteral = new PTypeUnion(
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
            return Literal.ToLiteral((object[])paEntry.Get());
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
                var da = Literal.ToObjects(lit);
                //в памяти сам литерал уже не хранится
                lit.Value = null;
                lit.Offset = dataCell.Root.AppendElement(da);
            }
            writeBuffer.Clear();
            dataCell.Flush();
        }
    }
}