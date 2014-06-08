/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */

grammar sparql2Pac;
options
{
	language = CSharp2;
}
@parser::namespace { Generated }
@lexer::namespace  { Generated }
@header{
	using System;
	using System.Linq;
	using System.Xml.Linq;		
	using System.Text.RegularExpressions;
    using Sparql;
    using TrueRdfViewer;
	using System.Linq.Expressions;
	using TripleIntClasses;
}


@members{		  	

public static Regex PrefixNSSlpit=new Regex("^([^:]*:)(.*)$");
public readonly Query q=new Query();		
}


query	 : prologue ( selectQuery | constructQuery | describeQuery | askQuery ) ;	 


prologue	 :basedecl? prefixDecl*;


basedecl	 :'BASE' IRI_REF ;


prefixDecl	 :'PREFIX'  PNAME_NS IRI_REF  
{ 	
	var iri=$IRI_REF.text;
	iri=iri.Substring(1,iri.Length-2);
	q.prefixes.Add($PNAME_NS.text, iri);	
 } ;


selectQuery	 :'SELECT'	 
( 'DISTINCT' { q.isDistinct=true;	}  | 'REDUCED' { q.isReduce=true;	} )? 
( (varLiteral	{ q.variables.Add($varLiteral.text);	 } )+  | '*'  { q.all=true;	} )		   
datasetClause* whereClause solutionModifier
{  q.CreateSelectRun(); } ;


constructQuery	 :'CONSTRUCT' constructTemplate datasetClause* whereClause solutionModifier
{	    
q.CreateConstructRun();
};


describeQuery	
 : 'DESCRIBE'  
( (varLiteral {q.variables.Add($varLiteral.text);} | iRIref { q.constants.Add(TripleInt.CodeEntities( $iRIref.text)); } )+ 
| '*' { q.all=true; }) datasetClause* whereClause? solutionModifier  
{q.CreateDescribeRun();};


askQuery	 :'ASK'  datasetClause* whereClause {q.CreateAsqRun();};


datasetClause	 :'FROM'  ( defaultGraphClause | namedGraphClause );


defaultGraphClause	 : sourceSelector;


namedGraphClause	 :'NAMED' sourceSelector;


sourceSelector	: iRIref ;


whereClause	 :'WHERE'?
 groupGraphPattern { q.Where=$groupGraphPattern.value; };


solutionModifier 
: (orderClause { q.solutionModifierOrder = $orderClause.value; } )? 
(limitOffsetClauses 
{
q.solutionModifierCount=$limitOffsetClauses.value;
})? ;


limitOffsetClauses		returns [Func<IEnumerable<object[]>, IEnumerable<object[]>> value]
 : (limitClause { $value =$limitClause.value; } ( offsetClause { var valueClone=$value.Clone()  as Func<IEnumerable<object[]>,IEnumerable<object[]>>; $value = packs=> $offsetClause.value (valueClone(packs)); })? 
 | offsetClause { $value =$offsetClause.value; } (limitClause { var valueClone=$value.Clone() as Func<IEnumerable<object[]>,IEnumerable<object[]>>; $value = packs=> $limitClause.value (valueClone(packs)); })? ) ;


orderClause	returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> value]
 :'ORDER' 'BY' main= orderCondition { $value = $main.value; }
  (others = orderCondition { var valueClone=$value.Clone()  as Func<IEnumerable<RPackInt>,IEnumerable<RPackInt>>; var othersRes=$others.value; $value = packs => othersRes(valueClone(packs)); })*	;


