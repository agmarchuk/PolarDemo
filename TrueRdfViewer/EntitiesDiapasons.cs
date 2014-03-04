using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;

namespace TrueRdfViewer
{
    class EntitiesMemoryHashTable
    {
private EntitiesWideTable entites;

        public EntitiesMemoryHashTable(EntitiesWideTable entites)
        {
            this.entites = entites;
            count = entites.EWTable.Root.Count();
        }

        private long minValueShift;
        private long count;
        public int shift=3;
        public void Load()
        {
            var groupBy = entites.EWTable.Root.Elements()
                .GroupBy(entry => GetHash((int) entry.Field(0).Get()));
            Console.WriteLine("hash-values count = " + groupBy.Count());
            Console.WriteLine("max elements count in hash-value = " + groupBy.Max(entries => entries.Count()));
        }

        public int GetHash(int source)
        {
            var hash = source >> shift;
            return hash;
        }
    }
}
