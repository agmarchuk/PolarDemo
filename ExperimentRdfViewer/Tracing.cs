using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TripleIntClasses;

namespace TrueRdfViewer
{
    public class TracingTripleStoreInt :TripleStoreInt
    {
        public XElement x;
        public string xPath;
        private int countCalls = 0, callsMaxCount=10*1000;
        private bool isWrite=true;

        public TracingTripleStoreInt(string path)
            : base(path)
        {
            xPath = path + "tracing.xml";
            if (File.Exists(xPath))
            {
        //    File.Delete(xPath);
            }
            x=new XElement("tracing");
        }
        public override bool ChkOSubjPredObj(int subj, int pred, int obj)
        {
            if(countCalls++>=callsMaxCount)  return false;
            bool exists;
            if (!spoCache.TryGetValue(new OTripleInt(){subject = subj, obj = obj, predicate = pred}, out exists))
            {
                bool res = base.ChkOSubjPredObj(subj, pred, obj);
                if (isWrite)
                    x.Add(new XElement("spo", new XAttribute("subj", TripleInt.DecodeEntities(subj)),
                        new XAttribute("pred", TripleInt.DecodePredicates(pred)),
                        new XAttribute("obj", TripleInt.DecodeEntities(obj)), new XAttribute("res", res)));
                exists = res;
            }
            return exists;
        }

        public override IEnumerable<Literal> GetDataBySubjPred(int subj, int pred)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<Literal>();
            Literal[] res;
            if (!spDCache.TryGetValue(new KeyValuePair<int, int>(subj, pred), out res))
            {
                res = base.GetDataBySubjPred(subj, pred) as Literal[];
                if (isWrite)
                    x.Add(new XElement("spD", new XAttribute("subj", TripleInt.DecodeEntities(subj)),
                        new XAttribute("pred", TripleInt.DecodePredicates(pred)),
                        new XAttribute("res", string.Join(" ", res.Select(literal => literal.ToString())))));
            }
            return res;
        }

        public override IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<int>();
            int[] objects;
            if (!spOCache.TryGetValue(new KeyValuePair<int, int>(subj, pred), out objects))
            {
                var res = base.GetObjBySubjPred(subj, pred);
                if (isWrite)
                    x.Add(new XElement("spO", new XAttribute("subj", TripleInt.DecodeEntities(subj)),
                        new XAttribute("pred", TripleInt.DecodePredicates(pred)),
                        new XAttribute("res", string.Join(" ", res.Select(TripleInt.DecodeEntities)))));
                objects = res as int[];
            }
            return objects;
        }

        public override IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<int>();
            int[] subjects;
            if (!SpoCache.TryGetValue(new KeyValuePair<int, int>(obj, pred), out subjects))
            {
                var res = base.GetSubjectByObjPred(obj, pred);
                if (isWrite)
                    x.Add(new XElement("Spo", new XAttribute("obj", TripleInt.DecodeEntities(obj)),
                        new XAttribute("pred", TripleInt.DecodePredicates(pred)),
                        new XAttribute("res", string.Join(" ", res.Select(TripleInt.DecodeEntities)))));
                subjects = res as int[];
            }
            return subjects; 
        }
        public override IEnumerable<KeyValuePair<Int32, Int32>> GetObjBySubj(int subj)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<Int32, Int32>>();
            IEnumerable<KeyValuePair<int, int>> op;
            if (!sPOCache.TryGetValue(subj, out op))
            {
                var res = base.GetObjBySubj(subj);

                if (isWrite)
                    x.Add(new XElement("sPO", new XAttribute("subj", TripleInt.DecodeEntities(subj)),
                        new XAttribute("res",
                            string.Join(" ",
                                res.Select(
                                    literal =>
                                        TripleInt.DecodeEntities(literal.Key) + " " +
                                        TripleInt.DecodePredicates(literal.Value))))));
                op = res;
            }
            return op;
        }
        public override IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<Literal, Int32>>();
            IEnumerable<KeyValuePair<Literal, int>> dp;
            if (!sPDCache.TryGetValue(subj, out dp))
            {
                var res = base.GetDataBySubj(subj);
                if (isWrite)
                    x.Add(new XElement("sPD", new XAttribute("subj", TripleInt.DecodeEntities(subj)),
                        new XAttribute("res",
                            string.Join(" ",
                                res.Select(literal => literal.Key + " " + TripleInt.DecodePredicates(literal.Value))))));
             dp=res;
            }
            return dp;
        }

        public override IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<int, Int32>>();
            IEnumerable<KeyValuePair<int, int>> sp;
            if (!SPoCache.TryGetValue(obj, out sp))
            {
                var res = base.GetSubjectByObj(obj);
                if (isWrite)
                    x.Add(new XElement("SPo", new XAttribute("obj", TripleInt.DecodeEntities(obj)),
                        new XAttribute("res",
                            string.Join(" ",
                                res.Select(
                                    literal =>
                                        TripleInt.DecodeEntities(literal.Key) + " " +
                                        TripleInt.DecodePredicates(literal.Value))))));
                sp=res;
            }
            return sp;
        }
    }
}