orderCondition	 returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> value, bool isDescending]
:  { q.currentFilterParameter = Expression.Parameter(typeof (RPackInt)); }
(( 'ASC' | 'DESC' { $isDescending=true;  } ) brackettedExpression 
{ 
 var orderFunc = Expression.Lambda($brackettedExpression.value, q.currentFilterParameter).Compile();
 if($isDescending)
	$value = pacs => pacs.OrderByDescending(pac=>
						{
						    var o = orderFunc.DynamicInvoke(pac);
                            if(o is Int32)
						    return (int)o;
                            else if (!(o is Literal))
                                return o;
                            else
                            {
                                var l = (Literal)o;
                                if (l.Value is double)
                                    return (double) l.Value;
                                if (l.Value is long)
                                    return (long)l.Value;
                                return l.ToString();
                            }
						});
 else 
$value = pacs => pacs.OrderByDescending(pac=>
						{
						    var o = orderFunc.DynamicInvoke(pac);
                            if(o is Int32)
						    return (int)o;
                            else if (!(o is Literal))
                                return o;
                            else
                            {
                                var l = (Literal)o;
                                if (l.Value is double)
                                    return (double) l.Value;
                                if (l.Value is long)
                                    return (long)l.Value;
                                return l.ToString();
                            }
						});
 	 } ) 
| ( constraint {
  var orderExpr = Expression.Lambda($constraint.value, q.currentFilterParameter).Compile();	 
	$value = pacs=> pacs.OrderBy(pac=>
						{
						    var o = orderExpr.DynamicInvoke(pac);
                            if(o is Int32)
						    return (int)o;
                            else if (!(o is Literal))
                                return o;
                            else
                            {
                                var l = (Literal)o;
                                if (l.Value is double)
                                    return (double) l.Value;
                                if (l.Value is long)
                                    return (long)l.Value;
                                return l.ToString();
                            }
						});
 } | var {
 var orderExpr = Expression.Lambda(q.Parameter($var.p), q.currentFilterParameter).Compile();	 
	$value = pacs=> pacs.OrderBy(pac=>
						{
						    var o = orderExpr.DynamicInvoke(pac);
                            if(o is Int32)
						    return (int)o;
                            else if (!(o is Literal))
                                return o;
                            else
                            {
                                var l = (Literal)o;
                                if (l.Value is double)
                                    return (double) l.Value;
                                if (l.Value is long)
                                    return (long)l.Value;
                                return l.ToString();
                            }
						});
 } )	  ;


limitClause	 returns [Func<IEnumerable<object[]>, IEnumerable<object[]>> value]
:'LIMIT' INTEGER  { $value=sequence=>sequence.Take(int.Parse($INTEGER.text)); };


offsetClause	returns [Func<IEnumerable<object[]>, IEnumerable<object[]>> value] 
:'OFFSET' INTEGER { $value=sequence=>sequence.Skip(int.Parse($INTEGER.text)); };


groupGraphPattern	 returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> value]	
:'{' (strt=triplesBlock {$value=$strt.value;})? 

( ( graphPatternNotTriples  
		{ if($value==null) { $value=$graphPatternNotTriples.value;  }
					else { var valueClone=$value.Clone() as Func<IEnumerable<RPackInt>,IEnumerable<RPackInt>>;
					  var graphPatternNotTriplesvalue=$graphPatternNotTriples.value;
					     $value=packs=>graphPatternNotTriplesvalue(valueClone(packs)); }}

	| filter 
		{ if($value==null) { $value=$filter.value;  }
					else {var valueClone=$value.Clone() as Func<IEnumerable<RPackInt>,IEnumerable<RPackInt>>; 
					var filtervalue=$filter.value; 
					$value=packs=> filtervalue(valueClone(packs));
		}			 }
	) '.'? 
	(end=triplesBlock 
		{	var valueClone=$value.Clone() as Func<IEnumerable<RPackInt>,IEnumerable<RPackInt>>;
			var endvalue=$end.value; $value=packs=>endvalue(valueClone(packs)); }  
	)? 
)* '}'	;


triplesBlock returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> value]	 
: triplesSameSubject {$value=$triplesSameSubject.value;} ( '.' (next=triplesBlock 
{ 
var valueClone=$value.Clone() as Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>>;  
var nextvalue=$next.value; 
$value=packs=>nextvalue(valueClone(packs));} 
)? 
)?;


graphPatternNotTriples	returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> value] 
: optionalGraphPattern {$value=$optionalGraphPattern.value;}
| groupOrUnionGraphPattern  {$value=$groupOrUnionGraphPattern.value;}
| graphGraphPattern {$value=$graphGraphPattern.value;} ;


