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
using Equus.Fjord;
using Equus.Gidran;
using Equus.Mustang;
using Equus.Thoroughbred.ARizenTalent;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;


namespace Equus.HScript
{

    public static class CommandCompiler
    {

        // Reads //
        public static StagedReadName RenderStagedReadPlan(Workspace Home, HScriptParser.Crudam_readContext context)
        {

            // Get the data source //
            DataSet data = VisitorHelper.GetData(Home, context.full_table_name());
            string alias =
                (context.K_AS() != null)
                ? context.IDENTIFIER().GetText()
                : data.Name;

            // Create a local heap to work off of //
            MemoryStruct local_heap = new MemoryStruct(true);

            // Create a record register //
            StreamRegister memory = new StreamRegister(null);

            // Create expression visitor //
            ExpressionVisitor exp_vis = new ExpressionVisitor(local_heap, Home, alias, data.Columns, memory);

            // Where clause //
            Predicate where = VisitorHelper.GetWhere(exp_vis, context.where_clause());

            // Create a reader //
            RecordReader reader = data.OpenReader(where);

            // Attach the reader to the register //
            memory.BaseStream = reader;

            // Create the action visitor //
            ActionVisitor act_vis = new ActionVisitor(Home, local_heap, exp_vis);

            // Get the declarations //
            if (context.crudam_declare_many() != null)
            {
                VisitorHelper.AllocateMemory(Home, local_heap, exp_vis, context.crudam_declare_many());
            }

            // Get the initial actionset //
            TNode pre_run =
                (context.init_action() != null)
                ? act_vis.ToNode(context.init_action().query_action())
                : new TNodeNothing(null);

            // Get the main actionset //
            TNode run = act_vis.ToNode(context.main_action().query_action());

            // Get the final actionset //
            TNode post_run =
                (context.final_action() != null)
                ? act_vis.ToNode(context.final_action().query_action())
                : new TNodeNothing(null);

            return new StagedReadName(reader, pre_run, run, post_run);

        }

        public static FastReadPlan RenderFastReadPlan(Workspace Home, HScriptParser.Crudam_read_fastContext context)
        {

            // Get the data source //
            DataSet data = VisitorHelper.GetData(Home, context.full_table_name());
            string alias =
                (context.K_AS() != null)
                ? context.IDENTIFIER().GetText()
                : data.Name;

            // Create a record register //
            StreamRegister memory = new StreamRegister(null);

            // Create expression visitor //
            ExpressionVisitor exp_vis = new ExpressionVisitor(new MemoryStruct(true), Home, alias, data.Columns, memory);

            // Where clause //
            Predicate where = VisitorHelper.GetWhere(exp_vis, context.where_clause());

            // Create a reader //
            RecordReader reader = data.OpenReader(where);

            // Attach the reader to the register //
            memory.BaseStream = reader;

            // Get the fields being returned //
            FNodeSet nodes = VisitorHelper.GetReturnStatement(exp_vis, context.return_action().expression_or_wildcard_set());

            // Get the output cursor from the return statement //
            RecordWriter writer = VisitorHelper.GetWriter(Home, nodes.Columns, context.return_action());

            return new FastReadPlan(data, where, nodes, writer);

        }

        public static ConcurrentReadPlan RenderMapReducePlan(Workspace Home, HScriptParser.Crudam_read_maprContext context)
        {

            DataSet data = VisitorHelper.GetData(Home, context.full_table_name());
            int threads = VisitorHelper.GetPartitions(new ExpressionVisitor(null, Home), context.partitions());
            ReadMapNodeFactory factory = new ReadMapNodeFactory(Home, context);
            ReadReduceNode reducer = new ReadReduceNode();
            MRJob<ReadMapNode> job = new MRJob<ReadMapNode>(data, reducer, factory, threads);

            return new ConcurrentReadPlan(job);

        }

