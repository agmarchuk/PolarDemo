using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TripleStoreForDNR;


namespace SparqlParseRun
{
    public class SparqlQuery
    {
        internal readonly SparqlWhere SparqlWhere = new SparqlWhere();
        internal readonly SparqlWhere Construct = new SparqlWhere();
        internal readonly Dictionary<string, string> prefixes = new Dictionary<string, string>();
        internal static readonly Dictionary<string, Regex> Regexes = new Dictionary<string, Regex>();
        internal readonly List<Func<IEnumerable<SparqlResult>, IEnumerable<SparqlResult>>> ListSolutionModifiersOrder =
            new List<Func<IEnumerable<SparqlResult>, IEnumerable<SparqlResult>>>();
        internal readonly List<Func<IEnumerable<SparqlResult>, IEnumerable<SparqlResult>>> ListSolutionModifiersCount =
            new List<Func<IEnumerable<SparqlResult>, IEnumerable<SparqlResult>>>();

        internal bool isDistinct, isReduce, all;
        internal List<string> variables = new List<string>();
        internal List<string> constants = new List<string>();
        internal SparqlResultSet ResultSet = new SparqlResultSet();
        public List<SparqlNode> FilterConstants = new List<SparqlNode>();
        public SparqlResultSet Run(PolarTripleStore store)
        {
            ResultSet.Store = store;
            SparqlWhere.CreateNodes(store);

            foreach (SparqlNode filterConstant in FilterConstants)
                filterConstant.CreateNode(store);
            SparqlWhere.Run(ResultSet);


            switch (ResultSet.ResultType)
            {
                case ResultType.Describe:
                    ResultSet.GraphResult = store.CreateGraph();
                    var variablesIndexes = variables.Select(s => ResultSet.Variables[s].index).ToArray();
                    foreach (var result in ResultSet.Results)
                        foreach (var variableIndex in variablesIndexes)
                        {
                            foreach (var triple in store.GetTriplesWithSubject(result[variableIndex]))
                                ResultSet.GraphResult.Assert(triple);
                            foreach (var triple in store.GetTriplesWithObject(result[variableIndex]))
                                ResultSet.GraphResult.Assert(triple);
                        }
                    ResultSet.GraphResult.Build();
                    break;
                case ResultType.Select:
                    foreach (var func in ListSolutionModifiersOrder)
                        ResultSet.Results = new List<SparqlResult>(func(ResultSet.Results));

                    if (!all)
                        ResultSet = new SparqlResultSet(ResultSet, variables);
                    ResultSet.DistinctReduse(isDistinct, isReduce);
                    foreach (var func in ListSolutionModifiersCount)
                        ResultSet.Results = new List<SparqlResult>(func(ResultSet.Results));
                    break;
                case ResultType.Construct:
                    ResultSet.GraphResult = store.CreateGraph();
                    foreach (var result in ResultSet.Results)
                    {
                        Func<SparqlNode, INode> GetValueNode = node =>
                        {
                            if (node is VariableNode)
                                return result[(node as VariableNode).index];
                            SparqlUriNode uriNode = node as SparqlUriNode;
                            if (uriNode != null)
                                return store.GetUriNode(uriNode.Uri) ??
                                       ResultSet.GraphResult.CreateUriNode(uriNode.Uri);
                            SparqlLiteralNode literlNode = node as SparqlLiteralNode;
                            if (literlNode != null)
                                return store.GetLiteralNode(literlNode.type.Uri, literlNode.Content, literlNode.lang) ??
                                       (literlNode.lang != null
                                           ? ResultSet.GraphResult.CreateLiteralNode(literlNode.Content.ToString(),
                                               literlNode.lang)
                                           : ResultSet.GraphResult.CreateLiteralNode(literlNode.Content.ToString(),
                                               literlNode.type.Uri));
                            throw new NotImplementedException();
                        };
                        foreach (var sparqlTriplet in Construct.Triples.Cast<SparqlQuard>())
                        {       
                            ResultSet.GraphResult.Assert(new Triple(GetValueNode(sparqlTriplet.Subj),
                                GetValueNode(sparqlTriplet.Pred),
                                GetValueNode(sparqlTriplet.Obj)));
                        }
                    }
                    ResultSet.GraphResult.Build();
                    break;
                case ResultType.Ask:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return ResultSet;
        }


        internal VariableNode GetVariable(string name)
        {
            VariableNode parameter;
            var exists = (ResultSet.Variables.TryGetValue(name, out parameter));
            if (!exists)
                ResultSet.Variables.Add(name, parameter = new VariableNode()
                {
                    index = ResultSet.Variables.Count,
                    isNew = true
                });
            return parameter;
        }

        internal void SetSubjectIsDataFalse(VariableNode s)
        {

            throw new Exception();

        }

        internal Func<SparqlResult, dynamic> Call(SparqlUriNode name, IEnumerable<Func<SparqlResult, dynamic>> args)
        {
            if (name.Uri.AbsoluteUri == XmlSchema.XMLSchemaDouble.AbsoluteUri)
            {
                return result =>
                {
                    dynamic o = (args.First()(result));
                    if (o is string)
                        return (double.Parse(o.Replace(".", ",")));
                    if (o is double || o is int || o is float)
                        return (double) o;
                    throw new NotImplementedException();
                };

            }

            throw new NotImplementedException("mathod call " + name);
            //   if (name == ts.LiteralStore.@double)
            //    return store => 
            //Expression.Call(typeof(double).GetMethod("Parse", new[] { typeof(string), typeof(NumberStyles), typeof(CultureInfo) }), args.First(), Expression.Constant(NumberStyles.Any), Expression.Constant(new CultureInfo("en-us")));
        }

        internal static Func<SparqlResult, dynamic> RegExpression(VariableNode parameter, string regex,
            string extraParameters)
        {
            regex = regex.Trim('"');
            if (!Regexes.ContainsKey(regex))
                Regexes.Add(regex, new Regex(regex));
            //if (parameter.Type == typeof (object))
            //  parameter = Expression.Call(Expression.Convert(parameter, typeof (ILiteralNode)), "GetString", new Type[0]);
            return pac => Regexes[regex].IsMatch(parameter.Value.ToString());
            //typeof(Regex).GetMethod("IsMatch", new []{typeof(string)}), parameter);
        }


        //internal VariableNode Parameter(string parameter)
        //{
        //    VariableNode varNode;
        //    if (!ResultSet.Variables.TryGetValue(parameter, out varNode))
        //        ResultSet.Variables.Add(parameter, varNode = new VariableNode());
        //    return varNode;
        //}

        //internal static Expression BinaryCompareExpression(ExpressionType typeBinExp, Expression expressionLeft, Expression expressionRight)
        //{   
        //    if (expressionLeft.Type == typeof (bool))
        //        switch (typeBinExp)
        //        {
        //            case ExpressionType.LessThan:
        //                return Expression.And(Expression.Not(expressionLeft), expressionRight);
        //            case ExpressionType.GreaterThan:
        //                return Expression.And(expressionLeft, Expression.Not(expressionRight));
        //            case ExpressionType.LessThanOrEqual:
        //                return Expression.Or(Expression.Not(expressionLeft), expressionRight);
        //            case ExpressionType.GreaterThanOrEqual:
        //                return Expression.Or(expressionLeft, Expression.Not(expressionRight));
        //        }

        //    var type = typeof(string);
        //    if (expressionLeft.Type == type)
        //        switch (typeBinExp)
        //        {
        //            case ExpressionType.LessThan:
        //                return Expression.Equal(Expression.Call(expressionLeft, type.GetMethod("CompareTo", new []{ type }), expressionRight), Expression.Constant(-1));
        //            case ExpressionType.GreaterThan:
        //                return Expression.Equal(Expression.Call(expressionLeft, type.GetMethod("CompareTo", new []{ type }), expressionRight), Expression.Constant(1));
        //            case ExpressionType.LessThanOrEqual:
        //                return Expression.Not(Expression.Equal(Expression.Call(expressionLeft, type.GetMethod("CompareTo", new[] { type }), expressionRight), Expression.Constant(1)));
        //            case ExpressionType.GreaterThanOrEqual:
        //                return Expression.Not(Expression.Equal(Expression.Call(expressionLeft, type.GetMethod("CompareTo", new []{ type }), expressionRight), Expression.Constant(-1)));
        //        }
        //    return Expression.MakeBinary(typeBinExp, expressionLeft, expressionRight);
        //}


        internal Func<SparqlResult, dynamic> Langmatch(Func<SparqlResult, dynamic> variable,
            Func<SparqlResult, dynamic> lang)
        {
            return (result) =>
            {
                dynamic variableNode = variable(result);
                dynamic langNode = lang(result);
                if (variableNode is string && langNode is SLiteralNode)
                    return langNode.Value != null && variableNode.ToLower() == langNode.Value.ToString().ToLower();
                if (variableNode is string && langNode is string)
                    return variableNode.ToLower() == langNode.ToLower();
                throw new NotImplementedException();
            };
                // Expression.Equal(Expression.Call(expression1, typeof(string).GetMethod("ToLower", new Type[0])), Expression.Call(expression2, typeof(string).GetMethod("ToLower", new Type[0])));
        }

        internal Func<SparqlResult, dynamic> Langmatch(Func<SparqlResult, dynamic> variable)
        {
            return (result) =>
            {
                dynamic variableValue = variable(result);
                if (variableValue is VariableNode)
                    return !string.IsNullOrWhiteSpace(variableValue.Value);
                if (variableValue is string)
                    return !string.IsNullOrWhiteSpace(variableValue);
                throw new NotImplementedException();
            };
                // Expression.Equal(Expression.Call(expression1, typeof(string).GetMethod("ToLower", new Type[0])), Expression.Call(expression2, typeof(string).GetMethod("ToLower", new Type[0])));
        }

        internal Func<SparqlResult, dynamic> Bound(VariableNode variable)
        {
            return (result) => result[variable.index] != null;      }

        internal Func<SparqlResult, dynamic> Lang(SparqlLiteralNode literal)
        {
            //  if(literal is ILiteralNode)
            return (result) => ((ILiteralNode) literal.Value).Language;
            //  if (text != null) return Expression.Constant(text.lang);
            //  throw new Exception("8");
        }

        internal Func<SparqlResult, dynamic> Lang(VariableNode variable)
        {
            return (result) => variable.Value == null ? null : ((ILiteralNode) variable.Value).Language;
        }
    }
}
