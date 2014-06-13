using System;
using System.Collections.Generic;
using TripleIntClasses;

namespace TrueRdfViewer
{
    public interface IRDFIntStore
    {
        void InitTypes();
        void WarmUp();
        void LoadTurtle(string filepath);
        IEnumerable<int> GetSubjectByObjPred(int obj, int pred);
        IEnumerable<int> GetObjBySubjPred(int subj, int pred);
        IEnumerable<Literal> GetDataBySubjPred(int subj, int pred);
        bool ChkOSubjPredObj(int subj, int pred, int obj);
        bool CheckInScale(int subj, int pred, int obj);

        IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj);
        IEnumerable<Int32> GetSubjectByDataPred(int p, Literal d);
        IEnumerable<KeyValuePair<Int32, Int32>> GetObjBySubj(int subj);
        IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj);
    }
}