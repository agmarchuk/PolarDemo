using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TrueRdfViewer
{
    class EntitiesDiapasons
    {
        public PType type=new PTypeSequence(new PTypeRecord(new NamedType("id code", new PType(PTypeEnumeration.integer)),
            new NamedType("start offset", new PType(PTypeEnumeration.longinteger)),new NamedType("count", new PType(PTypeEnumeration.longinteger))));
       public PaCell EntitiesTable;
        private readonly PaCell sourceCell;
        private readonly Func<PaEntry, int> keyProducer;

        public EntitiesDiapasons(string directoryPath, PaCell sourceCell, Func<PaEntry, int> keyProducer)
        {
            this.sourceCell = sourceCell;
            this.keyProducer = keyProducer;
            EntitiesTable = new PaCell(type, directoryPath + "entities diapasons.pac", false);
        }

        public void Load()
        {
            EntitiesTable.Clear();
            EntitiesTable.Fill(null);
            int currentIdCode = Int32.MinValue, currentCount = 0;
            long currentIdOffset = 0;
            bool any = false;
            hashIndex = new DiaposonShot[(int)Math.Pow(2, bytesPerHash)];
            foreach (var entry in sourceCell.Root.Elements())
            {
                var idCode = keyProducer(entry);
                if (idCode == currentIdCode) currentCount++;
                else
                {
                    if (any)
                    {
                      long offsetOnEntity =  EntitiesTable.Root.AppendElement(new object[] {currentIdCode, currentIdOffset, currentCount});
                        var hashe = Hashe(currentIdCode);
                        if (hashIndex[hashe].Numb == 0)
                            hashIndex[hashe].Start = offsetOnEntity;
                        hashIndex[hashe].Numb++;
                    }
                    else
                        any = true;

                    currentIdCode = idCode;
                    currentIdOffset = entry.offset;
                    currentCount = 1;
                }
            }
            if (any)
                EntitiesTable.Root.AppendElement(new object[] {currentIdCode, currentIdOffset, currentCount});

        }


        private static short bytesPerHash = 22, hashShift=(short)(32-bytesPerHash);
        int Hashe(int code)
        {
            return code >> hashShift;
        }

        DiaposonShot[] hashIndex=new DiaposonShot[(int)Math.Pow(2,bytesPerHash)];
        public Diapason GetDiapason(int idCode)
        {
            var hashDiapason = hashIndex[Hashe(idCode)];
            if (hashDiapason.Numb == 0) return Diapason.Empty;
           PaEntry entryRow= EntitiesTable.Root.BinarySearchFirst(hashDiapason.Start, Convert.ToInt64(hashDiapason.Numb),
                entry => (int) entry.Field(0).Get() - idCode);
           return new Diapason{ start = (long)entryRow.Field(1).Get(), numb = (long)entryRow.Field(2).Get()};
        }
    }

    struct DiaposonShot
    {
        public long Start;
        public short Numb;

        public DiaposonShot(long start, short numb)
        {
            Start = start;
            Numb = numb;
        }
    }
}
