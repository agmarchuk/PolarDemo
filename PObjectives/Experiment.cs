using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace PObjectivesSpec
{
    class Database
    {
        public Collection CreateCollection(string coll) { return null; } // Реализации, часто не имеющие смысла
        public void DropCollection(string coll) { }
        public IEnumerable<Collection> Collections() { return Enumerable.Empty<Collection>(); }
        public Collection Collection(string coll) { return new Collection(); }
    }
    class Collection
    {
        public void Clear() { }
        public Element CreateElement(object pvalue) { return null; }
        public void RemoveElement() { }
        public IEnumerable<Element> Elements() { return Enumerable.Empty<Element>(); }
        public Element Element(int key) { return null; }
        public void UpdateElement(Element el, object pvalue) { }
        public TElement Element<TElement>(int key) where TElement : Element, new()
        {
            Element el = this.Element(key);
            var res = new TElement() { inCollection = el.inCollection, entry = el.entry };
            return res;
        }
    }
    class Element
    {
        internal Collection inCollection;
        internal PaEntry entry;
        public object Get() { return entry.Field(2).Get(); }
    }
    class Person : Element
    {
        public Person() {  }
        public string Name
        {
            get { return (string)entry.Field(2).Field(0).Get(); }
            set
            {
                object[] pvalue = (object[])this.Get();
                pvalue[0] = value;
                inCollection.UpdateElement(this, pvalue);
            }
        }
        public int Age
        {
            get { return (int)entry.Field(2).Field(1).Get(); }
            set
            {
                entry.Field(2).Field(1).Set(value);
            }
        }

    }
    class Experiment
    {
        //public static void Main(string[] args)
        //{
        //    Collection collection = new Collection();
        //    Person pers = collection.Element<Person>(33);
        //    string name = pers.Name;
        //    pers.Name = "Сидоренко";
        //}
    }
}
