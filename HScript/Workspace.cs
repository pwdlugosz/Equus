using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Shire;
using Equus.QuarterHorse;
using Equus.HScript;
using Equus.Thoroughbred;

namespace Equus.HScript
{

    public sealed class Workspace
    {

        public const string TEMP_DB_ALIAS = "temp";
        public const string MEMORY_DB = "global";
        public const char TABLE_NAME_DOT = '.';

        private Heap<RecordSet> _ChunkHeap;
        private Heap<Lambda> _Lambdas;
        private MemoryStruct _GlobalHeap;
        private Heap<string> _Connections;
        private Communicator _IO;
        private Dictionary<string, DataSet> _OpenData;
        private Heap<Model> _Models;
        private Heap<FNode> _Beacons;
        
        public Workspace(string TempDirectory, Communicator UseIO)
        {

            // Create the heaps //
            this._ChunkHeap = new Heap<RecordSet>();
            this._GlobalHeap = new MemoryStruct(false);
            this._Connections = new Heap<string>();
            this._IO = UseIO;
            this._Lambdas = new Heap<Lambda>();
            this._OpenData = new Dictionary<string, DataSet>(StringComparer.OrdinalIgnoreCase);
            this._Models = new Heap<Model>();
            this._Beacons = new Heap<FNode>();
            
            // Add the temp connections //
            this._Connections.Allocate(TEMP_DB_ALIAS, TempDirectory);

            // Turn on communication //
            this.SupressIO = false;

        }

        public Workspace(string TempDirectory)
            : this(TempDirectory, new ConsoleCommunicator())
        {
        }

        public Heap<RecordSet> ChunkHeap
        {
            get { return this._ChunkHeap; }
        }

        public MemoryStruct GlobalHeap
        {
            get { return this._GlobalHeap; }
        }

        public Heap<string> Connections
        {
            get { return this._Connections; }
        }

        public Heap<Lambda> Lambdas
        {
            get
            {
                return this._Lambdas;
            }
        }

        public Heap<Model> Models
        {
            get { return this._Models; }
        }

        public Heap<FNode> Beacons
        {
            get { return this._Beacons; }
        }

        public string TempSpace
        {
            get { return this._Connections[TEMP_DB_ALIAS]; }
        }

        public Communicator IO
        {
            get { return this._IO; }
        }

        public bool SupressIO
        {
            get;
            set;
        }

        public Table GetStaticTable(string DataBase, string Name)
        {
            string dir = this.Connections[DataBase];
            string path = TableHeader.FilePath(dir, Name);
            return BinarySerializer.BufferTable(path);
        }

        public DataSet GetData(string DataBase, string Name)
        {

            if (this.Exists(DataBase, Name))
                return this.GetStaticTable(DataBase, Name);
            if (StringComparer.OrdinalIgnoreCase.Compare(MEMORY_DB, DataBase) == 0)
                return this.ChunkHeap[Name];

            throw new Exception(string.Format("Table '{0}' does not exist", Name));

        }

        public DataSet GetData(string FullName)
        {
            string[] tokens = FullName.Split(TABLE_NAME_DOT);
            string db = 
                (tokens.Length == 2) 
                ? tokens[0] 
                : TEMP_DB_ALIAS;
            string name = 
                (tokens.Length == 2) 
                ? tokens[1] 
                : tokens[0];
            if (tokens.Length == 1)
                return this.ChunkHeap[name];
            return this.GetData(db, name);
        }

        public bool Exists(string DataBase, string Name)
        {

            // In memory database //
            if (DataBase.ToLower() == MEMORY_DB)
                return false;

            string dir = this.Connections[DataBase];
            string path = TableHeader.FilePath(dir, Name);
            return System.IO.File.Exists(path);
        }

        public bool Exists(string FullName)
        {
            string[] tokens = FullName.Split(TABLE_NAME_DOT);
            string db = (tokens.Length == 2) ? tokens[0] : TEMP_DB_ALIAS;
            string name = (tokens.Length == 2) ? tokens[1] : tokens[0];
            return this.Exists(db, name);
        }

        public bool DropTable(string DataBase, string Name)
        {
            if (!this.Exists(DataBase, Name))
                return false;
            string dir = this.Connections[DataBase];
            string path = TableHeader.FilePath(dir, Name);
            DataSetManager.DropTable(path);
            return true;
        }

        public bool DropTable(string FullName)
        {
            string[] tokens = FullName.Split(TABLE_NAME_DOT);
            string db = (tokens.Length == 2) ? tokens[0] : TEMP_DB_ALIAS;
            string name = (tokens.Length == 2) ? tokens[1] : tokens[0];
            return this.DropTable(db, name);
        }

        public Workspace CloneWithConnections
        {

            get
            {
                
                Workspace ws = new Workspace(this.TempSpace);
                foreach (KeyValuePair<string, string> kv in this._Connections.Entries)
                {
                    if (kv.Key != TEMP_DB_ALIAS)
                        ws.Connections.Allocate(kv.Key, kv.Value);
                }
                return ws;

            }

        }

        // Internals //
        internal bool TableIsInOpenCache(string Name)
        {
            return this._OpenData.ContainsKey(Name);
        }

        internal DataSet GetOpenCacheElement(string Name)
        {
            return this._OpenData[Name];
        }

        internal void AppendOpenCacheElement(string Name, DataSet Data)
        {

            if (this._OpenData.ContainsKey(Name))
            {
                this._OpenData[Name] = Data;
            }
            else
            {
                this._OpenData.Add(Name, Data);
            }

        }

        internal void BurnCache()
        {
            this._OpenData = new Dictionary<string, DataSet>(StringComparer.OrdinalIgnoreCase);
        }

    }

}
