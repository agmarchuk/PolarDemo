/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */

grammar sparq11lTranslator;
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
 queryUnit : query {Console.WriteLine("ok");};
 query : prologue
( selectQuery | constructQuery | describeQuery | askQuery )
valuesClause ;
 updateUnit : update;
 prologue : ( baseDecl |prefixDecl )+;
 baseDecl : BASE IRIREF;
 prefixDecl : PREFIX  PNAME_NS IRIREF
 {

 };
 selectQuery : selectClause datasetClause* whereClause solutionModifier;
 subSelect : selectClause whereClause solutionModifier valuesClause;
 selectClause : SELECT ( 'DISTINCT' | 'REDUCED' )? ( ( var | ( '(' expression 'AS' var ')' ) )+ | '*' );
 constructQuery : 'CONSTRUCT' ( constructTemplate datasetClause* whereClause solutionModifier | datasetClause* WHERE '{' triplesTemplate? '}' solutionModifier );
 describeQuery : DESCRIBE ( varOrIri+ | '*' ) datasetClause* whereClause? solutionModifier;
 askQuery : 'ASK' datasetClause* whereClause solutionModifier;
 datasetClause : 'FROM' ( defaultGraphClause | namedGraphClause );
 defaultGraphClause : sourceSelector;
 namedGraphClause :'NAMED' sourceSelector;
 sourceSelector : iri;
 whereClause : 'WHERE'? groupGraphPattern;
 solutionModifier : groupClause? havingClause? orderClause? limitOffsetClauses?;
 groupClause : 'GROUP' 'BY' groupCondition+;
 groupCondition : builtInCall | functionCall | '(' expression ( 'AS' var )? ')' | var;
 havingClause : 'HAVING' havingCondition+;
 havingCondition : constraint;
 orderClause : 'ORDER' 'BY' orderCondition+;
 orderCondition : ( ( 'ASC' | 'DESC' ) brackettedExpression )
