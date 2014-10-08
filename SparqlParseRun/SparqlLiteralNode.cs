using TripleStoreForDNR;

namespace SparqlParseRun
{
    public class SparqlLiteralNode : SparqlNode
    {
        public dynamic Content;
        public SparqlUriNode type;
        public string lang;

        public SparqlLiteralNode(SparqlUriNode type, string content, string lang)
        {
            this.type = type;
            Content = content;
            this.lang = lang;
        }

        public SparqlLiteralNode()
        {
            
        }
        public SparqlLiteralNode(dynamic parsed)
        {
            Content = parsed;
        }

        internal override void CreateNode(PolarTripleStore store)
        {   
            Value = store.GetLiteralNode( type==null ? null :type.Uri, Content, lang);
        }
    }
}