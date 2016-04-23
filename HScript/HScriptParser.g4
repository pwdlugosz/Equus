parser grammar HScriptParser;

options
{
	tokenVocab = HScriptLexer;
}

compile_unit : command_set EOF;

// Commands //
command_set : command CTERM+ (command CTERM+)*;

command

	// Actions //
	: command_action

	// CRUDAM Commands //
	| crudam_read
	| crudam_read_fast
	| crudam_read_mapr
	| crudam_create_table
	| crudam_declare_table
	| crudam_declare_many
	| crudam_lambda
	| crudam_update
	| crudam_delete
	| crudam_aggregate
	| crudam_merge
	| file_method
	;

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
	Commands:
		-- Inplement CRUDAM
		-- C - Create/Declare
		-- R - Read
		-- U - Update
		-- D - Delete
		-- A - Aggregate
		-- M - Merge (Join)

	Note: Read comes first because we need the declarations

 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */ 

/* * * * * * * * * * * * * * * * * * * * * READ - STRUCTURED * * * * * * * * * * * * * * * * * * * * *
	
	READ DATA.STOCKS
		WHERE YEAR(TRADE_DATE) == 2015;
	DECLARE
		LOG_RETURN AS DOUBLE = 0D, OPEN_CLOSE AS DOUBLE = 0D;
	INIT
		BEGIN

		END;
	MAIN
		BEGIN

		END;
	FINAL
		BEGIN

		END;
	GO;

*/
crudam_read : 
	K_READ full_table_name (K_AS IDENTIFIER)? (where_clause)? SEMI_COLON
	(crudam_declare_many)? 
	(init_action)? 
	main_action
	(final_action)?;

/* * * * * * * * * * * * * * * * * * * * * READ - FAST * * * * * * * * * * * * * * * * * * * * *
	
	READ DATA.STOCKS
		WHERE YEAR(TRADE_DATE) == 2015;
		CREATE TABLE DATA.STOCKS_2015 AS TABLE.*;
	GO;

*/
crudam_read_fast : 
	K_READ full_table_name (K_AS IDENTIFIER)? (where_clause)? SEMI_COLON 
	return_action;

/* * * * * * * * * * * * * * * * * * * * * READ - MAP/REDUCE * * * * * * * * * * * * * * * * * * * * *
	
	READ DATA.STOCKS
		WHERE YEAR(TRADE_DATE) == 2015;
		MAP
			BEGIN

			END;
		REDUCE
			BEGIN
			END;
	GO;

*/
crudam_read_mapr : 
	K_READ full_table_name (K_AS IDENTIFIER)? (where_clause)? SEMI_COLON
	(partitions)?
	(crudam_declare_many)? 
	map_action
	(reduce_action)?;

init_action : K_INITIAL query_action;
main_action : K_MAIN query_action;
map_action : K_MAP query_action;
reduce_action : K_REDUCE query_action;
final_action : K_FINAL query_action;


/* * * * * * * * * * * * * * * * * * * * * CREATE TABLE * * * * * * * * * * * * * * * * * * * * *
	
	CREATE TABLE Financial.STOCKS
	(
		TICKER STRING TRUE, 
		TRADE_DATE DATE TRUE, 
		VOLUME INT TRUE, 
		PRICE DOUBLE TRUE
	);
	CHUNK SIZE 10000;
	GO;
	

*/
crudam_create_table : 
	K_CREATE K_TABLE full_table_name LPAREN create_table_unit (COMMA create_table_unit)* RPAREN SEMI_COLON 
	(create_table_size SEMI_COLON)?;
crudam_declare_table : K_DECLARE K_TABLE? IDENTIFIER LPAREN create_table_unit (COMMA create_table_unit)* RPAREN SEMI_COLON;
create_table_unit : IDENTIFIER K_AS? type (expression)?;
create_table_size : K_CHUNK K_SIZE expression;

/* * * * * * * * * * * * * * * * * * * * * LAMBDA * * * * * * * * * * * * * * * * * * * * *
	
	LAMBDA LOGIT(X) AS DOUBLE => 1 / (1 + EXP(-X)); GO;

*/
crudam_lambda : lambda_unit SEMI_COLON;

/* * * * * * * * * * * * * * * * * * * * * DECLARE * * * * * * * * * * * * * * * * * * * * *
	
	DECLARE
		A AS INT = 0,
		B AS DOUBLE = 0D,
		C AS DATE = NOW()
	;
	GO;

*/
crudam_declare_many : K_DECLARE declare_generic (COMMA declare_generic)* SEMI_COLON;
declare_generic
	: IDENTIFIER K_AS type (ASSIGN expression)?														# DeclareScalar
	| IDENTIFIER LBRAC expression RBRAC K_AS type													# DeclareMatrix1D
	| IDENTIFIER LBRAC expression COMMA expression RBRAC K_AS type									# DeclareMatrix2D
	| IDENTIFIER LBRAC RBRAC K_AS type ASSIGN matrix_expression										# DeclareMatrixLiteral
	;