        internal static ReadMapNode RenderMapNode(Workspace Home, int PartitionID, HScriptParser.Crudam_read_maprContext context)
        {

            // Get the data source //
            DataSet data = VisitorHelper.GetData(Home, context.full_table_name());
            string alias =
                (context.K_AS() != null)
                ? context.IDENTIFIER().GetText()
                : data.Name;

            // Create a local heap to work off of //
            MemoryStruct local_heap = new MemoryStruct(true);

            // Create a record register //
            StaticRegister memory = new StaticRegister(null);

            // Create expression visitor //
            ExpressionVisitor exp_vis = new ExpressionVisitor(local_heap, Home, alias, data.Columns, memory);

            // Where clause //
            Predicate where = VisitorHelper.GetWhere(exp_vis, context.where_clause());

            // Create a reader //
            RecordReader reader = data.OpenReader(where);

            // Get the declarations //
            if (context.crudam_declare_many() != null)
            {
                VisitorHelper.AllocateMemory(Home, local_heap, exp_vis, context.crudam_declare_many());
            }

            // Get the map actions //
            ActionVisitor act_vis = new ActionVisitor(Home, local_heap, exp_vis);
            act_vis.IsAsync = true;
            TNode map = act_vis.ToNode(context.map_action().query_action());

            // Get the reduce actions //
            act_vis = new ActionVisitor(Home, local_heap, exp_vis);
            act_vis.IsAsync = false;
            TNode red = act_vis.ToNode(context.reduce_action().query_action());

            ReadMapNode node = new ReadMapNode(PartitionID, map, red, memory, where);

            return node;

        }

        // Aggreagtes //
        public static AggregatePlan RenderAggregatePlan(Workspace Home, HScriptParser.Crudam_aggregateContext context)
        {

            // Get the data source //
            DataSet data = VisitorHelper.GetData(Home, context.full_table_name());
            string alias =
                (context.K_AS() != null)
                ? context.IDENTIFIER().GetText()
                : data.Name;

            // Create a register //
            StaticRegister memory = new StaticRegister(null);

            // Create expression visitor //
            ExpressionVisitor exp_vis = new ExpressionVisitor(null, Home, alias, data.Columns, memory);

            // Get where //
            Predicate where = VisitorHelper.GetWhere(exp_vis, context.where_clause());

            // Get the reader //
            //RecordReader reader = data.OpenReader(where);

            // Get the keys //
            FNodeSet keys =
                (context.K_BY() != null)
                ? exp_vis.ToNodes(context.expression_alias_list())
                : new FNodeSet();

            // Get the reducers //
            AggregateSet values =
                (context.K_OVER() != null)
                ? exp_vis.ToReducers(context.beta_reduction_list())
                : new AggregateSet();

            // Create a second register for the return memory //
            StaticRegister return_memory = new StaticRegister(null);

            // Need to build a visitor off of the aggregator schema //
            ExpressionVisitor agg_vis = new ExpressionVisitor(null, Home, "agg", AggregatePlan.GetInterimSchema(keys, values), return_memory);

            // Get the output //
            FNodeSet return_vars = VisitorHelper.GetReturnStatement(agg_vis, context.return_action().expression_or_wildcard_set());

            // Get the output cursor //
            RecordWriter out_put_writter = VisitorHelper.GetWriter(Home, return_vars.Columns, context.return_action());

            return new AggregatePlan(out_put_writter, data, where, keys, values, return_vars, memory, return_memory, data.Directory);
           
        }

        public static PartitionedAggregatePlan RenderPartitionedAggregatePlan(Workspace Home, HScriptParser.Crudam_aggregateContext context)
        {

            // Get the data source //
            DataSet data = VisitorHelper.GetData(Home, context.full_table_name());
            string alias =
                (context.K_AS() != null)
                ? context.IDENTIFIER().GetText()
                : data.Name;

            // Create a register //
            StaticRegister memory = new StaticRegister(null);

            // Create expression visitor //
            ExpressionVisitor exp_vis = new ExpressionVisitor(null, Home, alias, data.Columns, memory);

            // Get where //
            Predicate where = VisitorHelper.GetWhere(exp_vis, context.where_clause());

            // Get the reader //
            //RecordReader reader = data.OpenReader(where);

            // Get the keys //
            FNodeSet keys =
                (context.K_BY() != null)
                ? exp_vis.ToNodes(context.expression_alias_list())
                : new FNodeSet();

            // Get the reducers //
            AggregateSet values =
                (context.K_OVER() != null)
                ? exp_vis.ToReducers(context.beta_reduction_list())
                : new AggregateSet();

            // Create a second register for the return memory //
            StaticRegister return_memory = new StaticRegister(null);

            // Need to build a visitor off of the aggregator schema //
            ExpressionVisitor agg_vis = new ExpressionVisitor(null, Home, "agg", AggregatePlan.GetInterimSchema(keys, values), return_memory);

            // Get the output //
            FNodeSet return_vars = VisitorHelper.GetReturnStatement(agg_vis, context.return_action().expression_or_wildcard_set());

            // Get the output cursor //
            RecordWriter out_put_writter = VisitorHelper.GetWriter(Home, return_vars.Columns, context.return_action());

            // Get the partitioner //
            int Partitions = VisitorHelper.GetPartitions(exp_vis, context.partitions());

            return new PartitionedAggregatePlan(out_put_writter, data, where, keys, values, return_vars, Home.TempSpace, Partitions);

        }

