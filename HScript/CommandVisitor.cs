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
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
namespace Equus.HScript
{

    public sealed class CommandVisitor : HScriptParserBaseVisitor<CommandPlan>
    {

        public CommandVisitor(Workspace UseHome)
            : base()
        {
            this.Home = UseHome;
        }

        public Workspace Home
        {
            get;
            private set;
        }

        /* CRUDAM Methods
         * 
         * Create
         * Read
         * Update
         * Delete
         * Aggregate
         * Merge
         * 
         */

        // Create //
        public override CommandPlan VisitCrudam_create_table(HScriptParser.Crudam_create_tableContext context)
        {

            CreateTablePlan plan = CommandCompiler.RenderCreatePlan(this.Home, context);
            return plan;

        }

        public override CommandPlan VisitCrudam_declare_table(HScriptParser.Crudam_declare_tableContext context)
        {

            CreateChunkPlan plan = CommandCompiler.RenderCreateChunk(this.Home, context);
            return plan;

        }

        public override CommandPlan VisitCrudam_declare_many(HScriptParser.Crudam_declare_manyContext context)
        {

            DeclarePlan plan = CommandCompiler.RenderDeclarePlan(this.Home, context);
            return plan;

        }

        public override CommandPlan VisitCrudam_lambda(HScriptParser.Crudam_lambdaContext context)
        {
            Lambda l = VisitorHelper.RenderLambda(Home, context.lambda_unit());
            return new LambdaPlan(Home.Lambdas, l.Name, l);
        }

        // Read //
        public override CommandPlan VisitCrudam_read(HScriptParser.Crudam_readContext context)
        {

            StagedReadName read = CommandCompiler.RenderStagedReadPlan(this.Home, context);
            return read;

        }

        public override CommandPlan VisitCrudam_read_fast(HScriptParser.Crudam_read_fastContext context)
        {

            FastReadPlan read = CommandCompiler.RenderFastReadPlan(this.Home, context);
            return read;

        }

        public override CommandPlan VisitCrudam_read_mapr(HScriptParser.Crudam_read_maprContext context)
        {
            return CommandCompiler.RenderMapReducePlan(this.Home, context);
        }

        // Aggregate //
        public override CommandPlan VisitCrudam_aggregate(HScriptParser.Crudam_aggregateContext context)
        {

            int partitions = VisitorHelper.GetPartitions(new ExpressionVisitor(Home.GlobalHeap, Home), context.partitions());
            if (partitions == 1)
                return CommandCompiler.RenderAggregatePlan(this.Home, context);
            return CommandCompiler.RenderPartitionedAggregatePlan(this.Home, context);

        }

        // Update //
        public override CommandPlan VisitCrudam_update(HScriptParser.Crudam_updateContext context)
        {

            UpdatePlan plan = CommandCompiler.RenderUpdatePlan(this.Home, context);
            return plan;

        }

        // Delete //
        public override CommandPlan VisitCrudam_delete(HScriptParser.Crudam_deleteContext context)
        {

            DeletePlan plan = CommandCompiler.RenderDeletePlan(this.Home, context);
            return plan;

        }

        // Merge //
        public override CommandPlan VisitCrudam_merge(HScriptParser.Crudam_mergeContext context)
        {

            MergePlan plan = CommandCompiler.RenderMergePlan(this.Home, context);
            return plan;

        }

        /* QUERY ACTIONS 
         * 
         * 
         */
        public override CommandPlan VisitCommand_action(HScriptParser.Command_actionContext context)
        {

            ActionPlan plan = CommandCompiler.RenderActionPlan(this.Home, context.query_action());
            return plan;

        }

    }

}
