using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using ANTLR_Test;
using Sharpen;
using TrueRdfViewer;

namespace SparqlParser
{
    public class Variable
    {                  
        public bool isNew;
        public short index;
        public object pacElement;
        public GraphIsDataProperty graph;
    }
    public class Query
    {                   
        internal Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> Where;
        internal Dictionary<string, Variable> Variables = new Dictionary<string, Variable>();
        internal short currentNewVariablesIndex = 0;
        internal readonly Dictionary<string, string> prefixes = new Dictionary<string, string>();
        internal static Dictionary<string, Regex> Regexes = new Dictionary<string, Regex>();
        internal Dictionary<object, GraphIsDataProperty> isDataGraph = new Dictionary<object, GraphIsDataProperty>();     
        internal ParameterExpression currentFilterParameter = Expression.Parameter(typeof (RPackInt));

        internal bool isDistinct, isReduce, all;
        internal Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> solutionModifierOrder;
        internal Func<IEnumerable<object[]>, IEnumerable<object[]>> solutionModifierCount;
        internal List<string> variables=new List<string>();
        internal List<int> constants=new List<int>();

        #region Run

        public string Run(TripleStoreInt ts)
        {
            if (AsqRun != null) return AsqRun(ts).ToString();
            if (SelectRun != null)
                return string.Join(Environment.NewLine, SelectRun(ts).Select(spo => string.Join(", ", spo)));
            if (DescribeRun != null)
                return string.Join(Environment.NewLine, DescribeRun(ts).Select(spo => string.Join(" ", spo)));
            if (ConstructRun != null)
                return string.Join(Environment.NewLine, ConstructRun(ts).Select(spo => string.Join(" ", spo)));
            throw new Exception();
        }

        public Func<TripleStoreInt, IEnumerable<IEnumerable<object>>> SelectRun;

        internal void CreateSelectRun()
        {
            SelectRun = ts =>
            {
                var row = new object[Variables.Count];

                if (isReduce) throw new NotImplementedException();
                IEnumerable<RPackInt> result = Where(Repeat(row, ts)).Select(i => (RPackInt)((ICloneable)i).Clone());
                
                if (solutionModifierOrder != null) result = solutionModifierOrder(result);
                var selectResult = all
                    ? result.Select(pack => pack.row)
                    //TODO unbounded variables;
                    : result.Select(pack => 
                        variables.Select(s => Variables[s].index)
                                 .Select(index => pack[index]).ToArray());

                if (isDistinct) selectResult = new HashSet<object[]>(selectResult,new SequenceEqualityComparer<object>());

                if (solutionModifierCount != null) selectResult = solutionModifierCount(selectResult);    
                return selectResult;
            };
        }

        private static IEnumerable<RPackInt> Repeat(object[] row, TripleStoreInt ts)
        {                                    
           yield return new RPackInt(row, ts);
        }

        public Func<TripleStoreInt, IEnumerable<IEnumerable<string>>> DescribeRun;

        internal void CreateDescribeRun()
        {
            DescribeRun = ts =>
            {
                var result = Where != null
                    ? Where(Enumerable.Repeat(new RPackInt(Variables.Values.Select(variable => variable.pacElement).ToArray(), ts), 1))
                    : Enumerable.Empty<RPackInt>();
                if (solutionModifierOrder != null) result = solutionModifierOrder(result);

                IEnumerable<int> describeObjects;
                if (all)
                    describeObjects = result.SelectMany(pack => pack.row.Cast<int>());
                else
                    try
                    {
                        describeObjects =
                            variables.Select(s => Variables[s].index).SelectMany(i => result.Select(pack => (int)pack[i]));
                    }
                    catch (KeyNotFoundException e)
                    {
                        throw new Exception("describe variable was not founded at query", e);
                    }
                
                describeObjects = describeObjects.Concat(constants).ToArray();
            //    if (describeObjects.Any(o => o is Literal)) throw new Exception("describe literal");

                IEnumerable<object[]> describeResults = 
                    describeObjects
                    .SelectMany(o => ts.GetObjBySubj(o)
                        .Select(pair =>new object[]{o, pair.Value, pair.Key})
                    .Concat(ts.GetSubjectByObj(o)
                        .Select(pair =>new Object[] {pair.Key, pair.Value, o}))
                    .Concat(ts.GetDataBySubj(o)
                        .Select(pair => new Object[] { o, pair.Value, pair.Key})));
                if (solutionModifierCount != null) describeResults = solutionModifierCount(describeResults);
                return
                    describeResults.Select(
                        objects => objects.Select(o => o is Int32 ? TripleInt.Decode((int) o) : o.ToString()));

            };
        }