        // Merge //
        public static MergePlan RenderMergePlan(Workspace Home, HScriptParser.Crudam_mergeContext context)
        {

            // Get the data sources //
            DataSet data1 = VisitorHelper.GetData(Home, context.merge_source()[0].full_table_name());
            DataSet data2 = VisitorHelper.GetData(Home, context.merge_source()[1].full_table_name());

            // Get the aliases //
            string alias1 = (context.merge_source()[0].IDENTIFIER() ?? context.merge_source()[0].full_table_name().table_name().IDENTIFIER()).GetText();
            string alias2 = (context.merge_source()[1].IDENTIFIER() ?? context.merge_source()[1].full_table_name().table_name().IDENTIFIER()).GetText();

            // Build the registers; the join functions only use static registers //
            StaticRegister mem1 = new StaticRegister(null);
            StaticRegister mem2 = new StaticRegister(null);

            // Create our expression builder //
            ExpressionVisitor exp_vis = new ExpressionVisitor(null, Home);
            exp_vis.AddSchema(alias1, data1.Columns, mem1);
            exp_vis.AddSchema(alias2, data2.Columns, mem2);

            // Get the equality keys //
            Key eq1 = new Key();
            Key eq2 = new Key();
            foreach (HScriptParser.Merge_equi_predicateContext ctx in context.merge_equi_predicate())
            {

                string a1 = ctx.table_variable()[0].IDENTIFIER()[0].GetText();
                string a2 = ctx.table_variable()[1].IDENTIFIER()[0].GetText();
                string c1 = ctx.table_variable()[0].IDENTIFIER()[1].GetText();
                string c2 = ctx.table_variable()[1].IDENTIFIER()[1].GetText();
                int idx1 = -1;
                int idx2 = -1;

                if (a1 == alias1 && a2 == alias2)
                {

                    // Look up indicides //
                    idx1 = data1.Columns.ColumnIndex(c1);
                    idx2 = data2.Columns.ColumnIndex(c2);

                    // Check for invalid keys //
                    if (idx1 == -1) 
                        throw new Exception(string.Format("Column '{0}' does not exist in '{1}'", c1, idx1));
                    if (idx2 == -1)
                        throw new Exception(string.Format("Column '{0}' does not exist in '{1}'", c2, idx2));

                }
                else if (a1 == alias2 && a2 == alias1)
                {

                    // Look up indicides //
                    idx1 = data1.Columns.ColumnIndex(c2);
                    idx2 = data2.Columns.ColumnIndex(c1);

                    // Check for invalid keys //
                    if (idx1 == -1)
                        throw new Exception(string.Format("Column '{0}' does not exist in '{1}'", c2, idx1));
                    if (idx2 == -1)
                        throw new Exception(string.Format("Column '{0}' does not exist in '{1}'", c1, idx2));

                }
                else
                    throw new Exception("Aliases passed are invalid");

                // add the keys //
                eq1.Add(idx1);
                eq2.Add(idx2);

            }

            // Get the predicate //
            Predicate where = VisitorHelper.GetWhere(exp_vis, context.where_clause());

            // Get the list of expressions //
            FNodeSet nodes = VisitorHelper.GetReturnStatement(exp_vis, context.return_action().expression_or_wildcard_set());

            // Get the output cursor //
            RecordWriter out_data = VisitorHelper.GetWriter(Home, nodes.Columns, context.return_action());

            // Get the join method //
            MergeMethod method = VisitorHelper.GetMergeMethod(context.merge_type());

            // Find the best algorithm //
            MergeAlgorithm alg = MergeAlgorithm.SortMerge;
            if (context.merge_algorithm() != null)
            {
                string suggest_alg = exp_vis.ToNode(context.merge_algorithm().expression()).Evaluate().valueSTRING.ToUpper();
                if (suggest_alg == "NL")
                    alg = MergeAlgorithm.NestedLoop;
                else if (suggest_alg == "SM")
                    alg = MergeAlgorithm.SortMerge;
                else if (suggest_alg == "HT")
                    alg = MergeAlgorithm.HashTable;
            }
            if (eq1.Count == 0)
                alg = MergeAlgorithm.NestedLoop;

            return new MergePlan(method, alg, out_data, nodes, where, data1, data2, eq1, eq2, mem1, mem2);

        }

