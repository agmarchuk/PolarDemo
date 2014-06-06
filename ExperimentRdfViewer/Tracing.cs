using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TrueRdfViewer
{
    public class TracingTripleStoreInt :TripleStoreInt
    {
        public XElement x;
        public string xPath;
        private int countCalls = 0, callsMaxCount=1000*1000;
        private bool isWrite=false;

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
            bool res =base.ChkOSubjPredObj(subj, pred, obj);
            if (isWrite)
                x.Add(new XElement("spo", new XAttribute("subj", TripleInt.DecodeEntities(subj)),
                new XAttribute("pred", TripleInt.DecodePredicates(pred)),
                new XAttribute("obj", TripleInt.DecodeEntities(obj)), new XAttribute("res", res)));

            return res;
        }

        public override IEnumerable<Literal> GetDataBySubjPred(int subj, int pred)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<Literal>();
            var res = base.GetDataBySubjPred(subj, pred);
            if (isWrite)
                x.Add(new XElement("spD", new XAttribute("subj", TripleInt.DecodeEntities(subj)), new XAttribute("pred", TripleInt.DecodePredicates(pred)), 
                new XAttribute("res", string.Join(" ", res.Select(literal => literal.ToString())))));
            return res;
        }

        public override IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<int>();
            var res = base.GetObjBySubjPred(subj, pred);
            if (isWrite)
                x.Add(new XElement("spO", new XAttribute("subj", TripleInt.DecodeEntities(subj)), new XAttribute("pred", TripleInt.DecodePredicates(pred)),
                new XAttribute("res", string.Join(" ", res.Select(TripleInt.DecodeEntities)))));
            return res;
        }

        public override IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<int>();
            var res = base.GetSubjectByObjPred(obj, pred);
            if (isWrite)
            x.Add(new XElement("Spo", new XAttribute("obj", TripleInt.DecodeEntities(obj)), new XAttribute("pred", TripleInt.DecodePredicates(pred)),
                new XAttribute("res", string.Join(" ", res.Select(TripleInt.DecodeEntities)))));
            return res; 
        }
        public override IEnumerable<KeyValuePair<Int32, Int32>> GetObjBySubj(int subj)

        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<Int32, Int32>>();
            var res = base.GetObjBySubj(subj);
            if(isWrite)
            x.Add(new XElement("sPO", new XAttribute("subj", TripleInt.DecodeEntities(subj)),new XAttribute("res", string.Join(" ", res.Select(literal => TripleInt.DecodeEntities(literal.Key) + " " + TripleInt.DecodePredicates(literal.Value))))));
            return res;
        }
        public override IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<Literal, Int32>>();
            var res = base.GetDataBySubj(subj);
            if (isWrite)
            x.Add(new XElement("sPD", new XAttribute("subj", TripleInt.DecodeEntities(subj)),
                new XAttribute("res", string.Join(" ", res.Select(literal => literal.Key + " " + TripleInt.DecodePredicates(literal.Value))))));
            return res;
        }
        public override IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<int, Int32>>();
            var res = base.GetSubjectByObj(obj);
            if (isWrite)
                x.Add(new XElement("SPo", new XAttribute("obj", TripleInt.DecodeEntities(obj)),
                new XAttribute("res", string.Join(" ", res.Select(literal => TripleInt.DecodeEntities(literal.Key) + " " + TripleInt.DecodePredicates(literal.Value))))));
            return res;
        }

    }
}
