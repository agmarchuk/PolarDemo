/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */

grammar sparql2PacNS;
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
    using SparqlParseRun;
using TripleStoreForDNR;

	using System.Linq.Expressions;
}


@members{		  	

public static Regex PrefixNSSlpit=new Regex("^([^:]*:)(.*)$");
public SparqlQuery q;		
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
{   q.ResultSet.ResultType=ResultType.Select;} ;


constructQuery	 :'CONSTRUCT' constructTemplate datasetClause* whereClause solutionModifier
{	    
 q.ResultSet.ResultType=ResultType.Construct;
};


describeQuery	
 : 'DESCRIBE'  
( (varLiteral {q.variables.Add($varLiteral.text);} | iRIref { q.constants.Add($iRIref.text); } )+ 
| '*' { q.all=true; }) datasetClause* whereClause? solutionModifier  
{ q.ResultSet.ResultType=ResultType.Describe; };


askQuery	 :'ASK'  datasetClause* whereClause {q.ResultSet.ResultType=ResultType.Ask;};


datasetClause	 :'FROM'  ( defaultGraphClause | namedGraphClause );


defaultGraphClause	 : sourceSelector;

 
namedGraphClause	 :'NAMED' sourceSelector;


sourceSelector	: iRIref ;


whereClause	 :'WHERE'?
 groupGraphPattern {q.SparqlWhere.Triples.AddRange($groupGraphPattern.value.Triples);};


solutionModifier 
: (orderClause )? 
(limitOffsetClauses )? ;


limitOffsetClauses		
 : (limitClause { q.ListSolutionModifiersCount.Add($limitClause.value); } ( offsetClause { q.ListSolutionModifiersCount.Add($offsetClause.value); })? 
 | offsetClause { q.ListSolutionModifiersCount.Add($offsetClause.value); } (limitClause { q.ListSolutionModifiersCount.Add($limitClause.value); })? ) ;


orderClause
 :'ORDER' 'BY' main= orderCondition {q.ListSolutionModifiersOrder.Add($main.value); }
  (others = orderCondition { q.ListSolutionModifiersOrder.Add($others.value); })*;


orderCondition	 returns [Func<IEnumerable<SparqlResult>, IEnumerable<SparqlResult>> value, bool isDescending]
:  
(( 'ASC' | 'DESC' { $isDescending=true;  } ) brackettedExpression 
{ 
 var orderExpr = $brackettedExpression.value;
 
 	Func<SparqlResult, dynamic> orderFunc =  pac =>
						{ var o = orderExpr(pac);
                            if(o is VariableNode )
							return pac[((VariableNode)o).index];
                            //if(o is IUriNode)
						    return o;
                            //if(o is ILiteralNode)                            
                            {
                                var l = (ILiteralNode)o;
                                //if (l.Value is double)
                                //    return double.Parse(l.Value);
                                //if (l.Value is long)
                                //    return long.Parse(l.Value);
                                return l;
                            }
							throw new NotImplementedException();						     
						};
 if($isDescending)$value = pacs => pacs.OrderByDescending(orderFunc);
 else  $value = pacs => pacs.OrderBy(orderFunc);
  	 } ) 
| ( constraint {
  var orderExpr = $constraint.value;
		Func<SparqlResult, dynamic> orderFunc = pac=>
						{
						    var o = orderExpr(pac);
                            if(o is VariableNode )
														return pac[((VariableNode)o).index];
                            if(o is IUriNode)
						    return o;                            
                            if(o is ILiteralNode)                            
                            {
                                var l = (ILiteralNode)o;
                                //if (l.Value is double)
                                //    return double.Parse(l.Value);
                                //if (l.Value is long)
                                //    return long.Parse(l.Value);
                                return l;
                            }
							throw new NotImplementedException();
						};
						$value = packs=>packs.OrderBy(orderFunc);

 } | var {
int i = ($var.p.index);
	Func<SparqlResult, dynamic> orderFunc = pac=> pac[i];
						
						$value = pack => pack.OrderBy(orderFunc);
 } )	  ;


limitClause	 returns [Func<IEnumerable<SparqlResult>, IEnumerable<SparqlResult>> value]
:'LIMIT' INTEGER  { $value=sequence=>sequence.Take(int.Parse($INTEGER.text)); };


offsetClause	returns [Func<IEnumerable<SparqlResult>, IEnumerable<SparqlResult>> value] 
:'OFFSET' INTEGER { $value=sequence=>sequence.Skip(int.Parse($INTEGER.text)); };


groupGraphPattern	returns [SparqlWhere value=new SparqlWhere()]	 
:'{' (triplesBlock {$value.Triples.AddRange($triplesBlock.value.Triples);})? 
( ( graphPatternNotTriples { $value.Triples.Add($graphPatternNotTriples.value);  }
	| filter 		{ $value.Triples.Add($filter.value);  }	) '.'? 
	(triplesBlock {$value.Triples.AddRange($triplesBlock.value.Triples);})? 
)* '}'	;


triplesBlock returns [SparqlWhere value=new SparqlWhere()]	 
: triplesSameSubject {$value.Triples.AddRange($triplesSameSubject.value.Triples);} 
( '.' (next=triplesBlock {$value.Triples.AddRange($next.value.Triples); } )? )?;


graphPatternNotTriples	returns [ISparqlWhereItem value] 
: optionalGraphPattern {$value=$optionalGraphPattern.value;}
| groupOrUnionGraphPattern  {$value=$groupOrUnionGraphPattern.value;}
| graphGraphPattern {$value=$graphGraphPattern.value;} ;


optionalGraphPattern returns [OptionalWhere value]	 
:'OPTIONAL' {int parametersStartIndex = q.ResultSet.Variables.Count; } groupGraphPattern
{
	$value=new OptionalWhere(q.ResultSet);
	$value.Triples= $groupGraphPattern.value.Triples;
	$value.StartIndex=parametersStartIndex;;
	$value.EndIndex = q.ResultSet.Variables.Count;
} ;


graphGraphPattern	returns [ISparqlWhereItem value] :'GRAPH' varOrIRIref groupGraphPattern {			throw new NotImplementedException();};


groupOrUnionGraphPattern	returns [UnionWhere value] : 
{
 $value=new UnionWhere(q.ResultSet);  
}
first=groupGraphPattern { $value.Add($first.value, q.ResultSet.Variables.Count);}
 ('UNION' second=groupGraphPattern { $value.Add($second.value, q.ResultSet.Variables.Count);})* ;

filter		returns [SparqlFilter value]  	
:'FILTER'  
constraint
{
var f=$constraint.value;
     $value = new SparqlFilter(q.ResultSet){ Filter  = f };
} ;


constraint	returns [Func<SparqlResult, dynamic> value]
: brackettedExpression {$value=$brackettedExpression.value;}
| builtInCall {$value=$builtInCall.value;}
| functionCall	{$value=$functionCall.value;};


functionCall returns [Func<SparqlResult, dynamic> value]	 
:iRIref argList {$value=q.Call($iRIref.value, $argList.value);};


argList		returns [List<Func<SparqlResult, dynamic>> value] 
: ( NIL {} | '(' main=expression { $value=new List<Func<SparqlResult, dynamic>>(){ $main.value }; } 
( ',' second=expression { $value.Add($second.value); } )* ')' );


constructTemplate 
:'{' (constructTriples )? '}'  ;

constructTriples 
:	triplesSameSubject	 { q.Construct.Triples.AddRange($triplesSameSubject.value.Triples);} 
( '.' (next= constructTriples )? )?	;

triplesSameSubject	 returns [SparqlWhere value]
locals [SparqlNode subj]
:	varOrTermSub propertyListNotEmpty  
{  $value=$propertyListNotEmpty.value; } 
|	triplesNode propertyList	{} ;

propertyListNotEmpty  returns [SparqlWhere value]
: main= verbObjectList  { $value=$main.value; } 
( ';'(seconds = verbObjectList { $value.Triples.AddRange($seconds.value.Triples); } )?)*  ;

propertyList : (propertyListNotEmpty {  })?;

verbObjectList  returns [SparqlWhere value]
locals[SparqlNode Predicate]
: verb objectList { $value=$objectList.value; } ;

objectList returns [SparqlWhere value]
 : o0=graphNode { $value=$o0.value;	 } 
( ',' o1=graphNode { $value.Triples.AddRange($o1.value.Triples); } )*;

verb
: varOrIRIref { $verbObjectList::Predicate=$varOrIRIref.p; } 
| 'a' {	$verbObjectList::Predicate=new SparqlUriNode{ Uri=new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")}; } ;
											
triplesNode	 returns	[SparqlWhere value]
 :	collection { $value=$collection.value; }
 |	blankNodePropertyList {};

blankNodePropertyList	returns	[SparqlWhere f] :'[' propertyListNotEmpty ']' {	} ;

collection	 returns	[SparqlWhere value=new SparqlWhere()] :'(' ( graphNode { $value.Triples.AddRange($graphNode.value.Triples); } )+ ')';

graphNode  returns [SparqlWhere value =new SparqlWhere()]
: varOrTerm	{ $value.Triples.Add($varOrTerm.value); }
|	triplesNode {$value=$triplesNode.value;} ;


varOrTermSub 
: var  { $triplesSameSubject::subj=$var.p; }
| graphTerm  {	$triplesSameSubject::subj = $graphTerm.value; };


 varOrTerm returns [SparqlTriple value]
: var 
{	   
	var p = $verbObjectList::Predicate;
	var sVar = $triplesSameSubject::subj;		  	
		$value = new SparqlTriple(sVar, p, $var.p);	 		
  } 
| graphTerm 
{
	var p = $verbObjectList::Predicate;
	var sVar = $triplesSameSubject::subj;
	$value = new SparqlTriple(sVar, p, $graphTerm.value);	
};

varOrIRIref	  returns[SparqlNode p]
:var  {  $p= $var.p;  } 
| iRIref 
{	
$p= $iRIref.value;    
};

var returns [VariableNode p]
: varLiteral
{
$p = q.GetVariable($varLiteral.text);
};

varLiteral 	 
: VAR1  
| VAR2 ;

graphTerm returns[SparqlNode value]	 
:	iRIref 	{   $value =$iRIref.value; } 
|	rDFLiteral {$value =$rDFLiteral.value;}
|	numeric {  $value =new SparqlLiteralNode{Content=$numeric.num}; } 
|	BooleanLiteral {	 $value =new SparqlLiteralNode{ Content=bool.Parse($BooleanLiteral.text)}; } 
|	BlankNode  { $value = new SparqlBlankNode($BlankNode.text); } 
|	NIL	{	$value = null;  }   ;

expression	 returns [Func<SparqlResult, dynamic> value] 
:  main=conditionalAndExpression { $value=$main.value;  } 
( '||' alt=conditionalAndExpression 
{ Func<SparqlResult, dynamic> cloneValue=$value; 
Func<SparqlResult, dynamic> cloneAlt=$alt.value;
	$value = x=> cloneValue(x) || cloneAlt(x); 
	} )* ;

conditionalAndExpression returns [Func<SparqlResult, dynamic> value] 
: main=valueLogical		{$value=$main.value; } 
( '&&' alt=valueLogical {
var cloneValue=$value;
var cloneAlt=$alt.value;
$value= x=>cloneValue(x) && cloneAlt(x); } )*	;

valueLogical returns [Func<SparqlResult, dynamic> value] 
:	    main = additiveExpression { $value=$main.value; }
( '=' second = additiveExpression { var f= $value; var f1=$second.value; $value=x => f(x)==f1(x);  }
| '!=' second = additiveExpression { var f= $value; var f1=$second.value; $value=x => f(x)!=f1(x); }
| '<' second = additiveExpression { var f= $value; var f1=$second.value; $value=x => f(x)<f1(x);  }
| '>' second = additiveExpression { var f= $value; var f1=$second.value; $value=x => f(x)>f1(x);  }
| '<=' second = additiveExpression { var f= $value; var f1=$second.value; $value=x => f(x)<=f1(x);  }
| '>=' second = additiveExpression { var f= $value; var f1=$second.value; $value=x => f(x)>=f1(x);  } )? ;

additiveExpression returns [Func<SparqlResult, dynamic> value]	 
:	    main = multiplicativeExpression { $value=$main.value;  }
( '+' second = multiplicativeExpression {   var f= $value; var f1=$second.value; $value=x => f(x)+f1(x);  }
| '-' second = multiplicativeExpression {   var f= $value; var f1=$second.value; $value=x => f(x)-f1(x);  }
| NumericLiteralPositive   { var f= $value; $value=x => f(x)+double.Parse($NumericLiteralPositive.text);  }
| NumericLiteralNegative   { var f= $value; $value=x => f(x)+double.Parse($NumericLiteralNegative.text);   })* ;

multiplicativeExpression  returns [Func<SparqlResult, dynamic> value]	
:	    main = unaryExpression   { $value=$main.value;  }
( '*' second = unaryExpression {  var f= $value; var f1=$second.value; $value=x => f(x)*f1(x);  }
| '/' second = unaryExpression {  var f= $value; var f1=$second.value; $value=x => f(x)/f1(x); })* ;

unaryExpression	 returns [Func<SparqlResult, dynamic> value]	 
:   '!' primaryExpression { var f=$primaryExpression.value; $value=store=>! f(store); }
|	'+' primaryExpression { $value= $primaryExpression.value; }
|	'-' primaryExpression { var f=$primaryExpression.value; $value=store=> - f(store);  }
|		primaryExpression { $value=$primaryExpression.value; };
															   
primaryExpression returns [Func<SparqlResult, dynamic> value]
: brackettedExpression { $value=$brackettedExpression.value; }
| builtInCall { $value=$builtInCall.value;  }
| iRIrefOrFunction { $value = $iRIrefOrFunction.value;}
| rDFLiteral { int index=q.FilterConstants.Count;  q.FilterConstants.Add($rDFLiteral.value);  $value = result=> q.FilterConstants[index].Value;  }
| numeric { var rDFLiteral=new SparqlLiteralNode($numeric.num);  $value = store=>rDFLiteral.Value;  q.FilterConstants.Add(rDFLiteral); }
| BooleanLiteral { var rDFLiteral=new SparqlLiteralNode(bool.Parse($BooleanLiteral.text));  $value = store=>rDFLiteral.Value;  q.FilterConstants.Add(rDFLiteral); }
| var {	 $value = pac => pac[$var.p.index]; };

brackettedExpression returns [Func<SparqlResult, dynamic> value] :'(' expression ')' { $value=$expression.value; } ;

builtInCall	 returns [Func<SparqlResult, dynamic> value] 
:   ('STR' | 'str' | 'Str' ) '(' expression ')' { var f= $expression.value; $value =store => f(store).ToString();  }
|	('LANG' | 'lang' | 'Lang' ) '(' rDFLiteral ')'  { var f=$rDFLiteral.value; $value=q.Lang($rDFLiteral.value);  }
|	('LANG' | 'lang' | 'Lang' ) '(' var ')'  { $value=q.Lang($var.p);  }
|	('LANGMATCHES' | 'langmatches' | 'Langmatches' | 'langMatches' | 'LangMatches' ) '(' l=expression ',' '"*"' ')'  { $value  = q.Langmatch($l.value);  }
|	('LANGMATCHES' | 'langmatches' | 'Langmatches' | 'langMatches' | 'LangMatches' ) '(' l=expression ',' r=expression ')'  { $value  = q.Langmatch($l.value, $r.value);  }
|	('DATATYPE' | 'datatype' | 'Datatype' | 'dataType' | 'DataType' ) '(' expression ')' {  } 
|	('BOUND'| 'bound' | 'Bound' ) '(' var ')'   { $value = q.Bound($var.p);  }
|	'sameTerm' '(' l=expression ',' r=expression ')' { var lf=$l.value; var rf= $r.value; $value = pac=> lf(pac) == rf(pac);  } 
|	'isIRI' '(' expression ')' 	  {}
|	'isURI' '(' expression ')' { } 
|	'isBLANK' '(' expression ')'   {  }
|	'isLITERAL' '(' expression ')'  { }
|	regexExpression  { $value=$regexExpression.value;  } ;

regexExpression	 returns [Func<SparqlResult, dynamic> value] : ( 'REGEX'| 'regex' | 'Regex' ) '(' v = var ',' rex = String ( ',' extraParam= String )? ')' 
{ $value=SparqlQuery.RegExpression($v.p, $rex.text, $extraParam==null ? null : $extraParam.text); };

iRIrefOrFunction	returns [Func<SparqlResult, dynamic> value] 
: iRIref { 
int index=q.FilterConstants.Count;  
q.FilterConstants.Add($iRIref.value);
  $value = result=> q.FilterConstants[index].Value;  
   }
 (argList { throw new Exception(); })? ;

rDFLiteral	returns [SparqlLiteralNode value] 
: String LANGTAG 
{
var s1= $String.text;	 
s1 = s1.Substring(1,s1.Length-2);
$value = new SparqlLiteralNode(null, s1, $LANGTAG.text.ToLower());
} | String '^^' iRIref  
{
 var s2= $String.text;	 
s2 = s2.Substring(1,s2.Length-2);
var t=$iRIref.value;
 $value = new SparqlLiteralNode(t, s2, null);
}
| String
{
var s= $String.text;	 
s = s.Substring(1,s.Length-2);
$value= new SparqlLiteralNode(null, s,null);} ;


iRIref returns [SparqlUriNode value]	 
:	IRI_REF  	{	
		var iri=$IRI_REF.text;
		iri = iri.Substring(1, iri.Length - 2);
		//= q.ts.NameSpaceStore.FromFullName(iri);
		$value  = new SparqlUriNode{ Uri = new Uri(iri) };			
		 } 
|	PREFIXED_NAME 	{	
var match = PrefixNSSlpit.Match($PREFIXED_NAME.text);		            
 $value = new SparqlUriNode{Uri = new Uri(q.prefixes[match.Groups[1].Value] + match.Groups[2].Value)}; 
 } ;	
 		
 						 
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
IRI_REF	: '<'([a-zA-Zà-ÿÀ-ß0-9:/\\#.%-])*'>';
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