        public Func<TripleStoreInt, IEnumerable<IEnumerable<object>>> ConstructRun;

        internal void CreateConstructRun()
        {
            ConstructRun = ts =>
            {
                IEnumerable<RPackInt> result = Where != null
                    ? Where(Enumerable.Repeat(new RPackInt(Variables.Values.Select(variable => variable.pacElement).ToArray(), ts), 1)).ToArray()
                    : Enumerable.Empty<RPackInt>();
                if (solutionModifierOrder != null) result = solutionModifierOrder(result).ToArray();
                 
                if (constructTriples == null) return Enumerable.Empty<IEnumerable<string>>();
                var constructResult = result.SelectMany(constructTemplate);
                var constructResultSequences = constructResult.Select(tuple => new object[] { tuple.Item1, tuple.Item2, tuple.Item3 });
                if (solutionModifierCount != null)
                    constructResultSequences = solutionModifierCount(constructResultSequences);
                return constructResultSequences;
            };
        }

        public Func<TripleStoreInt, bool> AsqRun;
        internal Func<RPackInt, IEnumerable<Tuple<string, string, string>>> constructTemplate;

        internal readonly List<Func<RPackInt, IEnumerable<Tuple<string, string, string>>>> constructTriples = new List<Func<RPackInt, IEnumerable<Tuple<string, string, string>>>>();

        internal void CreateAsqRun()
        {
            AsqRun = ts => Where(Enumerable.Repeat(new RPackInt(Variables.Values.Select(variable => variable.pacElement).ToArray(), ts), 1)).Any();
        }


        internal void CreateConstructTemplate()
        {
            constructTemplate = pac => constructTriples.SelectMany(func => func(pac));
        }

        #endregion

        internal Variable GetVariable(string name)
        {
            Variable parameter;
            var exists = (Variables.TryGetValue(name, out parameter));
            if (exists)
                parameter.graph = isDataGraph[parameter.index];
            else
            {
                Variables.Add(name, parameter = new Variable() {index = (short) Variables.Count, isNew = true});
                isDataGraph.Add(parameter.index, parameter.graph = new GraphIsDataProperty());
            }
            parameter.pacElement = parameter.index;
            return parameter;
        }

