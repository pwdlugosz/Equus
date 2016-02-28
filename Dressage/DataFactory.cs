using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.HScript;
using Equus.Calabrese;
using Equus.QuarterHorse;
using Equus.Shire;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Equus.Dressage
{
    
    public sealed class DataFactory
    {

        private HScript.HScriptProcessor _base_processor;
        private HScript.Workspace _base_workspace;

        public DataFactory(string TempDB)
        {
            this._base_workspace = new Workspace(TempDB);
            this._base_processor = new HScriptProcessor(this._base_workspace);
        }

        public void Connect(string Alias, string Directory)
        {
            this._base_workspace.Connections.Allocate(Alias, Directory);
        }

        public RecordSet RenderRecordSet(string Script)
        {
            this._base_processor.Execute(Script);
            string name = this._base_workspace.ChunkHeap.Name(0);
            RecordSet rs = this._base_workspace.ChunkHeap[name];
            this._base_workspace.ChunkHeap.Deallocate(name);
            return rs;
        }

        public Table RenderTable(string Script)
        {
            this._base_processor.Execute(Script);
            string full_name = Script.Split('.')[2];
            string db = full_name.Split('.')[0];
            string name = full_name.Split('.')[1];
            Table t = this._base_workspace.GetStaticTable(db, name);
            return t;
        }

    }

    public static class QueryFactory
    {

        // RecordSet SELECTS //
        public static RecordSet SELECT(DataSet Data, FNodeSet Nodes, Predicate Where)
        {

            RecordSet rs = new RecordSet(Nodes.Columns);
            RecordWriter w = rs.OpenWriter();
            FastReadPlan plan = new FastReadPlan(Data, Where, Nodes, w);
            plan.Execute();
            w.Close();
            return rs;

        }

        public static RecordSet SELECT(DataSet Data, FNodeSet Nodes)
        {
            return SELECT(Data, Nodes, Predicate.TrueForAll);
        }

        public static RecordSet SELECT(DataSet Data, Predicate Where)
        {
            return SELECT(Data, new FNodeSet(Data.Columns), Where);
        }

        // Table SELECTS //
        public static Table SELECT(string Dir, string Name, DataSet Data, FNodeSet Nodes, Predicate Where)
        {

            Table rs = new Table(Dir, Name, Nodes.Columns);
            RecordWriter w = rs.OpenWriter();
            FastReadPlan plan = new FastReadPlan(Data, Where, Nodes, w);
            plan.Execute();
            w.Close();
            return rs;

        }

        public static Table SELECT(string Dir, string Name, DataSet Data, FNodeSet Nodes)
        {
            return SELECT(Dir, Name, Data, Nodes, Predicate.TrueForAll);
        }

        public static Table SELECT(string Dir, string Name, DataSet Data, Predicate Where)
        {
            return SELECT(Dir, Name, Data, new FNodeSet(Data.Columns), Where);
        }

        // RecordSet DELETES //
        public static long DELETE(RecordSet Data, Predicate Where)
        {
            RecordSet rs = SELECT(Data, Where.NOT);
            long deletes = Data.Count - rs.Count;
            Data._Cache = rs._Cache;
            return deletes;
        }

        public static long DELETE(RecordSet Data)
        {
            long l = Data.Count;
            Data._Cache.Clear();
            return l;
        }

        // Table DELETES //
        public static long DELETE(Table Data, Predicate Where)
        {

            DeletePlan plan = new DeletePlan(Data, Where);
            plan.Execute();
            return plan.Reads - plan.Writes;

        }

        public static long DELETE(Table Data)
        {
            long l = Data.Count;
            Data.Clear();
            return l;
        }

        // Updates //
        public static long UPDATE(DataSet Data, Key Fields, FNodeSet Values, Predicate Where)
        {
            UpdatePlan plan = new UpdatePlan(Data, Fields, Values, Where);
            plan.Execute();
            return plan.Reads;
        }

        public static long UPDATE(DataSet Data, Key Fields, FNodeSet Values)
        {
            return UPDATE(Data, Fields, Values, Predicate.TrueForAll);
        }

    }

    public static class ExpressionFactory
    {

        public static FNodeSet ParseFNodeSet(string Text, MemoryStruct LocalHeap, Workspace Home, string Alias, Schema Columns, Register Memory)
        {

            // Build text stream //
            AntlrInputStream ais = new AntlrInputStream(Text);
            HScriptLexer lex = new HScriptLexer(ais);

            // Build token tree //
            CommonTokenStream cts = new CommonTokenStream(lex);
            HScriptParser par = new HScriptParser(cts);

            // Build AST //
            IParseTree tree = par.expression_alias_list();
            if (tree == null)
                tree = par.expression_or_wildcard_set();
            
            // Visit each node getting the final node //
            ExpressionVisitor v = new ExpressionVisitor(LocalHeap, Home);
            v.AddSchema(Alias, Columns, Memory);

            if (tree is HScriptParser.Expression_or_wildcard_setContext)
            {
                HScriptParser.Expression_or_wildcard_setContext a = tree as HScriptParser.Expression_or_wildcard_setContext;
                return VisitorHelper.GetReturnStatement(v, a);
            }
            else if (tree is HScriptParser.Expression_alias_listContext)
            {
                HScriptParser.Expression_alias_listContext b = tree as HScriptParser.Expression_alias_listContext;
                return v.ToNodes(b);
            }

            throw new Exception("Expression is not an expression set: " + Text);

        }

        public static FNodeSet ParseFNodeSet(string Text, Schema Columns)
        {
            return ExpressionFactory.ParseFNodeSet(Text, new MemoryStruct(false), new Workspace("HORSE"), "T", Columns, new StaticRegister(Columns));
        }

        public static FNodeSet ParseFNodeSet(string Text)
        {
            return ExpressionFactory.ParseFNodeSet(Text, new Schema());
        }

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

        public static FNode ParseFNode(string Text, Schema Columns)
        {
            return ExpressionFactory.ParseFNode(Text, new MemoryStruct(false), new Workspace("HORSE"), "T", Columns, new StaticRegister(Columns));
        }

        public static FNode ParseFNode(string Text)
        {
            return ExpressionFactory.ParseFNode(Text, new Schema());
        }

        public static Predicate ParsePredicate(string Text, MemoryStruct LocalHeap, Workspace Home, string Alias, Schema Columns, Register Memory)
        {
            FNode node = ExpressionFactory.ParseFNode(Text, LocalHeap, Home, Alias, Columns, Memory);
            return new Predicate(node);
        }

        public static Predicate ParsePredicate(string Text, Schema Columns)
        {
            FNode node = ExpressionFactory.ParseFNode(Text, Columns);
            return new Predicate(node);
        }

        public static Predicate ParsePredicate(string Text)
        {
            FNode node = ExpressionFactory.ParseFNode(Text);
            return new Predicate(node);
        }

    }

}
