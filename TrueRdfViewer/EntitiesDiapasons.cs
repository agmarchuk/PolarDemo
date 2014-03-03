using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;

namespace TrueRdfViewer
{
    class EntitiesMemoryDiapasons
    {
        public EntitiesMemoryDiapasons(Entities entities)
        {
            this.entities = entities;
        
        }
                                      
        private readonly Entities entities;
        private readonly Dictionary<int, Diapason> diapasonsTable;
        public PaEntry GetDiapason(int idCode)
        {
            
        }
    }
}
