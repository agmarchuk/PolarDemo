using System;
using System.Collections.Generic;
using System.Linq;
using LiteralStores;
using NameTable;
using ScaleBit4Check;
using TripleIntClasses;

namespace RDFStores
{
    public class CashingTripleStoreInt : RDFIntStoreAbstract
    {
        readonly RDFIntStoreAbstract @base;
        private readonly Dictionary<int, IEnumerable<KeyValuePair<int, int>>> SPoCache = new Dictionary<int, IEnumerable<KeyValuePair<int, int>>>();
        private readonly Dictionary<int, IEnumerable<KeyValuePair<int, int>>> sPOCache = new Dictionary<int, IEnumerable<KeyValuePair<int, int>>>();
        private readonly Dictionary<int, IEnumerable<KeyValuePair<Literal, int>>> sPDCache = new Dictionary<int, IEnumerable<KeyValuePair<Literal, int>>>();
        private readonly Dictionary<KeyValuePair<int, int>, Literal[]> spDCache = new Dictionary<KeyValuePair<int, int>, Literal[]>();
        private readonly Dictionary<KeyValuePair<int, int>, int[]> SpoCache = new Dictionary<KeyValuePair<int, int>, int[]>();
        private readonly Dictionary<KeyValuePair<int, int>, int[]> spOCache = new Dictionary<KeyValuePair<int, int>, int[]>();
        private readonly Dictionary<OTripleInt, bool> spoCache = new Dictionary<OTripleInt, bool>();
        public CashingTripleStoreInt(RDFIntStoreAbstract @base) : base(@base.EntityCoding, @base.PredicatesCoding, @base.NameSpaceStore, @base.LiteralStore, @base.Scale)
        {
            this.@base = @base;    
          
        }
        public override bool ChkOSubjPredObj(int subj, int pred, int obj)
        {
            bool exists;
            var key = new OTripleInt(subj, obj,  pred);
            if (!spoCache.TryGetValue(key, out exists))
                spoCache.Add(key, exists = CheckContains(subj, pred, obj));
            return exists;
        }

        public override bool CheckInScale(int subj, int pred, int obj)
        {
            return @base.CheckInScale(subj, pred, obj);
        }

        private bool CheckContains(int subj, int pred, int obj)
        {
            if(!@base.CheckInScale(subj,pred,obj)) return false;

            //SubjPredObjInt key = new SubjPredObjInt() { subj = subj, pred = pred, obj = obj };
            //var entry = otriples.Root.BinarySearchFirst(ent => (new SubjPredObjInt(ent.Get())).CompareTo(key));
            int[] resSubj, resObj;
            var keySub = new KeyValuePair<int, int>(subj, pred);
            var keyObj = new KeyValuePair<int, int>(obj, pred);

            //если даже в оперативной памяти закешированы предикаты, но их больше, чем эта константа, то будет проверено количество предикатов в другом
            const int limit = 1000;


            // проверка кешей 
            var subjExists = spOCache.TryGetValue(keySub, out resSubj);
            var objExists = SpoCache.TryGetValue(keyObj, out resObj);
            long subjLength = 0;
            if (subjExists)
            {
                subjLength = resSubj.Length;
                // но если и обратыне тоже есть в кеше, то будет сравниваться их количество
                if (!objExists && subjLength < limit)
                    return resSubj.Contains(obj);
            }
            long objLength = 0;
            if (objExists)
            {
                objLength = resObj.Length;
                if (!subjExists && objLength < limit)
                    return resObj.Contains(subj);
            }
            if (subjExists && objExists)
                if (subjLength > objLength)
                    return resObj.Contains(subj);
                else return resSubj.Contains(obj);

            return GetObjBySubjPred(subj, pred).Contains(obj);
        }

        public override IEnumerable<Literal> GetDataBySubjPred(int subj, int pred)
        {
            Literal[] res;
            var key = new KeyValuePair<int, int>(subj, pred);
            if (!spDCache.TryGetValue(key, out res))
               spDCache.Add(key, res = @base.GetDataBySubjPred(subj, pred) as Literal[]);
            return res;
        }

        public override IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            int[] objects;
            var key = new KeyValuePair<int, int>(subj, pred);
            if (!spOCache.TryGetValue(key, out objects))
               spOCache.Add(key, objects = @base.GetObjBySubjPred(subj, pred) as int[]);
            return objects;
        }

        public override void InitTypes()
        {
            throw new NotImplementedException();
        }

        public override void WarmUp()
        {
           @base.WarmUp();
        }

        public override void LoadTurtle(string filepath, bool useBuffer)
        {
            @base.LoadTurtle(filepath, useBuffer);
        }

        public override IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
        {
            int[] subjects;
            var key = new KeyValuePair<int, int>(obj, pred);
            if (!SpoCache.TryGetValue(key, out subjects))
               SpoCache.Add(key,  subjects = @base.GetSubjectByObjPred(obj, pred) as int[]);
            return subjects;
        }

        public override IEnumerable<int> GetSubjectByDataPred(int p, Literal d)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<KeyValuePair<Int32, Int32>> GetObjBySubj(int subj)
        {
            IEnumerable<KeyValuePair<int, int>> op;
            if (!sPOCache.TryGetValue(subj, out op))
                sPOCache.Add(subj, op = @base.GetObjBySubj(subj)); 
            return op;
        }
        public override IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj)
        {
            IEnumerable<KeyValuePair<Literal, int>> dp;
            if (!sPDCache.TryGetValue(subj, out dp))
                sPDCache.Add(subj, dp = @base.GetDataBySubj(subj)); 
            return dp;
        }

        public override IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj)
        {
            IEnumerable<KeyValuePair<int, int>> sp;
            if (!SPoCache.TryGetValue(obj, out sp))
                SPoCache.Add(obj, sp = @base.GetSubjectByObj(obj)); 
            return sp;
        }

        public override LiteralStoreAbstract LiteralStore
        {
            get { return @base.LiteralStore; }
        }

        public override IStringIntCoding EntityCoding
        {
            get { return @base.EntityCoding; }
        }

        public override NameSpaceStore NameSpaceStore
        {
            get { return @base.NameSpaceStore; }
        }

        public override PredicatesCoding PredicatesCoding
        {
            get { return @base.PredicatesCoding; }
        }

        public override void Clear()
        {
            @base.Clear();
        }

        public override void MakeIndexed()
        {
            @base.MakeIndexed();
        }
        public override ScaleCell Scale
        {
            get { return @base.Scale; }
        }
    }

}