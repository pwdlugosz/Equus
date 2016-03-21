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
using Equus.Clydesdale;
using Equus.HScript;
using Equus.Fjord;
using Equus.Gidran;
using Equus.HScript.Parameters;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Equus.HScript
{


    public sealed class ActionVisitor : HScriptParserBaseVisitor<TNode>
    {

        /*
         * This will house open streams so that the return statement doesnt open a new
         * stream for identical tables every time a new return statement is compiled form 
         * the same visitor factory.
         * 
         */
        private Dictionary<string, RecordWriter> _OpenStreams;
        
        public ActionVisitor(Workspace UseEnviro, MemoryStruct UseLocalHeap, ExpressionVisitor Expressor)
            : base()
        {
            this.Evaluator = Expressor;
            this.Home = UseEnviro;
            this.LocalHeap =
                (UseLocalHeap == null)
                ? new MemoryStruct(true)
                : UseLocalHeap;
            this._OpenStreams = new Dictionary<string, RecordWriter>(StringComparer.OrdinalIgnoreCase);
            this.MatrixEvaluator = new MatrixVisitor(this.Home, this.LocalHeap, this.Evaluator);
            this.IsAsync = false;
        }

        public Workspace Home
        {
            get;
            private set;
        }

        public MemoryStruct LocalHeap
        {
            get;
            private set;
        }

        public TNode MasterNode
        {
            get;
            private set;
        }

        public ExpressionVisitor Evaluator
        {
            get;
            private set;
        }

        public MatrixVisitor MatrixEvaluator
        {
            get;
            private set;
        }

        public bool IsAsync
        {
            get;
            set;
        }

        // Helpers //
        /// <summary>
        /// Determines the level of a variable
        /// </summary>
        /// <param name="context">Abstract variable context</param>
        /// <returns>1 == global, 2 = local, 3 = table, -1 = error</returns>
        internal int VariableType(HScriptParser.VariableContext context)
        {

            if (context is HScriptParser.VariableGlobalContext)
                return 1;
            else if (context is HScriptParser.VariableLocalContext)
                return 2;
            else if (context is HScriptParser.VariableTableContext)
                return 3;
            else if (context is HScriptParser.VariableNakedContext)
            {
                string name = (context as HScriptParser.VariableNakedContext).IDENTIFIER().GetText();
                if (this.LocalHeap.Scalars.Exists(name))
                    return 2;
                else if (this.Home.GlobalHeap.Scalars.Exists(name))
                    return 1;
            }

            return -1;

        }

        /// <summary>
        /// Extracts the variable name from the variable context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal string VariableName(HScriptParser.VariableContext context)
        {

            if (context is HScriptParser.VariableGlobalContext)
                return (context as HScriptParser.VariableGlobalContext).global_variable().IDENTIFIER().GetText();
            else if (context is HScriptParser.VariableLocalContext)
                return (context as HScriptParser.VariableLocalContext).local_variable().IDENTIFIER().GetText();
            else if (context is HScriptParser.VariableTableContext)
                return (context as HScriptParser.VariableTableContext).table_variable().IDENTIFIER()[1].GetText();
            else if (context is HScriptParser.VariableNakedContext)
                return (context as HScriptParser.VariableNakedContext).IDENTIFIER().GetText();

            throw new Exception();

        }

        // Actions //
        public override TNode VisitActAssign(HScriptParser.ActAssignContext context)
        {

            FNode node = this.Evaluator.ToNode(context.expression());
            string var_name = this.VariableName(context.variable());
            int heap_type = this.VariableType(context.variable());
            if (heap_type != 1 && heap_type != 2)
                throw new Exception();
            MemoryStruct h = 
                (heap_type == 1) 
                ? this.Home.GlobalHeap 
                : this.LocalHeap;
            TNode t = new TNodeAssignScalar(this.MasterNode, h, h.Scalars.GetPointer(var_name), node, 0);
            this.MasterNode = t;
            return t;

        }

        public override TNode VisitActInc(HScriptParser.ActIncContext context)
        {

            FNode node = this.Evaluator.ToNode(context.expression());
            string var_name = this.VariableName(context.variable());
            int heap_type = this.VariableType(context.variable());
            if (heap_type != 1 && heap_type != 2)
                throw new Exception();
            MemoryStruct h =
               (heap_type == 1)
               ? this.Home.GlobalHeap
               : this.LocalHeap;
            TNode t = new TNodeAssignScalar(this.MasterNode, h, h.Scalars.GetPointer(var_name), node, 1);
            this.MasterNode = t;
            return t;

        }

        public override TNode VisitActDec(HScriptParser.ActDecContext context)
        {

            FNode node = this.Evaluator.ToNode(context.expression());
            string var_name = this.VariableName(context.variable());
            int heap_type = this.VariableType(context.variable());
            if (heap_type != 1 && heap_type != 2)
                throw new Exception();
            MemoryStruct h =
               (heap_type == 1)
               ? this.Home.GlobalHeap
               : this.LocalHeap;
            TNode t = new TNodeAssignScalar(this.MasterNode, h, h.Scalars.GetPointer(var_name), node, 2);
            this.MasterNode = t;
            return t;

        }

        public override TNode VisitActAutoInc(HScriptParser.ActAutoIncContext context)
        {

            string var_name = this.VariableName(context.variable());
            int heap_type = this.VariableType(context.variable());
            if (heap_type != 1 && heap_type != 2)
                throw new Exception();
            MemoryStruct h =
               (heap_type == 1)
               ? this.Home.GlobalHeap
               : this.LocalHeap;
            TNode t = new TNodeAssignScalar(this.MasterNode, h, h.Scalars.GetPointer(var_name), null, 3);
            this.MasterNode = t;
            return t;

        }

        public override TNode VisitActAutoDec(HScriptParser.ActAutoDecContext context)
        {

            string var_name = this.VariableName(context.variable());
            int heap_type = this.VariableType(context.variable());
            if (heap_type != 1 && heap_type != 2)
                throw new Exception();
            MemoryStruct h =
               (heap_type == 1)
               ? this.Home.GlobalHeap
               : this.LocalHeap;
            TNode t = new TNodeAssignScalar(this.MasterNode, h, h.Scalars.GetPointer(var_name), null, 4);
            this.MasterNode = t;
            return t;

        }

        public override TNode VisitActReturn(HScriptParser.ActReturnContext context)
        {

            if (this.IsAsync)
                return this.ParseConcurrentReturn(context);
            else
                return this.ParseSequentialReturn(context);

        }

        public override TNode VisitActIf(HScriptParser.ActIfContext context)
        {

            Predicate condition = this.Evaluator.ToPredicate(context.expression());
            TNode if_node = new TNodeIf(this.MasterNode, condition);
            if_node.AddChild(this.Visit(context.query_action()[0]));
            if (context.K_ELSE() != null)
                if_node.AddChild(this.Visit(context.query_action()[1]));

            this.MasterNode = if_node;

            return if_node;

        }

        public override TNode VisitActFor(HScriptParser.ActForContext context)
        {

            // Get the variable name, which is located on one of the heaps //
            string var_name = this.VariableName(context.variable());

            // Determine which heap to pull from //
            int heap_type = this.VariableType(context.variable());
            
            // if the variable doesnt exist, create a variable in the global heap //
            if (heap_type != 1 && heap_type != 2)
            {
                heap_type = 1;
                this.Home.GlobalHeap.Scalars.Reallocate(context.variable().GetText().Split('.').Last(), new Cell(0));
            }

            // Get the correct heap //
            MemoryStruct h = 
                (heap_type == 1) 
                ? this.Home.GlobalHeap 
                : this.LocalHeap;

            // Determine the begin and end values //
            int beg = (int)this.Evaluator.ToNode(context.expression()[0]).Evaluate().INT;
            int end = (int)this.Evaluator.ToNode(context.expression()[1]).Evaluate().INT;

            // Create the parent node //
            TNode t = new TNodeFor(this.MasterNode, beg, end, h, h.Scalars.GetPointer(var_name));

            // Get the sub - action //
            TNode sub_action = this.ToNode(context.query_action());

            // Assign the sub-action to t //
            t.AddChild(sub_action);

            // Assign the master node to the node we just built //
            this.MasterNode = t;

            return t;

        }

        public override TNode VisitActBeginEnd(HScriptParser.ActBeginEndContext context)
        {

            TNode t = new TNodeBeginEnd(this.MasterNode);
            foreach (HScriptParser.Query_actionContext x in context.query_action())
                t.AddChild(this.Visit(x));

            this.MasterNode = t;

            return t;

        }

        public override TNode VisitPrintScalar(HScriptParser.PrintScalarContext context)
        {
            FNodeSet nodes = VisitorHelper.GetReturnStatement(this.Evaluator, context.expression_or_wildcard_set());
            TNode t = new TNodePrint(this.MasterNode, this.Home, nodes);
            return t;
        }

        public override TNode VisitPrintMatrix(HScriptParser.PrintMatrixContext context)
        {
            MNode node = this.MatrixEvaluator.ToMatrix(context.matrix_expression());
            return new TNodePrintMatrix(this.MasterNode, node);
        }

        public override TNode VisitActEscapeFor(HScriptParser.ActEscapeForContext context)
        {
            return new TNodeEscapeLoop(this.MasterNode);
        }

        public override TNode VisitActEscapeRead(HScriptParser.ActEscapeReadContext context)
        {
            return new TNodeEscapeRead(this.MasterNode);
        }

        public override TNode VisitActSys(HScriptParser.ActSysContext context)
        {

            // Get the name //
            string Name = context.system_action().IDENTIFIER().GetText();
            
            // Get the leaf node set //
            HParameterSet parameters = VisitorHelper.GetHParameter(this.Home, context.system_action().hparameter_set());

            // Lookup the procedure //
            if (!SystemProcedures.Exists(Name))
                throw new Exception(string.Format("Procedure '{0}' does not exist", Name));
            Procedure proc = SystemProcedures.Lookup(Name, this.Home, parameters);

            // Walk the node //
            TNode t = new TNodeProcedure(this.MasterNode, proc, parameters);
            return t;

        }

        public override TNode VisitActMatAssign(HScriptParser.ActMatAssignContext context)
        {

            // Get the name //
            string name = context.matrix_name().IDENTIFIER().GetText();
            int idx = -1;
            if (this.LocalHeap.Arrays.Exists(name))
                idx = this.LocalHeap.Arrays.GetPointer(name);
            else if (this.Home.GlobalHeap.Arrays.Exists(name))
                idx = this.Home.GlobalHeap.Arrays.GetPointer(name);
            else
                throw new Exception(string.Format("Matrix '{0}' does not exist", name));

            // Create a visitor //
            MNode mat = this.MatrixEvaluator.ToMatrix(context.matrix_expression());

            // Build a node //
            return new TNodeMatrixAssign(this.MasterNode, this.LocalHeap.Arrays, idx, mat);

        }

        public override TNode VisitMUnit2DAssign(HScriptParser.MUnit2DAssignContext context)
        {

            CellMatrix mat;
            string name = context.IDENTIFIER().GetText();
            if (context.K_GLOBAL() != null && this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (context.K_LOCAL() != null && this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else if (this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else
            {
                throw new Exception(string.Format("Matrix does not exist in either the local or global heaps '{0}'", name));
            }
            FNode row = this.Evaluator.ToNode(context.expression()[0]);
            FNode col = this.Evaluator.ToNode(context.expression()[1]);
            FNode exp = this.Evaluator.ToNode(context.expression()[2]);

            return new TNodeMatrixUnitAssign(this.MasterNode, mat, exp, row, col, 0);

        }

        public override TNode VisitMUnit2DInc(HScriptParser.MUnit2DIncContext context)
        {

            CellMatrix mat;
            string name = context.IDENTIFIER().GetText();
            if (context.K_GLOBAL() != null && this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (context.K_LOCAL() != null && this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else if (this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else
            {
                throw new Exception(string.Format("Matrix does not exist in either the local or global heaps '{0}'", name));
            }
            FNode row = this.Evaluator.ToNode(context.expression()[0]);
            FNode col = this.Evaluator.ToNode(context.expression()[1]);
            FNode exp = this.Evaluator.ToNode(context.expression()[2]);

            return new TNodeMatrixUnitAssign(this.MasterNode, mat, exp, row, col, 1);

        }

        public override TNode VisitMUnit2DDec(HScriptParser.MUnit2DDecContext context)
        {

            CellMatrix mat;
            string name = context.IDENTIFIER().GetText();
            if (context.K_GLOBAL() != null && this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (context.K_LOCAL() != null && this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else if (this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else
            {
                throw new Exception(string.Format("Matrix does not exist in either the local or global heaps '{0}'", name));
            }
            FNode row = this.Evaluator.ToNode(context.expression()[0]);
            FNode col = this.Evaluator.ToNode(context.expression()[1]);
            FNode exp = this.Evaluator.ToNode(context.expression()[2]);

            return new TNodeMatrixUnitAssign(this.MasterNode, mat, exp, row, col, 2);

        }

        public override TNode VisitMUnit2DAutoInc(HScriptParser.MUnit2DAutoIncContext context)
        {

            CellMatrix mat;
            string name = context.IDENTIFIER().GetText();
            if (context.K_GLOBAL() != null && this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (context.K_LOCAL() != null && this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else if (this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else
            {
                throw new Exception(string.Format("Matrix does not exist in either the local or global heaps '{0}'", name));
            }
            FNode row = this.Evaluator.ToNode(context.expression()[0]);
            FNode col = this.Evaluator.ToNode(context.expression()[1]);
            FNode exp = this.Evaluator.ToNode(context.expression()[2]);

            return new TNodeMatrixUnitAssign(this.MasterNode, mat, exp, row, col, 3);

        }

        public override TNode VisitMUnit2DAutoDec(HScriptParser.MUnit2DAutoDecContext context)
        {

            CellMatrix mat;
            string name = context.IDENTIFIER().GetText();
            if (context.K_GLOBAL() != null && this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (context.K_LOCAL() != null && this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else if (this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else
            {
                throw new Exception(string.Format("Matrix does not exist in either the local or global heaps '{0}'", name));
            }
            FNode row = this.Evaluator.ToNode(context.expression()[0]);
            FNode col = this.Evaluator.ToNode(context.expression()[1]);
            FNode exp = this.Evaluator.ToNode(context.expression()[2]);

            return new TNodeMatrixUnitAssign(this.MasterNode, mat, exp, row, col, 4);

        }

        public override TNode VisitMUnit1DAssign(HScriptParser.MUnit1DAssignContext context)
        {

            CellMatrix mat;
            string name = context.IDENTIFIER().GetText();
            if (context.K_GLOBAL() != null && this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (context.K_LOCAL() != null && this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else if (this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else
            {
                throw new Exception(string.Format("Matrix does not exist in either the local or global heaps '{0}'", name));
            }
            FNode row = this.Evaluator.ToNode(context.expression()[0]);
            FNode col = FNodeFactory.Value(0);
            FNode exp = this.Evaluator.ToNode(context.expression()[1]);

            return new TNodeMatrixUnitAssign(this.MasterNode, mat, exp, row, col, 0);

        }

        public override TNode VisitMUnit1DInc(HScriptParser.MUnit1DIncContext context)
        {

            CellMatrix mat;
            string name = context.IDENTIFIER().GetText();
            if (context.K_GLOBAL() != null && this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (context.K_LOCAL() != null && this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else if (this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else
            {
                throw new Exception(string.Format("Matrix does not exist in either the local or global heaps '{0}'", name));
            }
            FNode row = this.Evaluator.ToNode(context.expression()[0]);
            FNode col = FNodeFactory.Value(0);
            FNode exp = this.Evaluator.ToNode(context.expression()[1]);

            return new TNodeMatrixUnitAssign(this.MasterNode, mat, exp, row, col, 1);

        }

        public override TNode VisitMUnit1DDec(HScriptParser.MUnit1DDecContext context)
        {

            CellMatrix mat;
            string name = context.IDENTIFIER().GetText();
            if (context.K_GLOBAL() != null && this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (context.K_LOCAL() != null && this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else if (this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else
            {
                throw new Exception(string.Format("Matrix does not exist in either the local or global heaps '{0}'", name));
            }
            FNode row = this.Evaluator.ToNode(context.expression()[0]);
            FNode col = FNodeFactory.Value(0);
            FNode exp = this.Evaluator.ToNode(context.expression()[1]);

            return new TNodeMatrixUnitAssign(this.MasterNode, mat, exp, row, col, 2);

        }

        public override TNode VisitMUnit1DAutoInc(HScriptParser.MUnit1DAutoIncContext context)
        {

            CellMatrix mat;
            string name = context.IDENTIFIER().GetText();
            if (context.K_GLOBAL() != null && this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (context.K_LOCAL() != null && this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else if (this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else
            {
                throw new Exception(string.Format("Matrix does not exist in either the local or global heaps '{0}'", name));
            }
            FNode row = this.Evaluator.ToNode(context.expression());
            FNode col = FNodeFactory.Value(0);
            FNode exp = FNodeFactory.Value(1);

            return new TNodeMatrixUnitAssign(this.MasterNode, mat, exp, row, col, 3);

        }

        public override TNode VisitMUnit1DAutoDec(HScriptParser.MUnit1DAutoDecContext context)
        {

            CellMatrix mat;
            string name = context.IDENTIFIER().GetText();
            if (context.K_GLOBAL() != null && this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (context.K_LOCAL() != null && this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else if (this.Home.GlobalHeap.Arrays.Exists(name))
            {
                mat = this.Home.GlobalHeap.Arrays[name];
            }
            else if (this.LocalHeap.Arrays.Exists(name))
            {
                mat = this.LocalHeap.Arrays[name];
            }
            else
            {
                throw new Exception(string.Format("Matrix does not exist in either the local or global heaps '{0}'", name));
            }
            FNode row = this.Evaluator.ToNode(context.expression());
            FNode col = FNodeFactory.Value(0);
            FNode exp = FNodeFactory.Value(1);

            return new TNodeMatrixUnitAssign(this.MasterNode, mat, exp, row, col, 4);

        }

        public override TNode VisitExecute_script(HScriptParser.Execute_scriptContext context)
        {

            string script = this.Evaluator.ToNode(context.expression()).Evaluate().valueSTRING;
            HScriptProcessor instance = new HScriptProcessor(this.Home.CloneWithConnections);

            Dictionary<string, FNode> bindings = new Dictionary<string, FNode>();
            if (context.bind_element_set() != null)
            {
                foreach (HScriptParser.Bind_elementContext ctx in context.bind_element_set().bind_element())
                {
                    string scalar_name = ctx.SCALAR().GetText();
                    FNode value = this.Evaluator.ToNode(ctx.expression());
                    bindings.Add(scalar_name, value);
                }
            }
            return new TNodeExecute(this.MasterNode, instance, bindings, script);

        }

        public override TNode VisitActWhile(HScriptParser.ActWhileContext context)
        {


            // Get the controll structure //
            FNode control = this.Evaluator.ToNode(context.expression());

            // Create the parent node //
            TNode t = new TNodeWhile(this.MasterNode, control);

            // Get the sub - action //
            TNode sub_action = this.ToNode(context.query_action());

            // Assign the sub-action to t //
            t.AddChild(sub_action);

            // Assign the master node to the node we just built //
            this.MasterNode = t;

            return t;

        }

        public TNode ToNode(HScriptParser.Query_actionContext context)
        {
            this.MasterNode = null;
            return this.Visit(context);
        }

        public TNodeSet ToNodes(HScriptParser.Query_action_setContext context)
        {

            TNodeSet tree = new TNodeSet();
            foreach (HScriptParser.Query_actionContext ctx in context.query_action())
                tree.Add(this.ToNode(ctx));
            return tree;

        }

        private TNode ParseSequentialReturn(HScriptParser.ActReturnContext context)
        {

            // Get the FNodeSet //
            FNodeSet nodes = VisitorHelper.GetReturnStatement(this.Evaluator, context.return_action().expression_or_wildcard_set());

            // Open a writer stream //
            RecordWriter writer;
            string key = context.return_action().full_table_name().GetText();

            // First check if the open stream collection //
            if (this._OpenStreams.ContainsKey(key))
            {
                writer = this._OpenStreams[key];
            }
            // Otherwise, create a new writer, then allocate to the open stream collection 
            else
            {
                writer = VisitorHelper.GetWriter(this.Home, nodes.Columns, context.return_action());
                this._OpenStreams.Add(key, writer);
            }

            // Create the node //
            TNode node = new TNodeAppendTo(this.MasterNode, writer, nodes);

            this.MasterNode = node;

            return node;

        }

        private TNode ParseConcurrentReturn(HScriptParser.ActReturnContext context)
        {

            // Get the FNodeSet //
            FNodeSet nodes = VisitorHelper.GetReturnStatement(this.Evaluator, context.return_action().expression_or_wildcard_set());

            string name = context.return_action().full_table_name().GetText();
            DataSet t;
            if (this.Home.TableIsInOpenCache(name))
            {
                t = this.Home.GetOpenCacheElement(name);
            }
            else
            {
                t = VisitorHelper.GetData(this.Home, nodes, context.return_action());
                this.Home.AppendOpenCacheElement(name, t);
            }
            
            TNode node;
            if (t.IsBig)
            {
                node = new TNodeAppendToTableAsync(this.MasterNode, t.ToBigRecordSet, nodes);
            }
            else
            {
                node = new TNodeAppendToChunkAsync(this.MasterNode, t.ToRecordSet, nodes);
            }

            this.MasterNode = node;

            return node;

        }


    }


}