/* * * * * * * * * * * * * * * * * * * * * UPDATE * * * * * * * * * * * * * * * * * * * * *
	
	UPDATE Financial.DIVIDENDS WHERE YEAR(TRADE_DATE) == 2010 SET DIVIDEND = DIVIDEND ?? 0D;

*/
crudam_update : 
	K_UPDATE full_table_name
	(where_clause)? SEMI_COLON
	K_SET update_unit (COMMA update_unit)* SEMI_COLON;
update_unit : IDENTIFIER ASSIGN expression;

/* * * * * * * * * * * * * * * * * * * * * DELETE * * * * * * * * * * * * * * * * * * * * *
	
	DELETE Financial.TICKERS WHERE SUBSTR(TICKER,1,1) = '^';
	
*/
crudam_delete : 
	K_DELETE full_table_name (where_clause)? SEMI_COLON;

/* * * * * * * * * * * * * * * * * * * * * AGGREGATE * * * * * * * * * * * * * * * * * * * * *
	
	AGGREGATE Financial.STOCK 
	WHERE YEAR(TRADE_DATE) == 2015 
	BY TICKER, MONTH(TRADE_DATE) AS TRADE_MONTH 
	OVER SUM(VOLUME) AS SUM_VOLUME, SUM(PRICE * VOLUME) AS SUM_PRICE, MIN(PRICE) AS LOW_PRICE, MAX(PRICE) AS HIGH_PRICE
	RETURN TICKER, TRADE_MONTH, SUM_PRICE / SUM_VOLUME AS AVG_PRICE, LOW_PRICE, HIGH_PRICE
	;

*/
crudam_aggregate : 
	K_AGGREGATE full_table_name (K_AS IDENTIFIER)? (where_clause)?  SEMI_COLON
	(partitions)?
	(K_BY expression_alias_list SEMI_COLON)? 
	(K_OVER beta_reduction_list SEMI_COLON)? 
	return_action SEMI_COLON;
by_clause : K_BY expression_alias_list;

/* * * * * * * * * * * * * * * * * * * * * MERGE * * * * * * * * * * * * * * * * * * * * *
	
	MERGE Financial.STOCKS AS S WITH Financial.DIVIDENDS AS D
	ON S.TRADE_DATE TO D.TRADE_DATE AND S.TICKER TO D.TICKER
	WHERE YEAR(S.TRADE_DATE) == 2011
	RETURN S.*, D.DIVIDEND
	;

*/
crudam_merge : 
	K_MERGE (merge_type)? merge_source K_WITH merge_source 
		(K_ON merge_equi_predicate (AND merge_equi_predicate)*)? 
		(where_clause)? SEMI_COLON
	(merge_algorithm SEMI_COLON)?
	return_action SEMI_COLON;
merge_source : full_table_name K_AS IDENTIFIER;
merge_equi_predicate : table_variable K_TO table_variable;
merge_algorithm : K_USING expression;
merge_type
	: K_INNER
	| K_ANTI
	| K_ANTI K_LEFT
	| K_ANTI K_RIGHT
	| K_LEFT
	| K_RIGHT
	| K_FULL
	;
	
// Partition Statement //
partitions : K_PARTITIONS (expression)? SEMI_COLON;


/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
	File Actions
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */ 
file_method : K_FILE DOT file_name (expression (COMMA expression)*)? SEMI_COLON;
file_name : IDENTIFIER | K_CREATE | K_DELETE;

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
	Actions:
		-- Have the ability to be built into a tree
		-- Are limited in some respects
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */ 

// Actions //
command_action : query_action;
query_action_set : query_action (SEMI_COLON query_action)*;
query_action
	: variable_assign																								# ActScalarAssign // x = y
	| print_action																									# ActPrint
	| return_action SEMI_COLON																						# ActReturn // Return A, B AS C, D * E / F AS G
	| K_BEGIN (query_action)+ K_END SEMI_COLON																		# ActBeginEnd // Begin <...> End
	| K_ESCAPE K_FOR SEMI_COLON																						# ActEscapeFor
	| K_ESCAPE K_READ SEMI_COLON																					# ActEscapeRead
	| system_action																									# ActSys
	| matrix_name ASSIGN matrix_expression SEMI_COLON																# ActMatAssign
	| matrix_unit_assign																							# ActMatUnitAssign
	| execute_script																								# ActExecuteScript
	| K_IF expression SEMI_COLON K_THEN query_action (SEMI_COLON K_ELSE query_action)?								# ActIf // IF t == v THEN (x++) ELSE (x--)
	| K_FOR variable ASSIGN expression K_TO expression SEMI_COLON query_action										# ActFor // For T = 0 to 10 (I++,I--)
	| K_WHILE expression SEMI_COLON query_action																	# ActWhile
	;

