using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Equus.Horse;
using Equus.Calabrese;
using Equus.HScript;
using Equus.Shire;
using Equus.HScript.Parameters;

namespace Equus.Clydesdale
{

    // Lookup Table //
    public static class SystemProcedures
    {

        public static string Dot = ".";

        public static string Name_SystemProcedures = "SYS_PROCEDURES";
        public static string Name_SystemExplain = "SYS_EXPLAIN";
        public static string Name_SystemConnect = "SYS_CONNECT";
        public static string Name_SystemDisconnect = "SYS_DISCONNECT";
        public static string Name_SystemSort = "SYS_SORT";
        public static string Name_SystemReverse = "SYS_REVERSE";
        public static string Name_SystemShuffle = "SYS_SHUFFLE";
        public static string Name_SystemPrintTable = "SYS_PRINT_TABLE";
        public static string Name_SystemImportText = "SYS_IMPORT_TEXT";
        public static string Name_SystemExportText = "SYS_EXPORT_TEXT";
        public static string Name_SystemDownload = "SYS_DOWNLOAD";
        public static string Name_SystemDebugAST = "SYS_DEBUG_AST";
        public static string Name_SystemDropTable = "SYS_DROP_TABLE";
        public static string Name_SystemDeallocate = "SYS_DEALLOCATE";
        public static string Name_SystemMemoryDump = "SYS_MEMORY_DUMP";
        public static string Name_SystemToTable = "SYS_TO_TABLE";
        public static string Name_SystemDebugLambda = "SYS_DEBUG_LAMBDA";
        public static string Name_SystemIO = "SYS_IO";

        public static string Name_FileMove = "FILE_MOVE";
        public static string Name_FileCopy = "FILE_COPY";
        public static string Name_FileDelete = "FILE_DEL";
        public static string Name_FileZip = "FILE_ZIP";
        public static string Name_FileUnzip = "FILE_UNZIP";

        public static string Name_MinerLM = "MINER_LM";
        public static string Name_MinerGLM = "MINER_GLM";
        public static string Name_MinerNLM = "MINER_NLM";
        public static string Name_MinerRC = "MINER_ROWCLUS";
        public static string Name_MinerFFNN = "MINER_FFNN";

        private static Dictionary<string, Func<Workspace, HParameterSet, Procedure>> PROCEDURES = new Dictionary<string, Func<Workspace, HParameterSet, Procedure>>(StringComparer.OrdinalIgnoreCase)
        {

            { SystemProcedures.Name_SystemExplain, (home, parameters) => { return new SystemProcedureExplain(home, parameters);}},
            { SystemProcedures.Name_SystemConnect, (home, parameters) => { return new SystemProcedureConnect(home, parameters);}},
            { SystemProcedures.Name_SystemDisconnect, (home, parameters) => { return new SystemProcedureDisconnect(home, parameters);}},
            { SystemProcedures.Name_SystemSort, (home, parameters) => { return new SystemProcedureSort(home, parameters);}},
            { SystemProcedures.Name_SystemReverse, (home, parameters) => { return new SystemProcedureReverse(home, parameters);}},
            { SystemProcedures.Name_SystemShuffle, (home, parameters) => { return new SystemProcedureShuffle(home, parameters);}},
            { SystemProcedures.Name_SystemPrintTable, (home, parameters) => { return new SystemProcedurePrintTable(home, parameters);}},
            { SystemProcedures.Name_SystemImportText, (home, parameters) => { return new SystemProcedureImportText(home, parameters);}},
            { SystemProcedures.Name_SystemExportText, (home, parameters) => { return new SystemProcedureExportText(home, parameters);}},
            { SystemProcedures.Name_SystemDownload, (home, parameters) => { return new SystemProcedureDownload(home, parameters);}},
            { SystemProcedures.Name_SystemDebugAST, (home, parameters) => { return new SystemProcedureDebugAST(home, parameters);}},
            { SystemProcedures.Name_SystemDeallocate, (home, parameters) => { return new SystemProcedureDeallocate(home, parameters);}},
            { SystemProcedures.Name_SystemDropTable, (home, parameters) => { return new SystemProcedureDropTable(home, parameters);}},
            { SystemProcedures.Name_SystemMemoryDump, (home, parameters) => { return new SystemProcedureMemoryDump(home, parameters);}},
            { SystemProcedures.Name_SystemToTable, (home, parameters) => { return new SystemProcedureToTable(home, parameters);}},
            { SystemProcedures.Name_SystemDebugLambda, (home, parameters) => { return new SystemProcedureDebugLambda(home, parameters);}},
            { SystemProcedures.Name_SystemIO, (home, parameters) => { return new SystemProcedureIO(home, parameters);}},
            
            { SystemProcedures.Name_FileMove, (home, parameters) => { return new FileProcedureFileMove(home, parameters);}},
            { SystemProcedures.Name_FileCopy, (home, parameters) => { return new FileProcedureFileCopy(home, parameters);}},
            { SystemProcedures.Name_FileDelete, (home, parameters) => { return new FileProcedureFileDelete(home, parameters);}},
            { SystemProcedures.Name_FileZip, (home, parameters) => { return new FileProcedureFileZip(home, parameters);}},
            { SystemProcedures.Name_FileUnzip, (home, parameters) => { return new FileProcedureFileUnZip(home, parameters);}},

            { SystemProcedures.Name_MinerLM, (home, parameters) => { return new MinerLinearModel(home, parameters);}},
            { SystemProcedures.Name_MinerGLM, (home, parameters) => { return new MinerGeneralizedLinearModel(home, parameters);}},
            { SystemProcedures.Name_MinerNLM, (home, parameters) => { return new MinerNonLinearModel(home, parameters);}},
            { SystemProcedures.Name_MinerRC, (home, parameters) => { return new MinerRowCluster(home, parameters);}},
            { SystemProcedures.Name_MinerFFNN, (home, parameters) => { return new MinerFFNeural(home,parameters);}},

        };

