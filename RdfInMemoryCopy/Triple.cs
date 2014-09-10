namespace RdfInMemoryCopy
{
    public class Triple
    {
        private IGraph g;
        private INode subj, pred, obj;
        public Triple(INode subj, INode pred, INode obj) 
        { 
            this.subj = subj; this.pred = pred; this.obj = obj;
            //this.g = subj.Graph;
           // if (!g.Equals(pred.Graph) || !g.Equals(obj.Graph)) throw new Exception("Err in Triple constructor");
        }
        public IGraph Graph { get { return g; } }
        public INode Subject { get { return subj; } }
        public INode Predicate { get { return pred; } }
        public INode Object { get { return obj; } }
    }
}