// Print //
print_action 
	: K_PRINT expression_or_wildcard_set SEMI_COLON					# PrintScalar
	| K_PRINT matrix_expression SEMI_COLON							# PrintMatrix
	;

// Matrix //
matrix_unit_assign
	: (K_GLOBAL DOT | K_LOCAL DOT)? IDENTIFIER LBRAC expression COMMA expression RBRAC ASSIGN expression SEMI_COLON		# MUnit2DAssign
	| (K_GLOBAL DOT | K_LOCAL DOT)? IDENTIFIER LBRAC expression COMMA expression RBRAC INC expression SEMI_COLON			# MUnit2DInc
	| (K_GLOBAL DOT | K_LOCAL DOT)? IDENTIFIER LBRAC expression COMMA expression RBRAC AUTO_INC SEMI_COLON					# MUnit2DAutoInc
	| (K_GLOBAL DOT | K_LOCAL DOT)? IDENTIFIER LBRAC expression COMMA expression RBRAC DEC expression SEMI_COLON			# MUnit2DDec
	| (K_GLOBAL DOT | K_LOCAL DOT)? IDENTIFIER LBRAC expression COMMA expression RBRAC AUTO_DEC SEMI_COLON					# MUnit2DAutoDec
	| (K_GLOBAL DOT | K_LOCAL DOT)? IDENTIFIER LBRAC expression RBRAC ASSIGN expression SEMI_COLON							# MUnit1DAssign
	| (K_GLOBAL DOT | K_LOCAL DOT)? IDENTIFIER LBRAC expression RBRAC INC expression SEMI_COLON							# MUnit1DInc
	| (K_GLOBAL DOT | K_LOCAL DOT)? IDENTIFIER LBRAC expression RBRAC AUTO_INC SEMI_COLON									# MUnit1DAutoInc
	| (K_GLOBAL DOT | K_LOCAL DOT)? IDENTIFIER LBRAC expression RBRAC DEC expression SEMI_COLON							# MUnit1DDec
	| (K_GLOBAL DOT | K_LOCAL DOT)? IDENTIFIER LBRAC expression RBRAC AUTO_DEC SEMI_COLON									# MUnit1DAutoDec
	;

// Assign //
variable_assign
	: variable ASSIGN expression SEMI_COLON		# ActAssign
	| variable INC expression SEMI_COLON		# ActInc
	| variable AUTO_INC SEMI_COLON				# ActAutoInc
	| variable DEC expression SEMI_COLON		# ActDec
	| variable AUTO_DEC SEMI_COLON				# ActAutoDec
	;

// System action //
system_action : K_EXEC IDENTIFIER SEMI_COLON hparameter_set?;

// Execute - MARK FOR DELETE //
execute_script : K_EXEC K_SCRIPT expression SEMI_COLON (bind_element_set)?;
bind_element_set : K_BIND SEMI_COLON (bind_element SEMI_COLON)+;
bind_element : SCALAR ASSIGN (K_STATIC | K_DYNAMIC)? expression;

hparameter_set : hparameter*;
hparameter : SCALAR ASSIGN (K_DATA full_table_name | expression | expression_alias_list | lambda_unit | matrix_expression | K_OUT IDENTIFIER) SEMI_COLON;

// 'TO' methods //
return_action : (K_INSERT K_INTO | K_CREATE K_TABLE) full_table_name K_VALUES expression_or_wildcard_set;

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
	Matricies:

 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */ 
 matrix_expression
	: MINUS matrix_expression															# MatrixMinus
	| NOT matrix_expression																# MatrixInvert
	| TILDA matrix_expression															# MatrixTranspose
	| matrix_expression MUL MUL matrix_expression										# MatrixTrueMul

	| matrix_expression op=(MUL | DIV | DIV2) matrix_expression							# MatrixMulDiv
	| matrix_expression op=(MUL | DIV | DIV2) expression								# MatrixMulDivLeft
	| expression op=(MUL | DIV | DIV2) matrix_expression								# MatrixMulDivRight

	| matrix_expression op=(PLUS | MINUS) matrix_expression								# MatrixAddSub
	| expression op=(PLUS | MINUS) matrix_expression									# MatrixAddSubLeft
	| matrix_expression op=(PLUS | MINUS) expression									# MatrixAddSubRight

	| matrix_name																		# MatrixLookup
	| matrix_literal																	# MatrixLiteral
	| K_IDENTITY LPAREN type COMMA expression RPAREN									# MatrixIdent

	| LPAREN matrix_expression RPAREN													# MatrixParen
	;