        public static bool Exists(string Name)
        {
            return PROCEDURES.ContainsKey(Name);
        }

        public static Procedure Lookup(string Name, Workspace Home, HParameterSet Parameters)
        {
            return PROCEDURES[Name](Home, Parameters);
        }

    }

    // System Proceduers //
    public sealed class SystemProcedureExplain : Procedure
    {

        public SystemProcedureExplain(Workspace Home, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemExplain, "Prints meta data for a procedure", Home, Parameters)
        {
            
            this._ParametersMap.Add("@NAME", new ProcedureParameterMetaData("@NAME!true!expression!The name of the stored procedure"));
            
        }

        public override void Invoke()
        {

            string ProcName = this._Parameters["@NAME"].Expression.Evaluate().valueSTRING;
            if (SystemProcedures.Exists(ProcName))
                Comm.WriteLine(SystemProcedures.Lookup(ProcName, null, null).Info());
            else
                Comm.WriteLine("Procedure '{0}' does not exist", ProcName);

        }

    }

    public sealed class SystemProcedureConnect : Procedure
    {

        public SystemProcedureConnect(Workspace Home, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemConnect, "Connects to a datastore", Home, Parameters)
        {

            this._ParametersMap.Add("@ALIAS", new ProcedureParameterMetaData("@ALIAS!true!expression!The alias to assign to the connection path"));
            this._ParametersMap.Add("@PATH", new ProcedureParameterMetaData("@PATH!true!expression!The path to the datastore"));
            
        }

        public override void Invoke()
        {

            string alias = this._Parameters["@ALAIS"].Expression.Evaluate().valueSTRING;
            string dir = this._Parameters["@PATH"].Expression.Evaluate().valueSTRING;
            this.Home.Connections.Reallocate(alias, dir);

        }

    }

    public sealed class SystemProcedureDisconnect : Procedure
    {

        public SystemProcedureDisconnect(Workspace Home, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemDisconnect, "Disconnects a datastore", Home, Parameters)
        {
            this._ParametersMap.Add("@ALIAS", new ProcedureParameterMetaData("@ALIAS!true!expression!The alias to assign to the connection path"));
        }

        public override void Invoke()
        {

            string alias = this._Parameters["@ALIAS"].Expression.Evaluate().valueSTRING;
            this.Home.Connections.Deallocate(alias);

        }

    }

    public sealed class SystemProcedureSort : Procedure
    {

        public SystemProcedureSort(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemSort, "Does an inplace merge-sort of the dataset", UseHome, Parameters)
        {
            
            this._ParametersMap.Add("@TABLE", new ProcedureParameterMetaData("@TABLE!true!dataset!The full table name of the table to sort"));
            this._ParametersMap.Add("@KEY", new ProcedureParameterMetaData("@KEY!true!expression!The sort key as a string"));
            
        }

        public override void Invoke()
        {

            // Get the table //
            DataSet data = this._Parameters["@TABLE"].Data;

            // Get the sort key //
            string key_text = this._Parameters["@KEY"].Expression.Evaluate().valueSTRING;
            Key k = SortKeyFactory.Default.Render(data.Columns, key_text);

            // Sort //
            data.Sort(k);

            // Close //
            if (data.IsBig)
                BinarySerializer.Flush(data);

        }

    }

    public sealed class SystemProcedureShuffle : Procedure
    {

        public SystemProcedureShuffle(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemShuffle, "Randomly shuffles records in a dataset", UseHome, Parameters)
        {

            this._ParametersMap.Add("@TABLE", new ProcedureParameterMetaData("@TABLE!true!dataset!The full table name of the table to shuffle"));
            this._ParametersMap.Add("@SEED", new ProcedureParameterMetaData("@SEED!false!expression!The random seed"));

        }

        public override void Invoke()
        {

            // Get the table //
            DataSet data = this._Parameters["@TABLE"].Data;

            // Get the shuffle seed //
            int seed = CellRandom.RandomSeed;
            if (this._Parameters.Exists("@SEED"))
                seed = (int)this._Parameters["@SEED"].Expression.Evaluate().INT;

            // Shuffle //
            data.Shuffle((int)seed);

            // Close //
            if (data.IsBig)
                BinarySerializer.Flush(data);

        }

    }

    public sealed class SystemProcedureReverse : Procedure
    {

        public SystemProcedureReverse(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemReverse, "Reverses all records in a dataset", UseHome, Parameters)
        {
            this._ParametersMap.Add("@TABLE", new ProcedureParameterMetaData("@TABLE!true!dataset!The full table name of the table to reverse"));
        }

        public override void Invoke()
        {

            // Get the table //
            DataSet data = this._Parameters["@TABLE"].Data;

            // Shuffle //
            data.Reverse();

            // Close //
            if (data.IsBig)
                BinarySerializer.Flush(data);

        }

    }

    public sealed class SystemProcedurePrintTable : Procedure
    {

