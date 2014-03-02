using System;
using PolarDB;

namespace TrueRdfViewer
{
    class EntitiesDiapasons
    {
        public EntitiesDiapasons(Entities entities)
        {
            _entities = entities;
        }


        private static short bytesPerHash = 22, hashShift=(short)(32-bytesPerHash);
        int Hash(int code)
        {
            return code >> hashShift;
        }

        DiapasonShot[] hashIndex=new DiapasonShot[(int)Math.Pow(2,bytesPerHash)];
        private readonly Entities _entities;

        public PaEntry GetDiapason(int idCode)
        {
            var hashDiapason = hashIndex[Hash(idCode)];
            if (hashDiapason.Numb == 0) return PaEntry.Empty;
            return _entities.EntitiesTable.Root.BinarySearchFirst(hashDiapason.Start, Convert.ToInt64(hashDiapason.Numb),
                entry => (int) entry.Field(0).Get() - idCode);
        }
    }

    struct DiapasonShot
    {
        public int Start;
        public short Numb;

        public DiapasonShot(int start, short numb)
        {
            Start = start;
            Numb = numb;
        }
    }
}
