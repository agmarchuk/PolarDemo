/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */

grammar sparql2XML;
options
{
	language = CSharp2;
}
@parser::namespace { Generated }
@lexer::namespace  { Generated }
@header{
	using System;
	using System.Xml.Linq;	
	using System.Collections;
}

@members{
	
public	XElement x=new XElement("sparql"), spo=new XElement("spo");
}

query	 : prologue ( selectQuery | constructQuery | describeQuery | askQuery )
 { 	Console.WriteLine("ok");  } ;	 

prologue	 :basedecl? prefixDecl*;

basedecl	 :'BASE' IRI_REF ;

prefixDecl	 :'PREFIX'  PNAME_NS IRI_REF  
{ 	x.Add(new XElement("prefix",new XElement("name", $PNAME_NS.text), new XElement("name", $IRI_REF.text))); } ;

selectQuery	 :'SELECT'	 { x.Add(new XElement("select"));  } 
( 'DISTINCT' {	x.Element("select").Add(new XElement("DISTINCT"));} 
| 'REDUCED' {	x.Element("select").Add(new XElement("REDUCED"));}
)? 
( (var	{	x.Element("select").Add(new XElement("variable", $var.text));} )+ 
| '*'  { 	x.Element("select").Add(new XElement("all_variables"));	} )

datasetClause* whereClause solutionModifier ;

constructQuery	 :'CONSTRUCT' constructTemplate datasetClause* whereClause solutionModifier
{
    x.Add($constructTemplate.value);
};

describeQuery	 :'DESCRIBE' { x.Add(new XElement("DESCRIBE"));} 
( (varOrIRIref {x.Element("DESCRIBE").Add($varOrIRIref.value);})+ 
| '*' {x.Element("DESCRIBE").Add("all");}) datasetClause* whereClause? solutionModifier  ;

askQuery	 :'ASK' {x.Add(new XElement("ask"));} datasetClause* whereClause;

datasetClause	 :'FROM'  { if(x.Element("from")==null) x.Add(new XElement("from")); }( defaultGraphClause | namedGraphClause );

defaultGraphClause	 :sourceSelector;

namedGraphClause	 :'NAMED' sourceSelector;

sourceSelector	:iRIref 
{
  x.Element("from").Add($iRIref.value);
};

whereClause	 :'WHERE'?
 groupGraphPattern{
   x.Add(new XElement("WHERE", $groupGraphPattern.value)); 
};

solutionModifier	 : (orderClause { x.Add($orderClause.value); })? (limitOffsetClauses {	  x.Add($limitOffsetClauses.value); })?		  ;

limitOffsetClauses		returns [List<XElement> value]
 :( limitClause {$value=new List<XElement>(){$limitClause.value};} ( offsetClause {$value.Add($offsetClause.value);})? 
 | offsetClause {$value=new List<XElement>(){$offsetClause.value};} (limitClause {$value.Add($limitClause.value);})? )  ;

orderClause	returns [XElement value]
 :'ORDER' 'BY' {$value=new XElement("orderBy");} (orderCondition {$value.Add($orderCondition.value);})+	;

orderCondition	 returns [XElement value]
: ( {$value=new XElement("brackettedExpression");} ( 'ASC' | 'DESC' { $value.Add(new XAttribute("direction", "desc"));} ) brackettedExpression {$value.Add($brackettedExpression.value);} ) 
| ( constraint {$value=new XElement("constraint", $constraint.value);} | var {$value= $var.value;} )	  ;

limitClause	 returns [XElement value]
:'LIMIT' INTEGER  {$value=new XElement("limit",$INTEGER.text);};

offsetClause	returns [XElement value] 
:'OFFSET' INTEGER {$value=new XElement("offset",$INTEGER.text);};

groupGraphPattern	 returns [List<XElement> value]	
:'{' (strt=triplesBlock {$value=$strt.value;})? 
( ( graphPatternNotTriples { if($value==null) $value = new List<XElement>(){ $graphPatternNotTriples.value}; else $value.Add($graphPatternNotTriples.value); }
| filter { if($value==null) $value = new List<XElement>(){$filter.value}; else $value.Add($filter.value); }
) '.'? 
(end=triplesBlock { $value.AddRange($end.value); }  )? )* '}'	;

