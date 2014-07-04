using System.Collections.Generic;
using System.Linq;

namespace NameTable
{
    public class SimpleUnsequentalyCoding : IStringIntCoding
    {

        public void Open(bool readonlyMode)
        {
            
        }

        public void Close()
        {
          
        }

        public void Clear()  {
    
        }

        public int GetCode(string name)
        {
            return name.GetHashCode(); 
        }

        public string GetName(int code)
        {
            return "noname" + code;
        }

        public Dictionary<string, int> InsertPortion(string[] portion)
        {
            return InsertPortion(new HashSet<string>(portion));
        }

        public Dictionary<string, int> InsertPortion(HashSet<string> entities)
        {
            return entities.ToDictionary(s => s, s => s.GetHashCode());
        }

        public void MakeIndexed()
        {
        }

        public int Count { get; private set; }
        public void WarmUp()
        {
        }

        public int InsertOne(string entity)
        {
            return entity.GetHashCode();
        }
    }
}