| ( constraint | var );
 limitOffsetClauses : limitClause offsetClause? | offsetClause limitClause?;
 limitClause : 'LIMIT' integer;
 offsetClause : 'OFFSET' integer;
 valuesClause : ( 'VALUES' dataBlock )?;
 update : prologue ( update1 ( ';' update )? )?;
 update1 : load | clear | drop | add | move | copy | create | insertData | deleteData | deleteWhere | modify;
 load : 'LOAD' 'SILENT'? iri ( 'INTO' graphRef )?;
 clear : 'CLEAR' 'SILENT'? graphRefAll;
 drop : 'DROP' 'SILENT'? graphRefAll;
 create : 'CREATE' 'SILENT'? graphRef;
 add : 'ADD' 'SILENT'? graphOrDefault 'TO' graphOrDefault;
 move : 'MOVE' 'SILENT'? graphOrDefault 'TO' graphOrDefault;
 copy : 'COPY' 'SILENT'? graphOrDefault 'TO' graphOrDefault;
 insertData : 'INSERT dATA' quadData;
 deleteData : 'DELETE dATA' quadData;
 deleteWhere : 'DELETE wHERE' quadPattern;
 modify : ( 'WITH' iri )? ( deleteClause insertClause? | insertClause ) usingClause* 'WHERE' groupGraphPattern;
 deleteClause : 'DELETE' quadPattern;
 insertClause : 'INSERT' quadPattern;
 usingClause : 'USING' ( iri | 'NAMED' iri );
 graphOrDefault : 'DEFAULT' | 'GRAPH'? iri;
 graphRef : 'GRAPH' iri;
 graphRefAll : graphRef | 'DEFAULT' | 'NAMED' | 'ALL';
 quadPattern : '{' quads '}';
 quadData : '{' quads '}';
 quads : triplesTemplate? ( quadsNotTriples '.'? triplesTemplate? )*;
 quadsNotTriples : 'GRAPH' varOrIri '{' triplesTemplate? '}';
 triplesTemplate : triplesSameSubject ( '.' triplesTemplate? )?;
 groupGraphPattern : '{' ( subSelect | groupGraphPatternSub ) '}';
 groupGraphPatternSub : triplesBlock? ( graphPatternNotTriples '.'? triplesBlock? )*;
 triplesBlock : triplesSameSubjectPath ( '.' triplesBlock? )?;
 graphPatternNotTriples : groupOrUnionGraphPattern | optionalGraphPattern | minusGraphPattern | graphGraphPattern | serviceGraphPattern | filter | bind | inlineData;
 optionalGraphPattern : 'OPTIONAL' groupGraphPattern;
 graphGraphPattern : 'GRAPH' varOrIri groupGraphPattern;
 serviceGraphPattern : 'SERVICE' 'SILENT'? varOrIri groupGraphPattern;
 bind : 'BIND' '(' expression 'AS' var ')';
 inlineData : 'VALUES' dataBlock;
 dataBlock : inlineDataOneVar | inlineDataFull;
 inlineDataOneVar : var '{' dataBlockValue* '}';
 inlineDataFull : ( NIL | '(' var* ')' ) '{' ( '(' dataBlockValue* ')' | NIL )* '}';
 dataBlockValue : iri |	RDFLiteral |	NumericLiteral |	BooleanLiteral |	'UNDEF';
 minusGraphPattern : 'MINUS' groupGraphPattern;
 groupOrUnionGraphPattern : groupGraphPattern ( 'UNION' groupGraphPattern )*;
 filter : 'FILTER' constraint;
 constraint : brackettedExpression | builtInCall | functionCall;
 functionCall : iri argList;
 argList : NIL | '(' 'DISTINCT'? expression ( ',' expression )* ')';
 expressionList : NIL | '(' expression ( ',' expression )* ')';
 constructTemplate : '{' constructTriples? '}';
 constructTriples : triplesSameSubject ( '.' constructTriples? )?;
 triplesSameSubject : varOrTerm propertyListNotEmpty |	TriplesNode propertyList;
 propertyList : propertyListNotEmpty?;
 propertyListNotEmpty : verb objectList ( ';' ( verb objectList )? )*;
 verb : varOrIri | 'a';
 objectList : object ( ',' object )*;
 object : graphNode;
 triplesSameSubjectPath : varOrTerm propertyListPathNotEmpty |	TriplesNodePath propertyListPath;
 propertyListPath : propertyListPathNotEmpty?;
 propertyListPathNotEmpty : ( verbPath | verbSimple ) objectListPath ( ';' ( ( verbPath | verbSimple ) objectList )? )*;
 verbPath : path;
 verbSimple : var;
 objectListPath : objectPath ( ',' objectPath )*;
 objectPath : graphNodePath;
 path : pathAlternative;
 pathAlternative : pathSequence ( '|' pathSequence )*;
 pathSequence : pathEltOrInverse ( '/' pathEltOrInverse )*;
 pathElt : pathPrimary pathMod?;
 pathEltOrInverse : pathElt | '^' pathElt;
 pathMod : '?' | '*' | '+';
 pathPrimary : iri | 'a' | '!' pathNegatedPropertySet | '(' path ')';
 pathNegatedPropertySet : pathOneInPropertySet | '(' ( pathOneInPropertySet ( '|' pathOneInPropertySet )* )? ')';
 pathOneInPropertySet : iri | 'a' | '^' ( iri | 'a' );
 integer : INTEGER;
 triplesNode : collection |	BlankNodePropertyList;
 blankNodePropertyList : '[' propertyListNotEmpty ']';
 triplesNodePath : collectionPath |	BlankNodePropertyListPath;
 blankNodePropertyListPath : '[' propertyListPathNotEmpty ']';
 collection : '(' graphNode+ ')';
 collectionPath : '(' graphNodePath+ ')';
 graphNode : varOrTerm |	TriplesNode;
 graphNodePath : varOrTerm |	TriplesNodePath;
 varOrTerm : var | graphTerm;
 varOrIri : var | iri;
 var : VAR1 | VAR2;
 graphTerm : iri |	RDFLiteral |	NumericLiteral |	BooleanLiteral |	BlankNode |	NIL;
 expression : conditionalOrExpression;
 conditionalOrExpression : conditionalAndExpression ( '||' conditionalAndExpression )*;
 conditionalAndExpression : valueLogical ( '&&' valueLogical )*;
 valueLogical : relationalExpression;
 relationalExpression : numericExpression ( '=' numericExpression | '!=' numericExpression | '<' numericExpression | '>' numericExpression | '<=' numericExpression | '>=' numericExpression | 'IN' expressionList | 'NOT' 'IN' expressionList )?;
 numericExpression : additiveExpression;
 additiveExpression : multiplicativeExpression ( '+' multiplicativeExpression | '-' multiplicativeExpression | ( numericLiteralPositive | numericLiteralNegative ) ( ( '*' unaryExpression ) | ( '/' unaryExpression ) )* )*;
 multiplicativeExpression : unaryExpression ( '*' unaryExpression | '/' unaryExpression )*;
 unaryExpression :   '!' primaryExpression 