optionalGraphPattern returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> value]	 
:'OPTIONAL' {short parametersStartIndex = (short)q.Variables.Count; } groupGraphPattern
{
	$value = $groupGraphPattern.value.Optional(parametersStartIndex, (short)q.Variables.Count);
} ;


graphGraphPattern	returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> value] :'GRAPH' varOrIRIref groupGraphPattern;


groupOrUnionGraphPattern	returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> value] : 
{short lastKnownIndex = (short)q.Variables.Count;}
first=groupGraphPattern {$value=$first.value;}
 (
 {  foreach(var newV in q.Variables.Skip(lastKnownIndex))
	newV.Value.isNew=true; }
 'UNION' second=groupGraphPattern { $value= $value.Union($second.value);})*   
 { foreach(var newV in q.Variables.Skip(lastKnownIndex))
	newV.Value.isNew=false; };


filter		returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> value]  	
:'FILTER' { q.currentFilterParameter = Expression.Parameter(typeof (RPackInt));}  
constraint
{
var f=Expression.Lambda<Func<RPackInt, bool>>($constraint.value, q.currentFilterParameter).Compile();
     $value = pacs => pacs.Where(f);
} ;


constraint	returns [Expression value]
: brackettedExpression {$value=$brackettedExpression.value;}
| builtInCall {$value=$builtInCall.value;}
| functionCall	{$value=$functionCall.value;};


functionCall returns [Expression value]	 
:iRIref argList {$value=Query.Call($iRIref.value, $argList.value);};


argList		returns [List<Expression> value] 
: ( NIL {} | '(' main=expression { $value=new List<Expression>(){ $main.value }; } ( ',' second=expression { $value.Add($second.value); } )* ')' );


constructTemplate 
:'{' (constructTriples { q.CreateConstructTemplate(); } )? '}'  ;

constructTriples 
:	triplesSameSubjectConstruct 	{ q.constructTriples.Add($triplesSameSubjectConstruct.value); }
( '.' (next= constructTriples)? )?	;

triplesSameSubjectConstruct	 returns [Func<RPackInt,IEnumerable<Tuple<string,string,string>>> value]
locals [Func<RPackInt, string> subj=null]
:	varOrTermSubConstruct propertyListNotEmptyConstruct  
{  $value=$propertyListNotEmptyConstruct.value; } 
|	triplesNode propertyList	{} ;

varOrTermSubConstruct
: varLiteral {  $triplesSameSubjectConstruct::subj= pac=> pac.Get(q.Variables[$varLiteral.text].index); } 
| graphTermConstuct  {	$triplesSameSubjectConstruct::subj= pac=> $graphTermConstuct.value; };

propertyListNotEmptyConstruct returns [Func<RPackInt, IEnumerable<Tuple<string,string,string>>> value]
: main= verbObjectListConstruct { $value=$main.value; } 
( ';'(seconds = verbObjectListConstruct {  var valueClone=$value.Clone() as Func<RPackInt,IEnumerable<Tuple<string,string,string>>>;var scnds=$seconds.value; $value=packs=>scnds(packs).Concat(valueClone(packs)); } )?)*  ;

verbObjectListConstruct  returns [Func<RPackInt, IEnumerable<Tuple<string,string,string>>> value]
locals[Func<RPackInt, string> pred=null]
: verbConstruct objectListConstruct { $value=$objectListConstruct.value; } ;

verbConstruct
: varLiteral { $verbObjectListConstruct::pred = pac=> pac.Get(q.Variables[$varLiteral.text].index); } 
| iRIref	  { $verbObjectListConstruct::pred = pac=> $iRIref.value; }
| 'a' { $verbObjectListConstruct::pred = pac=> "a"; } ;

objectListConstruct returns [Func<RPackInt, IEnumerable<Tuple<string,string,string>>> value]
 : o0=graphNodeConstruct { var o0value= $o0.value; $value=pack => Enumerable.Repeat(o0value(pack),1);	 } 