triplesBlock returns [List<XElement> value]	 : triplesSameSubject {$value=new List<XElement>(){$triplesSameSubject.value};} ( '.' (triplesBlock {$value.AddRange($triplesBlock.value);} )? )?	  ;

graphPatternNotTriples	returns [XElement value] 
: optionalGraphPattern {$value=$optionalGraphPattern.value;}
| groupOrUnionGraphPattern  {$value=$groupOrUnionGraphPattern.value;}
| graphGraphPattern {$value=$graphGraphPattern.value;} ;

optionalGraphPattern returns [XElement value]	 
:'OPTIONAL' groupGraphPattern
{
$value = new XElement("Optional", $groupGraphPattern.value);
} ;

graphGraphPattern	returns [XElement value] :'GRAPH' varOrIRIref groupGraphPattern;

groupOrUnionGraphPattern	returns [XElement value] : 
first=groupGraphPattern {$value=new XElement("UNION", $first.value);} ( 'UNION' second=groupGraphPattern { $value.Add($second.value);})*  ;

filter		returns [XElement value]  :'FILTER' constraint	
{ $value=new XElement("Filter", $constraint.value);} ;

constraint	returns [XElement value]
: brackettedExpression {$value=$brackettedExpression.value;}
| builtInCall {$value=$builtInCall.value;}
| functionCall	{$value=$functionCall.value;};

functionCall returns [XElement value]	 
:iRIref argList {$value=new XElement("func", $iRIref.value, $argList.value);};

argList		returns [List<XElement> value] 
: ( NIL {} | '(' main=expression { $value=new List<XElement>(){ $main.value }; } ( ',' second=expression { $value.Add($second.value); } )* ')' );
constructTemplate	 returns [XElement value] 
:'{' (constructTriples {$value=new XElement("constructTriples", $constructTriples.value);})? '}'  ;

constructTriples	returns [List<XElement> value] 
:	triplesSameSubject 	{ $value=new List<XElement>(){$triplesSameSubject.value}; }
( '.' (next= constructTriples { $value.AddRange($next.value); })? )?	;

triplesSameSubject	 returns [XElement value]
:	varOrTermSub propertyListNotEmpty  { $value = new XElement("s", $varOrTermSub.value, $propertyListNotEmpty.value);} 
|	triplesNode propertyList	{ $value = new XElement("tripletsGroup", $triplesNode.value, $propertyList.value);} ;

propertyListNotEmpty returns [List<XElement> value]
: v0=verb o0=objectList {  $value=new List<XElement>(){new XElement("p", $v0.value, $o0.value)};} 
( ';' (  v1=verb o1=objectList { $value.Add(new XElement("p",  $v1.value, $o1.value));} )? )*  ;

propertyList returns	[XElement value]	  : (propertyListNotEmpty { $value = new XElement("propertyList", $propertyListNotEmpty.value); })?;

objectList returns	[List<XElement> value]
 : o0=object {	 $value=new List<XElement>(){$o0.value}; } 
( ',' 
	o1=object {	 $value.Add($o1.value); } 
)*;

object returns [XElement value]	 
: graphNode	{	$value = new XElement("o", $graphNode.value); } ;

verb returns [XElement value]	 
: varOrIRIref {	$value=$varOrIRIref.value; } 
| 'a' {	$value = new XElement("iRIref", "rdf type"); } ;

triplesNode	 returns	[XElement value] 
 :	collection {$value=new XElement("collection",$collection.value);}
 |	blankNodePropertyList {$value=new XElement("blankNodePropertyList", $blankNodePropertyList.value);};
blankNodePropertyList	returns	[List<XElement> value] :'[' propertyListNotEmpty ']' {	$value=$propertyListNotEmpty.value; } ;

collection	 returns	[List<XElement> value] :'(' {$value = new List<XElement>();} ( graphNode {	$value.Add($graphNode.value); } )+ ')';