        public SystemProcedurePrintTable(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemPrintTable, "Prints all records in a table", UseHome, Parameters)
        {
            this._ParametersMap.Add("@TABLE", new ProcedureParameterMetaData("@TABLE!true!dataset!The full table name of the table to print"));
            this._ParametersMap.Add("@COUNT", new ProcedureParameterMetaData("@COUNT!false!expression!The max count of records to print; if blank, it will print all records"));
        }

        public override void Invoke()
        {

            // Get the table //
            DataSet data = this._Parameters["@TABLE"].Data;

            // Get the count //
            long count = (long)int.MaxValue;
            if (this._Parameters.Exists("@COUNT"))
                count = this._Parameters["@COUNT"].Expression.Evaluate().INT;

            // Print all the records //
            RecordReader rr = data.OpenReader();
            long Ticks = 0;

            // Message the headers //
            Record r = new Record(new Cell[] { new Cell(data.Columns.ToNameString()) });
            this._Home.IO.AppendBuffer(r);

            // Message the affinities //
            r = new Record(new Cell[] { new Cell(data.Columns.ToAffinityString()) });
            this._Home.IO.AppendBuffer(r);

            // Message the data //
            while (!rr.EndOfData && Ticks < count)
            {
                Ticks++;
                this._Home.IO.AppendBuffer(rr.ReadNext());

            }

        }

    }

    public sealed class SystemProcedureImportText : Procedure
    {

        public SystemProcedureImportText(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemImportText, "Imports a deliminated flat file into a table", UseHome, Parameters)
        {
            this._ParametersMap.Add("@TABLE", new ProcedureParameterMetaData("@TABLE!true!dataset!The full table name of the table to load"));
            this._ParametersMap.Add("@PATH", new ProcedureParameterMetaData("@PATH!true!expression!The path to the flat file to import"));
            this._ParametersMap.Add("@DELIM", new ProcedureParameterMetaData("@DELIM!false!expression!The deliminator of the file, defaults to TAB"));
            this._ParametersMap.Add("@ESCAPE", new ProcedureParameterMetaData("@ESCAPE!false!expression!The escape character of the string, defaults to null"));
            this._ParametersMap.Add("@SKIPS", new ProcedureParameterMetaData("@SKIPS!false!expression!The number of records in the flat file to skip (like headers), defaults to 0"));
        }

        public override void Invoke()
        {

            // Get the table //
            string file_path = this._Parameters["@PATH"].Expression.Evaluate().valueSTRING;
            char[] delim = {'\t'};
            char escape = char.MinValue;
            int skips = 0;

            // Get the data //
            DataSet data = this._Parameters["@TABLE"].Data;

            if (this._Parameters.Exists("@DELIM"))
                delim = this._Parameters["@DELIM"].Expression.Evaluate().valueSTRING.ToCharArray();
            if (this._Parameters.Exists("@ESCAPE"))
                escape = this._Parameters["@ESCAPE"].Expression.Evaluate().valueSTRING.ToCharArray().First();
            if (this._Parameters.Exists("@SKIPS"))
                skips = (int)this._Parameters["@SKIPS"].Expression.Evaluate().valueINT;

            // Import //
            RecordWriter writer = data.OpenWriter();
            TextSerializer.BufferText
            (
                file_path,
                writer,
                skips,
                delim,
                escape
            );
            writer.Close();

        }

    }

    public sealed class SystemProcedureExportText : Procedure
    {

        public SystemProcedureExportText(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemExportText, "Exports a dataset into a text file", UseHome, Parameters)
        {
            this._ParametersMap.Add("@TABLE", new ProcedureParameterMetaData("@TABLE!true!dataset!The full table name of the table to export"));
            this._ParametersMap.Add("@PATH", new ProcedureParameterMetaData("@PATH!true!expression!The path to the flat file to export to"));
            this._ParametersMap.Add("@DELIM", new ProcedureParameterMetaData("@DELIM!false!expression!The deliminator of the file, defaults to TAB"));

        }

        public override void Invoke()
        {

            // Get the table //
            string file_path = this._Parameters["@PATH"].Expression.Evaluate().valueSTRING;
            char delim = (this._Parameters.Exists("@DELIM") ? this._Parameters["@DELIM"].Expression.Evaluate().valueSTRING.ToCharArray().First() : '\t');

            // Get the data //
            DataSet data = this._Parameters["@TABLE"].Data;

            // Import //
            RecordReader reader = data.OpenReader();
            TextSerializer.FlushText(file_path, reader, delim, true);

        }

    }

    public sealed class SystemProcedureDownload : Procedure
    {

        public SystemProcedureDownload(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemDownload, "Downloads a web page or file to a local file", UseHome, Parameters)
        {
            this._ParametersMap.Add("@URL", new ProcedureParameterMetaData("@URL!true!expression!The URL to download"));
            this._ParametersMap.Add("@PATH", new ProcedureParameterMetaData("@PATH!true!expression!The path to the flat file to export to"));
        }

        public override void Invoke()
        {

            // Get the table //
            string url = this._Parameters["@URL"].Expression.Evaluate().valueSTRING;
            string file_path = this._Parameters["@PATH"].Expression.Evaluate().valueSTRING;

            Friesian.WebQuery.TryProcessWebRequest(url, file_path);

        }

    }

    public sealed class SystemProcedureDebugAST : Procedure
    {

