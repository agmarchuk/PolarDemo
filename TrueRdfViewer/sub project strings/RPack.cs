using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueRdfViewer
{
    public class RPack
    {
        //public bool result;
        public object[] row;
        private TripleStore ts;
        public TripleStore Store { get { return ts; } }
        public RPack(object[] row, TripleStore ts)
        {
            this.row = row;
            this.ts = ts;
        }
        public object Get(object si)
        {
            return si is int ? row[(int)si] : si;
        }
        public string Ges(object si)
        {
            return si is int ? (string)row[(int)si] : (string)si;
        }
        public Literal Val(int ind)
        {
            return (Literal)row[ind];
        }
        public int Vai(int ind)
        {
            Literal lit = (Literal)row[ind];
            if (lit.vid != LiteralVidEnumeration.integer) throw new Exception("Wrong literal vid in Vai method");
            return (int)lit.value;
        }
        public void Set(object si, object valu)
        {
            if (!(si is int)) throw new Exception("argument must be an index");
            int ind = (int)si;
            row[ind] = valu;
        }
    }
    public static class RPackExtention
    {
        public static IEnumerable<RPack> spo(this IEnumerable<RPack> pack, object subj, object pred, object obj)
        {
            return pack.Where(pk => pk.Store.ChkOSubjPredObj(pk.Ges(subj), pk.Ges(pred), pk.Ges(obj)));
        }
        public static IEnumerable<RPack> Spo(this IEnumerable<RPack> pack, object subj, object pred, object obj)
        {
            if (!(subj is int)) throw new Exception("subject must be an index");
            return pack.SelectMany(pk => pk.Store
                .GetSubjectByObjPred(pk.Ges(obj), pk.Ges(pred))
                .Select(su => 
                {
                    pk.Set(subj, su);
                    return new RPack(pk.row, pk.Store);
                }));
        }
        public static IEnumerable<RPack> spO(this IEnumerable<RPack> pack, object subj, object pred, object obj)
        {
            if (!(obj is int)) throw new Exception("object must be an index");
            return pack.SelectMany(pk => pk.Store
                .GetObjBySubjPred(pk.Ges(subj), pk.Ges(pred))
                .Select(ob =>
                {
                    pk.Set(obj, ob);
                    return new RPack(pk.row, pk.Store);
                }));
        }
        public static IEnumerable<RPack> spD(this IEnumerable<RPack> pack, object subj, object pred, object dat)
        {
            if (!(dat is int)) throw new Exception("data must be an index");
            return pack.SelectMany(pk => pk.Store
                .GetDataBySubjPred(pk.Ges(subj), pk.Ges(pred))
                .Select(da =>
                {
                    pk.Set(dat, da); //((Text)da.value).s);
                    return new RPack(pk.row, pk.Store);
                }));
        }
    }
}
