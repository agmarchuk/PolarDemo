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
            new NamedType("start offset", new PType(PTypeEnumeration.longinteger)),new NamedType("count", new PType(PTypeEnumeration.integer))));
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
            int currentIdCode = 0;
            foreach (var entry in sourceCell.Root.Elements())
            {
                
            }
        }
    }
}