        public SystemProcedureDebugAST(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemDebugAST, "Prints the abstract syntax tree of a given expression", UseHome, Parameters)
        {
            this._ParametersMap.Add("@EXPRESSION", new ProcedureParameterMetaData("@EXPRESSION!true!expression!The expression to decompile"));
        }

        public override void Invoke()
        {

            // Get the table //
            string t = FNodeAnalysis.Tree(this._Parameters["@EXPRESSION"].Expression);
            this.Home.IO.AppendBuffer(this._Parameters["@EXPRESSION"].Expression.Unparse(null));
            this.Home.IO.AppendBuffer(t);

        }

    }

    public sealed class SystemProcedureDropTable : Procedure
    {

        public SystemProcedureDropTable(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemDropTable, "Remove an in memory table from memory or drops a static table from disk", UseHome, Parameters)
        {
            this._ParametersMap.Add("@TABLE", new ProcedureParameterMetaData("@TABLE!true!expression!The full table name of the table to drop"));

        }

        public override void Invoke()
        {

            DataSet t = this._Parameters["@TABLE"].Data;
            if (t.IsBig)
                DataSetManager.DropData(t);
            else
                this._Home.ChunkHeap.Deallocate(t.Name);

        }

    }

    public sealed class SystemProcedureDeallocate : Procedure
    {

        public SystemProcedureDeallocate(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemDeallocate, "Deallocates a scalar from memory", UseHome, Parameters)
        {
            this._ParametersMap.Add("@SCALAR", new ProcedureParameterMetaData("@SCALAR!true!expression!The name of scalar variable to deallocate"));
        }

        public override void Invoke()
        {

            string name = this._Parameters["@SCALAR"].Expression.Evaluate().valueSTRING;
            this._Home.ChunkHeap.Deallocate(name);
            this._Home.GlobalHeap.Arrays.Deallocate(name);
            this._Home.GlobalHeap.Scalars.Deallocate(name);
            
        }

    }

    public sealed class SystemProcedureMemoryDump : Procedure
    {

        public SystemProcedureMemoryDump(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemMemoryDump, "Prints all objects in memory", UseHome, Parameters)
        {
        }

        public override void Invoke()
        {
            
            // Dump Connections //
            this.Home.IO.AppendBuffer(":: Connections ::");
            foreach (KeyValuePair<string, string> kv in this._Home.Connections.Entries)
                this.Home.IO.AppendBuffer("\t{0} : {1}", kv.Key, kv.Value);

            // Dump Connections //
            this.Home.IO.AppendBuffer(":: Chunks ::");
            foreach (KeyValuePair<string, RecordSet> kv in this._Home.ChunkHeap.Entries)
                this.Home.IO.AppendBuffer("\t{0} : {1}", kv.Key, kv.Value.Name);

            // Dump Matricies //
            this.Home.IO.AppendBuffer(":: Matrix ::");
            foreach (KeyValuePair<string, Gidran.CellMatrix> kv in this._Home.GlobalHeap.Arrays.Entries)
                this.Home.IO.AppendBuffer("\t{0} : [{1},{2}] {3}", kv.Key, kv.Value.RowCount, kv.Value.ColumnCount, kv.Value.Affinity);

            // Dump Scalars //
            this.Home.IO.AppendBuffer(":: Scalars ::");
            foreach (KeyValuePair<string, Cell> kv in this._Home.GlobalHeap.Scalars.Entries)
                this.Home.IO.AppendBuffer("\t{0} : {1}", kv.Key, kv.Value.Affinity);


        }

    }

    public sealed class SystemProcedureToTable : Procedure
    {

        public SystemProcedureToTable(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemToTable, "Converts a global table to a static table", UseHome, Parameters)
        {
            this._ParametersMap.Add("@CHUNK", new ProcedureParameterMetaData("@CHUNK!true!expression!The name of the chunk to covert"));
            this._ParametersMap.Add("@ALIAS", new ProcedureParameterMetaData("@ALIAS!true!expression!The alias of the datastore to export to"));
            this._ParametersMap.Add("@NAME", new ProcedureParameterMetaData("@NAME!true!expression!The name of the new table to export"));
        }

        public override void Invoke()
        {

            // Get the chunk //
            string c_name = this._Parameters["@CHUNK"].Expression.Evaluate().valueSTRING;
            RecordSet c = this._Home.ChunkHeap[c_name];

            // Get the table //
            string db_name = this._Parameters["@ALIAS"].Expression.Evaluate().valueSTRING;
            string t_name = this._Parameters["@NAME"].Expression.Evaluate().valueSTRING;
            Table t = new Table(this.Home.Connections[db_name], t_name, c.Columns, c.MaxRecords);

            // Union //
            t.Union(c);
            BinarySerializer.Flush(t);

        }

    }

    public sealed class SystemProcedureDebugLambda : Procedure
    {

        public SystemProcedureDebugLambda(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemDebugLambda, "Debugs a lambda already in the heap", UseHome, Parameters)
        {
            this._ParametersMap.Add("@LAMBDA", new ProcedureParameterMetaData("@LAMBDA!true!expression!The name of the lambda to debug"));
        }

        public override void Invoke()
        {

            // Get the chunk //
            string c_name = this._Parameters["@LAMBDA"].Expression.Evaluate().valueSTRING;
            if (this._Home.Lambdas.Exists(c_name))
            {
                this._Home.IO.AppendBuffer(this._Home.Lambdas[c_name].InnerNode.Unparse(null));
            }
            else
            {
                this._Home.IO.AppendBuffer("Lambda '{0}' not found", c_name);
            }


        }

    }

