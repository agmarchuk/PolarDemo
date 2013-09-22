using System;
using System.Collections;
using System.Collections.Generic;

namespace GraphTesting
{
    public abstract class Triplet
    {
        public string s, p;
        /// <summary>
        /// Порождает объект класса Triplet по объектному представлению триплета 
        /// </summary>
        /// <param name="valu">Объектное представление триплета</param>
        /// <returns></returns>
        public static Triplet Create(object valu)
        {
            object[] uni = (object[])valu;
            int tag = (int)uni[0];
            object[] rec = (object[])uni[1];
            if (tag == 1) return new OProp((string)rec[0], (string)rec[1], (string)rec[2]);
            else if (tag == 2) return new DProp((string)rec[0], (string)rec[1], (string)rec[2], (string)rec[3]);
            else throw new Exception("Can't create instance of Triplet class");
        }
    }
    public class OProp : Triplet
    {
        public string o;
        public OProp(string s, string p, string o) { this.s = s; this.p = p; this.o = o; }
    }
    public class DProp : Triplet
    {
        public string d; public string lang;
        public DProp(string s, string p, string d) { this.s = s; this.p = p; this.d = d; }
        public DProp(string s, string p, string d, string l) { this.s = s; this.p = p; this.d = d; this.lang = l; }
    }

    public class TValue
    {
        public static Func<string, Item> ItemCtor;
        private Item item;
        public bool IsNewParametr;

        public string Value;

        public Item Item
        {
            get { return item ?? (item=ItemCtor(Value)); }
            set { item = value; }
        }

        public TValue SetValue(string value, bool isOptA)
        {
            if (value != Value)
            {
                Value = value;
                IsNewParametr = false;
                item = null;
            }
            return this;
        }
        public void DropValue(bool isOptA)
        {
                IsNewParametr = true;
        }
    }

    public class QueryTriplet
    {
        public TValue S, P ,O;
    }

    public class Item:Hashtable
    {
        public Item(Dictionary<object,Property> container)
            :base(container)
        {
        }
    }

    public class Property: List<string>
    {
        public bool Direction;
        //public string Name;
    }
}