graphNode	 returns [XElement value]
: varOrTerm {$value=$varOrTerm.value; }
|	triplesNode	;

varOrTermSub returns[XElement value]	
: var {	     
    $value = new XElement("var", $var.text); 
	} 
| graphTerm  {	         $value = $graphTerm.value; };

varOrTerm returns[XElement value]	
: var {	         $value = new XElement("var", $var.text); } 
| graphTerm  {	         $value = $graphTerm.value; };

varOrIRIref	  returns[XElement value]
:var  {	         $value = $var.value; } 
| iRIref {	         $value = $iRIref.value; } ;

var returns[XElement value]	 
: VAR1 { $value = new XElement("var", $VAR1.text); } 
| VAR2 { $value = new XElement("var", $VAR2.text); } ;

graphTerm returns[XElement value]	 
:	iRIref 	{	 $value =  $iRIref.value; } 
|	rDFLiteral {	 $value = new XElement("rDFLiteral", $rDFLiteral.text); } 
|	numericLiteral {	 $value = new XElement("numericLiteral", $numericLiteral.text); } 
|	BooleanLiteral {	 $value = new XElement("BooleanLiteral", $BooleanLiteral.text); } 
|	BlankNode  {	 $value = new XElement("BlankNode", $BlankNode.text); } 
|	NIL	{	 $value = new XElement("NIL"); }   ;

expression	  returns [XElement value]	 
: conditionalOrExpression  {$value=$conditionalOrExpression.value;};

conditionalOrExpression	 returns [XElement value] 
: conditionalAndExpression {$value=$conditionalAndExpression.value;} 
( '||' conditionalAndExpression {$value=new XElement("or", $value, $conditionalAndExpression.value);})*	;

conditionalAndExpression	 returns [XElement value] 
: main=valueLogical {$value=$main.value;} 
( '&&' alt=valueLogical {$value=new XElement("and",  $value, $alt.value); } )*	;

valueLogical returns [XElement value] : relationalExpression { $value=$relationalExpression.value; };

relationalExpression returns [XElement value] 
:	    main = numericExpression { $value=$main.value; }
( '=' second = numericExpression { $value=new XElement("eq",  $value, $second.value); }
| '!=' second = numericExpression { $value=new XElement("eqNot",  $value, $second.value); }
| '<' second = numericExpression { $value=new XElement("les",  $value, $second.value); }
| '>' second = numericExpression { $value=new XElement("more",  $value, $second.value); }
| '<=' second = numericExpression { $value=new XElement("lesEq",  $value, $second.value); }
| '>=' second = numericExpression { $value=new XElement("moreEq",  $value, $second.value); } )? ;

numericExpression returns [XElement value] : additiveExpression { $value=$additiveExpression.value; }; 

additiveExpression returns [XElement value]	 
:	    main = multiplicativeExpression { $value=$main.value; }
( '+' second = multiplicativeExpression { $value=new XElement("plus",  $value, $second.value); }
| '-' second = multiplicativeExpression { $value=new XElement("mines", $value, $second.value); }
| NumericLiteralPositive   { $value= new XElement("NumericLiteralPositive", $NumericLiteralPositive.text); }
| NumericLiteralNegative   { $value=new XElement("NumericLiteralNegative", $NumericLiteralNegative.text); })* ;

multiplicativeExpression  returns [XElement value]	
:	    main = unaryExpression   { $value=$main.value; }
( '*' second = unaryExpression { $value=new XElement("mult", $value, $second.value); }
| '/' second = unaryExpression { $value=new XElement("div",  $value, $second.value); })* ;

unaryExpression	 returns [XElement value]	 
:   '!' second = primaryExpression { $value=new XElement("not",   $second.value); }
|	'+' second = primaryExpression { $value=new XElement("plus",  $second.value); }
|	'-' second = primaryExpression { $value=new XElement("div",   $second.value); }
|		  main = primaryExpression  { $value=$main.value; };
															   