        // Delete //
        public static DeletePlan RenderDeletePlan(Workspace Home, HScriptParser.Crudam_deleteContext context)
        {

            // Get the data source //
            DataSet data = VisitorHelper.GetData(Home, context.full_table_name());

            // Create expression visitor //
            ExpressionVisitor exp_vis = new ExpressionVisitor(null, Home, data.Name, data.Columns, null);

            // Get where //
            Predicate where = VisitorHelper.GetWhere(exp_vis, context.where_clause());

            return new DeletePlan(data, where);

        }

        // Update //
        public static UpdatePlan RenderUpdatePlan(Workspace Home, HScriptParser.Crudam_updateContext context)
        {

            // Get the data source //
            DataSet data = VisitorHelper.GetData(Home, context.full_table_name());

            // Create expression visitor //
            ExpressionVisitor exp_vis = new ExpressionVisitor(null, Home, data.Name, data.Columns, null);

            // Get where //
            Predicate where = VisitorHelper.GetWhere(exp_vis, context.where_clause());

            // Create the key and fnodeset //
            Key keys = new Key();
            FNodeSet expressions = new FNodeSet();
            foreach (HScriptParser.Update_unitContext ctx in context.update_unit())
            {
                keys.Add(data.Columns.ColumnIndex(ctx.IDENTIFIER().GetText()));
                expressions.Add(exp_vis.ToNode(ctx.expression()));
            }

            return new UpdatePlan(data, keys, expressions, where);

        }

        // Create //
        public static CreateTablePlan RenderCreatePlan(Workspace Home, HScriptParser.Crudam_create_tableContext context)
        {

            // Build visitor //
            ExpressionVisitor exp_vis = new ExpressionVisitor(null, Home);
            
            // Build the schema //
            Schema columns = new Schema();
            foreach (HScriptParser.Create_table_unitContext ctx in context.create_table_unit())
            {
                columns.Add(
                    ctx.IDENTIFIER().GetText(),
                    VisitorHelper.GetAffinity(ctx.type()),
                    (ctx.expression() == null) ? true : exp_vis.ToNode(ctx.expression()).Evaluate().valueBOOL,
                    VisitorHelper.GetSize(ctx.type(), true));
            }

            string name = context.full_table_name().table_name().GetText();
            string db = context.full_table_name().database_name().GetText();
            string db_path = Home.Connections[db];
            long chunk_size =
                (context.create_table_size() == null)
                ? RecordSet.EstimateMaxRecords(columns)
                : exp_vis.ToNode(context.create_table_size().expression()).Evaluate().valueINT;

            return new CreateTablePlan(db_path, name, columns, (int)chunk_size);

        }

        public static CreateChunkPlan RenderCreateChunk(Workspace Home, HScriptParser.Crudam_declare_tableContext context)
        {

            ExpressionVisitor exp_vis = new ExpressionVisitor(null, Home);

            // Build the schema //
            Schema columns = new Schema();
            foreach (HScriptParser.Create_table_unitContext ctx in context.create_table_unit())
            {
                columns.Add(
                    ctx.IDENTIFIER().GetText(),
                    VisitorHelper.GetAffinity(ctx.type()),
                    (ctx.expression() == null) ? true : exp_vis.ToNode(ctx.expression()).Evaluate().valueBOOL,
                    VisitorHelper.GetSize(ctx.type(), true));
            }

            string name = context.IDENTIFIER().GetText();

            return new CreateChunkPlan(name, columns, Home);

        }

        public static DeclarePlan RenderDeclarePlan(Workspace Home, HScriptParser.Crudam_declare_manyContext context)
        {

            ExpressionVisitor eval = new ExpressionVisitor(null, Home);
            DeclarePlan plan = new DeclarePlan(Home.GlobalHeap);
            foreach (HScriptParser.Declare_genericContext ctx in context.declare_generic())
            {

                if (ctx is HScriptParser.DeclareScalarContext)
                    plan.Add(RenderDeclareNode(Home, eval, ctx as HScriptParser.DeclareScalarContext));
                else if (ctx is HScriptParser.DeclareMatrix1DContext)
                    plan.Add(RenderDeclareNode(Home, eval, ctx as HScriptParser.DeclareMatrix1DContext));
                else if (ctx is HScriptParser.DeclareMatrix2DContext)
                    plan.Add(RenderDeclareNode(Home, eval, ctx as HScriptParser.DeclareMatrix2DContext));
                else if (ctx is HScriptParser.DeclareMatrixLiteralContext)
                    plan.Add(RenderDeclareNode(Home, eval, ctx as HScriptParser.DeclareMatrixLiteralContext));

            }
            return plan;

        }

