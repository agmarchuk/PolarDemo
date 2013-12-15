using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace NameTable
{
    public class NameTableInt
    {
        private string path;
        private PaCell nc_cell;
        private DynaIndex<string> n_index;
        private DynaIndex<int> c_index;
        private int next_code = 0;
        public NameTableInt(string path)
        {
            this.path = path;
            this.nc_cell = new PaCell(new PTypeSequence(new PTypeRecord(
                new NamedType("code", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring))
                )), 
                path + "_nc.pac", false);
            if (nc_cell.IsEmpty) nc_cell.Fill(new object[0]);
            //PaEntry nc_entry = 
            this.n_index = new DynaIndex<string>(path, "_n", off =>
            {
                PaEntry ent = nc_cell.Root.Element(0); // это возможно только если есть элемент, надо проверить
                ent.offset = (long)off;
                return (string)ent.Field(1).Get();
            });
            this.c_index = new DynaIndex<int>(path, "_c", off =>
            {
                PaEntry ent = nc_cell.Root.Element(0); // это возможно только если есть элемент, надо проверить
                ent.offset = (long)off;
                return (int)ent.Field(0).Get();
            });
            // Кодирование будет - номер записи, в которую попал идентификатор
            next_code = (int)n_index.Count();
            // Количество записей должно совпадать
            if (n_index.Count() != c_index.Count()) throw new Exception("Assert error: 9434");
        }
        public int SetNameGetCode(string name)
        {
            long off = n_index.GetFirst(name);
            if (off != Int64.MinValue)
            { // Есть такой
                PaEntry ent = nc_cell.Root.Element(0);
                ent.offset = off;
                return (int)ent.Field(0).Get();
            }
            else
            {
                int code = next_code;
                next_code++;
                var r_off = nc_cell.Root.AppendElement(new object[] { code, name });
                //nc_cell.Flush();
                n_index.Add(name, r_off);
                c_index.Add(code, r_off);
                return code;
            }
        }
        public string GetName(int code)
        {
            long off = c_index.GetFirst(code);
            if (off != Int64.MinValue)
            { // Есть такой
                PaEntry ent = nc_cell.Root.Element(0);
                ent.offset = off;
                return (string)ent.Field(1).Get();
            }
            else
            {
                return null;
            }
        }
        private List<string> namePool = new List<string>();
        public void PushName(string name)
        {
            namePool.Add(name);
        }
        public void Flush()
        {
            FlushPool();
            n_index.Flush();
            c_index.Flush();
            nc_cell.Flush();
        }
        public void Close()
        {
            FlushPool();
            n_index.Close();
            c_index.Close();
            nc_cell.Close();
        }
        public IEnumerable<int> GetKeys()
        {
            return c_index.Keys();
        }
        private void FlushPool()
        {
            // Эксперимент
            DateTime tt0 = DateTime.Now;
            foreach (var ent in nc_cell.Root.Elements())
            {
                //string name = (string)ent.Field(1).Get();
                object val = ent.Get();
            }
            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L);


            //namePool = namePool.OrderBy(id => id).Distinct<string>().ToList();
            SortedSet<string> ss = new SortedSet<string>(namePool);
            var query = n_index.Keys();
            Console.WriteLine("\n nindex.Count()=" + query.Count());
        }
        public long Count()
        {
            return n_index.Count();
        }
    }
}
