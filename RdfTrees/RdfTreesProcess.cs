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


        //public override IEnumerable<KeyValuePair<int, int>> GetObjBySubj(int subj)
        //{
        //    if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<Int32, Int32>>();
        //    var res = base.GetObjBySubj(subj);
        //    if (isWrite)
        //        x.Add(new XElement("sPO", new XAttribute("subj", TripleInt.DecodeEntities(subj)), new XAttribute("res", string.Join(" ", res.Select(literal => TripleInt.DecodeEntities(literal.Key) + " " + TripleInt.DecodePredicates(literal.Value))))));
        //    return res;
        //}
        //public override IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj)
        //{
        //    if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<Literal, Int32>>();
        //    var res = base.GetDataBySubj(subj);
        //    if (isWrite)
        //        x.Add(new XElement("sPD", new XAttribute("subj", TripleInt.DecodeEntities(subj)),
        //            new XAttribute("res", string.Join(" ", res.Select(literal => literal.Key + " " + TripleInt.DecodePredicates(literal.Value))))));
        //    return res;
        //}
        //public override IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj)
        //{
        //    if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<int, Int32>>();
        //    var res = base.GetSubjectByObj(obj);
        //    if (isWrite)
        //        x.Add(new XElement("SPo", new XAttribute("obj", TripleInt.DecodeEntities(obj)),
        //        new XAttribute("res", string.Join(" ", res.Select(literal => TripleInt.DecodeEntities(literal.Key) + " " + TripleInt.DecodePredicates(literal.Value))))));
        //    return res;
        //}
    }
}
