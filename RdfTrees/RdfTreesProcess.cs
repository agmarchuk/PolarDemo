using System.Collections.Generic;
using System.Linq;
using PolarDB;

using TripleIntClasses;



namespace RdfTrees
{
    // Файл 3 (3)
    public partial class RdfTrees
    {
        public override bool ChkOSubjPredObj(int subj, int pred, int obj)
        {
            var rec_ent = this.entitiesTree.Root.BinarySearchFirst(ent => ((int)ent.Field(0).Get()).CompareTo(subj));
            if (rec_ent.IsEmpty) return false;
            object[] pairs = (object[])rec_ent.Field(2).Get();
            return pairs.Cast<object[]>().Any(pair => (int)pair[0] == pred && (int)pair[1] == obj);  
        }
        public override IEnumerable<Literal> GetDataBySubjPred(int subj, int pred)
        {
            var rec_ent = this.entitiesTree.Root.BinarySearchFirst(ent => ((int)ent.Field(0).Get()).CompareTo(subj));
            if (rec_ent.IsEmpty) return Enumerable.Empty<Literal>();
            object[] pairs = (object[])rec_ent.Field(1).Get();
            PaEntry dtriple_entry = dtriples.Root.Element(0);
            var query = pairs.Cast<object[]>().Where(pair => (int)pair[0] == pred)
                .Select(pair => 
                {
                    dtriple_entry.offset = (long)pair[1];
                    var literal_obj = dtriple_entry.Field(2).Get();
                    return Literal.ToLiteral((object[]) literal_obj);
                });
            return query;
        }
        public override IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            var rec_ent = this.entitiesTree.Root.BinarySearchFirst(ent => ((int)ent.Field(0).Get()).CompareTo(subj));
            if (rec_ent.IsEmpty) return Enumerable.Empty<int>();
            object[] pairs = (object[])rec_ent.Field(2).Get();
            var query = pairs.Cast<object[]>().Where(pair => (int)pair[0] == pred)
                .Select(pair => (int)pair[1]);
            return query;
        }
        public override IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
        {
            var rec_ent = this.entitiesTree.Root.BinarySearchFirst(ent => ((int)ent.Field(0).Get()).CompareTo(obj));
            if (rec_ent.IsEmpty) return Enumerable.Empty<int>();
            object[] subjs = null;
            foreach (var pred_subjseq in rec_ent.Field(3).Elements())
            {
                var p = (int)pred_subjseq.Field(0).Get();
                if (p == pred) { subjs = (object[])pred_subjseq.Field(1).Get(); break; }
            }
            if (subjs == null) return Enumerable.Empty<int>();
            var query = subjs.Cast<int>();
            return query;
        }


        /// <summary>
        /// keys entities
        /// </summary>
        /// <param name="subj"></param>
        /// <returns></returns>
        public override IEnumerable<KeyValuePair<int, int>> GetObjBySubj(int subj)
        {
            var rec_ent = this.entitiesTree.Root.BinarySearchFirst(ent => ((int)ent.Field(0).Get()).CompareTo(subj));
            if (rec_ent.IsEmpty) return Enumerable.Empty<KeyValuePair<int, int>>();
            object[] pairs = (object[])rec_ent.Field(2).Get();
            var query = pairs.Cast<object[]>().Select(pair => new KeyValuePair<int, int>((int)pair[1], (int) pair[0]));
            return query;
        }
     
        public override IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj)
        {
            var rec_ent = this.entitiesTree.Root.BinarySearchFirst(ent => ((int)ent.Field(0).Get()).CompareTo(subj));
            if (rec_ent.IsEmpty) return Enumerable.Empty<KeyValuePair<Literal, int>>();
            object[] pairs = (object[])rec_ent.Field(1).Get();
            PaEntry dtriple_entry = dtriples.Root.Element(0);
            var query = pairs.Cast<object[]>()
                .Select(pair =>
                {
                    dtriple_entry.offset = (long)pair[1];
                    var literal_obj = dtriple_entry.Field(2).Get();
                    return new KeyValuePair<Literal, int>(Literal.ToLiteral((object[])literal_obj), (int) pair[0]); 
                });
            return query;
        }
        /// <summary>
        /// keys entities
        /// </summary>
        /// <param name="subj"></param>
        /// <returns></returns>
        public override IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj)
        {
            var rec_ent = this.entitiesTree.Root.BinarySearchFirst(ent => ((int)ent.Field(0).Get()).CompareTo(obj));
            if (rec_ent.IsEmpty) return Enumerable.Empty<KeyValuePair<int, int>>();
            List<KeyValuePair<int,int>> subjs = new List<KeyValuePair<int, int>>();
            foreach (var pred_subjseq in rec_ent.Field(3).Elements())
            {
                var p = (int)pred_subjseq.Field(0).Get();
                subjs.AddRange(from int s in (object[]) pred_subjseq.Field(1).Get() select new KeyValuePair<int, int>(s, p));
            }
            return subjs;
        }
    }
}
