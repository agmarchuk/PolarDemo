using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PolarDB;

namespace TrueRdfViewer
{
    public class PaTableHelper
    {
        public long lastElementIndex { get; set; }
        public IEnumerator<int> seq { get; set; }
        public int i { get; set; }
    }

    class Entities
    {
        public PType type;
        public PaCell EntitiesTable;
        private readonly PaKeyValueTable[] paKeyValueTables;

        public Entities(string directoryPath, params PaKeyValueTable[] paKeyValueTables)
        {
            var filePath = directoryPath + "entities diapasons.pac";
            if(File.Exists(filePath)) File.Delete(filePath);
            EntitiesTable =new PaCell( 
               new PTypeSequence(new PTypeRecord( 
                   Enumerable.Repeat(new NamedType("id code", new PType(PTypeEnumeration.integer)), 1)
                             .Union(
                             Enumerable.Range(0, paKeyValueTables.Length)
                                       .SelectMany(i => new []{
                                           new NamedType("start"+i, new PType(PTypeEnumeration.longinteger)),
                                           new NamedType("count"+i, new PType(PTypeEnumeration.longinteger))}))
                             .ToArray())),
                 filePath, false);                                             
            this.paKeyValueTables = paKeyValueTables;
        }

        public void Load()
        {
            EntitiesTable.Clear();
            EntitiesTable.Fill(new object[0]);

            var paEntries = new LinkedList<PaTableHelper>(
                Enumerable.Range(0, paKeyValueTables.Length)
                          .Where(i1 => !paKeyValueTables[i1].sourceCell.IsEmpty)
                          .Select(j =>
                            new PaTableHelper
                            {
                                lastElementIndex = 0,
                                seq =
                                    paKeyValueTables[j].sourceCell.Root.Elements()
                                        .Select(paKeyValueTables[j].keyProducer)
                                        .GetEnumerator(),
                                i = j
                            })
                            //двигаемся к первому, заполняем seq.Current
                          .Where(paEntry => paEntry.seq.MoveNext())); 
            while (paEntries.Count > 0)
            {
                var diapasons = new Diapason[paKeyValueTables.Length];
                // выбирается наименьший
                int  currentIdCode = paEntries.Min(arg => arg.seq.Current);
               //цикл только по потокам с наименьшим текущим
                foreach (var paEntry in paEntries.Where(paEntry => paEntry.seq.Current == currentIdCode).ToArray())
                {
                    diapasons[paEntry.i].start = paEntry.lastElementIndex;
                    // первый уже пройден
                    long count = 1;
                    bool notEmpty;
                    while ((notEmpty = paEntry.seq.MoveNext()) && paEntry.seq.Current == currentIdCode)
                        count++;
                    paEntry.lastElementIndex += count;
                    diapasons[paEntry.i].numb = count;
                    //Больше элементов не осталось
                    if (!notEmpty) paEntries.Remove(paEntry);
                }                                 
                EntitiesTable.Root.AppendElement( 
                    Enumerable.Repeat((object) currentIdCode, 1)
                            .Concat(diapasons
                            .SelectMany(d => new object[] {d.start, d.numb}))
                            .ToArray());   
            }
        }
    }
}