using System.Collections.Generic;
using NameTable;

namespace TripleIntClasses
{
    public class CasheCoding : IStringIntCoding
    {  
        public Dictionary<string, int> EntitiesCodeCache = new Dictionary<string, int>();



        private readonly IStringIntCoding @base;

        public CasheCoding(IStringIntCoding stringIntEncoded)
        {
            @base = stringIntEncoded;
        }

        public void Open(bool readonlyMode)
        {
            @base.Open(readonlyMode);
        }

        public void Close()
        {
            @base.Close();
        }

        public void Clear()
        {
            @base.Clear();
        }

        public int GetCode(string name)
        {
            int c;
            if (!EntitiesCodeCache.TryGetValue(name, out c))
            {
                c = @base.GetCode(name);
                EntitiesCodeCache.Add(name, c);
            }
            return c;
        }

        public string GetName(int code)
        {
            return @base.GetName(code);
        }

        public Dictionary<string, int> InsertPortion(string[] portion)
        {
            return EntitiesCodeCache = @base.InsertPortion(portion);
        }

        public Dictionary<string, int> InsertPortion(HashSet<string> portion)
        {
            return EntitiesCodeCache = @base.InsertPortion(portion);
        }

        public void MakeIndexed()
        {
            @base.MakeIndexed();
        }

        public int Count { get{ return @base.Count; } }
        public void WarmUp()
        {
            @base.WarmUp();
        }

        public int InsertOne(string entity)
        {
            return @base.InsertOne(entity);
        }

    }
}