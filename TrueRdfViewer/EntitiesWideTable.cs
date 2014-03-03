using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TrueRdfViewer
{
    public class EntitiesWideTable
    {
        private PaCell ewtable;
        public PaCell EWTable { get { return ewtable; } }
        private DiapasonScanner<int>[] scanners;
        public EntitiesWideTable(string path, DiapasonScanner<int>[] scanners) 
        {
            this.scanners = scanners;
            PType DiaRec = new PTypeRecord(
                new NamedType("start", new PType(PTypeEnumeration.longinteger)),
                new NamedType("number", new PType(PTypeEnumeration.longinteger)));
            PType tp = new PTypeSequence(new PTypeRecord(
                new NamedType("entity", new PType(PTypeEnumeration.integer)),
                new NamedType("spo", DiaRec),
                new NamedType("spo_op", DiaRec),
                new NamedType("spd", DiaRec)));
            ewtable = new PaCell(tp, path + "ewtable.pac", false); 
        }
        public void Load()
        {
            ewtable.Clear();
            ewtable.Fill(new object[0]);

            while (NotFinished(scanners))
            {
                int key = Least(scanners);
                Diapason[] diaps = Enumerable.Repeat<Diapason>(new Diapason() { start = 0L, numb = 0L }, 3).ToArray();
                object[] pval = new object[4];
                pval[0] = key;
                for (int ind = 0; ind < 3; ind++)
                {
                    if (scanners[ind].HasValue && scanners[ind].KeyCurrent == key)
                    {
                        Diapason di = scanners[ind].Scan();
                        diaps[ind] = di;
                    }
                    pval[ind + 1] = new object[] { diaps[ind].start, diaps[ind].numb };
                }
                ewtable.Root.AppendElement(pval);
            }
            ewtable.Flush();
        }
        public static bool NotFinished(DiapasonScanner<int>[] scanners)
        {
            var query = scanners.Any(ds => ds.HasValue);
            return query;
        }
        public static int Least(DiapasonScanner<int>[] scanners)
        {
            var keyCurrent = scanners
                .Where(ds => ds.HasValue)
                .Aggregate((ds1, ds2) => ds1.KeyCurrent < ds2.KeyCurrent ? ds1 : ds2).KeyCurrent;
            return keyCurrent;    
        }
    }
}
