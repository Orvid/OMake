grammer OMake;

options
{
	language=CSharp2;
}

@parser::namespace { OMake.Processor.Parser }
@lexer::namespace { OMake.Processor.Lexer }

tokens
{
	LCURLY		=	'{'	;
	RCURLY		=	'}'	;
	LPAREN		=	'('	;
	RPAREN		=	')'	;
	LSQBRACK	=	'['	;
	RSQBRACK	=	']'	;
	FSLASH		=	'/'	;
	BSLASH		=	'\'	;
	AND			=	'&'	;
	CASH		=	'$'	;
	DQUOTE		=	'"'	;
	SHARP		=	'#'	;
	COMMA		=	','	;
	SEMICOL		=	';'	;
	UNDSCOR		=	'_'	;
}

// Due to the need to be case-insensitive
// in certain places, and still be sensitive
// in others, we have to use our own method of
// case insensitivity.

fragment A: 'A' | 'a';
fragment B: 'B' | 'b';
fragment C: 'C' | 'c';
fragment D: 'D' | 'd';
fragment E: 'E' | 'e';
fragment F: 'F' | 'f';
fragment G: 'G' | 'g';
fragment H: 'H' | 'h';
fragment I: 'I' | 'i';
fragment J: 'J' | 'j';
fragment K: 'K' | 'k';
fragment L: 'L' | 'l';
fragment M: 'M' | 'm';
fragment N: 'N' | 'n';
fragment O: 'O' | 'o';
fragment P: 'P' | 'p';
fragment Q: 'Q' | 'q';
fragment R: 'R' | 'r';
fragment S: 'S' | 's';
fragment T: 'T' | 't';
fragment U: 'U' | 'u';
fragment V: 'V' | 'v';
fragment W: 'W' | 'w';
fragment X: 'X' | 'x';
fragment Y: 'Y' | 'y';
fragment Z: 'Z' | 'z';

/*------------------------------------------------------------------
 * LEXER RULES
 *------------------------------------------------------------------*/
// First let's define a few basic tokens,
// aka. Whitespace and End Of Line.
WS			: (' ' | '\t')+ {skip();} ;
EOL			: ('\r\n' | '\r' | '\n') ;
IDENTIFIER	: ('a'..'z' | 'A'..'Z' | '_') ('a'..'z' | 'A'..'Z' | '0'..'9' | '_')+;
PLATFORM_IDENTIFIER			: IDENTIFIER;
PLATFORM_ALIAS_IDENTIFIER	: IDENTIFIER;
TARGET_IDENTIFIER			: IDENTIFIER;
TOOL_IDENTIFIER				: IDENTIFIER;
CONSTANT_IDENTIFIER			: IDENTIFIER
SOURCE_IDENTIFIER			: IDENTIFIER;
TOOL_DATA					: ( ( ( (.)+ ) ');') );
CONSTANT_DATA				: TOOL_DATA;

// Now the Case-Insensitive identifiers (all others are case-sensitive)
DEFINE_TOK					: (D E F I N E);
TOOL_TOK					: (T O O L);
PLATFORM_TOK				: (P L A T F O R M);
PLATFORM_ALIAS_TOK			: (PLATFORM_TOK UNDSCOR A L I A S);
CONSTANT_TOK				: ( (C O N S T) | (C O N S T A N T) );
MANGLER_TOK					: (M A N G L E R);
SOURCE_TOK					: (S O U R C E);

/*

// Uncomment this when we support platform sepecific source defines.
SOURCE_PLAT_SPEC_TOK		: (SOURCE_TOK UNDSCOR PLATFORM_IDENTIFIER);

// When we add dependancy tracking support, uncomment this.
DEPENDANCY_TOK				: ( (D E P E N D A N C Y) | (D E P E N D S) | (D E P S) );
DEPENDANCY_PLAT_SPEC_TOK	: (DEPENDANCY_TOK UNDSCOR PLATFORM_IDENTIFIER);

*/

TARGET_TOK					: (T A R G E T);


// And the slightly more complex tokens.
SDEFINE			: (SHARP WS? DEFINE_TOK);
REPLACEABLE		: (CASH WS? LCURLY WS? CONSTANT_IDENTIFIER WS? RCURLY);

SLINE_COMMENT	: (FSLASH FSLASH (.)* EOL) { $channel = HIDDEN; };
MLINE_COMMENT	: (FSLASH STAR (.)* STAR FSLASH) { $channel = HIDDEN; };




/*------------------------------------------------------------------
 * PARSER RULES
 *------------------------------------------------------------------*/

define
	: define_tool
	| define_target
	| define_constant
	| define_source
	| define_mangler
	| define_platform
	;

	
define_tool
	: define_tool_global
	| define_tool_plat_spec
	;
define_tool_global
	: SDEFINE WS TOOL_TOK WS? LPAREN WS? TOOL_IDENTIFIER WS? COMMA WS? TOOL_DATA WS? EOL
	;	
define_tool_plat_spec
	: SDEFINE WS TOOL_TOK UNDSCOR PLATFORM_IDENTIFIER WS? LPAREN WS? TOOL_IDENTIFIER WS? COMMA WS? TOOL_DATA WS? EOL
	;


define_target
	: SDEFINE WS TARGET_TOK WS? LPAREN WS? TARGET_IDENTIFIER WS? RPAREN WS? EOL? LCURLY WS? EOL
	;


define_constant
	: define_constant_global
	| define_constant_plat_spec
	;
define_constant_global
	: SDEFINE WS CONSTANT_TOK WS? LPAREN WS? CONSTANT_IDENTIFIER WS? COMMA WS? CONSTANT_DATA WS? EOL
	;
define_constant_plat_spec
	: SDEFINE WS CONSTANT_TOK UNDSCOR PLATFORM_IDENTIFIER WS? LPAREN WS? CONSTANT_IDENTIFIER WS? COMMA WS? CONSTANT_DATA WS? EOL
	;


define_source
	: define_source_global
	;
define_source_global
	: SDEFINE WS SOURCE_TOK WS? LPAREN WS? SOURCE_IDENTIFIER WS? RPAREN WS? EOL? LCURLY WS? EOL
	;


define_mangler
	: define_mangler_global
	;
	
	
define_platform
	: define_platform_global
	| define_platform_alias
	;
define_platform_global
	: SDEFINE WS PLATFORM_TOK WS? LPAREN WS? PLATFORM_IDENTIFIER WS? RPAREN WS? SEMICOL
	;
define_platform_alias
	: SDEFINE WS PLATFORM_ALIAS_TOK WS? LPAREN WS? PLATFORM_IDENTIFIER WS? COMMA WS? PLATFORM_ALIAS_IDENTIFIER WS? RPAREN WS? SEMICOL
	;
	