( ',' o1=graphNodeConstruct {  var valueClone=$value.Clone() as Func<RPackInt,IEnumerable<Tuple<string,string,string>>>; var o11=$o1.value; $value=packs=>valueClone(packs).Concat(Enumerable.Repeat(o11(packs),1)); } )*;

graphNodeConstruct returns [Func<RPackInt, Tuple<string,string,string>> value]
: varOrTermConstruct	{ $value=$varOrTermConstruct.value; }
|	triplesNode	;

 varOrTermConstruct returns [Func<RPackInt, Tuple<string,string,string>> value]
: varLiteral 
{	   
	var p = $verbObjectListConstruct::pred;
	var s = $triplesSameSubjectConstruct::subj;		  	
	string oVar=$varLiteral.text;
	$value = pac=> Tuple.Create(s(pac), p(pac), pac.Get(q.Variables[oVar].index));
  } 
| graphTermConstuct
{
	
	var p = $verbObjectListConstruct::pred;
	var s = $triplesSameSubjectConstruct::subj;		  	
	string oVar=$graphTermConstuct.text;
	$value =pac => Tuple.Create( s(pac), p(pac), $graphTermConstuct.value);
};

graphTermConstuct returns[string value]	 
:	iRIref 	{   $value = $iRIref.value; } 
|	rDFLiteral 	{ $value = $rDFLiteral.value.ToString(); } 
|	numeric {  $value = $numeric.text; } 
|	BooleanLiteral {	 $value = $BooleanLiteral.text; } 
|	BlankNode  { $value = $BlankNode.text; } 
|	NIL	{	$value =$NIL.text;  } ;




triplesSameSubject	 returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> value]
locals [Variable subj=new Variable()]
:	varOrTermSub propertyListNotEmpty  
{  $value=$propertyListNotEmpty.f; } 
|	triplesNode propertyList	{} ;

propertyListNotEmpty  returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> f]
: main= verbObjectList  { $f=$main.f; } 
( ';'(seconds = verbObjectList { var valueClone=$f.Clone() as Func<IEnumerable<RPackInt>,IEnumerable<RPackInt>>; var scnds=$seconds.f; $f=packs=>scnds(valueClone(packs)); } )?)*  ;

propertyList returns	[List<VarOrTermContext> value]	  : (propertyListNotEmpty {  })?;

verbObjectList  returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> f]
locals[Variable PredicateVariable]
: verb objectList { $f=$objectList.f; } ;

objectList returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> f]
 : o0=graphNode { $f=$o0.f;	 } 
( ',' o1=graphNode { var valueClone=$f.Clone() as Func<IEnumerable<RPackInt>,IEnumerable<RPackInt>>; var o11=$o1.f; $f=packs=>o11(valueClone(packs)); } )*;

verb
: varOrIRIref { $verbObjectList::PredicateVariable=$varOrIRIref.p; $varOrIRIref.p.isPredicate=true; } 
| 'a' {	
var PredicateVariable=new Variable(){isPredicate=true};							   
PredicateVariable.pacElement =  TripleInt.CodePredicates("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");  
PredicateVariable.graph	= new GraphIsDataProperty(){IsData=false};
$verbObjectList::PredicateVariable=PredicateVariable;	   
} ;
											
triplesNode	 returns	[Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> f] 
 :	collection { }
 |	blankNodePropertyList {};

blankNodePropertyList	returns	[Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> f] :'[' propertyListNotEmpty ']' {	} ;

collection	 returns	[Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> f] :'(' ( graphNode {	 } )+ ')';

graphNode  returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> f]
: varOrTerm	{ $f=$varOrTerm.f; }
|	triplesNode	;


varOrTermSub 
: var  { $triplesSameSubject::subj=$var.p; q.SetSubjectIsDataFalse($var.p);} 
| graphTerm  {	$triplesSameSubject::subj.pacElement = TripleInt.CodeEntities($graphTerm.entity); };


 varOrTerm returns [Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> f]
: var 
{	   
	var p = $verbObjectList::PredicateVariable;
	var sVar = $triplesSameSubject::subj;		  	
		$f = Query.CreateTriplet(sVar, p, $var.p, null);	 		
  } 