    public sealed class SystemProcedureIO : Procedure
    {

        public SystemProcedureIO(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_SystemIO, "Toggles the communicator's IO on or off", UseHome, Parameters)
        {
            this._ParametersMap.Add("@IO", new ProcedureParameterMetaData("@IO!true!expression!True (turns io on) or false (turns io off)"));
        }

        public override void Invoke()
        {

            // Get the chunk //
            this._Home.SupressIO = !this._Parameters["@IO"].Expression.Evaluate().valueBOOL;
            
        }

    }

    // File Procedures //
    public sealed class FileProcedureFileMove : Procedure
    {

        public FileProcedureFileMove(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_FileMove, "Moves a file from one location to another", UseHome, Parameters)
        {
            this._ParametersMap.Add("@PATH", new ProcedureParameterMetaData("@PATH!true!expression!The path of the file to move"));
            this._ParametersMap.Add("@NEW_PATH", new ProcedureParameterMetaData("@NEW_PATH!true!expression!The path of the new file name"));
        }

        public override void Invoke()
        {

            // Get the files //
            string original_file_name = this._Parameters["@PATH"].Expression.Evaluate().valueSTRING;
            string new_file_name = this._Parameters["@NEW_PATH"].Expression.Evaluate().valueSTRING;

            // Check to see if just a directory was passed as 'new_file_name' //
            if (Directory.Exists(new_file_name))
            {
                if (new_file_name.Last() != '\\')
                    new_file_name += '\\';
                FileInfo fi = new FileInfo(original_file_name);
                new_file_name += fi.Name;
            }

            // move //
            File.Move(original_file_name, new_file_name);

        }

    }

    public sealed class FileProcedureFileCopy : Procedure
    {

        public FileProcedureFileCopy(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_FileCopy, "Copies a file from one location to another", UseHome, Parameters)
        {
            this._ParametersMap.Add("@PATH", new ProcedureParameterMetaData("@PATH!true!expression!The path of the file to move"));
            this._ParametersMap.Add("@NEW_PATH", new ProcedureParameterMetaData("@NEW_PATH!true!expression!The path of the new file name"));
        }

        public override void Invoke()
        {

            // Get the files //
            string original_file_name = this._Parameters["@PATH"].Expression.Evaluate().valueSTRING;
            string new_file_name = this._Parameters["@NEW_PATH"].Expression.Evaluate().valueSTRING;

            // Check to see if just a directory was passed as 'new_file_name' //
            if (Directory.Exists(new_file_name))
            {
                if (new_file_name.Last() != '\\')
                    new_file_name += '\\';
                FileInfo fi = new FileInfo(original_file_name);
                new_file_name += fi.Name;
            }

            // move //
            File.Copy(original_file_name, new_file_name, true);

        }

    }

    public sealed class FileProcedureFileDelete : Procedure
    {

        public FileProcedureFileDelete(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_FileDelete, "Deletes a file", UseHome, Parameters)
        {
            this._ParametersMap.Add("@PATH", new ProcedureParameterMetaData("@PATH!true!expression!The path of the file to delete"));
        }

        public override void Invoke()
        {

            // Get the files //
            string original_file_name = this._Parameters["@PATH"].Expression.Evaluate().valueSTRING;

            // Check if it exists, then delete; do NOT throw an exception if it doesnt exist //
            if (File.Exists(original_file_name))
                File.Delete(original_file_name);
        }

    }

    public sealed class FileProcedureFileUnZip : Procedure
    {

        public FileProcedureFileUnZip(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_FileUnzip, "Unzips a file", UseHome, Parameters)
        {
            this._ParametersMap.Add("@ARC_PATH", new ProcedureParameterMetaData("@ARC_PATH!true!expression!The path of the archive"));
            this._ParametersMap.Add("@DIR_PATH", new ProcedureParameterMetaData("@DIR_PATH!true!expression!The path of a new directory to export to"));
        }

        public override void Invoke()
        {

            // Get the files //
            string archive_path = this._Parameters["@ARC_PATH"].Expression.Evaluate().valueSTRING;
            string dir_name = this._Parameters["@DIR_PATH"].Expression.Evaluate().valueSTRING;

            if (Directory.Exists(dir_name))
                Directory.Delete(dir_name, true);
            System.IO.Compression.ZipFile.ExtractToDirectory(archive_path, dir_name);

        }

    }

    public sealed class FileProcedureFileZip : Procedure
    {

        public FileProcedureFileZip(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_FileZip, "Zips a file or directory", UseHome, Parameters)
        {
            this._ParametersMap.Add("@ARC_PATH", new ProcedureParameterMetaData("@ARC_PATH!true!expression!The path of the archive"));
            this._ParametersMap.Add("@DIR_PATH", new ProcedureParameterMetaData("@DIR_PATH!true!expression!The path of a new directory to export to"));
        }

        public override void Invoke()
        {

            // Get the files //
            string archive_path = this._Parameters["@ARC_PATH"].Expression.Evaluate().valueSTRING;
            
            using (System.IO.Compression.ZipArchive arc = new System.IO.Compression.ZipArchive(File.Open(archive_path, FileMode.CreateNew)))
            {
                string path = this._Parameters["@DIR_PATH"].Expression.Evaluate().valueSTRING;
                arc.CreateEntry(path, System.IO.Compression.CompressionLevel.Optimal);
            }

        }

    }

