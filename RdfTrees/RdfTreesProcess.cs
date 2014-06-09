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
        

        public override bool ChkOSubjPredObj(int subj, int pred, int obj)
        {
            if (subj == Int32.MinValue || obj == Int32.MinValue || pred == Int32.MinValue) return false;
            bool exists;
            var key = new OTripleInt() { subject = subj, obj = obj, predicate = pred };
            if (!spoCache.TryGetValue(key, out exists))
            {
                
                exists = scale.ChkInScale(subj,pred,obj) && GetObjBySubjPred(subj, pred).Contains(obj);
                spoCache.Add(key, exists);
            }
            return exists;      
        }
        public override IEnumerable<Literal> GetDataBySubjPred(int subj, int pred)
        {
            if (subj == Int32.MinValue || pred == Int32.MinValue) return new Literal[0];
            Literal[] dp;
            var key = new KeyValuePair<int, int>(subj, pred);
            if (!spDCache.TryGetValue(key, out dp))
            {
                var rec_ent = this.entitiesTree.Root.BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(subj));
                if (rec_ent.IsEmpty) dp = new Literal[0];
                else
                {
                    object[] pairs = (object[]) rec_ent.Field(1).Get();
                    //PaEntry dtriple_entry = dtriples.Root.Element(0);
                    dp = pairs.Cast<object[]>()
                        .Where(pair => (int) pair[0] == pred)
                        .Select(pair => (long) pair[1])
                        .Select(LiteralStore.Literals.Read)
                        .ToArray();  
                    spDCache.Add(key, dp);
                }
            }
            return dp;
        }
        public override IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            if (subj == Int32.MinValue || pred == Int32.MinValue) return new int[0];
            var key = new KeyValuePair<int, int>(subj, pred);
            int[] objects;
            if (!spOCache.TryGetValue(key, out objects))
            {
                var rec_ent = this.entitiesTree.Root.Element(subj);// //.BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(subj));
                //if (rec_ent.IsEmpty) objects = new int[0]; else
                {
                    object[] pairs = (object[]) rec_ent.Field(2).Get();
                    objects = pairs.Cast<object[]>().Where(pair => (int) pair[0] == pred)
                        .Select(pair => (int) pair[1])
                        .ToArray();
                }
                spOCache.Add(key, objects);
            }

            return objects;
        }

        public override IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
        {
            int[] subjects;
            if (obj == Int32.MinValue || pred == Int32.MinValue) return new int[0];
            var key = new KeyValuePair<int, int>(obj, pred);
            if (!SpoCache.TryGetValue(key, out subjects))
            {
                var rec_ent = this.entitiesTree.Root.Element(obj); //.BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(obj));
                //if (rec_ent.IsEmpty) subjects = new int[0];else
                {
                    object[] subjs = null;
                    foreach (var pred_subjseq in rec_ent.Field(3).Elements())
                    {
                        var p = (int) pred_subjseq.Field(0).Get();
                        if (p == pred)
                        {
                            subjs = (object[]) pred_subjseq.Field(1).Get();
                            break;
                        }
                    }
                    subjects = subjs == null ? new int[0] : subjs.Cast<int>().ToArray();
                    SpoCache.Add(key, subjects);
                }
            }
            return subjects;
        }


        /// <summary>
        /// keys entities
        /// </summary>
        /// <param name="subj"></param>
        /// <returns></returns>
        public override IEnumerable<KeyValuePair<int, int>> GetObjBySubj(int subj)
        {
            IEnumerable<KeyValuePair<int, int>> op;
            if (subj == Int32.MinValue) return new KeyValuePair<int, int>[0];
            if (!sPOCache.TryGetValue(subj, out op))
            {
                var rec_ent = this.entitiesTree.Root.Element(subj);//.BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(subj));
                //if (rec_ent.IsEmpty) op = new KeyValuePair<int, int>[0]; else
                {
                    object[] pairs = (object[]) rec_ent.Field(2).Get();
                    op = pairs.Cast<object[]>()
                        .Select(pair => new KeyValuePair<int, int>((int) pair[1], (int) pair[0]))
                            .ToArray();
                }
                sPOCache.Add(subj, op);
            }  
            return op;
        }

        public override IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj)
        {
            if (subj == Int32.MinValue) return new KeyValuePair<Literal, int>[0];
            IEnumerable<KeyValuePair<Literal, int>> pd;
            if (!sPDCache.TryGetValue(subj, out pd))
            {
                var rec_ent = this.entitiesTree.Root.Element(subj);//BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(subj));
                //if (rec_ent.IsEmpty) pd = new KeyValuePair<Literal, int>[0];else
                {
                    object[] pairs = (object[]) rec_ent.Field(1).Get();
                 //   PaEntry dtriple_entry = dtriples.Root.Element(0);
                    pd = pairs.Cast<object[]>()
                        .Select(pair =>
                        {
                           var offset = (long) pair[1];
                            var literal_obj = LiteralStore.Literals.Read(offset);
                            return new KeyValuePair<Literal, int>(literal_obj, (int) pair[0]);
                        })
                        .ToArray();
                    sPDCache.Add(subj, pd);
                }
            }
            return pd;
        }
        /// <summary>
        /// keys entities
        /// </summary>
        /// <param name="subj"></param>
        /// <returns></returns>
        public override IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj)
        {
            if (obj == Int32.MinValue) return new KeyValuePair<int, int>[0];

            IEnumerable<KeyValuePair<int, int>> sp;
            if (!SPoCache.TryGetValue(obj, out sp))
            {
                var rec_ent = this.entitiesTree.Root.Element(obj);//.BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(obj));
                //if (rec_ent.IsEmpty) sp = new KeyValuePair<int, int>[0];else
                {
                    List<KeyValuePair<int, int>> subjs = new List<KeyValuePair<int, int>>();
                    foreach (var pred_subjseq in rec_ent.Field(3).Elements())
                    {
                        var p = (int) pred_subjseq.Field(0).Get();
                        subjs.AddRange(from int s in (object[]) pred_subjseq.Field(1).Get()
                            select new KeyValuePair<int, int>(s, p));
                    }
                    sp = subjs;
                }
                SPoCache.Add(obj, sp);
            }
            return sp;
        }

        public override void WarmUp()
        {
          entitiesTree.Close();
            File.ReadAllBytes(entitiesTreePath);
            entitiesTree=new PxCell(tp_entitiesTree, entitiesTreePath);
                  LiteralStore.Literals.WarmUp();
        }
    }
}