        internal void SetSubjectIsDataFalse(Variable s)
        {   
            if (s.graph == null)
                isDataGraph.Add(s.pacElement, s.graph = new GraphIsDataProperty{IsData = false});
            else if (s.graph.IsData == null)	        
                  s.graph.Set(false);
              else if (s.graph.IsData.Value)
                  throw new Exception();

        }
        
        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> CreateTriplet(Variable s, Variable p, Variable o, Literal d)
        {   
           GraphIsDataProperty.Sync(o.graph, p.graph);
            var triplet = s.isNew
                ? (p.isNew
                    ? (o.isNew
                        ? (o.graph.IsData == null
                            ? RPackComplexExtensionInt.CallObjectOrData(p.graph,
                                RPackComplexExtensionInt.SPO(s.index, p.index, o.index),
                                RPackComplexExtensionInt.SPD(s.index, p.index, o.index))
                            : (o.graph.IsData.Value
                                ? RPackComplexExtensionInt.SPD(s.index, p.index, o.index)
                                : RPackComplexExtensionInt.SPO(s.index, p.index, o.index)))
                        : (o.graph.IsData == null
                            ? RPackComplexExtensionInt.CallObjectOrData(p.graph,
                                RPackComplexExtensionInt.SPo(s.index, p.index, o.pacElement),
                                RPackComplexExtensionInt.SPd(s.index, p.index, d))
                            : (o.graph.IsData.Value
                                ? RPackComplexExtensionInt.SPd(s.index, p.index, d)
                                : RPackComplexExtensionInt.SPo(s.index, p.index, o.pacElement))))
                    : (o.isNew
                        ? (o.graph.IsData == null
                            ? RPackComplexExtensionInt.CallObjectOrData(p.graph,
                                RPackComplexExtensionInt.SpO(s.index, p.pacElement, o.index),
                                RPackComplexExtensionInt.SpD(s.index, p.pacElement, o.index))
                            : (o.graph.IsData.Value
                                ? RPackComplexExtensionInt.SpD(s.index, p.pacElement, o.index)
                                : RPackComplexExtensionInt.SpO(s.index, p.pacElement, o.index)))
                        : (o.graph.IsData == null
                            ? RPackComplexExtensionInt.CallObjectOrData(p.graph,
                                RPackComplexExtensionInt.Spo(s.index, p.pacElement, o.pacElement),
                                RPackComplexExtensionInt.Spd(s.index, p.pacElement, d))
                            : (o.graph.IsData.Value
                                ? RPackComplexExtensionInt.Spd(s.index, p.pacElement, d)
                                : RPackComplexExtensionInt.Spo(s.index, p.pacElement, o.pacElement)))))
                : (p.isNew
                    ? (o.isNew
                        ? (o.graph.IsData == null
                            ? RPackComplexExtensionInt.CallObjectAndData(
                                RPackComplexExtensionInt.sPO(s.pacElement, p.index, o.index, p.graph),
                                RPackComplexExtensionInt.sPD(s.pacElement, p.index, o.index, p.graph))
                            : (o.graph.IsData.Value
                                ? RPackComplexExtensionInt.sPD(s.pacElement, p.index, o.index, p.graph)
                                : RPackComplexExtensionInt.sPO(s.pacElement, p.index, o.index, p.graph)))
                        : (o.graph.IsData == null
                            ? RPackComplexExtensionInt.CallObjectOrData(p.graph,
                                RPackComplexExtensionInt.sPo(s.pacElement, p.index, o.index),
                                RPackComplexExtensionInt.sPd(s.pacElement, p.index, d))
                            : (o.graph.IsData.Value
                                ? RPackComplexExtensionInt.sPd(s.pacElement, p.index, d)
                                : RPackComplexExtensionInt.sPo(s.pacElement, p.index, o.pacElement))))
                    : (o.isNew
                        ? (o.graph.IsData == null
                            ? RPackComplexExtensionInt.CallObjectOrData(p.graph,
                                RPackComplexExtensionInt.spO(s.pacElement, p.pacElement, o.index),
                                RPackComplexExtensionInt.spD(s.pacElement, p.pacElement, o.index))
                            : (o.graph.IsData.Value
                                ? RPackComplexExtensionInt.spD(s.pacElement, p.pacElement, o.index)
                                : RPackComplexExtensionInt.spO(s.pacElement, p.pacElement, o.index)))
                        : (o.graph.IsData == null
                            ? RPackComplexExtensionInt.CallObjectOrData(p.graph,
                                RPackComplexExtensionInt.spo(s.pacElement, p.pacElement, o.pacElement),
                                RPackComplexExtensionInt.spd(s.pacElement, p.pacElement, d))
                            : (o.graph.IsData.Value
                                ? RPackComplexExtensionInt.spd(s.pacElement, p.pacElement, d)
                                : RPackComplexExtensionInt.spo(s.pacElement, p.pacElement, o.pacElement)))));
            s.isNew = p.isNew = o.isNew = false;
            return triplet;
        }

       

