using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.HScript;
using Equus.Calabrese;

namespace Equus.Andalusian
{
    
    public sealed class TNodeExecute : TNode
    {

        private Dictionary<string, FNode> _nodes;
        private HScriptProcessor _scripter;
        private string _script;

        public TNodeExecute(TNode Parent, HScriptProcessor UseScripter, Dictionary<string, FNode> UseBindings, string UseScript)
            : base(Parent)
        {
            this._nodes = UseBindings;
            this._scripter = UseScripter;
            this._script = UseScript;
        }

        public override void Invoke()
        {

            // Get the script to run //
            string t = ScriptHelper.BindParamters(this._nodes, this._script);

            // Run //
            this._scripter.Execute(t);

        }

        public override string Message()
        {
            return "EXECUTE";
        }

        public override TNode CloneOfMe()
        {
            
            // Clone the dictionary //
            Dictionary<string, FNode> NewNodes = new Dictionary<string, FNode>();
            foreach (KeyValuePair<string, FNode> kv in this._nodes)
                NewNodes.Add(kv.Key, kv.Value.CloneOfMe());

            // Clone the scriptor's workspace //
            Workspace ws = this._scripter.Home.CloneWithConnections;

            // Return a clone //
            return new TNodeExecute(this.Parent, new HScriptProcessor(ws), NewNodes, this._script);

        }

    }

}
