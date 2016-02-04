using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.HScript;

namespace Equus.Percheron
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

}