        internal Expression EqualOrAssign(Expression left, Expression right, Variable leftVariable, Variable rightVariable)
        {   
            var isRightNewSingleVariable = rightVariable != null && rightVariable.isNew;
            if (leftVariable!=null && leftVariable.isNew)
                if (isRightNewSingleVariable)
                    throw new Exception("both new parameters");
                else
                    left = ExpressionAssignValueToVariable(left, right, leftVariable);
            else if (isRightNewSingleVariable)
                right = ExpressionAssignValueToVariable(right, left, rightVariable);

            Sync(ref left, leftVariable, ref right, rightVariable);
            return Expression.Equal(left, right);
        }

        private Expression ExpressionAssignValueToVariable(Expression varExpression, Expression constExpression, Variable unknownVariable)
        {
            if (constExpression.Type == typeof(int))
            {
                varExpression = Expression.Property(currentFilterParameter, "Item", Expression.Constant(unknownVariable.index));
                unknownVariable.graph.Set(false);
            }
            else if (constExpression.Type == typeof(double))
            {
                varExpression = Expression.Property(Expression.ConvertChecked(varExpression, typeof(Literal)), "Value");
                unknownVariable.graph.Set(LiteralVidEnumeration.integer);
            }
            else if (constExpression.Type == typeof(long))
            {
                varExpression = Expression.Property(Expression.ConvertChecked(varExpression, typeof(Literal)), "Value");
                unknownVariable.graph.Set(LiteralVidEnumeration.date);
            }
            else if (constExpression.Type == typeof(bool))
            {
                varExpression = Expression.Property(Expression.ConvertChecked(varExpression, typeof(Literal)), "Value");
                unknownVariable.graph.Set(LiteralVidEnumeration.boolean);
            }
            else if (constExpression.Type == typeof(string))
            {
                varExpression = Expression.Property(Expression.Convert(Expression.Property(Expression.Convert(varExpression, typeof(Literal)), "Value"), typeof(Text)), "Value");
                unknownVariable.graph.Set(LiteralVidEnumeration.text);
            }
            varExpression = Expression.Convert(Expression.Assign(varExpression, Expression.Convert(constExpression, typeof(object))), constExpression.Type);
            
            return varExpression;
        }

        internal static Expression Call(string name, IEnumerable<Expression> args)
        {   
            if (name == @"http://www.w3.org/2001/XMLSchema#double")
                return Expression.Call(typeof(double).GetMethod("Parse", new []{ typeof(string)}), Expression.Call(args.First(), typeof(string).GetMethod("Replace", new Type[]{typeof(char),typeof(char)}),Expression.Constant('.'),Expression.Constant(',')));
            throw new NotImplementedException("mathod call " + name);
        }

        internal static Expression RegExpression(Expression parameter, string regex, string extraParameters)
        {
            regex = regex.Trim('"');
            if (!Regexes.ContainsKey(regex))
                Regexes.Add(regex, new Regex(regex));
            if (parameter.Type == typeof (object))
                parameter = Expression.Call(Expression.Convert(parameter, typeof (Literal)), "GetString", new Type[0]);
            return Expression.Call(Expression.Constant(Regexes[regex]),
                typeof(Regex).GetMethod("IsMatch", new []{typeof(string)}), parameter);
        }

      
        internal Expression Parameter(Variable parameter)
        {
            if (parameter.graph.IsData == null)
            {
                var obj = Expression.Property(currentFilterParameter, "Item", Expression.Constant(parameter.index));
                //return Expression.IfThenElse(Expression.TypeIs(obj, typeof(Int32)),
                //                             Expression.ConvertChecked(obj, typeof(Int32)),
                //    Expression.IfThenElse(Expression.Not(Expression.TypeIs(obj, typeof(Literal))), 
                //                    obj,
                //    Expression.IfThenElse(Expression.TypeIs(Expression.Property(Expression.ConvertChecked(obj, typeof(Literal)), "Value"), typeof(double)),
                //                   Expression.ConvertChecked(Expression.Property(Expression.ConvertChecked(obj, typeof(Literal)), "Value"), typeof(double)),
                //    Expression.IfThenElse(Expression.TypeIs(Expression.Property(Expression.ConvertChecked(obj, typeof(Literal)), "Value"), typeof(long)),
                //                   Expression.ConvertChecked(Expression.Property(Expression.ConvertChecked(obj, typeof(Literal)), "Value"), typeof(long)),
                //                             Expression.Call(Expression.ConvertChecked(obj, typeof(Literal)), typeof(Literal).GetMethod("ToString"))))));
                return obj;
            }
            if (!parameter.graph.IsData.Value)
               return Expression.Call(currentFilterParameter, typeof (RPackInt).GetMethod("GetE"), Expression.Constant(parameter.index, typeof (object)));
            return LiteraExpression(Expression.Property(
                Expression.Call(currentFilterParameter, typeof(RPackInt).GetMethod("Val"), Expression.Constant(parameter.index)),
                "Value"), parameter.graph.vid);
        }

