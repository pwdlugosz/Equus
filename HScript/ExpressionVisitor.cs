using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Shire;
using Equus.QuarterHorse;
using Equus.Nokota;
using Equus.Andalusian;
using Equus.HScript;
using Equus.Gidran;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Equus.HScript
{

    /// <summary>
    /// Walks a parser tree and creates an abstract syntax tree in the form of an FNode
    /// </summary>
    public class ExpressionVisitor : HScriptParserBaseVisitor<FNode>
    {

        const char SLIT1 = '\'';
        const char SLIT2 = '"';
        const char SLIT3 = '$';
        const char NEGATIVE = '~';
        const char DATE_SUFFIX_U = 'T';
        const char DATE_SUFFIX_L = 't';
        const char DOUBLE_SUFFIX_U = 'D';
        const char DOUBLE_SUFFIX_L = 'd';
        const string DEFAULT_ALIAS = "T";
        const string STRING_NEWLINE = "crlf";
        const string STRING_TAB = "tab";

        // Constructor //
        public ExpressionVisitor(MemoryStruct UseLocalHeap, Workspace Home)
            :base()
        {
            this.LocalHeap = UseLocalHeap ?? new MemoryStruct(true);
            this.GlobalHeap = Home.GlobalHeap ?? new MemoryStruct(false);
            this.Columns = new Dictionary<string, Schema>(StringComparer.OrdinalIgnoreCase);
            this.Registers = new Dictionary<string, Register>(StringComparer.OrdinalIgnoreCase);
            this.Lambdas = Home.Lambdas ?? new Heap<Lambda>();
            this.IsLocal = (UseLocalHeap != null);
        }

        public ExpressionVisitor(MemoryStruct UseLocalHeap, Workspace Home, string Alias, Schema Columns, Register Memory)
            : this(UseLocalHeap, Home)
        {
            this.AddSchema(Alias, Columns, Memory);
        }

        // Properties //
        public FNode MasterNode
        {
            get;
            private set;
        }

        public MemoryStruct LocalHeap
        {
            get;
            private set;
        }

        public MemoryStruct GlobalHeap
        {
            get;
            private set;
        }

        public Dictionary<string, Schema> Columns
        {
            get;
            private set;
        }

        public Dictionary<string, Register> Registers
        {
            get;
            private set;
        }

        public Heap<Lambda> Lambdas
        {
            get;
            protected set;
        }

        public bool IsLocal
        {
            get;
            private set;
        }

        // Add methods //
        public void AddSchema(string Alias, Schema Columns, Register MemoryLocation)
        {
            Alias = Alias ?? DEFAULT_ALIAS + this.Columns.Count.ToString();
            this.Columns.Add(Alias, Columns);
            this.Registers.Add(Alias, MemoryLocation);
        }

        // Tree methods //
        public override FNode VisitPointer(HScriptParser.PointerContext context)
        {
            string name = context.IDENTIFIER().GetText();
            CellAffinity type = VisitorHelper.GetAffinity(context.type());
            int size = VisitorHelper.GetSize(context.type(), false); // pointers can never come from a table
            return new FNodePointer(this.MasterNode, name, type, size);
        }

        public override FNode VisitUniary(HScriptParser.UniaryContext context)
        {

            FNode t;
            FNode right = this.Visit(context.expression());
            
            if (context.op.Type == HScriptParser.MINUS) // -A
                t = new FNodeResult(this.MasterNode, new CellUniMinus());
            else if (context.op.Type == HScriptParser.PLUS) // +A
                t = new FNodeResult(this.MasterNode, new CellUniPlus());
            else // !A
                t = new FNodeResult(this.MasterNode, new CellUniNot());

            // Add the node //
            t.AddChildNode(right);

            this.MasterNode = t;
            
            return t;

        }

        public override FNode VisitPower(HScriptParser.PowerContext context)
        {

            FNode t = new FNodeResult(this.MasterNode, new CellFuncFVPower());
            FNode left = this.Visit(context.expression()[0]);
            FNode right = this.Visit(context.expression()[1]);
            t.AddChildren(left, right);
            this.MasterNode = t;
            return t;

        }

        public override FNode VisitMultDivMod(HScriptParser.MultDivModContext context)
        {

            FNode t;
            FNode left = this.Visit(context.expression()[0]);
            FNode right = this.Visit(context.expression()[1]);

            if (context.op.Type == HScriptParser.MUL) // left * right
                t = new FNodeResult(this.MasterNode, new CellBinMult());
            else if (context.op.Type == HScriptParser.DIV) // left / right
                t = new FNodeResult(this.MasterNode, new CellBinDiv());
            else if (context.op.Type == HScriptParser.DIV2) // left /? right
                t = new FNodeResult(this.MasterNode, new CellBinDiv2());
            else // left % right
                t = new FNodeResult(this.MasterNode, new CellBinMod());

            // Add the node //
            t.AddChildren(left, right);

            this.MasterNode = t;

            return t;

        }

        public override FNode VisitAddSub(HScriptParser.AddSubContext context)
        {

            FNode t;
            FNode left = this.Visit(context.expression()[0]);
            FNode right = this.Visit(context.expression()[1]);

            if (context.op.Type == HScriptParser.PLUS) // left + right
                t = new FNodeResult(this.MasterNode, new CellBinPlus());
            else // left - right
                t = new FNodeResult(this.MasterNode, new CellBinMinus());
            
            // Add the node //
            t.AddChildren(left, right);

            this.MasterNode = t;

            return t;

        }

        public override FNode VisitGreaterLesser(HScriptParser.GreaterLesserContext context)
        {

            FNode t;
            FNode left = this.Visit(context.expression()[0]);
            FNode right = this.Visit(context.expression()[1]);

            if (context.op.Type == HScriptParser.LTE) // left <= right
                t = new FNodeResult(this.MasterNode, new CellBoolLTE());
            else if (context.op.Type == HScriptParser.LT) // left < right
                t = new FNodeResult(this.MasterNode, new CellBoolLT());
            else if (context.op.Type == HScriptParser.GTE) // left >= right
                t = new FNodeResult(this.MasterNode, new CellBoolGTE());
            else // left > right
                t = new FNodeResult(this.MasterNode, new CellBoolGT());

            // Add the node //
            t.AddChildren(left, right);

            this.MasterNode = t;

            return t;

        }

        public override FNode VisitEquality(HScriptParser.EqualityContext context)
        {

            FNode t;
            FNode left = this.Visit(context.expression()[0]);
            FNode right = this.Visit(context.expression()[1]);

            if (context.op.Type == HScriptParser.EQ) // left == right
                t = new FNodeResult(this.MasterNode, new CellBoolEQ());
            else // left != right
                t = new FNodeResult(this.MasterNode, new CellBoolNEQ());

            // Add the node //
            t.AddChildren(left, right);

            this.MasterNode = t;

            return t;

        }

        public override FNode VisitIsNull(HScriptParser.IsNullContext context)
        {

            FNode t = new FNodeResult(this.MasterNode, new CellFuncFKIsNull());
            FNode left = this.Visit(context.expression());
            
            // Add the node //
            t.AddChildren(left);

            this.MasterNode = t;

            return t;

        }

        public override FNode VisitLogicalAnd(HScriptParser.LogicalAndContext context)
        {
            FNode t = new FNodeResult(this.MasterNode, new CellFuncFVAND());
            FNode left = this.Visit(context.expression()[0]);
            FNode right = this.Visit(context.expression()[1]);

            // Add the node //
            t.AddChildren(left, right);

            this.MasterNode = t;

            return t;
        }

        public override FNode VisitLogicalOr(HScriptParser.LogicalOrContext context)
        {

            FNode t;
            FNode left = this.Visit(context.expression()[0]);
            FNode right = this.Visit(context.expression()[1]);

            if (context.op.Type == HScriptParser.OR) // left OR right
                t = new FNodeResult(this.MasterNode, new CellFuncFVOR());
            else // left XOR right
                t = new FNodeResult(this.MasterNode, new CellFuncFVXOR());

            // Add the node //
            t.AddChildren(left, right);

            this.MasterNode = t;

            return t;

        }

        public override FNode VisitCast(HScriptParser.CastContext context)
        {

            FNode left = this.Visit(context.expression());
            CellAffinity affinity = VisitorHelper.GetAffinity(context.type());
            int size = VisitorHelper.GetSize(context.type(), this.IsLocal);
            CellFunction f = new CellFunction("cast", 1, (x) => { return Cell.Cast(x[0], affinity); }, affinity);
            f.SetSize(size);
            FNode t = new FNodeResult(this.MasterNode, f);
            
            // Add the node //
            t.AddChildren(left);

            this.MasterNode = t;

            return t;

        }

        /*
        public override FNode VisitDynamicRef(HScriptParser.DynamicRefContext context)
        {

            FNode index = this.Visit(context.expression());
            CellAffinity t = VisitorHelper.GetAffinity(context.type());
            Register mem = this.Registers.Values.First();
            if (context.IDENTIFIER() != null)
                mem = this.Registers[context.IDENTIFIER().GetText()];
            return new FNodeDynamicRef(this.MasterNode, index, t, mem);

        }
        */

        public override FNode VisitIfNullOp(HScriptParser.IfNullOpContext context)
        {

            FNode t = new FNodeResult(this.MasterNode, new CellFuncFVIfNull());
            FNode left = this.Visit(context.expression()[0]);
            FNode right = this.Visit(context.expression()[1]);

            // Add the node //
            t.AddChildren(left, right);

            this.MasterNode = t;

            return t;

        }

        public override FNode VisitIfOp(HScriptParser.IfOpContext context)
        {

            FNode t = new FNodeResult(this.MasterNode, new CellFuncIf());
            FNode check = this.Visit(context.expression()[0]);
            FNode iftrue = this.Visit(context.expression()[1]);
            FNode iffalse = (context.ELSE_OP() != null) ? this.Visit(context.expression()[2]) : new FNodeValue(t, new Cell(iftrue.ReturnAffinity()));
            
            // Add the node //
            t.AddChildren(check, iftrue, iffalse);

            this.MasterNode = t;

            return t;

        }

        public override FNode VisitCaseOp(HScriptParser.CaseOpContext context)
        {

            List<FNode> when_nodes = new List<FNode>();
            List<FNode> then_nodes = new List<FNode>();
            FNode else_node = null;

            int when_then_count = context.K_WHEN().Count * 2;
            for (int i = 0; i < when_then_count; i += 2)
            {

                int when_idx = i;
                int then_idx = when_idx + 1;

                FNode when_node = this.Visit(context.expression()[when_idx]);
                FNode then_node = this.Visit(context.expression()[then_idx]);

                when_nodes.Add(when_node);
                then_nodes.Add(then_node);

            }

            // Check for the else //
            if (context.K_ELSE() != null)
                else_node = this.Visit(context.expression().Last());

            // Build the case statement //
            CellFuncCase func = new CellFuncCase(when_nodes, then_nodes, else_node);

            return new FNodeResult(this.MasterNode, func);

        }

        public override FNode VisitParens(HScriptParser.ParensContext context)
        {
            return this.Visit(context.expression());
        }

        public override FNode VisitFunction(HScriptParser.FunctionContext context)
        {
            
            // Get the function //
            string func_name = context.function_name().IDENTIFIER().GetText();

            // Check if the function is a lambda //
            if (this.Lambdas.Exists(func_name))
                return this.VisitLambdaInvoke(context);

            // Lookup the function //
            if (!CellFunctionFactory.Exists(func_name))
                throw new HScriptCompileException("Function '{0}' does not exist", func_name);
            CellFunction func_ref = CellFunctionFactory.LookUp(func_name);

            // Check the variable count //
            if (func_ref.ParamCount != -1 && func_ref.ParamCount != context.expression().Count)
                throw new HScriptCompileException("Function '{0}' expects {1} parameters but was passed {2} parameters", func_name, func_ref.ParamCount, context.expression().Count);

            // Create the node //
            FNode t = new FNodeResult(this.MasterNode, func_ref);

            // Get all the paramters //
            foreach (HScriptParser.ExpressionContext ctx in context.expression())
            {
                FNode node = this.Visit(ctx);
                t.AddChildNode(node);
            }

            this.MasterNode = t;

            return t;

        }

        public override FNode VisitMatrix2D(HScriptParser.Matrix2DContext context)
        {

            // Get the matrix //
            string name = context.IDENTIFIER().GetText();

            FNode row = this.Visit(context.expression()[0]);
            FNode col = this.Visit(context.expression()[1]);

            if (context.K_GLOBAL() != null && this.GlobalHeap.Arrays.Exists(name))
            {
                return new FNodeArrayDynamicRef(this.MasterNode, row, col, this.GlobalHeap, name);
            }
            else if (context.K_LOCAL() != null && this.LocalHeap.Arrays.Exists(name))
            {
                return new FNodeArrayDynamicRef(this.MasterNode, row, col, this.LocalHeap, name);
            }
            else if (this.GlobalHeap.Arrays.Exists(name))
            {
                return new FNodeArrayDynamicRef(this.MasterNode, row, col, this.GlobalHeap, name);
            }
            else if (this.LocalHeap.Arrays.Exists(name))
            {
                return new FNodeArrayDynamicRef(this.MasterNode, row, col, this.LocalHeap, name);
            }
            else
            {
                throw new HScriptCompileException("Matrix does not exist in either the local or global heaps '{0}'", name);
            }

        }

        public override FNode VisitMatrix1D(HScriptParser.Matrix1DContext context)
        {

            // Get the matrix //
            string name = context.IDENTIFIER().GetText();

            FNode row = this.Visit(context.expression());
            FNode col = FNodeFactory.Value(0);

            if (context.K_GLOBAL() != null && this.GlobalHeap.Arrays.Exists(name))
            {
                return new FNodeArrayDynamicRef(this.MasterNode, row, col, this.GlobalHeap, name);
            }
            else if (context.K_LOCAL() != null && this.LocalHeap.Arrays.Exists(name))
            {
                return new FNodeArrayDynamicRef(this.MasterNode, row, col, this.LocalHeap, name);
            }
            else if (this.GlobalHeap.Arrays.Exists(name))
            {
                return new FNodeArrayDynamicRef(this.MasterNode, row, col, this.GlobalHeap, name);
            }
            else if (this.LocalHeap.Arrays.Exists(name))
            {
                return new FNodeArrayDynamicRef(this.MasterNode, row, col, this.LocalHeap, name);
            }
            else
            {
                throw new HScriptCompileException("Matrix does not exist in either the local or global heaps '{0}'", name);
            }

        }

        private FNode VisitLambdaInvoke(HScriptParser.FunctionContext context)
        {

            string name = context.function_name().GetText();
            if (!this.Lambdas.Exists(name))
                throw new HScriptCompileException("Lambda '{0}' does not exist", name);
            Lambda mu = this.Lambdas[name];

            List<FNode> nodes = new List<FNode>();

            foreach (HScriptParser.ExpressionContext ctx in context.expression())
            {
                nodes.Add(this.Visit(ctx));
            }

            return mu.Bind(nodes);

        }

        // Cell parsing //
        public override FNode VisitCellLiteralBool(HScriptParser.CellLiteralBoolContext context)
        {

            // TRUE (any case)
            // FALSE (any case)
            bool value = bool.Parse(context.LITERAL_BOOL().GetText());
            Cell c = new Cell(value);

            return new FNodeValue(this.MasterNode, c);

        }

        public override FNode VisitCellLiteralInt(HScriptParser.CellLiteralIntContext context)
        {

            // ~12345 //
            string t = context.LITERAL_INT().GetText();
            bool negative = false;
            if (t[0] == NEGATIVE)
            {
                t = t.Substring(1, t.Length - 1);
                negative = true;
            }
            long value = long.Parse(t);
            if (negative)
                value = -value;
            Cell c = new Cell(value);
            return new FNodeValue(this.MasterNode, c);

        }

        public override FNode VisitCellLiteralDouble(HScriptParser.CellLiteralDoubleContext context)
        {

            // ~12345.6789//
            string t = context.LITERAL_DOUBLE().GetText();
            if (t.Last() == DOUBLE_SUFFIX_U)
                t = t.Substring(0, t.Length - 1);
            if (t.Last() == DOUBLE_SUFFIX_L)
                t = t.Substring(0, t.Length - 1);
            bool negative = false;
            if (t[0] == NEGATIVE)
            {
                t = t.Substring(1, t.Length - 1);
                negative = true;
            }
            double value = double.Parse(t);
            if (negative)
                value = -value;
            Cell c = new Cell(value);
            return new FNodeValue(this.MasterNode, c);

        }

        public override FNode VisitCellLiteralDate(HScriptParser.CellLiteralDateContext context)
        {

            // '2015-01-01'T -> '2015-01-01' //
            string t = context.LITERAL_DATE().GetText();
            t = t.Substring(0, t.Length - 1);
            Cell c = Cell.DateParse(t);
            return new FNodeValue(this.MasterNode, c);

        }

        public override FNode VisitCellLiteralString(HScriptParser.CellLiteralStringContext context)
        {
            
            Cell c = new Cell(CleanString(context.LITERAL_STRING().GetText()));
            return new FNodeValue(this.MasterNode, c);

        }

        public override FNode VisitCellLiteralBLOB(HScriptParser.CellLiteralBLOBContext context)
        {

            Cell c = Cell.ByteParse(context.LITERAL_BLOB().GetText());
            return new FNodeValue(this.MasterNode, c);

        }

        // Cell null parsing //
        public override FNode VisitCellNullBool(HScriptParser.CellNullBoolContext context)
        {
            return new FNodeValue(this.MasterNode, Cell.NULL_BOOL);
        }

        public override FNode VisitCellNullInt(HScriptParser.CellNullIntContext context)
        {
            return new FNodeValue(this.MasterNode, Cell.NULL_INT);
        }

        public override FNode VisitCellNullDouble(HScriptParser.CellNullDoubleContext context)
        {
            return new FNodeValue(this.MasterNode, Cell.NULL_DOUBLE);
        }

        public override FNode VisitCellNullDate(HScriptParser.CellNullDateContext context)
        {
            return new FNodeValue(this.MasterNode, Cell.NULL_DATE);
        }

        public override FNode VisitCellNullString(HScriptParser.CellNullStringContext context)
        {
            return new FNodeValue(this.MasterNode, Cell.NULL_STRING);
        }

        public override FNode VisitCellNullBlob(HScriptParser.CellNullBlobContext context)
        {
            return new FNodeValue(this.MasterNode, Cell.NULL_BLOB);
        }

        // Variables //
        public override FNode VisitVariableNaked(HScriptParser.VariableNakedContext context)
        {

            /*
             * Try the following for a naked variable
             * 1). Schema's w/o alias
             * 2). Local Heap 
             * 3). Global Heap 
             * 
             */

            string var_name = context.IDENTIFIER().GetText();
            
            // Check each schema //
            int Offset = 0;
            foreach(KeyValuePair<string,Schema> kv in this.Columns)
            {

                // Check the current schema //
                if (kv.Value.Contains(var_name))
                {
                    int idx = kv.Value.ColumnIndex(var_name);
                    Register mem = (this.Registers[kv.Key] == null) ? null : this.Registers[kv.Key];
                    //FNodeFieldRef field = new FNodeFieldRef(this.MasterNode, idx + Offset, kv.Value.ColumnAffinity(idx), mem);
                    FNodeFieldRef field = new FNodeFieldRef(this.MasterNode, idx, kv.Value.ColumnAffinity(idx), kv.Value.ColumnSize(idx), mem);
                    field.Name = var_name;
                    return field;
                }

                // Increment the offset //
                Offset += kv.Value.Count;

            }

            // Check local heap -- ctor will attach the name //
            if (this.LocalHeap.Scalars.Exists(var_name))
                return new FNodeHeapRef(this.MasterNode, this.LocalHeap, var_name);

            // Check global heap -- ctor will attach the name //
            if (this.GlobalHeap.Scalars.Exists(var_name))
                return new FNodeHeapRef(this.MasterNode, this.GlobalHeap, var_name);

            throw new HScriptCompileException("Variable '{0}' does not exist in the global heap, the local heap, or any known table", var_name);

        }

        public override FNode VisitVariableTable(HScriptParser.VariableTableContext context)
        {

            string table_alias = context.table_variable().IDENTIFIER()[0].GetText();
            string var_name = context.table_variable().IDENTIFIER()[1].GetText();

            // Check that the table exisits //
            if (!this.Columns.ContainsKey(table_alias))
                throw new HScriptCompileException("Table alias '{0}' does not exist", table_alias);

            // Check that the column exists //
            int idx = this.Columns[table_alias].ColumnIndex(var_name);
            if (idx == -1)
                throw new HScriptCompileException("Field '{0}' does not exist in '{1}'", var_name, table_alias);

            // Return node //
            Register mem = (this.Registers[table_alias] == null) ? null : this.Registers[table_alias];
            FNode t = new FNodeFieldRef(this.MasterNode, idx, this.Columns[table_alias].ColumnAffinity(idx), this.Columns[table_alias].ColumnSize(idx), mem);
            t.Name = var_name;
            return t;

        }

        public override FNode VisitVariableLocal(HScriptParser.VariableLocalContext context)
        {

            string var_name = context.local_variable().IDENTIFIER().GetText();
            if (this.LocalHeap.Scalars.Exists(var_name))
                return new FNodeHeapRef(this.MasterNode, this.LocalHeap, var_name);
            throw new HScriptCompileException("Variable '{0}' is not a local variable", var_name);

        }

        public override FNode VisitVariableGlobal(HScriptParser.VariableGlobalContext context)
        {

            string var_name = context.global_variable().IDENTIFIER().GetText();
            if (this.GlobalHeap.Scalars.Exists(var_name))
                return new FNodeHeapRef(this.MasterNode, this.GlobalHeap, var_name);
            throw new HScriptCompileException("Variable '{0}' is not a global variable", var_name);

        }

        // To methods //
        public FNode ToNode(HScriptParser.ExpressionContext context)
        {
            this.MasterNode = null;
            return this.Visit(context);
        }

        public FNodeSet ToNodes(HScriptParser.Expression_alias_listContext context)
        {

            FNodeSet fset = new FNodeSet();

            // Check for instances where we have no expressions //
            if (context == null)
                return fset;

            // Otherwise, parse each expression //
            foreach (HScriptParser.Expression_aliasContext c in context.expression_alias())
            {

                FNode node = this.ToNode(c.expression());
                string alias = "F" + fset.Count.ToString();
                if (node.Name != null)
                    alias = node.Name;
                if (c.K_AS() != null)
                    alias = c.IDENTIFIER().GetText();
                fset.Add(alias, node);

            }

            return fset;

        }

        public Predicate ToPredicate(HScriptParser.ExpressionContext context)
        {
            return new Predicate(this.ToNode(context));
        }

        public Aggregate ToReduce(HScriptParser.Beta_reductionContext context)
        {

            // Get the expressions //
            FNodeSet nodes = this.ToNodes(context.expression_alias_list());

            // Get the reduction ID //
            string RID = context.SET_REDUCTIONS().GetText().ToLower();

            // Get the reduction //
            Aggregate r;
            switch (RID)
            {

                case "avg":
                    if (nodes.Count == 2) r = CellReductions.Average(nodes[0], nodes[1]);
                    else r = CellReductions.Average(nodes[0]);
                    break;

                case "corr":
                    if (nodes.Count == 3) r = CellReductions.Correl(nodes[0], nodes[1], nodes[2]);
                    else r = CellReductions.Correl(nodes[0], nodes[1]);
                    break;

                case "count":
                    r = CellReductions.Count(nodes[0]);
                    break;

                case "count_all":
                    r = CellReductions.CountAll();
                    break;

                case "count_null":
                    r = CellReductions.CountNull(nodes[0]);
                    break;

                case "covar":
                    if (nodes.Count == 3) r = CellReductions.Covar(nodes[0], nodes[1], nodes[2]);
                    else r = CellReductions.Covar(nodes[0], nodes[1]);
                    break;

                case "freq":
                    if (nodes.Count == 2) r = CellReductions.Frequency(new Predicate(nodes[1]), nodes[0]);
                    else r = CellReductions.Frequency(new Predicate(nodes[0]));
                    break;

                case "intercept":
                    if (nodes.Count == 3) r = CellReductions.Intercept(nodes[0], nodes[1], nodes[2]);
                    else r = CellReductions.Intercept(nodes[0], nodes[1]);
                    break;

                case "max":
                    r = CellReductions.Max(nodes[0]);
                    break;

                case "min":
                    r = CellReductions.Min(nodes[0]);
                    break;

                case "slope":
                    if (nodes.Count == 3) r = CellReductions.Slope(nodes[0], nodes[1], nodes[2]);
                    else r = CellReductions.Slope(nodes[0], nodes[1]);
                    break;

                case "stdev":
                    if (nodes.Count == 2) r = CellReductions.Stdev(nodes[0], nodes[1]);
                    else r = CellReductions.Stdev(nodes[0]);
                    break;

                case "sum":
                    r = CellReductions.Sum(nodes[0]);
                    break;

                case "var":
                    if (nodes.Count == 2) r = CellReductions.Var(nodes[0], nodes[1]);
                    else r = CellReductions.Var(nodes[0]);
                    break;

                default:
                    throw new Exception(string.Format("Reducer with name '{0}' is invalid", RID));
            }

            // Check for a filter //
            if (context.where_clause() != null)
                r.BaseFilter = VisitorHelper.GetWhere(this, context.where_clause());

            // Return //
            return r;

        }

        public AggregateSet ToReducers(HScriptParser.Beta_reduction_listContext context)
        {

            AggregateSet aggregates = new AggregateSet();
            foreach (HScriptParser.Beta_reductionContext ctx in context.beta_reduction())
            {
                Aggregate agg = this.ToReduce(ctx);
                string alias = 
                    (ctx.IDENTIFIER() == null) 
                    ? "R" + aggregates.Count.ToString() 
                    : ctx.IDENTIFIER().GetText();
                aggregates.Add(this.ToReduce(ctx), alias);
            }
            return aggregates;

        }

        // Statics //
        public static string CleanString(string Value)
        {

            // Check for tab //
            if (Value.ToLower() == STRING_TAB)
                return "\t";

            // Check for newline //
            if (Value.ToLower() == STRING_NEWLINE)
                return "\n";

            // Check for lengths less than two //
            if (Value.Length < 2)
                return Value;

            // Handle 'ABC' to ABC //
            if (Value.First() == SLIT1 && Value.Last() == SLIT1)
                return Value.Substring(1, Value.Length - 2);

            // Handle "ABC" to ABC //
            if (Value.First() == SLIT2 && Value.Last() == SLIT2)
                return Value.Substring(1, Value.Length - 2);

            // Handle lengths less than four //
            if (Value.Length < 4)
                return Value;

            // Handle $$ABC$$ to ABC //
            int Len = Value.Length;
            if (Value[0] == SLIT3 && Value[1] == SLIT3 && Value[Len - 2] == SLIT3 && Value[Len - 1] == SLIT3)
                return Value.Substring(2, Value.Length - 4);

            // Otherwise, return Value //
            return Value;

        }

        // Parsers //
        public static FNode ParseFNode(string Text, MemoryStruct LocalHeap, Workspace Home, string Alias, Schema Columns, Register Memory)
        {

            // Build text stream //
            AntlrInputStream ais = new AntlrInputStream(Text);
            HScriptLexer lex = new HScriptLexer(ais);

            // Build token tree //
            CommonTokenStream cts = new CommonTokenStream(lex);
            HScriptParser par = new HScriptParser(cts);

            // Build AST //
            IParseTree tree = par.expression();

            // Visit each node getting the final node //
            ExpressionVisitor v = new ExpressionVisitor(LocalHeap, Home);
            v.AddSchema(Alias, Columns, Memory);

            return v.Visit(tree);

        }

        public static Predicate ParsePredicate(string Text, MemoryStruct LocalHeap, Workspace Home, string Alias, Schema Columns, Register Memory)
        {
            FNode w = ParseFNode(Text, LocalHeap, Home, Alias, Columns, Memory);
            return new Predicate(w);
        }

    }

    public sealed class PointerExpressionVisitor : ExpressionVisitor
    {

        private List<string> _Pointers;

        public PointerExpressionVisitor(List<string> Pointers, Workspace Home)
            : base(null, Home)
        {
            this._Pointers = Pointers;
        }

        public override FNode VisitVariableNaked(HScriptParser.VariableNakedContext context)
        {
            string name = context.IDENTIFIER().GetText();
            CellAffinity affinity = CellAffinity.INT;
            int size = Schema.FixSize(affinity, -1);
            return new FNodePointer(this.MasterNode, name, affinity, size);
        }

    }

}
