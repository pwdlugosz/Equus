using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Shire;
using Equus.QuarterHorse;
using Equus.Nokota;
using Equus.Andalusian;
using Equus.HScript;
using Equus.Gidran;
using Equus.HScript.Parameters;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Equus.HScript
{

    public static class VisitorHelper
    {

        // Get the data //
        public static DataSet GetData(Workspace Enviro, HScriptParser.Full_table_nameContext context)
        {

            // Get the name //
            string t_name = context.table_name().IDENTIFIER().GetText();

            // Global context //
            if (context.K_GLOBAL() != null)
            {
                if (Enviro.ChunkHeap.Exists(t_name))
                    return Enviro.ChunkHeap[t_name];
                throw new HScriptCompileException("Global chunk '{0}' does not exist", t_name);
            }

            // Table context //
            if (context.database_name() != null)
            {
                string d_base = context.database_name().IDENTIFIER().GetText();
                if (Enviro.Exists(d_base, t_name))
                    return Enviro.GetStaticTable(d_base, t_name);
                throw new HScriptCompileException("Table '{0}' does not exist", t_name);
            }

            throw new HScriptCompileException("Data '{0}' does not exist in memory or on disk", t_name);

        }

        public static DataSet GetData(Workspace Enviro, FNodeSet Nodes, HScriptParser.Return_actionContext context)
        {

            // Get the table name //
            string name = context.full_table_name().table_name().IDENTIFIER().GetText();
            string db =
                (context.full_table_name().database_name() == null)
                ? "global"
                : context.full_table_name().database_name().GetText();

            // Figure out if we need to append //
            bool appendto =
                (context.K_INSERT() != null)
                ? true
                : false;

            // Global -- Append //
            if (context.full_table_name().database_name() == null && appendto)
            {
                if (Enviro.ChunkHeap.Exists(name))
                    return Enviro.ChunkHeap[name];
                throw new HScriptCompileException(string.Format("Chunk '{0}' does not exist", name));
            }

            // Static -- Append //
            if (appendto)
            {
                string fullname = db + "." + name;
                if (Enviro.Exists(db, name))
                    return Enviro.GetStaticTable(db, name);
                throw new HScriptCompileException(string.Format("Table '{0}' does not exist", fullname));
            }

            // Global -- Create New //
            if (context.full_table_name().database_name() == null)
            {
                RecordSet data = new RecordSet(Nodes.Columns);
                Enviro.ChunkHeap.Reallocate(name, data);
                return data;
            }

            // Static -- Create New //
            string dir = Enviro.Connections[db];
            Table t = new Table(dir, name, Nodes.Columns);
            return t;

        }

        public static RecordWriter GetWriter(Workspace Enviro, Schema Columns, HScriptParser.Return_actionContext context)
        {

            // Get the table name //
            string name = context.full_table_name().table_name().IDENTIFIER().GetText();
            string db =
                (context.full_table_name().database_name() == null)
                ? "GLOBAL"
                : context.full_table_name().database_name().GetText();

            // Figure out if we need to append //
            bool appendto =
                (context.K_INSERT() != null)
                ? true
                : false;

            // Global -- Append //
            if (context.full_table_name().database_name() == null && appendto)
            {
                if (Enviro.ChunkHeap.Exists(name))
                    return Enviro.ChunkHeap[name].OpenWriter();
                throw new HScriptCompileException(string.Format("Chunk '{0}' does not exist", name));
            }

            // Static -- Append //
            if (appendto)
            {
                string fullname = db + "." + name;
                if (Enviro.Exists(db, name))
                    return Enviro.GetStaticTable(db, name).OpenWriter();
                throw new HScriptCompileException(string.Format("Table '{0}' does not exist", fullname));
            }

            // Global -- Create New //
            if (context.full_table_name().database_name() == null)
            {
                RecordSet data = new RecordSet(Columns);
                Enviro.ChunkHeap.Reallocate(name, data);
                return data.OpenWriter();
            }

            // Static -- Create New //
            string dir = Enviro.Connections[db];
            Table t = new Table(dir, name, Columns);
            return t.OpenWriter();

        }

        // Create data //
        public static bool DataExists(Workspace Enviro, HScriptParser.Full_table_nameContext context)
        {

            // Get the name //
            string t_name = context.table_name().IDENTIFIER().GetText();

            // Global context //
            if (context.database_name() == null)
            {
                return Enviro.ChunkHeap.Exists(t_name);
            }

            // Table context //
            if (context.database_name() != null)
            {
                string d_base = context.database_name().IDENTIFIER().GetText();
                return Enviro.Exists(d_base, t_name);
            }

            return false;

        }

        public static DataSet CreateData(Workspace Enviro, Schema Columns, HScriptParser.Full_table_nameContext context)
        {

            // Get the name //
            string t_name = context.table_name().IDENTIFIER().GetText();

            // Global context //
            if (context.database_name() == null)
            {
                RecordSet rs = new RecordSet(Columns);
                Enviro.ChunkHeap.Reallocate(t_name, rs);
                return rs;
            }

            // Table context //
            if (context.database_name() != null)
            {
                string d_base = context.database_name().IDENTIFIER().GetText();
                if (!Enviro.Connections.Exists(d_base))
                    throw new HScriptCompileException("Connection to '{0}' does not exist", d_base);
                string dir = Enviro.Connections[d_base];
                Table t = new Table(dir, t_name, Columns);
                return t;
            }

            throw new HScriptCompileException("Cannot create data '{0}'", t_name);

        }

        // Cell affinity //
        public static CellAffinity GetAffinity(HScriptParser.TypeContext context)
        {
            string t = context.GetText().Split('.').First();
            return CellAffinityHelper.Parse(t);
        }

        public static int GetSize(HScriptParser.TypeContext context, bool IsLocal)
        {

            CellAffinity t = GetAffinity(context);
            
            // double, int, date are 8 bytes //
            if (t == CellAffinity.INT || t == CellAffinity.DOUBLE || t == CellAffinity.DATE_TIME)
                return 8;

            // Bools are 1 byte //
            if (t == CellAffinity.BOOL)
                return 1;

            // Variable length with a predefined size //
            if (context.LITERAL_INT() != null)
            {
                int size = int.Parse(context.LITERAL_INT().GetText());
                if (IsLocal)
                    return Math.Min(size, Schema.MAX_VARIABLE_SIZE);
                else
                    return Math.Min(size, Cell.MAX_STRING_LENGTH);
            }
            
            // Default the sizes //
            if (t == CellAffinity.STRING)
                return Schema.DEFAULT_STRING_SIZE;
            return Schema.DEFAULT_BLOB_SIZE;


        }

        // Heap allocations //
        public static void AllocateMemory(Workspace Home, MemoryStruct Heap, ExpressionVisitor Evaluator, HScriptParser.DeclareScalarContext context)
        {

            string name = context.IDENTIFIER().GetText();
            CellAffinity type = GetAffinity(context.type());
            Cell value = (context.ASSIGN() != null) ? Evaluator.ToNode(context.expression()).Evaluate() : new Cell(type);

            Heap.Scalars.Reallocate(name, value);

        }

        public static void AllocateMemory(Workspace Home, MemoryStruct Heap, ExpressionVisitor Evaluator, HScriptParser.DeclareMatrix1DContext context)
        {

            string name = context.IDENTIFIER().GetText();
            CellAffinity type = GetAffinity(context.type());
            int rows = (int)Evaluator.ToNode(context.expression()).Evaluate().valueINT;
            CellMatrix mat = new CellVector(rows, type);
            Heap.Arrays.Reallocate(name, mat);

        }

        public static void AllocateMemory(Workspace Home, MemoryStruct Heap, ExpressionVisitor Evaluator, HScriptParser.DeclareMatrix2DContext context)
        {

            string name = context.IDENTIFIER().GetText();
            CellAffinity type = GetAffinity(context.type());
            int rows = (int)Evaluator.ToNode(context.expression()[0]).Evaluate().valueINT;
            int cols = (int)Evaluator.ToNode(context.expression()[1]).Evaluate().valueINT;
            CellMatrix mat = new CellMatrix(rows, cols, type);
            Heap.Arrays.Reallocate(name, mat);

        }

        public static void AllocateMemory(Workspace Home, MemoryStruct Heap, ExpressionVisitor Evaluator, HScriptParser.DeclareMatrixLiteralContext context)
        {

            string name = context.IDENTIFIER().GetText();
            CellAffinity type = GetAffinity(context.type());
            MatrixVisitor vis = new MatrixVisitor(Home, Heap, Evaluator);
            CellMatrix mat = vis.ToMatrix(context.matrix_expression()).Evaluate();
            Heap.Arrays.Reallocate(name, mat);

        }

        public static void AllocateMemory(Workspace Home, MemoryStruct Heap, ExpressionVisitor Evaluator, HScriptParser.Declare_genericContext context)
        {

            if (context is HScriptParser.DeclareScalarContext)
            {
                VisitorHelper.AllocateMemory(Home, Heap, Evaluator, context as HScriptParser.DeclareScalarContext);
                return;
            }

            if (context is HScriptParser.DeclareMatrix1DContext)
            {
                VisitorHelper.AllocateMemory(Home, Heap, Evaluator, context as HScriptParser.DeclareMatrix1DContext);
                return;
            }

            if (context is HScriptParser.DeclareMatrix2DContext)
            {
                VisitorHelper.AllocateMemory(Home, Heap, Evaluator, context as HScriptParser.DeclareMatrix2DContext);
                return;
            }

            if (context is HScriptParser.DeclareMatrixLiteralContext)
            {
                VisitorHelper.AllocateMemory(Home, Heap, Evaluator, context as HScriptParser.DeclareMatrixLiteralContext);
                return;
            }

        }

        public static void AllocateMemory(Workspace Home, MemoryStruct Heap, ExpressionVisitor Evaluator, HScriptParser.Crudam_declare_manyContext context)
        {

            foreach (HScriptParser.Declare_genericContext ctx in context.declare_generic())
            {
                AllocateMemory(Home, Heap, Evaluator, ctx);
            }

        }

        // Where clause //
        public static Predicate GetWhere(ExpressionVisitor Evaluator, HScriptParser.Where_clauseContext context)
        {
            if (context == null)
                return Predicate.TrueForAll;
            return Evaluator.ToPredicate(context.expression());
        }

        // Expression or wildcard handelers //
        private static void AppendSet(ExpressionVisitor Evaluator, FNodeSet Fields, HScriptParser.EOW_expressionContext context)
        {

            FNode node = Evaluator.ToNode(context.expression_alias().expression());
            string alias = ("F" + Fields.Count.ToString());
            if (node.Name != null)
                alias = node.Name;
            if (context.expression_alias().K_AS() != null)
                alias = context.expression_alias().IDENTIFIER().GetText();
            Fields.Add(alias, node);

        }

        private static void AppendSet(ExpressionVisitor Evaluator, FNodeSet Fields, HScriptParser.EOW_local_starContext context)
        {

            string suffix =
                (context.K_AS() == null)
                ? null
                : context.IDENTIFIER().GetText();

            for (int i = 0; i < Evaluator.LocalHeap.Scalars.Count; i++)
            {
                string alias =
                    (suffix == null)
                    ? Evaluator.LocalHeap.Scalars.Name(i)
                    : suffix + Evaluator.LocalHeap.Scalars.Name(i);
                FNode node = new FNodeHeapRef(null, Evaluator.LocalHeap, i);
                Fields.Add(alias, node);
            }

        }

        private static void AppendSet(ExpressionVisitor Evaluator, FNodeSet Fields, HScriptParser.EOW_global_starContext context)
        {

            string suffix =
                (context.K_AS() == null)
                ? null
                : context.IDENTIFIER().GetText();

            for (int i = 0; i < Evaluator.GlobalHeap.Scalars.Count; i++)
            {
                string alias =
                    (suffix == null)
                    ? Evaluator.LocalHeap.Scalars.Name(i)
                    : suffix + Evaluator.LocalHeap.Scalars.Name(i);
                FNode node = new FNodeHeapRef(null, Evaluator.GlobalHeap, i);
                Fields.Add(alias, node);
            }

        }

        private static void AppendSet(ExpressionVisitor Evaluator, FNodeSet Fields, HScriptParser.EOW_table_starContext context)
        {

            string alias = context.IDENTIFIER()[0].GetText();
            if (!Evaluator.Columns.ContainsKey(alias))
                throw new Exception(string.Format("Alias '{0}' does not exist", alias));

            FNodeSet nodes = new FNodeSet(Evaluator.Columns[alias]);
            nodes.AssignRegister(Evaluator.Registers[alias]);

            string suffix = (context.K_AS() == null) ? null : context.IDENTIFIER()[1].GetText();

            for (int i = 0; i < nodes.Count; i++)
            {
                Fields.Add((suffix == null) ? nodes.Alias(i) : suffix + nodes.Alias(i), nodes[i]);
            }

        }

        private static void AppendSet(ExpressionVisitor Evaluator, FNodeSet Fields, HScriptParser.EOW_tables_starContext context)
        {

            if (Evaluator.Columns.Count == 0)
                return; // no need to toss an exception

            string alias = Evaluator.Columns.Keys.First();
            FNodeSet nodes = new FNodeSet(Evaluator.Columns[alias]);
            nodes.AssignRegister(Evaluator.Registers[alias]);

            string suffix = (context.K_AS() == null) ? null : context.IDENTIFIER().GetText();

            for (int i = 0; i < nodes.Count; i++)
            {
                Fields.Add((suffix == null) ? nodes.Alias(i) : suffix + nodes.Alias(i), nodes[i]);
            }

        }

        private static void AppendSet(ExpressionVisitor Evaluator, FNodeSet Fields, HScriptParser.Expression_or_wildcardContext context)
        {

            if (context is HScriptParser.EOW_expressionContext)
            {
                AppendSet(Evaluator, Fields, context as HScriptParser.EOW_expressionContext);
                return;
            }

            if (context is HScriptParser.EOW_local_starContext)
            {
                AppendSet(Evaluator, Fields, context as HScriptParser.EOW_local_starContext);
                return;
            }

            if (context is HScriptParser.EOW_global_starContext)
            {
                AppendSet(Evaluator, Fields, context as HScriptParser.EOW_global_starContext);
                return;
            }

            if (context is HScriptParser.EOW_table_starContext)
            {
                AppendSet(Evaluator, Fields, context as HScriptParser.EOW_table_starContext);
                return;
            }

            if (context is HScriptParser.EOW_tables_starContext)
            {
                AppendSet(Evaluator, Fields, context as HScriptParser.EOW_tables_starContext);
                return;
            }

        }

        public static FNodeSet GetReturnStatement(ExpressionVisitor Evaluator, HScriptParser.Expression_or_wildcard_setContext context)
        {

            FNodeSet nodes = new FNodeSet();

            foreach (HScriptParser.Expression_or_wildcardContext ctx in context.expression_or_wildcard())
                AppendSet(Evaluator, nodes, ctx);

            return nodes;

        }

        // Lambdas //
        public static Lambda RenderLambda(Workspace Home, HScriptParser.LambdaGenericContext context)
        {

            // Get the name //
            string name = context.IDENTIFIER()[0].GetText();

            // Get all the pointers //
            List<string> pointers = new List<string>();
            for (int i = 1; i < context.IDENTIFIER().Count; i++)
            {
                string var_name = context.IDENTIFIER()[i].GetText();
                pointers.Add(var_name);
            }

            // Get the expression //
            PointerExpressionVisitor pev = new PointerExpressionVisitor(pointers, Home);

            // Build the node //
            FNode node = pev.ToNode(context.expression());

            // Build the lambda //
            Lambda mu = new Lambda(name, node, pointers);

            return mu;

        }

        public static Lambda RenderLambda(Workspace Home, HScriptParser.LambdaGradientContext context)
        {

            string NewLambdaName = context.IDENTIFIER()[0].GetText();
            string BaseLambdaName = context.IDENTIFIER()[1].GetText();
            string GradientVariable = context.IDENTIFIER()[2].GetText();

            Lambda mu = Home.Lambdas[BaseLambdaName];
            Lambda mu_prime = mu.Gradient(NewLambdaName, GradientVariable);

            return mu_prime;

        }

        public static Lambda RenderLambda(Workspace Home, HScriptParser.Lambda_unitContext context)
        {

            if (context is HScriptParser.LambdaGenericContext)
                return VisitorHelper.RenderLambda(Home, context as HScriptParser.LambdaGenericContext);
            else
                return VisitorHelper.RenderLambda(Home, context as HScriptParser.LambdaGradientContext);

        }

        // Merge support //
        public static MergeMethod GetMergeMethod(HScriptParser.Merge_typeContext context)
        {

            MergeMethod meth = MergeMethod.Inner;
            if (context == null)
                return meth;

            if (context.K_LEFT() != null && context.K_ANTI() != null)
                meth = MergeMethod.AntiLeft;
            else if (context.K_LEFT() != null)
                meth = MergeMethod.Left;
            else if (context.K_RIGHT() != null && context.K_ANTI() != null)
                meth = MergeMethod.AntiRight;
            else if (context.K_RIGHT() != null)
                meth = MergeMethod.Right;
            else if (context.K_ANTI() != null)
                meth = MergeMethod.AntiInner;
            else if (context.K_FULL() != null)
                meth = MergeMethod.Full;
            else if (context.K_INNER() != null)
                meth = MergeMethod.Inner;

            return meth;

        }

        // Parameters //
        public static HParameterSet GetHParameter(Workspace Home, HScriptParser.Hparameter_setContext context)
        {

            // Create a set //
            HParameterSet parmset = new HParameterSet();

            // Step one: cycle through all parameters and look for any tables //
            DataSet first_data = null;
            List<HScriptParser.HparameterContext> param_context = new List<HScriptParser.HparameterContext>();
            foreach (HScriptParser.HparameterContext ctx in context.hparameter())
            {

                if (ctx.full_table_name() != null)
                {
                    DataSet d = VisitorHelper.GetData(Home, ctx.full_table_name());
                    if (first_data == null)
                        first_data = d;
                    parmset.Add(ctx.SCALAR().GetText(), d);
                }
                else
                    param_context.Add(ctx);

            }

            ExpressionVisitor exp_vis =
                first_data != null
                ? new ExpressionVisitor(null, Home, first_data.Name, first_data.Columns, new StaticRegister(null)) 
                : new ExpressionVisitor(null, Home);

            MatrixVisitor mat_vis = new MatrixVisitor(Home, null, exp_vis);

            foreach (HScriptParser.HparameterContext ctx in param_context)
            {

                string name = ctx.SCALAR().GetText();
                if (ctx.expression() != null)
                    parmset.Add(name, exp_vis.ToNode(ctx.expression()));
                else if (ctx.expression_alias_list() != null)
                    parmset.Add(name, exp_vis.ToNodes(ctx.expression_alias_list()));
                else if (ctx.lambda_unit() != null)
                    parmset.Add(name, VisitorHelper.RenderLambda(Home, ctx.lambda_unit()));
                else if (ctx.matrix_expression() != null)
                    parmset.Add(name, mat_vis.ToMatrix(ctx.matrix_expression()));
                else if (ctx.K_OUT() != null)
                    parmset.Add(name, Home.GlobalHeap.Scalars, ctx.IDENTIFIER().GetText());

            }

            return parmset;

        }

        // Partitions //
        public static int GetPartitions(ExpressionVisitor Evaluator, HScriptParser.PartitionsContext context)
        {

            // Null then assume 1 partition //
            if (context == null)
                return 1;

            // If the expression is null, then max out cores //
            if (context.expression() == null)
                return Environment.ProcessorCount;

            // Otherwise, get the value //
            int cnt = (int)Evaluator.ToNode(context.expression()).Evaluate().valueINT;

            // Bound it //
            cnt = Math.Min(cnt, Environment.ProcessorCount * 2);
            cnt = Math.Max(cnt, 1);

            return cnt;

        }

    }

}
