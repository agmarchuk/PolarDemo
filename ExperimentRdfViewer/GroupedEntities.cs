using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;

namespace TrueRdfViewer
{
    public class GroupedEntities
    {
        private string path;
        private PaCell getable;
        private PType tGETable;
        public GroupedEntities(string path)
        {
            this.path = path;
            PType tDiapason = new PTypeRecord(
                new NamedType("start", new PType(PTypeEnumeration.longinteger)),
                new NamedType("number", new PType(PTypeEnumeration.longinteger)));
            PType tDiapLinks = new PTypeRecord(
                new NamedType("all", tDiapason),
                new NamedType("predList", new PTypeSequence(new PTypeRecord(
                    new NamedType("pred", new PType(PTypeEnumeration.integer)),
                    new NamedType("diap", tDiapason)))));
            this.tGETable = new PTypeSequence(new PTypeRecord(
                new NamedType("entity", new PType(PTypeEnumeration.integer)),
                new NamedType("spo", tDiapLinks),
                new NamedType("spo_op", tDiapLinks),
                new NamedType("spd", tDiapLinks)));
            this.getable = new PaCell(tGETable, path + "getable.pac", false);
        }
        public void ConstructGroupedEntities(DiapLinksScanner[] scanners)
        {
            getable.Clear();
            getable.Fill(new object[0]);
            //foreach (var scanner in scanners) scanner.Start();
            while (scanners.Any(dls => dls.HasValue))
            {
                int key = scanners.Where(dls => dls.HasValue).Select(dls => dls.KeyCurrent).Min(); //Least(scanners);
                DiapLinks[] diaplinks = new DiapLinks[3];
                object[] pval = new object[4];
                pval[0] = key;
                for (int ind = 0; ind < 3; ind++)
                {
                    DiapLinks di;
                    if (scanners[ind].HasValue && scanners[ind].KeyCurrent == key)
                    {
                        di = scanners[ind].Scan();
                        diaplinks[ind] = di;
                    }
                    else di = new DiapLinks();
                    pval[ind + 1] = di.ToPObject(); //diaplinks[ind].ToPObject();
                }
                getable.Root.AppendElement(pval);
            }
            getable.Close();
        }
        public Dictionary<int, object[]> GroupedEntitiesHash()
        {
            Dictionary<int, object[]> res = new Dictionary<int, object[]>();
            if (getable.IsEmpty) return res;
            getable.Root.Scan(row =>
            {
                object[] r = (object[])row;
                int e_code = (int)r[0];
                res.Add(e_code, r);
                return true;
            });
            return res;
        }
        public void CheckGroupedEntities()
        {
            // Количество сущностей
            Console.WriteLine("Количество сущностей {0}", getable.Root.Count());
            bool firsttime = true;
            int entity_code = Int32.MinValue;
            getable.Root.Scan(row =>
            {
                object[] r = (object[])row;
                // проверка на количество элементов в рядке
                if (r.Length != 4) Console.WriteLine("ERRROR 1");
                int e_code = (int)r[0];
                // проверка на рост сущностностного кода
                if (!firsttime && e_code <= entity_code) Console.WriteLine("ERRROR 2");
                entity_code = e_code;
                firsttime = false;
                // Выделение трех направлений
                for (int i = 1; i < 4; i++)
                {
                    object[] diapLinks = (object[])r[i];
                    CheckDiapLinks(diapLinks);
                }
                return true;
            });
            Console.WriteLine("CheckGroupedEntities ok.");
        }
        private static void CheckDiapLinks(object[] diapLinks)
        {
            if (diapLinks.Length != 2) Console.WriteLine("ERRROR 3");
            object[] all = (object[])diapLinks[0];
            long start = (long)all[0];
            long number = (long)all[1];
            object[] predList = (object[])diapLinks[1];
            if (number == 0 && predList.Length != 0) Console.WriteLine("Errror 4");
            if (number > 0)
            {
                long s = start;
                long n = 0;
                bool firsttime = true;
                int predicate = Int32.MaxValue;
                foreach (object[] preddiap in predList)
                {
                    int pred = (int)preddiap[0];
                    // проверка, что предикат не совпадает и растет
                    if (!firsttime && pred <= predicate) Console.WriteLine("Errror 5");
                    firsttime = false;
                    predicate = pred;
                    object[] diap = (object[])preddiap[1];
                    long st = (long)diap[0];
                    long nu = (long)diap[1];
                    if (st != s) Console.WriteLine("Errror 6");
                    s = st + nu;
                    n += nu;
                }
                if (n != number) Console.WriteLine("Errror 6");
            }
        }
    }
}
