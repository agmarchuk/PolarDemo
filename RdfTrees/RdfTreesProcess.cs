using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PolarDB;

using TripleIntClasses;
using TrueRdfViewer;


namespace RdfTrees
{
    // Файл 3 (3)
    public partial class RdfTrees
    {
        

        public bool ChkOSubjPredObj(int subj, int pred, int obj)
        {
            if (subj == Int32.MinValue || obj == Int32.MinValue || pred == Int32.MinValue) return false;

            return CheckContains(subj, pred, obj);
        }
        public bool CheckInScale(int subj, int pred, int obj)
        {
            return scale.ChkInScale(subj, pred, obj);
        }

        private bool CheckContains(int subj, int pred, int obj)
        {
            if (!scale.ChkInScale(subj, pred, obj)) return false;
            return GetObjBySubjPred(subj, pred).Contains(subj);   
        }
     
        public  IEnumerable<Literal> GetDataBySubjPred(int subj, int pred)
        {
            if (subj == Int32.MinValue || pred == Int32.MinValue) return new Literal[0];
            var rec_ent = this.entitiesTree.Root.Element(subj);; //.BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(subj));
            if (rec_ent.IsEmpty) return new Literal[0];
            object[] pairs = (object[]) rec_ent.Field(1).Get();
            return pairs.Cast<object[]>()
                .Where(pair => (int) pair[0] == pred)
                .Select(pair => (long) pair[1])
                .Select(LiteralStore.Literals.Read)
                .ToArray();
        }

        public  IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            if (subj == Int32.MinValue || pred == Int32.MinValue) return new int[0];
            var key = new KeyValuePair<int, int>(subj, pred);
            var rec_ent = this.entitiesTree.Root.Element(subj);// //.BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(subj));
            if (rec_ent.IsEmpty) return new int[0];
            return ((object[]) rec_ent.Field(2).Get())
                .Cast<object[]>()
                .Where(pair => (int) pair[0] == pred)
                .Select(pair => (int) pair[1])
                .ToArray();
        }

        public  IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
        {
            if (obj == Int32.MinValue || pred == Int32.MinValue) return new int[0];
            var key = new KeyValuePair<int, int>(obj, pred);
            var rec_ent = this.entitiesTree.Root.Element(obj);
                //.BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(obj));
            if (rec_ent.IsEmpty) return new int[0];
            var pred_subj = rec_ent.Field(3).Elements()
                .FirstOrDefault(pred_subjseq => (int) pred_subjseq.Field(0).Get() == pred);

            return pred_subj.offset==0 ? new int[0] : ((object[])pred_subj.Field(1).Get()).Cast<int>().ToArray();
        }


        /// <summary>
        /// keys entities
        /// </summary>
        /// <param name="subj"></param>
        /// <returns></returns>
        public  IEnumerable<KeyValuePair<int, int>> GetObjBySubj(int subj)
        {
            if (subj == Int32.MinValue) return new KeyValuePair<int, int>[0];
            var rec_ent = this.entitiesTree.Root.Element(subj);//.BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(subj));
            if (rec_ent.IsEmpty) return new KeyValuePair<int, int>[0];
            return ((object[]) rec_ent.Field(2).Get())
                .Cast<object[]>()
                .Select(pair => new KeyValuePair<int, int>((int) pair[1], (int) pair[0]))
                .ToArray();
        }

        public  IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj)
        {
            if (subj == Int32.MinValue) return new KeyValuePair<Literal, int>[0];
            var rec_ent = this.entitiesTree.Root.Element(subj);
            //BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(subj));
            //if (rec_ent.IsEmpty) pd = new KeyValuePair<Literal, int>[0];else

            object[] pairs = (object[]) rec_ent.Field(1).Get();
            //   PaEntry dtriple_entry = dtriples.Root.Element(0);
            return pairs.Cast<object[]>()
                .Select(pair =>
                {
                    var offset1 = (long) pair[1];
                    var literalObj = LiteralStore.Literals.Read(offset1);
                    return new KeyValuePair<Literal, int>(literalObj, (int) pair[0]);
                })
                .ToArray();
        }

        /// <summary>
        /// keys entities
        /// </summary>
        /// <param name="subj"></param>
        /// <returns></returns>
        public  IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj)
        {
            if (obj == Int32.MinValue) return new KeyValuePair<int, int>[0];

            var rec_ent = this.entitiesTree.Root.Element(obj);
            //.BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(obj));
            //if (rec_ent.IsEmpty) sp = new KeyValuePair<int, int>[0];else

            List<KeyValuePair<int, int>> subjs = new List<KeyValuePair<int, int>>();
            foreach (var pred_subjseq in rec_ent.Field(3).Elements())
            {
                var p = (int) pred_subjseq.Field(0).Get();
                subjs.AddRange(from int s in (object[]) pred_subjseq.Field(1).Get()
                    select new KeyValuePair<int, int>(s, p));
            }
            return subjs;
        }

        public  void WarmUp()
        {
          entitiesTree.Close();
            using (FileStream reader =new FileStream(entitiesTreePath,FileMode.Open))
            {
                int read=1;
                while (read>0)
                {
                    byte[] temp=new byte[500];
                    read = reader.Read(temp, 0, 500);
                    
                }
            }
            entitiesTree=new PxCell(tp_entitiesTree, entitiesTreePath);
                  LiteralStore.Literals.WarmUp();
        }
    }
}
