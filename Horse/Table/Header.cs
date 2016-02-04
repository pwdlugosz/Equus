using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Horse
{

    public enum HeaderType : byte
    {
        Table = 0,
        Fragment = 1,
        Union = 2,
        Big = 3
    }

    public sealed class Header : Record
    {

        public const int OFFSET_NAME = 0;
        public const int OFFSET_ID = 1;
        public const int OFFSET_DIRECTORY = 2;
        public const int OFFSET_EXTENSION = 3;
        public const int OFFSET_COLUMN_COUNT = 4;
        public const int OFFSET_RECORD_COUNT = 5;
        public const int OFFSET_TIME_STAMP = 6;
        public const int OFFSET_KEY_COUNT = 7;
        public const int OFFSET_MAX_RECORDS = 8;
        public const int OFFSET_TYPE = 9;
        
        public const int RECORD_LEN = 10;

        private const char DOT = '.';
        internal const string DEFAULT_EXTENSION = "equus";
        private const string SCHEMA_TEXT = "name string.64, id int, dir string.1024, extension string.64, columns int, records int, time date, keys int, max_records int, type string.64";

        // Constructor //
        public Header(string NewDirectory, string NewName, long NewID, long ColumnCount, long RecordCount, long KeyCount, long NewMaxCount, HeaderType Type)
            :base(RECORD_LEN)
        {

            // Fix the directory //
            if (NewDirectory.Last() != '\\') NewDirectory = NewDirectory.Trim() + '\\';

            // Fix the name //
            if (Name.Contains(DOT) == true) Name = Name.Split(DOT)[0].Trim();

            this[OFFSET_NAME] = new Cell(NewName);
            this[OFFSET_ID] = new Cell(NewID);
            this[OFFSET_DIRECTORY] = new Cell(NewDirectory);
            this[OFFSET_EXTENSION] = new Cell(DEFAULT_EXTENSION);
            this[OFFSET_COLUMN_COUNT] = new Cell(ColumnCount);
            this[OFFSET_RECORD_COUNT] = new Cell(RecordCount);
            this[OFFSET_TIME_STAMP] = new Cell(DateTime.Now);
            this[OFFSET_KEY_COUNT] = new Cell(KeyCount);
            this[OFFSET_MAX_RECORDS] = new Cell(NewMaxCount);
            this[OFFSET_TYPE] = new Cell((byte)Type);

        }

        public Header(string NewDirectory, string NewName, long NewID, RecordSet Data, HeaderType Type)
            : this(NewDirectory, NewName, NewID, Data.Columns.Count, Data.Count, Data.SortBy.Count, Data.MaxRecords, Type)
        {
        }

        public Header(Record R)
            :base(RECORD_LEN)
        {
            if (R.Count != RECORD_LEN) throw new Exception("Header-Record supplied has an invalid length");
            this._data = R.BaseArray;
        }

        // Properties //
        public string Path
        {
            get
            {
                return this.Directory + this.Name + DOT + Header.TypeQualifier(this.Type) + this.ID.ToString() + DOT + this.Extension;
            }
        }

        public string Name
        {
            get
            {
                return this[OFFSET_NAME].valueSTRING;
            }
            set
            {
                this[OFFSET_NAME] = new Cell(value);
            }
        }

        public long ID
        {
            get
            {
                return this[OFFSET_ID].INT;
            }
            set
            {
                this[OFFSET_ID] = new Cell(value);
            }
        }

        public string Directory
        {
            get
            {
                return this[OFFSET_DIRECTORY].valueSTRING;
            }
            set
            {
                this[OFFSET_DIRECTORY] = new Cell(value);
            }
        }

        public string Extension
        {
            get
            {
                return this[OFFSET_EXTENSION].valueSTRING;
            }
            set
            {
                this[OFFSET_EXTENSION] = new Cell(value);
            }
        }

        public long ColumnCount
        {
            get
            {
                return this[OFFSET_COLUMN_COUNT].INT;
            }
        }

        public long RecordCount
        {
            get
            {
                return this[OFFSET_RECORD_COUNT].INT;
            }
            set
            {
                this._data[OFFSET_RECORD_COUNT] = new Cell(value);
            }
        }

        public long KeyCount
        {
            get
            {
                return this[OFFSET_KEY_COUNT].INT;
            }
        }

        public DateTime TimeStamp
        {
            get
            {
                return this[OFFSET_TIME_STAMP].valueDATE_TIME;
            }
        }

        public long MaxRecordCount
        {
            get
            {
                return this[OFFSET_MAX_RECORDS].INT;
            }
        }

        public HeaderType Type
        {
            get { return (HeaderType)this[OFFSET_TYPE].INT; }
        }

        public bool Exists
        {
            get 
            { 
                return System.IO.File.Exists(this.Path); 
            }
        }

        // Methods //
        public void Stamp()
        {
            this[OFFSET_TIME_STAMP] = new Cell(DateTime.Now);
        }

        public void Update(RecordSet Data)
        {

            this[OFFSET_MAX_RECORDS] = new Cell(Data.MaxRecords);
            this[OFFSET_RECORD_COUNT] = new Cell(Data.Count);
            this[OFFSET_COLUMN_COUNT] = new Cell(Data.Columns.Count);
            this[OFFSET_KEY_COUNT] = new Cell(Data.SortBy.Count);
            this.Stamp();

        }

        public string ToMetaString()
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Name: " + this.Name);
            sb.AppendLine("ID: " + this.ID);
            sb.AppendLine("Directory: " + this.Directory);
            sb.AppendLine("Extension: " + this.Extension);
            sb.AppendLine("Columns: " + this.ColumnCount.ToString());
            sb.AppendLine("Records: " + this.RecordCount.ToString());
            sb.AppendLine("Keys: " + this.KeyCount.ToString());
            sb.AppendLine("Timestamp: " + this.TimeStamp.ToString());
            sb.AppendLine("Type: " + this.Type.ToString());
            return sb.ToString();
        }

        // Statics //
        public static string FilePath(string Dir, string Name, HeaderType Type)
        {
            Header h = new Header(Dir.Trim(), Name.Trim(), 0, 0, 0, 0, 0, Type);
            return h.Path;
        }

        public static string TempName()
        {
            Guid g = Guid.NewGuid();
            return g.ToString().Replace("-", "");
        }

        public static Header TempHeader(string Dir, RecordSet Data)
        {
            return new Header(Dir, Header.TempName(), 0, Data, HeaderType.Table);
        }

        public static char TypeQualifier(HeaderType T)
        {
            switch (T)
            {
                case HeaderType.Table: return 'T';
                case HeaderType.Fragment: return 'F';
                case HeaderType.Union: return 'U';
                case HeaderType.Big: return 'M';
                default : return 'X';
            }
        }

        public static int DataSize()
        {
            return (new Schema(SCHEMA_TEXT)).DataLength;
        }

        public static int RecordSize()
        {
            return (new Schema(SCHEMA_TEXT)).RecordLength;
        }

    }

}