    // Miner //
    public sealed class MinerLinearModel : Procedure
    {

        public MinerLinearModel(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_MinerLM, "Fits a linear model using ordinary least squares", UseHome, Parameters)
        {

            this._ParametersMap.Add("@NAME", new ProcedureParameterMetaData("@NAME!true!expression!The name of the model"));
            this._ParametersMap.Add("@INPUT", new ProcedureParameterMetaData("@INPUT!true!expressionset!The explanatory variables"));
            this._ParametersMap.Add("@OUTPUT", new ProcedureParameterMetaData("@OUTPUT!true!expression!The model variable"));
            this._ParametersMap.Add("@DATA", new ProcedureParameterMetaData("@DATA!true!dataset!The calibration dataset"));
            this._ParametersMap.Add("@WEIGHT", new ProcedureParameterMetaData("@WEIGHT!false!expression!The weight variable"));
            this._ParametersMap.Add("@WHERE", new ProcedureParameterMetaData("@WHERE!false!expression!The data filter"));
            this._ParametersMap.Add("@PARTITIONS", new ProcedureParameterMetaData("@PARTITIONS!false!expression!The number of partitions to run"));

        }

        public override void Invoke()
        {

            // Get data!!!! //
            string name = this._Parameters["@NAME"].Expression.Evaluate().valueSTRING;
            DataSet data = this._Parameters["@DATA"].Data;
            FNodeSet x = this._Parameters["@INPUT"].ExpressionSet;
            FNode y = this._Parameters["@OUTPUT"].Expression;

            FNode w = new FNodeValue(null, new Cell(1D));
            if (this._Parameters.Exists("@WEIGHT"))
                w = this._Parameters["@WEIGHT"].Expression;

            Predicate p = Predicate.TrueForAll;
            if (this._Parameters.Exists("@WHERE"))
                p = new Predicate(this._Parameters["@WHERE"].Expression);

            int threads = 1;
            if (this._Parameters.Exists("@PARTITIONS"))
                threads = (int)this._Parameters["@PARTITIONS"].Expression.Evaluate().valueINT;

            // Model //
            Thoroughbred.ARizenTalent.LinearRegression lm = new Thoroughbred.ARizenTalent.LinearRegression(name, data, p, y, x, w);
            if (threads == 1)
            {
                lm.Render();
            }
            else
            {
                lm.PartitionedRender(threads);
            }

            // Push the model to the home heap //
            //RecordSet rs1 = lm.ParameterData;
            //RecordSet rs2 = lm.ModelData;
            //this.Home.ChunkHeap.Reallocate(rs1.Name, rs1);
            //this.Home.ChunkHeap.Reallocate(rs2.Name, rs2);
            this.Home.Models.Reallocate(lm.Name, lm);
            this.Home.IO.AppendBuffer(lm.Statistics());

        }

    }

    public sealed class MinerGeneralizedLinearModel : Procedure
    {

        public MinerGeneralizedLinearModel(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_MinerGLM, "Fits a generalized linear model using Fischer's scoring", UseHome, Parameters)
        {

            this._ParametersMap.Add("@NAME", new ProcedureParameterMetaData("@NAME!true!expression!The name of the model"));
            this._ParametersMap.Add("@INPUT", new ProcedureParameterMetaData("@INPUT!true!expressionset!The explanatory variables"));
            this._ParametersMap.Add("@OUTPUT", new ProcedureParameterMetaData("@OUTPUT!true!expression!The model variable"));
            this._ParametersMap.Add("@DATA", new ProcedureParameterMetaData("@DATA!true!expressionset!The calibration dataset"));
            this._ParametersMap.Add("@LINK", new ProcedureParameterMetaData("@LINK!true!lambda!The link function"));
            this._ParametersMap.Add("@WEIGHT", new ProcedureParameterMetaData("@WEIGHT!false!expression!The weight variable"));
            this._ParametersMap.Add("@WHERE", new ProcedureParameterMetaData("@WEIGHT!false!expression!The data filter"));
            this._ParametersMap.Add("@MAXITT", new ProcedureParameterMetaData("@MAXITT!false!expression!The maximum itterations the model will step"));

        }

        public override void Invoke()
        {

            // Get data!!!! //
            string name = this._Parameters["@NAME"].Expression.Evaluate().valueSTRING;
            DataSet data = this._Parameters["@DATA"].Data;
            FNodeSet x = this._Parameters["@INPUT"].ExpressionSet;
            FNode y = this._Parameters["@OUTPUT"].Expression;
            FNode w = new FNodeValue(null, new Cell(1D));
            if (this._Parameters.Exists("@WEIGHT"))
                w = this._Parameters["@WEIGHT"].Expression;
            Lambda link = this._Parameters["@LINK"].Lambda;
            int MaxITT = 10;
            if (this._Parameters.Exists("@MAXITT"))
                MaxITT = (int)this._Parameters["@MAXITT"].Expression.Evaluate().valueINT;
            Predicate p = Predicate.TrueForAll;
            if (this._Parameters.Exists("@WHERE"))
                p = new Predicate(this._Parameters["@WHERE"].Expression);

            // Model //
            Thoroughbred.ARizenTalent.GeneralizedLinearModel glm = new Thoroughbred.ARizenTalent.GeneralizedLinearModel(name, data, p, y, x, w, link);
            glm.MaximumIterations = MaxITT;
            glm.Render();

            // Push the model to the home heap //
            //RecordSet rs1 = glm.ParameterData;
            //RecordSet rs2 = glm.ModelData;
            //this.Home.ChunkHeap.Reallocate(rs1.Name, rs1);
            //this.Home.ChunkHeap.Reallocate(rs2.Name, rs2);
            //this.Home.Lambdas.Reallocate(glm.Name + "_LINK", link);
            this.Home.Models.Reallocate(glm.Name, glm);

        }

    }