        internal static Expression LiteraExpression(Expression literalValueExpr, LiteralVidEnumeration literalVidEnumeration)
        {
            Type type=null;
            switch (literalVidEnumeration)
            {
                case LiteralVidEnumeration.text:
                    return Expression.Property(
                        Expression.Convert(literalValueExpr, typeof (Text)),
                        "Value");
                case LiteralVidEnumeration.typedObject:
                    return Expression.Property(
                        Expression.Convert(literalValueExpr, typeof(TypedObject)),
                        "Value");
                case LiteralVidEnumeration.integer:
                {
                    return Expression.Call(typeof(Query).GetMethod("Convert"), literalValueExpr);
                }
                    break;
                case LiteralVidEnumeration.date:
                    type = typeof (long);
                    break;
                case LiteralVidEnumeration.boolean:
                    type = typeof (bool);
                    break;
                case LiteralVidEnumeration.nil:
                    throw new NotImplementedException();
            }
            return Expression.ConvertChecked(literalValueExpr, type);
        } 



                     
        //internal string ReplaceNamespacePrefix(string value)
        //{

        //    var nsO = value.Split(':');
        //    string nsUri;
        //    if (!prefixes.TryGetValue(nsO[0].Trim(), out nsUri))
        //        throw new Exception("неизвестное пространство имён " + nsO[0]);
        //    return nsUri + nsO[1].Trim();
        //}

        internal static Expression BinaryCompareExpression(ExpressionType typeBinExp, Expression expressionLeft, Expression expressionRight)
        {   
            if (expressionLeft.Type == typeof (bool))
                switch (typeBinExp)
                {
                    case ExpressionType.LessThan:
                        return Expression.And(Expression.Not(expressionLeft), expressionRight);
                    case ExpressionType.GreaterThan:
                        return Expression.And(expressionLeft, Expression.Not(expressionRight));
                    case ExpressionType.LessThanOrEqual:
                        return Expression.Or(Expression.Not(expressionLeft), expressionRight);
                    case ExpressionType.GreaterThanOrEqual:
                        return Expression.Or(expressionLeft, Expression.Not(expressionRight));
                }

            var type = typeof(string);
            if (expressionLeft.Type == type)
                switch (typeBinExp)
                {
                    case ExpressionType.LessThan:
                        return Expression.Equal(Expression.Call(expressionLeft, type.GetMethod("CompareTo", new []{ type }), expressionRight), Expression.Constant(-1));
                    case ExpressionType.GreaterThan:
                        return Expression.Equal(Expression.Call(expressionLeft, type.GetMethod("CompareTo", new []{ type }), expressionRight), Expression.Constant(1));
                    case ExpressionType.LessThanOrEqual:
                        return Expression.Not(Expression.Equal(Expression.Call(expressionLeft, type.GetMethod("CompareTo", new[] { type }), expressionRight), Expression.Constant(1)));
                    case ExpressionType.GreaterThanOrEqual:
                        return Expression.Not(Expression.Equal(Expression.Call(expressionLeft, type.GetMethod("CompareTo", new []{ type }), expressionRight), Expression.Constant(-1)));
                }
            return Expression.MakeBinary(typeBinExp, expressionLeft, expressionRight);
        }

