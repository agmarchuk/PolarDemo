using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using LiteralStores;
using NameTable;
using RDFStores;
using ScaleBit4Check;
using TripleIntClasses;

namespace TrueRdfViewer
{
    public class TracingTripleStoreAbstractInt :RDFIntStoreAbstract
    {
        public XElement x;
        public string xPath;
        private int countCalls = 0, callsMaxCount=10*1000;
        private bool isWrite=true;
          RDFIntStoreAbstract @base;
        public TracingTripleStoreAbstractInt(string path, RDFIntStoreAbstract @base) : base(@base.EntityCoding, @base.PredicatesCoding, @base.NameSpaceStore, @base.LiteralStore, @base.Scale)
        {
            this.@base = @base;

            xPath = path + "tracing.xml";
            if (File.Exists(xPath))
            {
        //    File.Delete(xPath);
            }
            x=new XElement("tracing");
        }
        public override bool ChkOSubjPredObj(int subj, int pred, int obj)
        {
            if (countCalls++ >= callsMaxCount) return false;
            bool res = @base.ChkOSubjPredObj(subj, pred, obj);
            if (isWrite)
                x.Add(new XElement("spo", new XAttribute("subj", DecodeEntityFullName(subj)),
                    new XAttribute("pred", @base.DecodeEntityFullName(pred)),
                    new XAttribute("obj", @base.DecodeEntityFullName(obj)), new XAttribute("res", res)));
            return res;
        }

        public override bool CheckInScale(int subj, int pred, int obj)
        {
          return  @base.CheckInScale(subj, pred, obj);
        }

        public override IEnumerable<Literal> GetDataBySubjPred(int subj, int pred)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<Literal>();
            if (isWrite)
                x.Add(new XElement("spD", new XAttribute("subj", @base.DecodeEntityFullName(subj)),
                    new XAttribute("pred", @base.DecodePredicateFullName(pred)),
                    new XAttribute("res", string.Join(" ", (@base.GetDataBySubjPred(subj, pred) as Literal[]).Select(literal => literal.ToString())))));
            return @base.GetDataBySubjPred(subj, pred);
        }

        public override IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<int>();
            var res = @base.GetObjBySubjPred(subj, pred);
            if (isWrite)
                x.Add(new XElement("spO", new XAttribute("subj", @base.DecodeEntityFullName(subj)),
                    new XAttribute("pred", @base.DecodePredicateFullName(pred)),
                    new XAttribute("res", string.Join(" ", res.Select(@base.DecodeEntityFullName)))));
            return res;
        }

        public override void InitTypes()
        {
            @base.InitTypes();
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
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<int>();
            var res = @base.GetSubjectByObjPred(obj, pred);
            if (isWrite)
                x.Add(new XElement("Spo", new XAttribute("obj", @base.DecodeEntityFullName(obj)),
                    new XAttribute("pred", @base.DecodePredicateFullName(pred)),
                    new XAttribute("res", string.Join(" ", res.Select(@base.DecodeEntityFullName)))));
            return res;
        }

        public override IEnumerable<int> GetSubjectByDataPred(int p, Literal d)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<KeyValuePair<Int32, Int32>> GetObjBySubj(int subj)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<Int32, Int32>>();
            var res = @base.GetObjBySubj(subj);

            if (isWrite)
                x.Add(new XElement("sPO", new XAttribute("subj", @base.DecodeEntityFullName(subj)),
                    new XAttribute("res",
                        string.Join(" ",
                            res.Select(
                                literal =>
                                    @base.DecodeEntityFullName(literal.Key) + " " +
                                    @base.DecodePredicateFullName(literal.Value))))));
            return res;
        }

        public override IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<Literal, Int32>>();
            var res = @base.GetDataBySubj(subj);
            if (isWrite)
                x.Add(new XElement("sPD", new XAttribute("subj", @base.DecodeEntityFullName(subj)),
                    new XAttribute("res",
                        string.Join(" ",
                            res.Select(literal => literal.Key + " " + @base.DecodePredicateFullName(literal.Value))))));
            return res;
        }

        public override IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<int, Int32>>();
            var res = @base.GetSubjectByObj(obj);
            if (isWrite)
                x.Add(new XElement("SPo", new XAttribute("obj", @base.DecodeEntityFullName(obj)),
                    new XAttribute("res",
                        string.Join(" ",
                            res.Select(
                                literal =>
                                   @base.DecodeEntityFullName(literal.Key) + " " +
                                    @base.DecodePredicateFullName(literal.Value))))));
            return res;
        }

        public override void Clear()
        {
            @base.Clear();
        }

        public override void MakeIndexed()
        {
            @base.MakeIndexed();
        }

        public override LiteralStoreAbstract LiteralStore{get { return @base.LiteralStore; }}

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

        public override ScaleCell Scale
        {
            get { return @base.Scale; }
        }
    }
}
