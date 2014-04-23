using System;
using System.Collections.Generic;

namespace NameTable
{
    public static class  StringToIntBuffer
    {
        private static readonly Dictionary<string, Ref<int>> Buffer=new Dictionary<string, Ref<int>>();

        public static Ref<int> Insert(this StringIntCoding sic, string s)
        {
            int bufferMax = 5*1000*1000;
            Ref<int> codeRef=new Ref<int>();
            Buffer.Add(s, codeRef);
            if (Buffer.Count == bufferMax)
                InsertPortionForce(sic);
            return codeRef;
        }

        private static void InsertPortionForce(StringIntCoding sic)
        {
            string[] arr = new string[Buffer.Count];
            Buffer.Keys.CopyTo(arr, 0);
            Array.Sort(arr);
            var values = sic.InsertPortion(arr);
            foreach (var keyValue in Buffer)
                keyValue.Value.Value = values[keyValue.Key];
        }
    }
}