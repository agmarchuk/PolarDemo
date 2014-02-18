using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueRdfViewer
{
    public interface ICodable<in Entity>
    {
        Entity CodeString(string s);
        public static string DecodeEntity(Entity e);
        // Объектное представление - возможно методы не нужены, если Entity имеет прямое представление как объект Поляра
        public static object PObj(Entity e);
        public static Entity EObj(object p);
    }
    public abstract class Triple<Entity> { public Entity subject, predicate; }
    public class OTriple<Entity> : Triple<Entity> { public Entity obj; }
    public class DTriple<Entity> : Triple<Entity> { public Literal data; }
    public enum LiteralVidEnumeration { unknown, integer, text, date }
    public class Literal 
    { 
        public LiteralVidEnumeration vid; 
        public object value;
        public override string ToString()
        {
            switch (vid)
            {
                case LiteralVidEnumeration.text:
                    {
                        Text txt = (Text)value;
                        return "\"" + txt.s + "\"@" + txt.l;
                    }
                default: return value.ToString();
            }
        }
    } 
    public class Text { public string s, l; }

    public class SubjPred<Entity> : IComparable where Entity : IComparable
    {
        public Entity subj, pred;
        public int CompareTo(object sp)
        {
            int cmp = subj.CompareTo(((SubjPred<Entity>)sp).subj);
            if (cmp != 0) return cmp;
            return pred.CompareTo(((SubjPred<Entity>)sp).pred);
        }
    }
    public class SubjPredObj<Entity> : IComparable where Entity : IComparable
    {
        public Entity subj, pred, obj;
        public int CompareTo(object sp)
        {
            SubjPredObj<Entity> target = (SubjPredObj<Entity>)sp;
            int cmp = subj.CompareTo(target.subj);
            if (cmp != 0) return cmp;
            cmp = pred.CompareTo(target.pred);
            if (cmp != 0) return cmp;
            return obj.CompareTo(target.obj); 
        }
    }
    //public class SubjPredComparer : IComparer<SubjPred>
    //{
    //    public int Compare(SubjPred x, SubjPred y)
    //    {
    //        return x.CompareTo(y);
    //    }
    //}
}
