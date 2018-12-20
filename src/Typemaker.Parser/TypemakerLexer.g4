﻿lexer grammar TypemakerLexer;

//a lot of this was stolen from here: https://github.com/antlr/grammars-v4/blob/edc36d1/csharp/CSharpLexer.g4
@lexer::members
{
ulong regularAccessLevel;
System.Collections.Generic.Stack<bool> multiString = new System.Collections.Generic.Stack<bool>();
}

channels { COMMENTS_CHANNEL, WHITESPACE_CHANNEL }

SINGLE_LINE_COMMENT:     '//'  InputCharacter*    -> channel(COMMENTS_CHANNEL);
DELIMITED_COMMENT: '/*' .*? '*/' -> channel(COMMENTS_CHANNEL);

fragment InputCharacter: ~[\r\n\u0085\u2028\u2029];

SPACES: ' '+ -> channel(WHITESPACE_CHANNEL);
TABS: '\t'+ -> channel(WHITESPACE_CHANNEL);
WINDOWS_NEWLINES: '\r\n'+ -> channel(WHITESPACE_CHANNEL);
NEWLINES: '\n'+ -> channel(WHITESPACE_CHANNEL);

VERBATIUM_STRING: '@"' (~["\r\n])* '"';
MULTILINE_VERBATIUM_STRING: '@{"' (~'"')* '"}';

MULTI_STRING_START: '{"' { multiString.Push(true); } -> pushMode(INTERPOLATION_STRING);
STRING_START: '"' { multiString.Push(false); } -> pushMode(INTERPOLATION_STRING);

INTEGER: ('0'..'9')+;
REAL: ('0'..'9')* '.' ('0'..'9')+;

BLOCKCOMMENT: '/*' .*? '*/' -> skip;
LINECOMMENT: '//' ~[\r\n]* -> skip;

CONSTRUCTOR: 'New';
INC: '++';
DEC: '--';
ADD_EQUALS: '+=';
SUB_EQUALS: '-=';
EQ_EQUALS: '==';
NEQ_EQUALS: '!=';
MUL_EQUALS: '*=';
MOD_EQUALS: '%=';
LS_EQUALS: '<<=';
RS_EQUALS: '>>=';
DIV_EQUALS: '/=';
AND_EQUALS: '&=';
OR_EQUALS: '|=';
INV_EQUALS: '~=';
XOR_EQUALS: '^=';
COMMA: ',';
INVERT: '~';
RDEC: '->';
PLUS: '+';
MOD: '%';
MINUS: '-';
POW: '**';
STAR: '*';
XOR: '^';
LPAREN: '(';
RPAREN: ')';
LCURL: '{';
RCURL: '}';
LOR: '||';
LAND: '&&';
BOR: '|';
BAND: '&';
LBRACE: '[' { ++regularAccessLevel; };
RBRACE: ']' { if(regularAccessLevel > 0) --regularAccessLevel; else if(multiString.Count > 0) { PopMode(); } };
DOTDOTDOT: '...';
DOTDOT: '..';
DOT: '.';
COLON: ':';
SEMI: ';';
EQUALS: '=';
QUESTION: '?';
EXCLAIM: '!';
BSLASH: '\\';
RES: '\'' .+? '\'';
NEW: 'new';
LSHIFT: '<<';
RSHIFT: '>>';
LESSE: '<=';
GREATERE: '>=';
LESS: '<';
GREATER: '>';
TRUE: 'true';
FALSE: 'false';
LIST: 'list';
CONST: 'const';
ENUM: 'enum';
FOR: 'for';
WHILE: 'while';
IN: 'in';
INPUT: 'input';
TO: 'to';
USR: 'usr';
IF: 'if';
ELSE: 'else';
SWITCH: 'switch';
BREAK: 'break';
CONTINUE: 'continue';
THROW: 'throw';
RETURN: 'return';
DO: 'do';
AS: 'as';
STATIC: 'static';
VAR: 'var';
VOID: 'void';
EXPLICIT: 'explicit';

PUBLIC: 'public';
PROTECTED: 'protected';
VIRTUAL: 'virtual';
SEALED: 'sealed';
PRECEDENCE: 'precedence';
FINAL: 'final';
ABSTRACT: 'abstract';
INLINE: 'inline';
PARTIAL: 'partial';
READONLY: 'readonly';
NAMEOF: 'nameof';
IMPLEMENTS: 'implements';

SPAWN: 'spawn';

TRY: 'try';
CATCH: 'catch';

DECLARE: 'declare';
UNSAFE: 'unsafe';

SLASH: '/';

CONCRETE: 'concrete';
RESOURCE: 'resource';
BOOL: 'bool';
STRING: 'string';
PATH: 'path';
INT: 'int';
FLOAT: 'float';
INTERFACE: 'interface';
NULLABLE: 'nullable';
NULL: 'null';
PROC: 'proc';
DICT: 'dict';
GLOBAL: 'global';

CALL: 'call';
SRC: 'src';

EXCEPTION: 'exception';
VERB: 'verb';
SET: 'set';

MAP: 'map';

BANNED_MOJO_OPERATOR: 'operator';

IDENTIFIER: [_a-zA-Z][_a-zA-Z0-9]*;

mode INTERPOLATION_STRING;

CHAR_INSIDE: '\\\''
	| '\\"'
	| '\\['
	| '\\\\'
	| '\\0'
	| '\\a'
	| '\\b'
	| '\\f'
	| '\\n'
	| '\\r'
	| '\\t'
	| '\\v'
	;
	
EMBED_START: '[' -> pushMode(DEFAULT_MODE);

MULTI_STRING_CLOSE: {multiString.Peek()}? '"}' { multiString.Pop(); PopMode(); };
STRING_CLOSE: {!multiString.Peek()}? '"' { multiString.Pop(); PopMode(); };

STRING_INSIDE: {!multiString.Peek()}? ~('[' | '\\' | '"' | '\r' | '\n')+;
MULTI_STRING_QUOTE: {multiString.Peek()}? '"';
MULTI_STRING_INSIDE: {multiString.Peek()}? ~('[' | '\\' | '"')+;