| graphTerm 
{
	var p = $verbObjectList::PredicateVariable;
	var sVar = $triplesSameSubject::subj;
	var o=new Variable();						


		

if($graphTerm.d==null)	
		{		
		 o.graph=new GraphIsDataProperty(){IsData=false};
		o.pacElement = TripleInt.CodeEntities($graphTerm.entity);
		}
	else
		{ 
		o.graph=new GraphIsDataProperty(){IsData=true, vid=$graphTerm.d.vid};	 		
		}		 
	$f = Query.CreateTriplet(sVar, p, o, d:$graphTerm.d);	
};

varOrIRIref	  returns[Variable p]
:var  {  $p=$var.p;  } 
| iRIref 
{	
$p=new Variable(){
    pacElement = TripleInt.CodePredicates($iRIref.value)	 };
	GraphIsDataProperty graph;
	if (!q.isDataGraph.TryGetValue($p.pacElement, out graph)) 	
            {                
                q.isDataGraph.Add($p.pacElement, graph = new GraphIsDataProperty());
            }
			$p.graph= graph;
       
};

var returns [Variable p]
: varLiteral
{
$p =	q.GetVariable($varLiteral.text);
};

varLiteral 	 
: VAR1  
| VAR2 ;

graphTerm returns[string entity, Literal d]	 
:	iRIref 	{   $entity = $iRIref.value; } 
|	rDFLiteral {$d = $rDFLiteral.value;}
|	numeric {  $d = new Literal(LiteralVidEnumeration.integer) {Value = $numeric.num }; } 
|	BooleanLiteral {	 $d = new Literal(LiteralVidEnumeration.boolean) {Value = bool.Parse($BooleanLiteral.text)}; } 
|	BlankNode  { $entity =$BlankNode.text; } 
|	NIL	{	$d = new Literal(LiteralVidEnumeration.nil);  }   ;

expression	 returns [Expression value, Variable singleNewParameter] 
: {int lastKnownIndex=q.Variables.Count;} conditionalAndExpression {$value=$conditionalAndExpression.value; $singleNewParameter=$conditionalAndExpression.singleNewParameter; } 
( {  foreach(var newV in q.Variables.Skip(lastKnownIndex))	newV.Value.isNew=true; }
'||' conditionalAndExpression {$value=Expression.Or($value, $conditionalAndExpression.value); $singleNewParameter=null;})*
{  foreach(var newV in q.Variables.Skip(lastKnownIndex))	newV.Value.isNew=false; };

conditionalAndExpression	 returns [Expression value, Variable singleNewParameter] 
: main=valueLogical		{$value=$main.value;						$singleNewParameter=$main.singleNewParameter; } 
( '&&' alt=valueLogical {$value=Expression.And($value, $alt.value); $singleNewParameter=null;} )*	;

valueLogical returns [Expression value, Variable singleNewParameter] 
:	    main = additiveExpression { $value=$main.value; $singleNewParameter=$main.singleNewParameter;}
( '=' second = additiveExpression { $value=q.EqualOrAssign($value, $second.value, $singleNewParameter, $second.singleNewParameter); $singleNewParameter=null; }
| '!=' second = additiveExpression { Query.Sync(ref $value, $singleNewParameter, ref $second.value, $second.singleNewParameter);$value=Expression.NotEqual($value,  $second.value); $singleNewParameter=null; }
| '<' second = additiveExpression {Query.Sync(ref $value, $singleNewParameter, ref $second.value, $second.singleNewParameter); $value=Query.BinaryCompareExpression(ExpressionType.LessThan, $value, $second.value); $singleNewParameter=null; }
| '>' second = additiveExpression {Query.Sync(ref $value, $singleNewParameter, ref $second.value, $second.singleNewParameter); $value=Query.BinaryCompareExpression(ExpressionType.GreaterThan, $value, $second.value); $singleNewParameter=null; }
| '<=' second = additiveExpression {Query.Sync(ref $value, $singleNewParameter, ref $second.value, $second.singleNewParameter); $value=Query.BinaryCompareExpression(ExpressionType.LessThanOrEqual, $value, $second.value); $singleNewParameter=null; }
| '>=' second = additiveExpression { Query.Sync(ref $value, $singleNewParameter, ref $second.value, $second.singleNewParameter); $value=Query.BinaryCompareExpression(ExpressionType.GreaterThanOrEqual, $value, $second.value); $singleNewParameter=null; } )? ;

