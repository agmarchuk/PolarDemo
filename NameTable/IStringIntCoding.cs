using System.Collections.Generic;

namespace NameTable
{
    public interface IStringIntCoding
    {
        void Open(bool readonlyMode);
        void Close();
        void Clear();
        int GetCode(string name);
        string GetName(int code);

        Dictionary<string, int> InsertPortion(string[] portion)  //   (string[] sorted_arr) 
            ;
        Dictionary<string, int> InsertPortion(HashSet<string> entities);

        void MakeIndexed();
        int Count { get; }
        void WarmUp();
        int InsertOne(string entity);
    }
}