|	'+' primaryExpression 
|	'-' primaryExpression 
|	PrimaryExpression;
 primaryExpression : brackettedExpression | builtInCall | iriOrFunction | rDFLiteral | numericLiteral | booleanLiteral | var;
 brackettedExpression : '(' expression ')';
 builtInCall :   aggregate 
|	'STR' '(' expression ')' 
|	'LANG' '(' expression ')' 
|	'LANGMATCHES' '(' expression ',' expression ')' 
|	'DATATYPE' '(' expression ')' 
|	'BOUND' '(' var ')' 
|	'IRI' '(' expression ')' 
|	'URI' '(' expression ')' 
|	'BNODE' ( '(' expression ')' | NIL ) 
|	'RAND' NIL 
|	'ABS' '(' expression ')' 
|	'CEIL' '(' expression ')' 
|	'FLOOR' '(' expression ')' 
|	'ROUND' '(' expression ')' 
|	'CONCAT' expressionList 
|	SubstringExpression 
|	'STRLEN' '(' expression ')' 
|	StrReplaceExpression 
|	'UCASE' '(' expression ')' 
|	'LCASE' '(' expression ')' 
|	'ENCODE_FOR_URI' '(' expression ')' 
|	'CONTAINS' '(' expression ',' expression ')' 
|	'STRSTARTS' '(' expression ',' expression ')' 
|	'STRENDS' '(' expression ',' expression ')' 
|	'STRBEFORE' '(' expression ',' expression ')' 
|	'STRAFTER' '(' expression ',' expression ')' 
|	'YEAR' '(' expression ')' 
|	'MONTH' '(' expression ')' 
|	'DAY' '(' expression ')' 
|	'HOURS' '(' expression ')' 
|	'MINUTES' '(' expression ')' 
|	'SECONDS' '(' expression ')' 
|	'TIMEZONE' '(' expression ')' 
|	'TZ' '(' expression ')' 
|	'NOW' NIL 
|	'UUID' NIL 
|	'STRUUID' NIL 
|	'MD5' '(' expression ')' 
|	'SHA1' '(' expression ')' 
|	'SHA256' '(' expression ')' 
|	'SHA384' '(' expression ')' 
|	'SHA512' '(' expression ')' 
|	'COALESCE' expressionList 
|	'IF' '(' expression ',' expression ',' expression ')' 
|	'STRLANG' '(' expression ',' expression ')' 
|	'STRDT' '(' expression ',' expression ')' 
|	'sameTerm' '(' expression ',' expression ')' 
|	'isIRI' '(' expression ')' 
|	'isURI' '(' expression ')' 
|	'isBLANK' '(' expression ')' 
|	'isLITERAL' '(' expression ')' 
|	'isNUMERIC' '(' expression ')' 
|	RegexExpression 
|	ExistsFunc 
|	NotExistsFunc;
 regexExpression : 'REGEX' '(' expression ',' expression ( ',' expression )? ')';
 substringExpression : 'SUBSTR' '(' expression ',' expression ( ',' expression )? ')';
 strReplaceExpression : 'REPLACE' '(' expression ',' expression ',' expression ( ',' expression )? ')';
 existsFunc : 'EXISTS' groupGraphPattern;
 notExistsFunc : 'NOT' 'EXISTS' groupGraphPattern;
 aggregate :   'COUNT' '(' 'DISTINCT'? ( '*' | expression ) ')' 
| 'SUM' '(' 'DISTINCT'? expression ')' 
| 'MIN' '(' 'DISTINCT'? expression ')' 
| 'MAX' '(' 'DISTINCT'? expression ')' 
| 'AVG' '(' 'DISTINCT'? expression ')' 
| 'SAMPLE' '(' 'DISTINCT'? expression ')' 
| 'GROUP_CONCAT' '(' 'DISTINCT'? expression ( ';' 'SEPARATOR' '=' string )? ')';
 iriOrFunction : iri argList?;
 rDFLiteral : string ( NILLANGTAG | ( '^^' iri ) )?;
 numericLiteral : numericLiteralUnsigned | numericLiteralPositive | numericLiteralNegative;
 numericLiteralUnsigned : INTEGER |	DECIMAL |	DOUBLE;
 numericLiteralPositive : INTEGER_POSITIVE |	DECIMAL_POSITIVE |	DOUBLE_POSITIVE;
 numericLiteralNegative : INTEGER_NEGATIVE |	DECIMAL_NEGATIVE |	DOUBLE_NEGATIVE;
 booleanLiteral : 'true' |	'false';
 string : STRING_LITERAL1 | STRING_LITERAL2 | STRING_LITERAL_LONG1 | STRING_LITERAL_LONG2;
 iri : IRIREF |	prefixedName;
 prefixedName : PNAME_LN | PNAME_NS;
 blankNode : BLANK_NODE_LABEL |	ANON;

