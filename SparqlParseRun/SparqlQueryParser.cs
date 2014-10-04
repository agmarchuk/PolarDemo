using Antlr4.Runtime;

namespace SparqlParseRun
{
    public class SparqlQueryParser
    {
        public SparqlQuery Parse(string sparqlString)
        {

            ICharStream input = new AntlrInputStream(sparqlString);

            var lexer = new sparq11lTranslatorLexer(input);

            var commonTokenStream = new CommonTokenStream(lexer);
            SparqlQuery sparqlQuery = new SparqlQuery();
            var sparqlParser = new sparq11lTranslatorParser(commonTokenStream);// { q = sparqlQuery };
            sparqlParser.queryUnit();
            return sparqlQuery;

        }
    }
  
    
}