additiveExpression returns [Expression value, Variable singleNewParameter]	 
:	    main = multiplicativeExpression { $value=$main.value; $singleNewParameter=$main.singleNewParameter; }
( '+' second = multiplicativeExpression { Query.Sync(ref $value, $singleNewParameter, ref $second.value, $second.singleNewParameter, typeof(double)); $value=Expression.Add($value, $second.value); $singleNewParameter=null; }
| '-' second = multiplicativeExpression { Query.Sync(ref $value, $singleNewParameter, ref $second.value, $second.singleNewParameter, typeof(double)); $value=Expression.Subtract($value, $second.value); $singleNewParameter=null; }
| NumericLiteralPositive   { $value = Expression.Add($value, Expression.Constant(double.Parse($NumericLiteralPositive.text))); $singleNewParameter=null; }
| NumericLiteralNegative   { $value = Expression.Add($value, Expression.Constant(double.Parse($NumericLiteralNegative.text))); $singleNewParameter=null; })* ;

multiplicativeExpression  returns [Expression value, Variable singleNewParameter]	
:	    main = unaryExpression   { $value=$main.value; $singleNewParameter=$main.singleNewParameter; }
( '*' second = unaryExpression { Query.Sync(ref $value, $singleNewParameter, ref  $second.value, $second.singleNewParameter, typeof(double)); $value=Expression.Multiply($value, $second.value); $singleNewParameter=null; }
| '/' second = unaryExpression { Query.Sync(ref $value, $singleNewParameter, ref  $second.value, $second.singleNewParameter, typeof(double)); $value=Expression.Divide($value, $second.value); $singleNewParameter=null;})* ;

unaryExpression	 returns [Expression value, Variable singleNewParameter]	 
:   '!' primaryExpression { $value=Expression.Not(Query.SetVariableLiteraByType($primaryExpression.value, $primaryExpression.singleNewParameter, typeof(bool))); $singleNewParameter=null;}
|	'+' primaryExpression { $value= Query.SetVariableLiteraByType($primaryExpression.value, $primaryExpression.singleNewParameter, typeof(double)); $singleNewParameter=$primaryExpression.singleNewParameter; }
|	'-' primaryExpression { $value=Expression.Subtract(Expression.Constant(0.0), Query.SetVariableLiteraByType($primaryExpression.value, $primaryExpression.singleNewParameter, typeof(double))); $singleNewParameter=null; }
|		primaryExpression { $value=$primaryExpression.value; $singleNewParameter=$primaryExpression.singleNewParameter; };
															   
primaryExpression returns [Expression value, Variable singleNewParameter]
: brackettedExpression { $value=$brackettedExpression.value; $singleNewParameter=$brackettedExpression.singleNewParameter; }
| builtInCall { $value=$builtInCall.value;  }
| iRIrefOrFunction { $value = $iRIrefOrFunction.value; }
| rDFLiteral { $value=Query.LiteraExpression(Expression.Constant($rDFLiteral.value.Value), $rDFLiteral.value.vid); }
| numeric { $value = Expression.Constant($numeric.num); }
| BooleanLiteral { $value = Expression.Constant(bool.Parse($BooleanLiteral.text)); }
| var {	 $value = q.Parameter($var.p); $singleNewParameter=$var.p; };

brackettedExpression returns [Expression value, Variable singleNewParameter] :'(' expression ')' { $value=$expression.value; $singleNewParameter=$expression.singleNewParameter; } ;

