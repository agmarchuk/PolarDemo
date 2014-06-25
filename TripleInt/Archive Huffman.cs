using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PolarDB;

namespace Huffman
{
    class Archive
    {
        private readonly Dictionary<char, ArchiveEntity> codesHelp = new Dictionary<char, ArchiveEntity>();
        private readonly PxCell codesCell;
        private readonly Dictionary<char, bool[]> codes = new Dictionary<char, bool[]>();
        private readonly BinaryTree<char> decodeTree;

        private readonly static PTypeSequence _pTypeSequence = new PTypeSequence(new PTypeRecord(new NamedType("char", new PType(PTypeEnumeration.character)),
            new NamedType("code", new PTypeSequence(new PType(PTypeEnumeration.boolean)))));

        private readonly string path;

        public Archive(string path)
        {
            this.path = path;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            codesCell = new PxCell(_pTypeSequence, path + "/codes.pxc", false);
            decodeTree = new BinaryTree<char>(null, null);
            ReadCell();
        }


        private class ArchiveEntity
        {
            public string Char;
            public long Frequency=1;
            public readonly List<bool> Code=new List<bool>(); 
        }

        internal class BinaryTree<T>
        {
            public T Current;
            private BinaryTree<T> r;
            private BinaryTree<T> l;

            public BinaryTree(BinaryTree<T> r, BinaryTree<T> l)
            {
                this.r = r;
                this.l = l;
            }

            public BinaryTree<T> RCreate
            {
                get { return r ?? (r=new BinaryTree<T>(null,null)); }
            }

            public BinaryTree<T> LCreate
            {
                get { return l ?? (l=new BinaryTree<T>(null,null)); }
            }
            public BinaryTree<T> R
            {
                get { return r ; }
            }

            public BinaryTree<T> L
            {
                get { return l; }
            }

            public void Clear()
            {
                l = r = null;
                Current = default(T);
            }
        }

        public void Test()
        {
//test
            foreach (var code in codes)
            {
                Console.WriteLine(code.Key + " " + String.Join(" ", code.Value.Select(b => b ? 1 : 0)));
            }
            return;
            if (codes.Any(code => code.Value.Aggregate(decodeTree, (tree, bit) => bit ? tree.R : tree.L).Current != code.Key))
            {
                throw new Exception();
            }
           
          
        }

        public void ReadCell()
        {
            foreach (var charCode in codesCell.Root.Elements().Select(charProb => charProb.Get()).Cast<object[]>())
                ((object[]) charCode[1])
                    .Cast<bool>()
                    .Aggregate(decodeTree, (current, t) => t ? current.RCreate : current.LCreate)
                    .Current = (char) charCode[0];
        }

        public void WriteCell()
        {
            List<ArchiveEntity> tree = codesHelp.Values.OrderBy(prob => prob.Frequency).ToList();
            for (int i = 0; i < codesHelp.Count - 1; i++)
            {
                var o = tree[0];
                var t = tree[1];
                foreach (var c in o.Char) codesHelp[c].Code.Add(true);
                foreach (var c in t.Char) codesHelp[c].Code.Add(false);
                tree.Add(new ArchiveEntity() {Char = o.Char + t.Char, Frequency = o.Frequency + t.Frequency});
                tree = tree.Skip(2).OrderBy(prob => prob.Frequency).ToList();
            }
            foreach (var archiveEntity in codesHelp)
                codes.Add(archiveEntity.Key, archiveEntity.Value.Code.Cast<bool>().Reverse().ToArray());

            object[][] objects = codes.Select(pair => new object[] {pair.Key, pair.Value.Cast<object>().ToArray()})
                .ToArray();
            codesCell.Fill2(objects);
        }

        public void AddFrequency(string text)
        {   
            foreach (char c in text)
                if (codesHelp.ContainsKey(c))
                    codesHelp[c].Frequency++;
                else codesHelp.Add(c, new ArchiveEntity() {Char = c.ToString()});
        }

        public string DecompressFromFile()
        {   
            byte[] readAllBytes = File.ReadAllBytes(path+@"\data.zip");
            return Decompress(readAllBytes);
        }

        public string Decompress(byte[] readAllBytes)
        {  
            string text = "";
            var bits = new BitArray(readAllBytes);
            var tree = decodeTree;
            for (int i = 0; i < bits.Length; i++)
            {
                tree = bits[i] ? tree.R : tree.L;
                if (tree.Current == default(char)) continue;
                text += tree.Current;
                tree = decodeTree;
            }
            if (tree.Current != default(char))
            {
                text += tree.Current;
            }
            return text;
        }

        public void CompressToFile(string dataTxt)
        {
            string readAllText = File.ReadAllText(dataTxt);
            var bytes = Compress(readAllText);
            File.WriteAllBytes(path + @"\data.zip", bytes);
        }

        public byte[] Compress(string readAllText)
        {
            var bits = new BitArray(readAllText.SelectMany(c => codes[c]).ToArray());
            int bytesCount = bits.Length/8 + (bits.Length%8 > 0 ? 1 : 0);
            var bytes = new byte[bytesCount];
            bits.CopyTo(bytes, 0);
            return bytes;
        }

        public void Clear()
        {
            codesCell.Clear();
            codes.Clear();
            decodeTree.Clear();
            codesHelp.Clear();
        }
    }
}