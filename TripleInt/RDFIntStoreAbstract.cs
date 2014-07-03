using System;
using System.Collections.Generic;
using NameTable;

namespace TripleIntClasses
{
    public abstract class RDFIntStoreAbstract
    {
        private readonly LiteralStoreAbstract literalStore;
        private readonly IStringIntCoding entityCoding;
        private readonly NameSpaceStore nameSpaceStore;
        private readonly PredicatesCoding predicatesCoding;
        protected string path;

        protected RDFIntStoreAbstract(string path, IStringIntCoding entityCoding, PredicatesCoding predicatesCoding, NameSpaceStore nameSpaceStore, LiteralStoreAbstract literalStore)
        {
            // TODO: Complete member initialization
            this.path = path;
            this.literalStore = literalStore;
            this.entityCoding = entityCoding;
            this.nameSpaceStore = nameSpaceStore;
            this.predicatesCoding = predicatesCoding;
        }

        protected RDFIntStoreAbstract(IStringIntCoding entityCoding, PredicatesCoding predicatesCoding, NameSpaceStore nameSpaceStore, LiteralStoreAbstract literalStore)
        {
            this.literalStore = literalStore;
            this.entityCoding = entityCoding;
            this.nameSpaceStore = nameSpaceStore;
            this.predicatesCoding = predicatesCoding;
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

        public abstract void InitTypes();

        public virtual void WarmUp()
        {

            LiteralStore.WarmUp();
            EntityCoding.WarmUp();
            PredicatesCoding.WarmUp();
        //    NameSpaceStore.WarmUp();
        }
        public abstract void LoadTurtle(string filepath);
        public abstract IEnumerable<int> GetSubjectByObjPred(int obj, int pred);
        public abstract IEnumerable<int> GetObjBySubjPred(int subj, int pred);
        public abstract IEnumerable<Literal> GetDataBySubjPred(int subj, int pred);
        public abstract bool ChkOSubjPredObj(int subj, int pred, int obj);
        public abstract bool CheckInScale(int subj, int pred, int obj);
        public abstract IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj);
        public abstract IEnumerable<Int32> GetSubjectByDataPred(int p, Literal d);
        public abstract IEnumerable<KeyValuePair<Int32, Int32>> GetObjBySubj(int subj);
        public abstract IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj);

        public virtual void Clear()
        {
                       LiteralStore.Clear();
            EntityCoding.Clear();
            PredicatesCoding.Clear();
            NameSpaceStore.Clear();
            LiteralStore.InitConstants(NameSpaceStore);
        }

        public virtual void MakeIndexed()
        {
            EntityCoding.MakeIndexed();
            PredicatesCoding.MakeIndexed();
            NameSpaceStore.Flush();
            Console.WriteLine("writed namespaces ");
            LiteralStore.Flush();
        }

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