    public sealed class MinerNonLinearModel : Procedure
    {

        public MinerNonLinearModel(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_MinerLM, "Fits a non-linear model using the Levenberg-Marquadt algorithm", UseHome, Parameters)
        {

            this._ParametersMap.Add("@NAME", new ProcedureParameterMetaData("@NAME!true!expression!The name of the model"));
            this._ParametersMap.Add("@INPUT", new ProcedureParameterMetaData("@INPUT!true!expression!The explanatory equation"));
            this._ParametersMap.Add("@OUTPUT", new ProcedureParameterMetaData("@OUTPUT!true!expression!The model variable"));
            this._ParametersMap.Add("@DATA", new ProcedureParameterMetaData("@DATA!true!expressionset!The calibration dataset"));
            this._ParametersMap.Add("@WEIGHT", new ProcedureParameterMetaData("@WEIGHT!false!expression!The weight variable"));
            this._ParametersMap.Add("@WHERE", new ProcedureParameterMetaData("@WEIGHT!false!expression!The data filter"));
            this._ParametersMap.Add("@SCALE", new ProcedureParameterMetaData("@SCALE!false!expression!The starting scale"));
            this._ParametersMap.Add("@MAXITT", new ProcedureParameterMetaData("@MAXITT!false!expression!The maximum itterations the model will step"));
            this._ParametersMap.Add("@STRICT", new ProcedureParameterMetaData("@STRICT!false!expression!True: Trace(Design) * Scale, False: I * Scale"));
            this._ParametersMap.Add("@BETA", new ProcedureParameterMetaData("@BETA!false!matrix!The initial weights"));

        }

        public override void Invoke()
        {

            // Get data!!!! //
            string name = this._Parameters["@NAME"].Expression.Evaluate().valueSTRING;
            DataSet data = this._Parameters["@DATA"].Data;
            FNode x = this._Parameters["@INPUT"].Expression;
            FNode y = this._Parameters["@OUTPUT"].Expression;
            FNode w = new FNodeValue(null, new Cell(1D));
            if (this._Parameters.Exists("@WEIGHT"))
                w = this._Parameters["@WEIGHT"].Expression;
            Cell scale = new Cell(10D);
            if (this._Parameters.Exists("@SCALE"))
                scale = this._Parameters["@SCALE"].Expression.Evaluate();
            int MaxITT = 10;
            if (this._Parameters.Exists("@MAXITT"))
                MaxITT = (int)this._Parameters["@MAXITT"].Expression.Evaluate().valueINT;
            bool Strict = false;
            if (this._Parameters.Exists("@STRICT"))
                Strict = this._Parameters["@STRICT"].Expression.Evaluate().valueBOOL;
            Predicate p = Predicate.TrueForAll;
            if (this._Parameters.Exists("@WHERE"))
                p = new Predicate(this._Parameters["@WHERE"].Expression);

            // Model //
            Thoroughbred.ARizenTalent.NonlinearRegressionModel nlm = new Thoroughbred.ARizenTalent.NonlinearRegressionModel(name, data, p, y, x, w);
            nlm.Scale = scale;
            nlm.MaximumIterations = MaxITT;
            nlm.IsStrict = Strict;
            if (this._Parameters.Exists("@BETA"))
                nlm.Beta = this._Parameters["@BETA"].Matrix.Evaluate().ToVector;

            // Fit //
            nlm.Render();

            // Push the model to the home heap //
            //RecordSet rs1 = nlm.ParameterData;
            //RecordSet rs2 = nlm.ModelData;
            //this.Home.ChunkHeap.Reallocate(rs1.Name, rs1);
            //this.Home.ChunkHeap.Reallocate(rs2.Name, rs2);
            this.Home.Models.Reallocate(nlm.Name, nlm);

        }

    }

    public sealed class MinerRowCluster : Procedure
    {

        public MinerRowCluster(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_MinerRC, "Clusters similar rows using a modified k-means algorithm", UseHome, Parameters)
        {

            this._ParametersMap.Add("@NAME", new ProcedureParameterMetaData("@NAME!true!expression!The name of the model"));
            this._ParametersMap.Add("@INPUT", new ProcedureParameterMetaData("@INPUT!true!expressionset!The explanatory equation"));
            this._ParametersMap.Add("@DATA", new ProcedureParameterMetaData("@DATA!true!expressionset!The calibration dataset"));
            this._ParametersMap.Add("@CLUSTERS", new ProcedureParameterMetaData("@CLUSTERS!true!expression!The weight variable"));
            this._ParametersMap.Add("@WEIGHT", new ProcedureParameterMetaData("@WEIGHT!false!expression!The weight variable"));
            this._ParametersMap.Add("@WHERE", new ProcedureParameterMetaData("@WHERE!false!expression!The number of data clusters"));
            this._ParametersMap.Add("@MAXITT", new ProcedureParameterMetaData("@MAXITT!false!expression!The maximum itterations the model will step"));

        }

