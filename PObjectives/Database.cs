using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace PObjectives
{
    public class Database
    {
        // Спецификация
        //public Database(string path);
        //public Collection CreateCollection(string coll);
        //public void DropCollection(string coll);
        //public IEnumerable<Collection> Collections();
        //public Collection Collection(string coll);

        // директория для базы данных
        private string path;
        // ячейка для каталога коллекций
        internal string Path { get { return path; } }
        private PaCell cell_catalogue;
        private Dictionary<string, Collection> collections;
        public Database(string path)
        {
            this.path = path;
            PType tp_catalogue = new PTypeSequence(new PTypeRecord(
                new NamedType("collectionname", new PType(PTypeEnumeration.sstring)),
                new NamedType("collectionelementtype", PType.TType)));
            cell_catalogue = new PaCell(tp_catalogue, path + "catalogue.pac", false);
            if (cell_catalogue.IsEmpty) cell_catalogue.Fill(new object[0]);
            collections = new Dictionary<string, Collection>();
            foreach (var coll_element in cell_catalogue.Root.Elements())
            {
                string coll_name = (string)coll_element.Field(0).Get();
                object tobj = coll_element.Field(1).Get();
                PType coll_e_type = PType.FromPObject(tobj);
                Collection coll = new Collection(coll_name, coll_e_type, this);
                collections.Add(coll_name, coll);
            }
        }
        public void CreateCollection(string collectionname, PType collectionelementtype)
        {
            if (collections.ContainsKey(collectionname)) throw new Exception("collection name [" + collectionname +"] already exists");
            // Глубина типового объекта ограничена 8
            cell_catalogue.Root.AppendElement(new object[] {collectionname, collectionelementtype.ToPObject(8)});
            cell_catalogue.Flush();
            Collection collection = new Collection(collectionname, collectionelementtype, this);
            collections.Add(collectionname, collection);
        }
        public Collection Collection(string cname) 
        {
            Collection coll = null;
            if (collections.TryGetValue(cname, out coll)) return coll;
            return null; 
        }
    }
}
