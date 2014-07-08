using System;
using System.Collections.Generic;
using LiteralStores;
using NameTable;
using ScaleBit4Check;
using TripleIntClasses;

namespace RDFStores
{
    public abstract class RDFIntStoreAbstract
    {
        private readonly LiteralStoreAbstract literalStore;
        private readonly IStringIntCoding entityCoding;
        private readonly NameSpaceStore nameSpaceStore;
        private readonly PredicatesCoding predicatesCoding;
        private readonly ScaleCell scale ;
        
        protected string path;

        protected RDFIntStoreAbstract(string path, IStringIntCoding entityCoding, PredicatesCoding predicatesCoding,
            NameSpaceStore nameSpaceStore, LiteralStoreAbstract literalStore)
            :this(entityCoding,predicatesCoding,nameSpaceStore,literalStore, new ScaleCell(path))
        {
            // TODO: Complete member initialization
            this.path = path;

        }

        protected RDFIntStoreAbstract(IStringIntCoding entityCoding, PredicatesCoding predicatesCoding, NameSpaceStore nameSpaceStore, LiteralStoreAbstract literalStore,
            ScaleCell scale)
        {
            this.literalStore = literalStore;
            this.entityCoding = entityCoding;
            this.nameSpaceStore = nameSpaceStore;
            this.predicatesCoding = predicatesCoding;
            this.scale = scale;
            if (!scale.Cell.IsEmpty)
                scale.CalculateRange();
        }

        public virtual NameSpaceStore NameSpaceStore
        {
            get { return nameSpaceStore; }
        }

        public virtual PredicatesCoding PredicatesCoding
        {
            get { return predicatesCoding; }
        }

        public virtual IStringIntCoding EntityCoding
        {
            get { return entityCoding; }
        }

        public virtual LiteralStoreAbstract LiteralStore
        {
            get { return literalStore; }
        }

        public virtual ScaleCell Scale
        {
            get { return scale; }
        }

        public abstract void InitTypes();

        public virtual void WarmUp()
        {

            LiteralStore.WarmUp();
            EntityCoding.WarmUp();
            PredicatesCoding.WarmUp();
            if (Scale.Filescale)   
            Scale.WarmUp();
            //    NameSpaceStore.WarmUp();
        }  
        public virtual void Clear()
        {
            LiteralStore.Clear();
            EntityCoding.Clear();
            PredicatesCoding.Clear();
            NameSpaceStore.Clear();
            LiteralStore.InitConstants(NameSpaceStore);
            Scale.Clear();
        }

        public virtual void MakeIndexed()
        {
            EntityCoding.MakeIndexed();
            PredicatesCoding.MakeIndexed();
            NameSpaceStore.Flush();
            Scale.Flush();

            LiteralStore.Flush();
        }
        public abstract void LoadTurtle(string filepath, bool useBuffer=true);
        public abstract IEnumerable<int> GetSubjectByObjPred(int obj, int pred);
        public abstract IEnumerable<int> GetObjBySubjPred(int subj, int pred);
        public abstract IEnumerable<Literal> GetDataBySubjPred(int subj, int pred);
        public abstract bool ChkOSubjPredObj(int subj, int pred, int obj);
        public abstract bool CheckInScale(int subj, int pred, int obj);
        public abstract IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj);
        public abstract IEnumerable<Int32> GetSubjectByDataPred(int p, Literal d);
        public abstract IEnumerable<KeyValuePair<Int32, Int32>> GetObjBySubj(int subj);
        public abstract IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj);

      

        public string DecodeEntityFullName(int code)
        {
            return NameSpaceStore.DecodeNsShortName(EntityCoding.GetName(code));
        }
        public string DecodePredicateFullName(int code)
        {
            return NameSpaceStore.DecodeNsShortName(PredicatesCoding.GetName(code));
        }
        public int CodeEntityFullName(string name)
        {
            return EntityCoding.GetCode(NameSpaceStore.FromFullName(name.Substring(1, name.Length-2)));
        }
        public int CodePredicateFullName(string name)
        {
            return PredicatesCoding.GetCode(NameSpaceStore.FromFullName(name.Substring(1, name.Length - 2)));
        }
        public int CodeEntityFullOrShort(string name)
        {
            return EntityCoding.GetCode(NameSpaceStore.GetShortFromFullOrPrefixed(name));
        }
        public int CodePredicateFullOrShort(string name)
        {
            return PredicatesCoding.GetCode(NameSpaceStore.GetShortFromFullOrPrefixed(name));
        }     
     
    }
}