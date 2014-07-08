using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Huffman;


namespace TripleIntClasses
{
    public class StaticFreqEncoding  
    {
      
        private readonly Dictionary<char, bool[]> codes = new Dictionary<char, bool[]>();
        private readonly BinaryTree<char> decodeTree;
        private string frequencyQuery = "etaoinshrdlcumwfgypbvkjxqz";
      //  private string upperCaseQuery = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private int[] frequences = new[]
        {
            13000, 9056, 8167, 7507, 6966, 6749, 6327, 6094, 5987, 4253, 4025, 2782, 2758, 2406, 2306, 2228, 2015, 1974,
            1929, 1492, 978, 772, 153, 150, 95, 74     
        };
        //private int[] uppercaseFrequences = new[]
        //{
        //   11602,4702,3511,2670,2007,3779,1950,7232,6286,597,590,2705,4374,2365,6264,2564,2545,173,1653,  7755,16671,1487,649,6753,17,1620,34
        //};
        private readonly Dictionary<char, Archive.ArchiveEntity> codesHelp = new Dictionary<char, Archive.ArchiveEntity>();
        private HashSet<char> compressed;
        private static readonly Regex regexUpper = new Regex("[A-Z]");
        private static readonly Regex toLowerRegex = new Regex("#[a-z]");

        public StaticFreqEncoding()
        {
         //   codesHelp.Add(':', new Archive.ArchiveEntity() { Char = ":", Frequency = 200 * 1000 * 1000 });
            for (int i = 0; i < 10; i++)
                codesHelp.Add(i.ToString()[0], new Archive.ArchiveEntity() { Char = i.ToString(), Frequency = 200 * 1000 * 1000 });
            for (int i = 0; i < frequencyQuery.Length; i++)
                codesHelp.Add(frequencyQuery[i], new Archive.ArchiveEntity() { Char = frequencyQuery[i].ToString(), Frequency = frequences[i] * 1000 });
            //for (int i = 0; i < upperCaseQuery.Length; i++)
            //    codesHelp.Add(upperCaseQuery[i],
            //        new Archive.ArchiveEntity() {Char = upperCaseQuery[i].ToString(), Frequency = uppercaseFrequences[i]});
            codesHelp.Add('#', new Archive.ArchiveEntity() { Char = "#", Frequency = 200 * 1000 * 1000 });
            compressed = new HashSet<char>();
            List<Archive.ArchiveEntity> tree = codesHelp.Values.OrderBy(prob => prob.Frequency).ToList();
            for (int i = 0; i < codesHelp.Count - 1; i++)
            {
                var o = tree[0];
                var t = tree[1];
                foreach (var c in o.Char) codesHelp[c].Code.Add(true);
                foreach (var c in t.Char) codesHelp[c].Code.Add(false);
                tree.Add(new Archive.ArchiveEntity() { Char = o.Char + t.Char, Frequency = o.Frequency + t.Frequency });
                tree = tree.Skip(2).OrderBy(prob => prob.Frequency).ToList();
            }
            foreach (var archiveEntity in codesHelp)
                codes.Add(archiveEntity.Key, archiveEntity.Value.Code.Cast<bool>().Reverse().ToArray());
             decodeTree=new BinaryTree<char>(null,null);
            foreach (var code in codes)
            code.Value.Aggregate(decodeTree, (current, t) => t ? current.RCreate : current.LCreate)
                    .Current = code.Key;
        }
        public string Decode(byte[] bytes)
        {
            string text = "";
            var bits = new BitArray(bytes);
            var tree = decodeTree;
            var bitArray = bits.Cast<bool>().SkipWhile((b, i) => b == bits[0] && i < 8).ToArray();
            for (int i = 0; i < bitArray.Length; i++)
            {
                tree = bitArray[i] ? tree.R : tree.L;
                if (tree.Current == default(char)) continue;
                text += tree.Current;
                tree = decodeTree;
            }
            if (tree.Current != default(char))
            {
                text += tree.Current;
            }
            return toLowerRegex.Replace(text, match => match.Value.Substring(1).ToUpper()) ;
        }

        long bytesEncoded = 0;
        public byte[] Encode(string text, out bool hasNew)
        {
                hasNew = false;
            
            text = regexUpper.Replace(text, match => "#" + match.Value.ToLower());
            List<bool> bits = new List<bool>(text.Length*5);
            for (int i = 0; i < text.Length; i++)
            {
                hasNew =  hasNew || compressed.Contains(text[i]);
                bool[] code;
                if (!codes.TryGetValue(text[i], out code)) 
                {
                    Console.WriteLine("'{0}' unexcepted in encoding ",text[i]);
                    return null;
                }
                bits.AddRange(code);
            }
            int rest = bits.Count % 8;
            bool antiFirst = !bits[0];
            var bitsArray = new BitArray(Enumerable.Repeat(antiFirst, 8 - rest).Concat(bits).ToArray());
            int bytesCount = bits.Count / 8 + 1;//(rest > 0 ? 1 : 0);
            var bytes = new byte[bytesCount];
            bitsArray.CopyTo(bytes, 0);
            bytesEncoded += bytes.Length;
            return bytes;
        }
        
    }
}
