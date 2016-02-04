lexer grammar HScriptLexer;

// -- Reductions -- //
SET_REDUCTIONS 
	: A V G 
	| C O R R 
	| C O U N T 
	| C O U N T '_' A L L 
	| C O U N T '_' N U L L 
	| C O V A R 
	| F R E Q 
	| I N T E R C E P T 
	| M A X 
	| M I N 
	| S L O P E 
	| S T D E V 
	| S U M 
	| V A R
	;

// Opperators //
PLUS : '+';
MINUS : '-';
MUL : '*';
DIV : '/';
DIV2 : '/?';
MOD : '%';
POW : '^';
EQ : '==';
NEQ : '!=';
LT : '<';
LTE : '<=';
GT : '>';
GTE : '>=';
INC : '+=';
DEC : '-=';
AUTO_INC : '++';
AUTO_DEC : '--';
NULL_OP : '??';
IF_OP : '?';
ELSE_OP : ':';
LPAREN : '(';
RPAREN : ')';
LBRAC : '[';
RBRAC : ']';
LCURL : '{';
RCURL : '}';
COMMA : ',';
SEMI_COLON : ';';
CAST : '->';
LAMBDA : '=>';
DOT : '.';
ASSIGN : '=';
TILDA : '~';
OR : O R;
AND : A N D;
XOR : X O R;
NOT : N O T | '!';

// Keywods //
K_AGGREGATE : A G G R E G A T E;
K_ALL : A L L;
K_ANTI : A N T I;
K_APPEND : A P P E N D;
K_AS : A S;
K_ASSUME : A S S U M E;
K_BEGIN : B E G I N;
K_BIND : B I N D;
K_BY : B Y;
K_CASE : C A S E;
K_CHUNK : C H U N K;
K_CREATE : C R E A T E;
K_DATA : D A T A;
K_DECLARE : D E C L A R E;
K_DELETE : D E L E T E;
K_DYNAMIC : D Y N A M I C;
K_ELSE : E L S E;
K_END : E N D;
K_ESCAPE : E S C A P E;
K_EXEC : E X E C | E X E C U T E;
K_FINAL : F I N A L | F I N A L I Z E;
K_FOR : F O R;
K_FULL : F U L L;
K_GLOBAL : G L O B A L;
K_GRADIENT : G R A D I E N T;
K_IDENTITY : I D E N T I T Y | I D E N T;
K_IF : I F;
K_IN : I N;
K_INITIAL : I N I T | I N I T I A L;
K_INLINE : I N L I N E;
K_INNER : I N N E R;
K_INTO : I N T O;
K_IS : I S;
K_KEEP : K E E P;
K_LAMBDA : L A M B D A;
K_LEFT : L E F T;
K_LITERAL : L I T E R A L;
K_LOCAL : L O C A L;
K_MAIN : M A I N;
K_MAP : M A P;
K_MERGE : M E R G E;
K_NOT : N O T;
K_NULL : N U L L;
K_ON : O N;
K_ORDER : O R D E R;
K_OVER : O V E R;
K_PARTITIONS : P A R T I T I O N S | T H R E A D S;
K_PRINT : P R I N T;
K_PROCEDURE : P R O C | P R O C E D U R E;
K_READ : R E A D;
K_REDUCE : R E D U C E;
K_RETURN : R E T U R N;
K_RIGHT : R I G H T;
K_RUN : R U N ;
K_SCALAR : S C A L A R;
K_SCRIPT : S C R I P T;
K_SET : S E T;
K_SIZE : S I Z E;
K_STATIC : S T A T I C;
K_STRUCT : S T R U C T;
K_TABLE : T A B L E;
K_THEN : T H E N;
K_TO : T O;
K_UPDATE : U P D A T E;
K_USING : U S I N G;
K_VALUES : V A L U E S;
K_VARS : V A R S;
K_WHEN : W H E N;
K_WHERE : W H E R E;
K_WITH : W I T H;
K_WHILE : W H I L E;

