using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Shire;

namespace Equus.Calabrese
{

    public sealed class FNodeSet
    {

        private List<string> _Alias;
        private List<FNode> _Nodes;
        private bool _AllowNameDotName = false;

        // Constructor //
        public FNodeSet()
        {
            this._Alias = new List<string>();
            this._Nodes = new List<FNode>();
        }

        public FNodeSet(IEnumerable<string> Aliases, IEnumerable<FNode> Nodes)
            : this()
        {
            this._Alias = Aliases.ToList();
            this._Nodes = Nodes.ToList();
        }
        
        public FNodeSet(FNodeSet Set)
                :this(Set._Alias, Set._Nodes)
        {

        }
        
        public FNodeSet(string Aliases, FNode Node)
            : this()
        {
            this.Add(Aliases, Node);
        }

        public FNodeSet(Schema Columns, bool AlloowDotNames)
            : this()
        {
            
            this.AllowNameDotName = AlloowDotNames;
            for (int i = 0; i < Columns.Count; i++)
            {
                this.Add(Columns.ColumnName(i), new FNodeFieldRef(null, i, Columns.ColumnAffinity(i), Columns.ColumnSize(i), null));
            }
        }

        public FNodeSet(Schema Columns)
            : this(Columns, false)
        {
        }

        public FNodeSet(Schema Columns, Key Fields)
            : this()
        {

            this.AllowNameDotName = false;
            for (int i = 0; i < Fields.Count; i++)
            {
                this.Add(Columns.ColumnName(Fields[i]), new FNodeFieldRef(null, Fields[i], Columns.ColumnAffinity(Fields[i]), Columns.ColumnSize(Fields[i]), null));
            }
        }

        // Properties //
        public int Count
        {
            get
            {
                return this._Alias.Count;
            }
        }

        public Schema Columns
        {
            get
            {

                Schema cols = new Schema();
                for (int i = 0; i < this.Count; i++)
                    cols.Add(this._Alias[i], this._Nodes[i].ReturnAffinity());
                return cols;

            }
        }

        public bool AllowNameDotName
        {
            get { return this._AllowNameDotName; }
            set { this._AllowNameDotName = value; }
        }

        public FNode this[int Index]
        {
            get { return this._Nodes[Index]; }
        }

        public FNode this[string Alias]
        {
            get
            {
                int idx = this.Reference(Alias);
                return this[idx];
            }
        }

        public IEnumerable<FNode> Nodes
        {
            get { return this._Nodes; }
        }

        public List<int> FieldRefs
        {

            get
            {

                List<int> refs = new List<int>();
                foreach (FNode n in this._Nodes)
                {
                    refs.AddRange(FNodeAnalysis.AllFieldRefs(n));
                }
                return refs.Distinct().ToList();

            }

        }

        // Adds //
        public void Add(string Alias, FNode Node)
        {

            // Clean the alias //
            if (!this._AllowNameDotName)
                Alias = ExtractName(Alias);

            // Check if alias exists //
            if (this._Alias.Contains(Alias, StringComparer.OrdinalIgnoreCase))
                throw new Exception(string.Format("Alias '{0}' already exists", Alias));

            this._Alias.Add(Alias);
            this._Nodes.Add(Node);

        }

        public void Add(FNode Node)
        {
            this.Add("F" + this.Count.ToString(), Node);
        }

        // Methods //
        public Record Evaluate()
        {
            Cell[] c = new Cell[this.Count];
            for (int i = 0; i < this.Count; i++)
                c[i] = this._Nodes[i].Evaluate();
            return new Record(c);
        }

        public int Reference(string Name)
        {

            for (int i = 0; i < this.Count; i++)
            {
                if (StringComparer.OrdinalIgnoreCase.Compare(this._Alias[i], Name) == 0)
                    return i;
            }
            return -1;

        }

        public string Alias(int Index)
        {
            return this._Alias[Index];
        }

        public void AssignRegister(Register Memory)
        {
            foreach (FNode n in this._Nodes)
                n.AssignRegister(Memory);
        }

        public FNodeSet CloneOfMe()
        {
            FNodeSet nodes = new FNodeSet();
            for (int i = 0; i < this.Count; i++)
            {
                nodes.Add(this._Alias[i], this._Nodes[i].CloneOfMe());
            }
            return nodes;
        }

        public void AssignHeap(MemoryStruct Mem)
        {

            foreach (FNode node in this._Nodes)
                node.AssignHeap(Mem);

        }

        public string Unparse(Schema Columns)
        {
            StringBuilder sb = new StringBuilder();
            foreach (FNode n in this._Nodes)
                sb.Append(n.Unparse(Columns) + " , ");
            return sb.ToString();
        }

        // Statics //
        internal static string ExtractName(string Alias)
        {
            return Alias.Split(Schema.ALIAS_DELIM).Last();
        }

        internal static void Reverse(FNodeSet Tree)
        {
            Tree._Alias.Reverse();
            Tree._Nodes.Reverse();
        }

        public static FNodeSet Union(params FNodeSet[] NodeSets)
        {

            FNodeSet f = new FNodeSet();
            foreach (FNodeSet n in NodeSets)
            {

                for (int i = 0; i < n.Count; i++)
                {
                    f.Add(n._Alias[i], n._Nodes[i]);
                }

            }
            return f;

        }

    }



}
