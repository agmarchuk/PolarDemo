using System;
using PolarDB;

namespace TrueRdfViewer
{
    class PaKeyValueTable
    {
        public readonly PaCell sourceCell;
        public readonly Func<PaEntry, int> keyProducer;

        public PaKeyValueTable(PaCell sourceCell, Func<PaEntry, int> keyProducer)
        {
            this.sourceCell = sourceCell;
            this.keyProducer = keyProducer;
        }

        public int GetKey(long elementIndex)
        {
            return keyProducer(sourceCell.Root.Element(elementIndex));
        }
    }
}