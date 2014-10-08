using PolarDB;
using TripleIntClasses;

namespace LiteralStores
{
    public class LiteralStore   :LiteralStoreAbstract
    {
        private static readonly PType tp_rliteral = new PTypeUnion(
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
        private static readonly PType tp_data_seq = new PTypeSequence(tp_rliteral);
        public PaCell dataCell;      
     

        public LiteralStore(string path, NameSpaceStore nameSpaceStore) : base(path,nameSpaceStore)
        {
            dataCellPath = path + "data.pac";      
            dataCell = new PaCell(tp_data_seq, dataCellPath, false);
            if (dataCell.IsEmpty)
                dataCell.Fill(new object[0]);
        }   
        
      

        public override void Clear()
        {
            dataCell.Clear();
            dataCell.Fill(new object[0]);
        }

        public override void WarmUp()
        {
            foreach (var t in dataCell.Root.ElementValues()) ;
        }

        public override Literal Read(long offset, LiteralVidEnumeration? vid)
        {               
            var paEntry = dataCell.Root.Element(0);
            paEntry.offset = offset;
            return Literal.ToLiteral((object[])paEntry.Get(), null);
        }

        public override Literal Write(Literal literal)
        {
            var da = Literal.ToObjects(literal);
            //в памяти сам литерал уже не хранится
            literal.Value = null;
            literal.Offset = dataCell.Root.AppendElement(da);
            return literal;
        }

        public override void Flush()
        {
            dataCell.Flush();
        }   
       
    }
}