        internal static void Sync(ref Expression expressionLeft, Variable variableLeft,
                                  ref Expression expressionRight, Variable variableRight, Type forceType=null)
        {
            if (variableLeft == null)
                if (variableRight == null)
                {
                    if (expressionLeft.Type == expressionRight.Type) return;
                    if (expressionLeft.Type == typeof(object))
                    {
                        if(expressionRight.Type == typeof(double))
                            expressionLeft = Expression.Call(typeof(Query).GetMethod("Convert"), expressionLeft);
                        else
                            expressionLeft = Expression.ConvertChecked(expressionLeft, expressionRight.Type); 
                    }
                    else if (expressionRight.Type == typeof(object))
                    {
                        if (expressionLeft.Type == typeof(double))
                            expressionRight = Expression.Call(typeof(Query).GetMethod("Convert"), expressionRight);
                        else
                            expressionRight = Expression.ConvertChecked(expressionRight, expressionLeft.Type); 
                    }
                    throw new Exception("types Sync");
                }
                else expressionRight = Sync(expressionRight, variableRight, forceType ?? expressionLeft.Type);
            else if (variableRight == null)
                expressionLeft = Sync(expressionLeft, variableLeft, forceType ?? expressionRight.Type);
            else
            {
                if (forceType == null) forceType = typeof (string);
                expressionLeft = Sync(expressionLeft, variableLeft, forceType);
                expressionRight = Sync(expressionRight, variableRight, forceType);
            }
        }

        private static Expression Sync(Expression varExpression, Variable variable,  Type forceType)
        {
            if (varExpression.Type == forceType) return varExpression;                  
            return  SetVariableByType(varExpression, variable, forceType);   
        }

        internal static Expression SetVariableByType(Expression expressionUnknown, Variable variableUnknown, Type type)
        {
            if (expressionUnknown.Type == type) return expressionUnknown;
            if (type == typeof(int))
            {
                variableUnknown.graph.Set(false);
                return Expression.ConvertChecked(expressionUnknown, typeof(int));
            }
            return SetVariableLiteraByType(expressionUnknown, variableUnknown, type);
        }

        internal static Expression SetVariableLiteraByType(Expression expressionUnknown, Variable variableUnknown, Type type)
        {

            if (expressionUnknown.Type == type) return expressionUnknown;
            var literalValueExpression = Expression.Property(Expression.ConvertChecked(expressionUnknown, typeof(Literal)), "Value");
            if (variableUnknown == null) return Expression.ConvertChecked(literalValueExpression, type);
            var literalVidEnumeration = LiteralVidEnumeration.nil;
            if (type == typeof (string))      //TODO
            {
                literalVidEnumeration = LiteralVidEnumeration.text;
                literalValueExpression = Expression.Property(Expression.ConvertChecked(literalValueExpression, typeof(Text)),
                    "Value");
            }
            else if (type == typeof (double))
            {
                literalVidEnumeration = LiteralVidEnumeration.integer;
                if (variableUnknown.graph.IsData == null && variableUnknown.graph.vid != literalVidEnumeration)
                    variableUnknown.graph.Set(literalVidEnumeration);
                return Expression.Call(typeof(Query).GetMethod("Convert"), literalValueExpression);
            }
            else if (type == typeof (long))
                literalVidEnumeration = LiteralVidEnumeration.date;
            else if (type == typeof (bool))
                literalVidEnumeration = LiteralVidEnumeration.boolean;

            if (variableUnknown.graph.IsData == null)
                variableUnknown.graph.Set(literalVidEnumeration);
            return Expression.ConvertChecked(literalValueExpression, type);
        }