primaryExpression returns [XElement value]	 
: brackettedExpression { $value=$brackettedExpression.value; }
| builtInCall { $value=$builtInCall.value; }
| iRIrefOrFunction { $value=$iRIrefOrFunction.value; }
| rDFLiteral { $value=$rDFLiteral.value; }
| numericLiteral { $value = new XElement("numericLiteral", $numericLiteral.text); }
| BooleanLiteral { $value = new XElement("BooleanLiteral", $BooleanLiteral.text); }
| var { $value=$var.value; };

brackettedExpression returns [XElement value] :'(' expression ')' { $value=$expression.value; } ;

builtInCall	 returns [XElement value] 
:   ('STR' | 'str' | 'Str' ) '(' expression ')' { $value=new XElement("STR", $expression.value);  }
|	('LANG' | 'lang' | 'Lang' ) '(' expression ')'  { $value=new XElement("LANG", $expression.value);  }
|	('LANGMATCHES' | 'langmatches' | 'Langmatches' | 'langMatches' | 'LangMatches' ) '(' l=expression ',' r=expression ')'  { $value=new XElement("LANGMATCHES", $l.value, $r.value);  }
|	('DATATYPE' | 'datatype' | 'Datatype' | 'dataType' | 'DataType' ) '(' expression ')' { $value=new XElement("DATATYPE", $expression.value);  } 
|	('BOUND'| 'bound' | 'Bound' ) '(' var ')'   { $value=new XElement("BOUND", $var.value);  }
|	'sameTerm' '(' l=expression ',' r=expression ')' { $value=new XElement("sameTerm", $l.value, $r.value);  } 
|	'isIRI' '(' expression ')' 	  { $value=new XElement("isIRI", $expression.value);  }
|	'isURI' '(' expression ')' { $value=new XElement("isURI", $expression.value);  } 
|	'isBLANK' '(' expression ')'   { $value=new XElement("isBLANK", $expression.value);  }
|	'isLITERAL' '(' expression ')'  { $value=new XElement("isLITERAL", $expression.value);  }
|	regexExpression  { $value=$regexExpression.value;  } ;

regexExpression	 returns [XElement value] : ( 'REGEX'| 'regex' | 'Regex' ) '(' v = expression ',' rex= expression ( ',' extraParam= expression )? ')' 
{$value=new XElement("regex", $v.value, $rex.value);};

iRIrefOrFunction	returns [XElement value] 
: iRIref {$value=$iRIref.value;}
 (argList { $value=new XElement("iRIrefOrFunction", new XElement("f", $value), new XElement("argList", $argList.value));  })? ;

rDFLiteral	returns [XElement value] 
: String {$value = new XElement("RDFLiteral",  $String.text);} 
( LANGTAG { $value.Add(new XElement("lang", $LANGTAG.text));} 
| ( '^^' iRIref { $value.Add(new XElement("type", $iRIref.value)); }))? ;

iRIref returns [XElement value]	 
:	IRI_REF  	{	 $value = new XElement("IRI_REF", $IRI_REF.text); } 
|	PREFIXED_NAME 	{	 $value = new XElement("PREFIXED_NAME", $PREFIXED_NAME.text); } ;

numericLiteral	 : numericLiteralUnsigned | NumericLiteralPositive | NumericLiteralNegative ;

numericLiteralUnsigned	 :INTEGER |	DECIMAL |	DOUBLE ;

NumericLiteralPositive	 :INTEGER_POSITIVE |	DECIMAL_POSITIVE |	DOUBLE_POSITIVE ;

NumericLiteralNegative	 :INTEGER_NEGATIVE |	DECIMAL_NEGATIVE |	DOUBLE_NEGATIVE	 ;

BooleanLiteral	 :'true' |	'false' ;
String	 :STRING_LITERAL1 | STRING_LITERAL2 | STRING_LITERAL_LONG1 | STRING_LITERAL_LONG2 ;
BlankNode	 :BLANK_NODE_LABEL |	ANON ;


IRI_REF	: '<'([a-zA-Zà-ÿÀ-ß0-9:/\\#.\x00-\x20-])*'>';
PNAME_NS	 : PN_PREFIX? ':';		
PREFIXED_NAME	 : PNAME_LN ;

PNAME_LN	 :PNAME_NS PN_LOCAL ;
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