// Core types //
T_BLOB : B L O B;
T_BOOL : B O O L;
T_DATE : D A T E;
T_DOUBLE : D O U B L E;
T_INT : I N T;
T_STRING : S T R I N G;

// Cell Literal Support //
NULL_BOOL : '@@' N U L L '_' B O O L;
NULL_INT : '@@' N U L L '_' I N T;
NULL_DATE : '@@' N U L L '_' D A T E;
NULL_DOUBLE : '@@' N U L L '_' D O U B L E;
NULL_STRING : '@@' N U L L '_' S T R I N G;
NULL_BLOB : '@@' N U L L '_' B L O B;

LITERAL_BOOL 
	: T R U E 
	| F A L S E
	;
LITERAL_BLOB 
	: '0' X (HEX HEX)*;
LITERAL_DATE 
	: '\'' DIGIT+ '-' DIGIT+ '-' DIGIT+ '\'' T 												// 'YYYY-MM-DD'T
	| '\'' DIGIT+ '-' DIGIT+ '-' DIGIT+ ':' DIGIT+ ':' DIGIT+ ':' DIGIT+ '\'' T				// 'YYYY-MM-DD:HH:MM:SS'T
	| '\'' DIGIT+ '-' DIGIT+ '-' DIGIT+ ':' DIGIT+ ':' DIGIT+ ':' DIGIT+ '.' DIGIT+ '\'' T	// 'YYYY-MM-DD:HH:MM:SS.LLLLLLLL'T
	;
LITERAL_DOUBLE 
	: ('~')? DIGIT+ '.' DIGIT+ (D)?  // '~' INDICATES A NEGATIVE NUMBER
	| ('~')? (DIGIT+) D			// '~' INDICATES A NEGATIVE NUMBER, 'D' MEANS THIS HAS THE FORM OF AN INT, BUT WE WANT IT TO BE A DOUBLE; AVOIDS HAVING TO DO A CAST
	;
LITERAL_INT 
	: ('~')? DIGIT+ // '~' INDICATES A NEGATIVE NUMBER
	;
LITERAL_STRING 
	: '\'' ( ~'\'' | '\'\'' )* '\'' // NORMAL STRING -> 'abcdef'
	| '"' ( ~'"' | '""')* '"'		// NORMAL STRING -> "ABCDEF"
	| '\'\''						// EMPTY STRING -> ''
	| SLIT .*? SLIT					// COMPLEX STRING LITERAL $$ ANYTHING $$
	| C R L F						// \n
	| T A B							// \t
	;

// Command Term //
CTERM : '%;';

// Scalar Identifier Parameter Text //
//MATRIX : IDENTIFIER '[' ']';
RECORD_REF : '@@' R E C O R D;
SCALAR : '@' IDENTIFIER;
IDENTIFIER : [a-zA-Z_] [a-zA-Z_0-9]*;

// Comments and whitespace //
SINGLE_LINE_COMMENT : '//' ~[\r\n]* -> channel(HIDDEN);
MULTILINE_COMMENT : '/*' .*? ( '*/' | EOF ) -> channel(HIDDEN);
WS : ( ' ' | '\t' |'\r' | '\n' | '\r\n')* -> channel(HIDDEN);

fragment SLIT : '$$';
fragment DIGIT : [0-9];
fragment HEX : [aAbBcCdDeEfF0123456789];
fragment A : [aA];
fragment B : [bB];
fragment C : [cC];
fragment D : [dD];
fragment E : [eE];
fragment F : [fF];
fragment G : [gG];
fragment H : [hH];
fragment I : [iI];
fragment J : [jJ];
fragment K : [kK];
fragment L : [lL];
fragment M : [mM];
fragment N : [nN];
fragment O : [oO];
fragment P : [pP];
fragment Q : [qQ];
fragment R : [rR];
fragment S : [sS];
fragment T : [tT];
fragment U : [uU];
fragment V : [vV];
fragment W : [wW];
fragment X : [xX];
fragment Y : [yY];
fragment Z : [zZ];