        internal Expression Langmatch(Expression expression1, Variable variable1, Expression expression2, Variable variable2)
        {
            return Expression.Equal(Expression.Call(expression1, typeof(string).GetMethod("ToLower", new Type[0])), Expression.Call(expression2, typeof(string).GetMethod("ToLower", new Type[0])));
        }   
        internal Expression Bound(Variable variable)
        {
            if (variable.graph.IsData == null)
                return Expression.Call(currentFilterParameter, typeof (RPackInt).GetMethod("Hasvalue"),
                    Expression.Constant(variable.index));
            if (!variable.graph.IsData.Value)
                return Expression.NotEqual(Expression.Call(currentFilterParameter, typeof(RPackInt).GetMethod("GetE"), Expression.Constant((object)variable.index, typeof(object))),
                        Expression.Constant(Int32.MinValue)); 
            else
            switch (variable.graph.vid)
            {
                case LiteralVidEnumeration.integer:
                    return Expression.NotEqual(Expression.Call(currentFilterParameter, typeof(RPackInt).GetMethod("Vai"),
                        Expression.Constant(variable.index)), Expression.Constant(double.MinValue));
                case LiteralVidEnumeration.date:
                    return Expression.NotEqual(Expression.ConvertChecked(
                        Expression.Call(currentFilterParameter, typeof(RPackInt).GetMethod("Val"), Expression.Constant(variable.index)),
                        typeof(long)), Expression.Constant(DateTime.MinValue.ToBinary()));
                case LiteralVidEnumeration.boolean:
                    throw new NotImplementedException();
                case LiteralVidEnumeration.nil:
                    throw new NotImplementedException();
                case LiteralVidEnumeration.typedObject:
                    throw new NotImplementedException();  
                case LiteralVidEnumeration.text:
                    return Expression.NotEqual(Expression.Property(Expression.Convert(Expression.Property(
                        Expression.ConvertChecked(
                            Expression.Call(currentFilterParameter,  typeof(RPackInt).GetMethod("Val"),  Expression.Constant(variable.index)),
                            typeof (Literal)), "Value"), typeof(Text)),"Value"), Expression.Constant(string.Empty));
                default:
                    throw new ArgumentOutOfRangeException();
            }   
            //{
            //    var literalExpression = Expression.Convert(Expression.Property(currentFilterParameter, "Item", Expression.Constant(variable.index)),typeof(Literal));
            //    return Expression.And(Expression.NotEqual(Expression.Property(currentFilterParameter, "GetE",  Expression.Constant((object)variable.index)),
            //        Expression.Constant(Int32.MinValue)),
            //       Expression.And(Expression.NotEqual(Expression.Property(currentFilterParameter, "Item", Expression.Constant(variable.index)),
            //                            Expression.Constant(null)),
            //        Expression.And(Expression.NotEqual(literalExpression,Expression.Constant(null)),
            //            Expression.And(Expression.NotEqual(parameterIntExpression, Expression.Constant(double.MinValue)),
            //            Expression.And(Expression.NotEqual(parameterDateExpression, Expression.Constant(DateTime.MinValue.ToBinary())),
            //          Expression.NotEqual(parameterTextExpression,Expression.Constant(string.Empty)))))));
            //}
        }

        internal Expression Langmatch(Expression expression, Variable variableLeft)
        {
            if(variableLeft==null)
                          return Expression.NotEqual(expression, Expression.Constant(string.Empty));
            if (variableLeft.graph.IsData == null)
                variableLeft.graph.Set(LiteralVidEnumeration.text);
            return Expression.NotEqual(Parameter(variableLeft), Expression.Constant(string.Empty));

        }

        internal Expression Lang(Literal literal)
        {
            var text = literal.Value as Text;
            if (text != null) return Expression.Constant(text.Lang);
            throw new Exception("8");
        }

        internal Expression Lang(Variable variable)
        {
            if (variable.graph.IsData == null)
                variable.graph.Set(LiteralVidEnumeration.text);

           return  Expression.Property(Expression.Convert( Expression.Property(
                        Expression.Convert(
                                Expression.Call(currentFilterParameter, typeof(RPackInt).GetMethod("Val"), Expression.Constant(variable.index)),
                         typeof(Literal)),
                         "Value"),
                               typeof(Text)),
                   "Lang");
        }
        public static double Convert(object literal)
        {
            double s;
            if (literal is int)
                s = (double)(int)(literal);
            else if (literal is float)
                s = (double)(float)(literal);
            else
                s = (double)(literal);
            return s;
        }
    }
}
