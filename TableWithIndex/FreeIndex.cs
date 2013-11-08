using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TableWithIndex
{
    /// <summary>
    /// Класс индексирует колонку i_field в таблицы, на основе которой индекс построен. Предполагается, что нулевая колонка 
    /// таблиы - признак deleted, фиксирующий уничтожение записи из таблицы. deleted=false - запись не уничтожена. 
    /// </summary>
    public class FreeIndex
    {
        private PaEntry table;
        private int i_field;
        private PaCell index_cell;
        private PType columnType;
        /// <summary>
        /// Индекс конструируется для последовательности записей, расположенной в сяейке с  
        /// </summary>
        /// <param name="indexName">имя индекса вместе с путем в файловой системе</param>
        /// <param name="table">Таблица</param>
        /// <param name="i_field">номер индексируемой записи</param>
        public FreeIndex(string indexName, PaEntry table, int i_field)
        {
            this.table = table;
            this.i_field = i_field;
            index_cell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), indexName + ".pac", false);
            columnType = ((PTypeRecord)((PTypeSequence)table.Type).ElementType).Fields[i_field].Type;
        }
        public void Load()
        {
            index_cell.Clear();
            index_cell.Fill(new object[0]);
            foreach (var rec in table.Elements().Where(ent => (bool)ent.Field(0).Get() == false))
            {
                long offset = rec.offset;
                index_cell.Root.AppendElement(offset);
            }
            index_cell.Flush();
            if (index_cell.Root.Count() == 0) return; // потому что следующая операция не пройдет
            // Попробую сортировать index_cell специальным образом: значение (long) используется как offset ячейки и там прочитывается нулевое поле
            var ptr = table.Element(0);
            if (columnType.Vid == PTypeEnumeration.integer)
            {
                index_cell.Root.SortByKey<int>((object v) =>
                {
                    ptr.offset = (long)v;
                    return (int)ptr.Field(i_field).Get();
                });
            }
            else if (columnType.Vid == PTypeEnumeration.longinteger)
            {
                index_cell.Root.SortByKey<long>((object v) =>
                {
                    ptr.offset = (long)v;
                    return (long)ptr.Field(i_field).Get();
                });
            }
            else if (columnType.Vid == PTypeEnumeration.sstring)
            {
                index_cell.Root.SortByKey<string>((object v) =>
                {
                    ptr.offset = (long)v;
                    return (string)ptr.Field(i_field).Get();
                });
            }
            else if (columnType.Vid == PTypeEnumeration.real)
            {
                index_cell.Root.SortByKey<double>((object v) =>
                {
                    ptr.offset = (long)v;
                    return (double)ptr.Field(i_field).Get();
                });
            }
            else throw new Exception("Wrong type of column for indexing"); 
            
        }
        public PValue GetById(string id)
        {
            if (table.Count() == 0) return new PValue(null, Int64.MinValue, null);
            PaEntry entry = table.Element(0);
            PaEntry index_entry = index_cell.Root.BinarySearchFirst(ent =>
            {
                long off = (long)ent.Get();
                entry.offset = off;
                return 0 - id.CompareTo((string)entry.Field(i_field).Get());
            });
            if (index_entry.offset == Int64.MinValue) return new PValue(null, Int64.MinValue, null);
            long cell_offset = (long)index_entry.Get();
            entry.offset = cell_offset;
            var rec = entry.GetValue();
            return rec;
        }
        public IEnumerable<PaEntry> SearchAll(string ss)
        {
            if (table.Count() > 0)
            {
                ss = ss.ToLower();
                PaEntry entry = table.Element(0);
                var query = index_cell.Root.BinarySearchAll((PaEntry ent) =>
                {
                    long off = (long)ent.Get();
                    entry.offset = off;
                    string name = ((string)entry.Field(i_field).Get()).ToLower();
                    if (name.StartsWith(ss)) return 0;
                    return name.CompareTo(ss);
                }).Select(ent =>
                {
                    long off = (long)ent.Get();
                    entry.offset = off;
                    return entry;
                });
                return query;
            }
            else return Enumerable.Empty<PaEntry>();
        }
    }
}