PREFIX : 'PREFIX';
BASE : 'BASE';
SELECT : 'SELECT';
DESCRIBE : 'DESCRIBE';
WHERE : 'WHERE';
IRIREF	: '<'([a-zA-Zà-ÿÀ-ß0-9:/\\#.%-])*'>';
//IRIREF : '<' ([^<>"{}|^`\]-[\x00-\x20])* '>';
PNAME_LN : PNAME_NS PN_LOCAL;
PNAME_NS : PN_PREFIX? ':';
BLANK_NODE_LABEL : '_:' ( PN_CHARS_U | [0-9] ) ((PN_CHARS|'.')* PN_CHARS)?;
VAR1 : '?' VARNAME;
VAR2 : '$' VARNAME;
LANGTAG : '@' [a-zA-Z]+ ('-' [a-zA-Z0-9]+)*;
 INTEGER : [0-9]+;
DECIMAL : [0-9]* '.' [0-9]+;
DOUBLE : [0-9]+ '.' [0-9]* EXPONENT | '.' ([0-9])+ EXPONENT | ([0-9])+ EXPONENT;
 INTEGER_POSITIVE : '+' INTEGER;
DECIMAL_POSITIVE : '+' DECIMAL;
DOUBLE_POSITIVE : '+' DOUBLE;
 INTEGER_NEGATIVE : '-' INTEGER;
DECIMAL_NEGATIVE : '-' DECIMAL;
DOUBLE_NEGATIVE : '-' DOUBLE;
EXPONENT : [eE] [+-]? [0-9]+;																
STRING_LITERAL1 : '\''(~([\x27\x5C\xA\xD']) |  ECHAR  )*'\'';
STRING_LITERAL2 : '"' ( (~([\x22\x5C\xA\xD])) | ECHAR )* '"';
STRING_LITERAL_LONG1	 :'\'\'\'' ( ( '\'' | '\'\'\'' )? ( ~[\'\\] | ECHAR ) )* '\'\'\'' ;
STRING_LITERAL_LONG2	 :'"""' ( ( '"' | '""' )? ( ~["\\] | ECHAR ) )* '"""'  ;
ECHAR : '\\' [tbnrf\"'];
NIL : '(' WS* ')';
WS : [ \s\t\r\n]+ ->skip	;
ANON : '[' WS* ']';
PN_CHARS_BASE : [A-Z] | [a-z] | [\x00C0-\x00D6] | [\x00D8-\x00F6] | [\x00F8-\x02FF] | [\x0370-\x037D] | [\x037F-\x1FFF] | [\x200C-\x200D] | [\x2070-\xw218F] | [\x2C00- \x2FEF] | [\x3001-\xD7FF] | [\xF900-\xFDCF] | [\xFDF0-\xFFFD] | [\x10000-\xEFFFF];
PN_CHARS_U : PN_CHARS_BASE | '_';
VARNAME : ( PN_CHARS_U | [0-9] ) ( PN_CHARS_U | [0-9] | [\x00B7] | [\x0300-\x036F] | [\x203F-\x2040] )*;
PN_CHARS : PN_CHARS_U | '-' | [0-9] | [\x00B7] | [\x0300-\x036F] | [\x203F-\x2040];
PN_PREFIX : PN_CHARS_BASE ((PN_CHARS|'.')* PN_CHARS)?;
PN_LOCAL : (PN_CHARS_U | ':' | [0-9] | PLX ) ((PN_CHARS | '.' | ':' | PLX)* (PN_CHARS | ':' | PLX) )?;
PLX : PERCENT | PN_LOCAL_ESC;
PERCENT : '%' HEX HEX;
HEX : [0-9] | [A-F] | [a-f];
PN_LOCAL_ESC : '\\' ( '_' | '~' | '.' | '-' | '!' | '$' | '&' | '\'' | '(' | ')' | '*' | '+' | ',' | ';' | '=' | '/' | '?' | '#' | '@' | '%' );
