using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TripleIntClasses;

namespace TrueRdfViewer
{
    public class TracingTripleStoreInt :IRDFIntStore
    {
        public XElement x;
        public string xPath;
        private int countCalls = 0, callsMaxCount=10*1000;
        private bool isWrite=true;
          IRDFIntStore @base;
        public TracingTripleStoreInt(string path, IRDFIntStore @base)
        {
            this.@base = @base;

            xPath = path + "tracing.xml";
            if (File.Exists(xPath))
            {
        //    File.Delete(xPath);
            }
            x=new XElement("tracing");
        }
        public  bool ChkOSubjPredObj(int subj, int pred, int obj)
        {
            if (countCalls++ >= callsMaxCount) return false;
            bool res = @base.ChkOSubjPredObj(subj, pred, obj);
            if (isWrite)
                x.Add(new XElement("spo", new XAttribute("subj", TripleInt.DecodeEntities(subj)),
                    new XAttribute("pred", TripleInt.DecodePredicates(pred)),
                    new XAttribute("obj", TripleInt.DecodeEntities(obj)), new XAttribute("res", res)));
            return res;
        }

        public bool CheckInScale(int subj, int pred, int obj)
        {
          return  @base.CheckInScale(subj, pred, obj);
        }

        public  IEnumerable<Literal> GetDataBySubjPred(int subj, int pred)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<Literal>();
            if (isWrite)
                x.Add(new XElement("spD", new XAttribute("subj", TripleInt.DecodeEntities(subj)),
                    new XAttribute("pred", TripleInt.DecodePredicates(pred)),
                    new XAttribute("res", string.Join(" ", (@base.GetDataBySubjPred(subj, pred) as Literal[]).Select(literal => literal.ToString())))));
            return @base.GetDataBySubjPred(subj, pred);
        }

        public  IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<int>();
            var res = @base.GetObjBySubjPred(subj, pred);
            if (isWrite)
                x.Add(new XElement("spO", new XAttribute("subj", TripleInt.DecodeEntities(subj)),
                    new XAttribute("pred", TripleInt.DecodePredicates(pred)),
                    new XAttribute("res", string.Join(" ", res.Select(TripleInt.DecodeEntities)))));
            return res;
        }

        public void InitTypes()
        {
            @base.InitTypes();
        }

        public void WarmUp()
        {
            @base.WarmUp();
        }

        public void LoadTurtle(string filepath)
        {
         @base.LoadTurtle(filepath);
        }

        public  IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<int>();
            var res = @base.GetSubjectByObjPred(obj, pred);
            if (isWrite)
                x.Add(new XElement("Spo", new XAttribute("obj", TripleInt.DecodeEntities(obj)),
                    new XAttribute("pred", TripleInt.DecodePredicates(pred)),
                    new XAttribute("res", string.Join(" ", res.Select(TripleInt.DecodeEntities)))));
            return res;
        }

        public IEnumerable<int> GetSubjectByDataPred(int p, Literal d)
        {
            throw new NotImplementedException();
        }

        public  IEnumerable<KeyValuePair<Int32, Int32>> GetObjBySubj(int subj)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<Int32, Int32>>();
            var res = @base.GetObjBySubj(subj);

            if (isWrite)
                x.Add(new XElement("sPO", new XAttribute("subj", TripleInt.DecodeEntities(subj)),
                    new XAttribute("res",
                        string.Join(" ",
                            res.Select(
                                literal =>
                                    TripleInt.DecodeEntities(literal.Key) + " " +
                                    TripleInt.DecodePredicates(literal.Value))))));
            return res;
        }

        public  IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<Literal, Int32>>();
            var res = @base.GetDataBySubj(subj);
            if (isWrite)
                x.Add(new XElement("sPD", new XAttribute("subj", TripleInt.DecodeEntities(subj)),
                    new XAttribute("res",
                        string.Join(" ",
                            res.Select(literal => literal.Key + " " + TripleInt.DecodePredicates(literal.Value))))));
            return res;
        }

        public  IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj)
        {
            if (countCalls++ >= callsMaxCount) return Enumerable.Empty<KeyValuePair<int, Int32>>();
            var res = @base.GetSubjectByObj(obj);
            if (isWrite)
                x.Add(new XElement("SPo", new XAttribute("obj", TripleInt.DecodeEntities(obj)),
                    new XAttribute("res",
                        string.Join(" ",
                            res.Select(
                                literal =>
                                    TripleInt.DecodeEntities(literal.Key) + " " +
                                    TripleInt.DecodePredicates(literal.Value))))));
            return res;
        }
    }
}
