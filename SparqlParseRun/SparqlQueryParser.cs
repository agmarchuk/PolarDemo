using Antlr4.Runtime;

namespace SparqlParseRun
{
    public class SparqlQueryParser
    {
        public SparqlQuery Parse(string sparqlString)
        {

            ICharStream input = new AntlrInputStream(sparqlString);
          
            var lexer = new sparql2PacNSLexer(input);

            var commonTokenStream = new CommonTokenStream(lexer);
            SparqlQuery sparqlQuery = new SparqlQuery();
            var sparqlParser = new sparql2PacNSParser(commonTokenStream) { q = sparqlQuery };
            sparqlParser.query();
            return sparqlQuery;

        }
    }
  
    
}
