namespace TripleIntClasses
{
    public class DTripleInt : TripleInt { public Literal data;

        public DTripleInt(int subject, int predicate, Literal data)
        {
            this.subject = subject;
            this.predicate = predicate;
            this.data = data;
        }

        public DTripleInt()
        {
        
        }
    }
}