builtInCall	 returns [Expression value] 
:   ('STR' | 'str' | 'Str' ) '(' expression ')' {  $value=Expression.Call(Expression.Convert($expression.value, typeof (Literal)),"GetString", new Type[0]); }
|	('LANG' | 'lang' | 'Lang' ) '(' rDFLiteral ')'  { $value=q.Lang($rDFLiteral.value);  }
|	('LANG' | 'lang' | 'Lang' ) '(' var ')'  { $value=q.Lang($var.p);  }
|	('LANGMATCHES' | 'langmatches' | 'Langmatches' | 'langMatches' | 'LangMatches' ) '(' l=expression ',' '"*"' ')'  { $value  = q.Langmatch($l.value, $l.singleNewParameter);  }
|	('LANGMATCHES' | 'langmatches' | 'Langmatches' | 'langMatches' | 'LangMatches' ) '(' l=expression ',' r=expression ')'  { $value  = q.Langmatch($l.value, $l.singleNewParameter, $r.value, $r.singleNewParameter);  }
|	('DATATYPE' | 'datatype' | 'Datatype' | 'dataType' | 'DataType' ) '(' expression ')' {  } 
|	('BOUND'| 'bound' | 'Bound' ) '(' var ')'   { $value = q.Bound($var.p);  }
|	'sameTerm' '(' l=expression ',' r=expression ')' { $value=q.EqualOrAssign($l.value, $r.value, $l.singleNewParameter, $r.singleNewParameter);  } 
|	'isIRI' '(' expression ')' 	  {}
|	'isURI' '(' expression ')' { } 
|	'isBLANK' '(' expression ')'   {  }
|	'isLITERAL' '(' expression ')'  { }
|	regexExpression  { $value=$regexExpression.value;  } ;

regexExpression	 returns [Expression value] : ( 'REGEX'| 'regex' | 'Regex' ) '(' v = var ',' rex = String ( ',' extraParam= String )? ')' 
{ $value=Query.RegExpression(q.Parameter($v.p), $rex.text, $extraParam==null ? null : $extraParam.text); };

iRIrefOrFunction	returns [Expression value] 
: iRIref { var code=TripleInt.CodeEntities($iRIref.value); if(code==int.MinValue) code=TripleInt.CodePredicates($iRIref.value);  $value = Expression.Constant(code);}
 (argList { $value = Query.Call($iRIref.value,  $argList.value);  })? ;

rDFLiteral	returns [Literal value, Text literalText] 
: String {
var s= $String.text;	 
s = s.Substring(1,s.Length-2);
$literalText =  new Text(){Value=s};
$value=new Literal(LiteralVidEnumeration.text){Value=$literalText};	  
}
( LANGTAG { $literalText.Lang = $LANGTAG.text.ToLower(); } 
| ( '^^' iRIref { 
var type = $iRIref.value;		 
DateTime tempDate;
Double tempNum;
bool tempBool;
if(type =="http://www.w3.org/2001/XMLSchema#dateTime" || type =="http://www.w3.org/2001/XMLSchema#date")
{
if(!DateTime.TryParse(s, out tempDate)) throw new Exception("7");
	 $value=new Literal(LiteralVidEnumeration.date){Value = tempDate.ToBinary()};
}
else if(type =="http://www.w3.org/2001/XMLSchema#boolean")
{
if(!bool.TryParse(s, out tempBool)) throw new Exception("7");
	$value=new Literal(LiteralVidEnumeration.boolean){Value = tempBool };
}
else if(type =="http://www.w3.org/2001/XMLSchema#integer" || type =="http://www.w3.org/2001/XMLSchema#double" || type =="http://www.w3.org/2001/XMLSchema#float" )
{															 
if(!double.TryParse(s, out tempNum) && !double.TryParse(s.Replace(".",","), out tempNum)) throw new Exception("7");
	$value=new Literal(LiteralVidEnumeration.integer){Value = tempNum };
}
else if(type =="http://www.w3.org/2001/XMLSchema#string") {  $literalText.Lang ="en"; }
else $value = new Literal(LiteralVidEnumeration.typedObject){ Value = new TypedObject{Value=$literalText.Value, Type=type} };
}))? ;


