using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace RdfTrees
{
    // Файл 3 (3)
    public partial class RdfTrees
    {
        public bool ChkOSubjPredObj(int subj, int pred, int obj)
        {
            var rec_ent = this.entitiesTree.Root.BinarySearchFirst(ent => ((int)ent.Field(0).Get()).CompareTo(subj));
            if (rec_ent.IsEmpty) return false;
            object[] pairs = (object[])rec_ent.Field(2).Get();
            return pairs.Cast<object[]>().Any(pair => (int)pair[0] == pred && (int)pair[1] == obj);  
        }
        public IEnumerable<Literal> GetDataBySubjPred(int subj, int pred)
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
                    return GenerateLiteral(literal_obj);
                });
            return query;
        }
        public IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            var rec_ent = this.entitiesTree.Root.BinarySearchFirst(ent => ((int)ent.Field(0).Get()).CompareTo(subj));
            if (rec_ent.IsEmpty) return Enumerable.Empty<int>();
            object[] pairs = (object[])rec_ent.Field(2).Get();
            var query = pairs.Cast<object[]>().Where(pair => (int)pair[0] == pred)
                .Select(pair => (int)pair[1]);
            return query;
        }
        public IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
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
    }
}