        public static LambdaPlan RenderLambdaPlan(Workspace Home, HScriptParser.LambdaGenericContext context)
        {

            // Get the name //
            string name = context.IDENTIFIER()[0].GetText();

            // Get all the pointers //
            List<string> pointers = new List<string>();
            for(int i = 1; i < context.IDENTIFIER().Count; i++)
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

            return new LambdaPlan(Home.Lambdas, name, mu);

        }

        public static LambdaPlan RenderLambdaPlan(Workspace Home, HScriptParser.LambdaGradientContext context)
        {

            string NewLambdaName = context.IDENTIFIER()[0].GetText();
            string BaseLambdaName = context.IDENTIFIER()[1].GetText();
            string GradientVariable = context.IDENTIFIER()[2].GetText();

            Lambda mu = Home.Lambdas[BaseLambdaName];
            Lambda mu_prime = mu.Gradient(NewLambdaName, GradientVariable);

            return new LambdaPlan(Home.Lambdas, NewLambdaName, mu_prime);

        }

        public static Lambda RenderLambda(Workspace Home, HScriptParser.Crudam_lambdaContext context)
        {

            if (context.lambda_unit() is HScriptParser.LambdaGenericContext)
                return VisitorHelper.RenderLambda(Home, context.lambda_unit() as HScriptParser.LambdaGenericContext);
            else
                return VisitorHelper.RenderLambda(Home, context.lambda_unit() as HScriptParser.LambdaGradientContext);

        }

        internal static DeclareScalarNode RenderDeclareNode(Workspace Home, ExpressionVisitor Evaluator, HScriptParser.DeclareScalarContext context)
        {

            string name = context.IDENTIFIER().GetText();
            FNode node = Evaluator.ToNode(context.expression());
            return new DeclareScalarNode(Home.GlobalHeap, name, node);

        }

        internal static DeclareMatrixNode RenderDeclareNode(Workspace Home, ExpressionVisitor Evaluator, HScriptParser.DeclareMatrixLiteralContext context)
        {

            string name = context.IDENTIFIER().GetText();
            MatrixVisitor MEvaluator = new MatrixVisitor(Home, Home.GlobalHeap, Evaluator);
            MNode node = MEvaluator.ToMatrix(context.matrix_expression());
            return new DeclareMatrixNode(Home.GlobalHeap, name, node);

        }

        internal static DeclareMatrixNode RenderDeclareNode(Workspace Home, ExpressionVisitor Evaluator, HScriptParser.DeclareMatrix1DContext context)
        {
            string name = context.IDENTIFIER().GetText();
            int row = (int)Evaluator.ToNode(context.expression()).Evaluate().valueINT;
            CellAffinity affinity = VisitorHelper.GetAffinity(context.type());
            MNodeLiteral m = new MNodeLiteral(null, new Gidran.CellMatrix(row, 1, affinity));
            return new DeclareMatrixNode(Home.GlobalHeap, name, m);
        }

        internal static DeclareMatrixNode RenderDeclareNode(Workspace Home, ExpressionVisitor Evaluator, HScriptParser.DeclareMatrix2DContext context)
        {
            string name = context.IDENTIFIER().GetText();
            int row = (int)Evaluator.ToNode(context.expression()[0]).Evaluate().valueINT;
            int col = (int)Evaluator.ToNode(context.expression()[1]).Evaluate().valueINT;
            CellAffinity affinity = VisitorHelper.GetAffinity(context.type());
            MNodeLiteral m = new MNodeLiteral(null, new Gidran.CellMatrix(row, 1, affinity));
            return new DeclareMatrixNode(Home.GlobalHeap, name, m);
        }

        // Action Node //
        public static ActionPlan RenderActionPlan(Workspace Home, HScriptParser.Query_actionContext context)
        {

            ExpressionVisitor exp_vis = new ExpressionVisitor(null, Home);
            ActionVisitor act_vis = new ActionVisitor(Home, null, exp_vis);
            TNode act = act_vis.ToNode(context);

            return new ActionPlan(act);

        }



    }

}