        public override void Invoke()
        {

            // Get data!!!! //

            string name = this._Parameters["@NAME"].Expression.Evaluate().valueSTRING;

            DataSet data = this._Parameters["@DATA"].Data;

            FNodeSet x = this._Parameters["@INPUT"].ExpressionSet;

            int Clusters = (int)this._Parameters["@CLUSTERS"].Expression.Evaluate().INT;

            FNode w = new FNodeValue(null, new Cell(1D));
            if (this._Parameters.Exists("@WEIGHT"))
                w = this._Parameters["@WEIGHT"].Expression;

            int MaxITT = 25;
            if (this._Parameters.Exists("@MAXITT"))
                MaxITT = (int)this._Parameters["@MAXITT"].Expression.Evaluate().valueINT;

            Predicate p = Predicate.TrueForAll;
            if (this._Parameters.Exists("@WHERE"))
                p = new Predicate(this._Parameters["@WHERE"].Expression);

            // Model //
            Thoroughbred.Seabiscut.RowCluster clus = new Thoroughbred.Seabiscut.RowCluster(name, data, p, x, w, Clusters);
            clus.MaxItterations = MaxITT;

            // Fit //
            clus.Render();

            // Push the model to the home heap //
            this.Home.Models.Reallocate(clus.Name, clus);

            // Append the notificiations //
            this.Home.IO.AppendBuffer(clus.Statistics());

        }

    }

    public sealed class MinerFFNeural : Procedure
    {

        public MinerFFNeural(Workspace UseHome, HParameterSet Parameters)
            : base(SystemProcedures.Name_MinerFFNN, "Fits a linear model using ordinary least squares", UseHome, Parameters)
        {

            this._ParametersMap.Add("@NAME", new ProcedureParameterMetaData("@NAME!true!expression!The name of the model"));
            this._ParametersMap.Add("@INPUT", new ProcedureParameterMetaData("@INPUT!true!expressionset!The explanatory variables"));
            this._ParametersMap.Add("@OUTPUT", new ProcedureParameterMetaData("@OUTPUT!true!expressionset!The model variables"));
            this._ParametersMap.Add("@OUTACT", new ProcedureParameterMetaData("@OUTACT!true!expression!The output activation function"));
            this._ParametersMap.Add("@RULE", new ProcedureParameterMetaData("@RULE!ftrue!expression!The training rule"));
            
            this._ParametersMap.Add("@DATA", new ProcedureParameterMetaData("@DATA!true!expressionset!The calibration dataset"));
            this._ParametersMap.Add("@WHERE", new ProcedureParameterMetaData("@WHERE!false!expression!The filter to apply to the data"));
            
            this._ParametersMap.Add("@HIDDENMAP", new ProcedureParameterMetaData("@HIDDEN1!false!expression!A string that represents the hidden layers"));

        }

        public override void Invoke()
        {

            // Get the name //
            string name = this._Parameters["@NAME"].Expression.Evaluate().valueSTRING;

            // Get the input data and any filter //
            DataSet data = this._Parameters["@DATA"].Data;
            Predicate where = Predicate.TrueForAll;
            if (this._Parameters.Exists("@WHERE"))
                where = new Predicate(this._Parameters["@WHERE"].Expression);

            // Get inputs and outputs //
            FNodeSet inputs = this._Parameters["@INPUT"].ExpressionSet;
            FNodeSet outputs = this._Parameters["@OUTPUT"].ExpressionSet;

            // Get the output activation and learning rule //
            Numerics.ScalarFunction out_act = Numerics.ScalarFunctionFactory.Select(this._Parameters["@OUTACT"].Expression.Evaluate().valueSTRING);
            string rule = this._Parameters["@RULE"].Expression.Evaluate().valueSTRING;

            // Get the hidden layer mapping string //
            string hidden_map = this._Parameters["@HIDDENMAP"].Expression.Evaluate().valueSTRING;

            // Build the factor //
            Thoroughbred.ManOWar.NeuralNetworkFactory factory = new Thoroughbred.ManOWar.NeuralNetworkFactory(name, data, where);
            
            // Add the data layer (inputs) and prediction layer (outputs) //
            factory.AddDataLayer(true, inputs);
            factory.AddPredictionLayer(outputs, out_act);

            // Set the training rule and output activation //
            factory.DefaultRule = rule;
            factory.DefaultActivator = out_act;

            /* Hidden layer
             * 
             * The map string should have the form:
             * 'T1;T2;...;Tn'
             * where each T represents one layer, and has the form:
             *  Ti = 'ActivationFunction as string,Bias as bool, size as int'
             * 
             */
            string[] layers = hidden_map.Split(';');
            foreach (string hmap in layers)
            {

                string[] elements = hmap.Split(',');
                Numerics.ScalarFunction activator = Numerics.ScalarFunctionFactory.Select(elements[0]);
                bool bias_questionmark = (StringComparer.OrdinalIgnoreCase.Compare(elements[1], bool.TrueString) == 0);
                int size = int.Parse(elements[2]);

                factory.AddHiddenLayer(bias_questionmark, size, activator);

            }

            // Construct the network //
            Thoroughbred.ManOWar.NeuralNetwork ffnn = factory.Construct();

            // Render //
            ffnn.Render();

            // Push the result to the output stack //
            this._Home.Models.Reallocate(name, ffnn);

            // Push the statistics //
            this._Home.IO.AppendBuffer(ffnn.Statistics());

        }

    }

}