iRIref returns [string value]	 
:	IRI_REF  	{	
		var iri=$IRI_REF.text;
		$value = iri.Substring(1,iri.Length-2); } 
|	PREFIXED_NAME 	{	
var match = PrefixNSSlpit.Match($PREFIXED_NAME.text);		            
 $value = q.prefixes[match.Groups[1].Value] + match.Groups[2].Value; } ;	
 		
 						 
numeric returns [double num] : numericLiteral	{double value; string txt=$numericLiteral.text; if(!double.TryParse(txt, out value) && !double.TryParse(txt.Replace(".",","), out value)) throw new Exception("qe5645");   $num=value;}	 ;

numericLiteral	 : numericLiteralUnsigned | NumericLiteralPositive | NumericLiteralNegative ;

numericLiteralUnsigned  : INTEGER |	DECIMAL |	DOUBLE ; 

PNAME_NS	 : PN_PREFIX? ':';		
PREFIXED_NAME	 : PNAME_NS PN_LOCAL ;

NumericLiteralPositive	 :INTEGER_POSITIVE |	DECIMAL_POSITIVE |	DOUBLE_POSITIVE ;
NumericLiteralNegative	 :INTEGER_NEGATIVE |	DECIMAL_NEGATIVE |	DOUBLE_NEGATIVE	;
BooleanLiteral	 :'true' |	'false' ;
String	 : STRING_LITERAL1 | STRING_LITERAL2 | STRING_LITERAL_LONG1 | STRING_LITERAL_LONG2 ;
BlankNode	 : BLANK_NODE_LABEL |	ANON ;
IRI_REF	: '<'([a-zA-Zà-ÿÀ-ß0-9:/\\#.\x00-\x20-])*'>';
BLANK_NODE_LABEL	 :'_:' PN_LOCAL ;
VAR1	 :'?' VARNAME ;
VAR2	 :'$' VARNAME	 ;
LANGTAG	 :'@' [a-zA-Z]+ ('-' [a-zA-Z0-9]+)* ;
INTEGER	 :[0-9]+   ;
DECIMAL	 :[0-9]+ '.' [0-9]* | '.' [0-9]+ ;
DOUBLE	 :[0-9]+ '.' [0-9]* EXPONENT | '.' ([0-9])+ EXPONENT | ([0-9])+ EXPONENT ;
INTEGER_POSITIVE	 :'+' INTEGER	;
DECIMAL_POSITIVE	 :'+' DECIMAL;
DOUBLE_POSITIVE	 :'+' DOUBLE;
INTEGER_NEGATIVE	 :'-' INTEGER	 ;
DECIMAL_NEGATIVE	 :'-' DECIMAL;
DOUBLE_NEGATIVE	 :'-' DOUBLE;
EXPONENT	 :[eE] [+-]? [0-9]+;
STRING_LITERAL1	 :'\'' (.| ECHAR )*? '\''  ;
STRING_LITERAL2	 : '"'(.| ECHAR)*?'"';
STRING_LITERAL_LONG1	 :'\'\'\'' ( ( '\'' | '\'\'\'' )? ( ~[\'\\] | ECHAR ) )* '\'\'\'' ;
STRING_LITERAL_LONG2	 :'"""' ( ( '"' | '""' )? ( ~["\\] | ECHAR ) )* '"""'  ;
ECHAR	 : '\\' [tbnrf\"\'] ;
NIL	 :'(' WS* ')';
WS  :   [ \t\n\r]+ -> skip ;
ANON	 :'[' WS* ']'	;
PN_CHARS_BASE	 : [A-Z] | [a-z] ;
PN_CHARS_U	 :PN_CHARS_BASE | '_' ;			 
VARNAME	 :( PN_CHARS_U | [0-9] ) ( PN_CHARS_U | [0-9] )* ;
PN_CHARS	 : PN_CHARS_U | '-' | [0-9] ;
PN_PREFIX	 : PN_CHARS_BASE ((PN_CHARS|'.')* PN_CHARS)?;
PN_LOCAL	 : ( PN_CHARS_U | [0-9] ) ((PN_CHARS|'.')* PN_CHARS)?;