matrix_name : IDENTIFIER LBRAC RBRAC;
matrix_literal : vector_literal (COMMA vector_literal)*;
vector_literal : LCURL expression (COMMA expression)* RCURL;

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
	Beta Reductions:

 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */ 
 beta_reduction_list : beta_reduction (COMMA beta_reduction)*;
 beta_reduction : SET_REDUCTIONS LPAREN (expression_alias_list)? RPAREN (where_clause)? (K_AS IDENTIFIER)?;

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
	Expressions:

 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */ 
 // Lambdas //
lambda_unit
	: K_LAMBDA IDENTIFIER LPAREN (IDENTIFIER (COMMA IDENTIFIER)*)? RPAREN LAMBDA expression	# LambdaGeneric
	| K_LAMBDA IDENTIFIER LAMBDA K_GRADIENT IDENTIFIER K_OVER IDENTIFIER					# LambdaGradient 
	;

 // Return Expression //
expression_or_wildcard_set : expression_or_wildcard (COMMA expression_or_wildcard)*;
expression_or_wildcard
	: expression_alias # EOW_expression
	| K_LOCAL DOT MUL (K_AS IDENTIFIER)? # EOW_local_star
	| K_GLOBAL DOT MUL (K_AS IDENTIFIER)? # EOW_global_star
	| IDENTIFIER DOT MUL (K_AS IDENTIFIER)? # EOW_table_star
	| K_TABLE DOT MUL (K_AS IDENTIFIER)? # EOW_tables_star
	; 

// Where Clause //
where_clause : K_WHERE expression;

// Expression Lists //
expression_alias_list : expression_alias (COMMA expression_alias)*;
expression_list : expression (COMMA expression)*;

// Expressions //
expression_alias : expression (K_AS IDENTIFIER)?;
expression
	: type DOT IDENTIFIER (DOT LITERAL_INT)?															# Pointer
	| op=(NOT | PLUS | MINUS) expression																# Uniary
	| expression POW expression																			# Power
	| expression op=(MUL | DIV | MOD | DIV2) expression													# MultDivMod
	| expression op=(PLUS | MINUS) expression															# AddSub
	| expression op=(GT | GTE | LT | LTE) expression													# GreaterLesser
	| expression op=(EQ | NEQ) expression																# Equality
	| expression K_IS K_NULL																			# IsNull
	| expression AND expression																			# LogicalAnd
	| expression op=(OR | XOR) expression																# LogicalOr
	| expression CAST type  																			# Cast
	| variable																							# ExpressionVariable
	| cell																								# Static
	| expression NULL_OP expression																		# IfNullOp
	| expression IF_OP expression (ELSE_OP expression)?													# IfOp
	| K_CASE (K_WHEN expression K_THEN expression)+ (K_ELSE expression)? K_END							# CaseOp
	| function_name LPAREN ( expression ( COMMA expression )* )? RPAREN									# Function
	| (K_GLOBAL DOT | K_LOCAL DOT)? IDENTIFIER LBRAC expression COMMA expression RBRAC					# Matrix2D
	| (K_GLOBAL DOT | K_LOCAL DOT)? IDENTIFIER LBRAC expression RBRAC									# Matrix1D
	| LPAREN expression RPAREN																			# Parens
	;

/*

	Variables:
	FieldName -> look only at the table
	TableName.FieldName -> look only at table
	Global.FieldName -> look at global heap
	Local.FieldName -> look at local heap

*/
variable
	: IDENTIFIER			# VariableNaked
	| local_variable		# VariableLocal
	| global_variable		# VariableGlobal
	| table_variable		# VariableTable
	;

local_variable : K_LOCAL DOT IDENTIFIER;
global_variable : K_GLOBAL DOT IDENTIFIER;
table_variable : IDENTIFIER DOT IDENTIFIER;

// Cell Logic //
cell
	: LITERAL_BOOL			# CellLiteralBool
	| LITERAL_INT			# CellLiteralInt
	| LITERAL_DOUBLE		# CellLiteralDouble
	| LITERAL_DATE			# CellLiteralDate
	| LITERAL_STRING		# CellLiteralString
	| LITERAL_BLOB			# CellLiteralBLOB

	| NULL_BOOL				# CellNullBool
	| NULL_INT				# CellNullInt
	| NULL_DOUBLE			# CellNullDouble
	| NULL_DATE				# CellNullDate
	| NULL_STRING			# CellNullString
	| NULL_BLOB				# CellNullBlob
	;

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
	Support:

 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */ 

// Table logic //
full_table_name 
	: K_GLOBAL DOT table_name
	| database_name DOT table_name
	;
table_name : IDENTIFIER;
database_name : IDENTIFIER;
function_name : IDENTIFIER;

// Types //
type : (T_BLOB | T_BOOL | T_DATE | T_DOUBLE | T_INT | T_STRING) (DOT LITERAL_INT)?;
