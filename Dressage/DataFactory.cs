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



    }

}
