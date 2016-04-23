using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Equus.Shire;
using Equus.Horse;
using Equus.Andalusian;
using Equus.Calabrese;

namespace Equus.FileIO
{

    public sealed class FileOjbectLibrary
    {

        private CellHeap _heap;

        public FileOjbectLibrary(CellHeap Heap)
        {
            this._heap = Heap;
        }

        public void Create(string Path)
        {
            using (FileStream t = File.Create(Path))
            {
            }

        }

        public void Delete(string Path)
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }

        public void Copy(string FromPath, string ToPath)
        {
            File.Copy(FromPath, ToPath, true);
        }

        public void Move(string FromPath, string ToPath)
        {
            File.Move(FromPath, ToPath);
        }

        public void Unzip(string FromPath, string ToPath)
        {
            if (Directory.Exists(ToPath))
            {
                DirectoryInfo di = new DirectoryInfo(ToPath);
                foreach (FileInfo fi in di.GetFiles())
                {
                    fi.Delete();
                }
                di.Delete(true);
            }
            System.IO.Compression.ZipFile.ExtractToDirectory(FromPath, ToPath);
        }

        public void Zip(string FromPath, string ToPath)
        {
            if (File.Exists(ToPath))
                File.Delete(ToPath);
            System.IO.Compression.ZipFile.CreateFromDirectory(FromPath, ToPath);
        }

        public void ReadAllBytes(string FromPath, string HeapRef)
        {
            byte[] b = File.ReadAllBytes(FromPath);
            if (!this._heap.Exists(HeapRef))
                this._heap.Allocate(HeapRef, Cell.NULL_BLOB);
            this._heap[HeapRef] = new Cell(b);
        }

        public void ReadAllText(string FromPath, string HeapRef)
        {
            string s = File.ReadAllText(FromPath);
            if (!this._heap.Exists(HeapRef))
                this._heap.Allocate(HeapRef, Cell.NULL_STRING);
            this._heap[HeapRef] = new Cell(s);
        }

        public void WriteAllBytes(string ToPath, byte[] Value)
        {
            File.WriteAllBytes(ToPath, Value);
        }

        public void WriteAllText(string ToPath, string Value)
        {
            File.WriteAllText(ToPath, Value);
        }

        public void AppendAllBytes(string ToPath, byte[] Value)
        {
            using (FileStream f = File.OpenRead(ToPath))
            {
                f.Write(Value, 0, Value.Length);
            }
        }

        public void AppendAllText(string ToPath, string Value)
        {
            File.AppendAllText(ToPath, Value);
        }

        public sealed class FilePlan : QuarterHorse.CommandPlan
        {

            private int _id = -1;
            private FNodeSet _parameters;
            private FileOjbectLibrary _fol;
            private Dictionary<string, int> _id_table = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                {"CREATE",0},
                {"DELETE",1},
                {"COPY", 2},
                {"MOVE", 3},
                {"READ_ALL_TEXT",4},
                {"READ_ALL_BYTES", 5},
                {"WRITE_ALL_TEXT",6},
                {"WRITE_ALL_BYTES", 7},
                {"APPEND_TEXT", 8},
                {"APPEND_BYTES", 9},
                {"ZIP", 10},
                {"UNZIP", 11},
            };

            public FilePlan(int ID, FNodeSet Parameters, FileOjbectLibrary FOL)
                :base()
            {
                this.Name = "FILE";
                this._id = ID;
                this._parameters = Parameters;
                this._fol = FOL;
            }

            public FilePlan(string CommandName, FNodeSet Parameters, FileOjbectLibrary FOL)
                : this(-1, Parameters, FOL)
            {
                if (!this._id_table.ContainsKey(CommandName))
                    throw new ArgumentException(string.Format("Command does not exist '{0}'"));
                this._id = this._id_table[CommandName];
            }

            public FilePlan(int ID, FNodeSet Parameters, CellHeap Heap)
                : this(ID, Parameters, new FileOjbectLibrary(Heap))
            {
            }

            public FilePlan(string CommandName, FNodeSet Parameters, CellHeap Heap)
                : this(CommandName, Parameters, new FileOjbectLibrary(Heap))
            {
            }

            public override void Execute()
            {

                Record rec = this._parameters.Evaluate();

                switch (this._id)
                {
                    case 0:
                        this._fol.Create(rec[0].valueSTRING);
                        return;
                    case 1:
                        this._fol.Delete(rec[0].valueSTRING);
                        return;
                    case 2:
                        this._fol.Copy(rec[0].valueSTRING, rec[1].valueSTRING);
                        return;
                    case 3:
                        this._fol.Move(rec[0].valueSTRING, rec[1].valueSTRING);
                        return;
                    case 4:
                        this._fol.ReadAllText(rec[0].valueSTRING, rec[1].valueSTRING);
                        return;
                    case 5:
                        this._fol.ReadAllBytes(rec[0].valueSTRING, rec[1].valueSTRING);
                        return;
                    case 6:
                        this._fol.WriteAllText(rec[0].valueSTRING, rec[1].valueSTRING);
                        return;
                    case 7:
                        this._fol.WriteAllBytes(rec[0].valueSTRING, rec[1].valueBLOB);
                        return;
                    case 8:
                        this._fol.AppendAllText(rec[0].valueSTRING, rec[1].valueSTRING);
                        return;
                    case 9:
                        this._fol.AppendAllBytes(rec[0].valueSTRING, rec[1].valueBLOB);
                        return;
                    case 10:
                        this._fol.Zip(rec[0].valueSTRING, rec[1].valueSTRING);
                        return;
                    case 11:
                        this._fol.Unzip(rec[0].valueSTRING, rec[1].valueSTRING);
                        return;
                }

            }

        }

    }

}
