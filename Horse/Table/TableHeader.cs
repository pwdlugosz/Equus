using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Horse
{

    public sealed class TableHeader : Record
    {

        public const int OFFSET_NAME = 0;
        public const int OFFSET_SIZE = 1;
        public const int OFFSET_DIRECTORY = 2;
        public const int OFFSET_EXTENSION = 3;
        public const int OFFSET_COLUMN_COUNT = 4;
        public const int OFFSET_RECORD_COUNT = 5;
        public const int OFFSET_TIME_STAMP = 6;
        public const int OFFSET_KEY_COUNT = 7;
        public const int OFFSET_MAX_RECORDS = 8;
        public const int OFFSET_CAST = 9;
        public const int OFFSET_COALLATION = 10;

        public const int RECORD_LEN = 11;

        private const char DOT = '.';
        internal const string DEFAULT_EXTENSION = "equus";

        // Constructor //
        public TableHeader(string NewDirectory, string NewName, long NewSize, long ColumnCount, long RecordCount, long KeyCount, long NewMaxCount)
            : base(RECORD_LEN)
        {

            // Fix the directory //
            if (NewDirectory.Last() != '\\') 
                NewDirectory = NewDirectory.Trim() + '\\';

            // Fix the name //
            if (NewName.Contains(DOT) == true) 
                NewName = NewName.Split(DOT)[0].Trim();

            this[OFFSET_NAME] = new Cell(NewName);
            this[OFFSET_SIZE] = new Cell(NewSize);
            this[OFFSET_DIRECTORY] = new Cell(NewDirectory);
            this[OFFSET_EXTENSION] = new Cell(DEFAULT_EXTENSION);
            this[OFFSET_COLUMN_COUNT] = new Cell(ColumnCount);
            this[OFFSET_RECORD_COUNT] = new Cell(RecordCount);
            this[OFFSET_TIME_STAMP] = new Cell(DateTime.Now);
            this[OFFSET_KEY_COUNT] = new Cell(KeyCount);
            this[OFFSET_MAX_RECORDS] = new Cell(NewMaxCount);

        }

        public TableHeader(string NewDirectory, string NewName, Table Data)
            : this(NewDirectory, NewName, RecordSet.DEFAULT_MAX_RECORD_COUNT, Data.Columns.Count, Data.Count, Data.SortBy.Count, Data.MaxRecords)
        {
        }

        public TableHeader(Record R)
            : base(RECORD_LEN)
        {
            if (R.Count != RECORD_LEN) 
                throw new Exception("Header-Record supplied has an invalid length");
            this._data = R.BaseArray;
        }

        // Properties //
        public string Path
        {
            get
            {
                return this.Directory + this.Name + DOT + "M" + DOT + this.Extension;
            }
        }

        public string Name
        {
            get
            {
                return this[OFFSET_NAME].STRING;
            }
            set
            {
                this[OFFSET_NAME] = new Cell(value);
            }
        }

        public long Size
        {
            get
            {
                return this[OFFSET_SIZE].INT;
            }
            set
            {
                this[OFFSET_SIZE] = new Cell(value);
            }
        }

        public string Directory
        {
            get
            {
                return this[OFFSET_DIRECTORY].STRING;
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
                return this[OFFSET_EXTENSION].STRING;
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
                return this[OFFSET_TIME_STAMP].DATE_TIME;
            }
        }

        public long MaxRecordCount
        {
            get
            {
                return this[OFFSET_MAX_RECORDS].INT;
            }
        }

        public bool Exists
        {
            get
            {
                return System.IO.File.Exists(this.Path);
            }
        }

        public long CastLevel
        {
            get { return this[OFFSET_CAST].INT; }
            set { this._data[OFFSET_CAST] = new Cell(value); }
        }

        public long Coallation
        {
            get { return this[OFFSET_COALLATION].INT; }
            set { this._data[OFFSET_COALLATION] = new Cell(value); }
        }

        // Methods //
        public void Stamp()
        {
            this[OFFSET_TIME_STAMP] = new Cell(DateTime.Now);
        }

        public void Update(Table Data)
        {
            this[OFFSET_MAX_RECORDS] = new Cell(Data.MaxRecords);
            this[OFFSET_SIZE] = new Cell(Data.ExtentCount);
            this[OFFSET_RECORD_COUNT] = new Cell(Data.Count);
            this[OFFSET_COLUMN_COUNT] = new Cell(Data.Columns.Count);
            this[OFFSET_KEY_COUNT] = new Cell(Data.SortBy.Count);
            this.Stamp();
        }

        // Statics //
        public static string FilePath(string Dir, string Name)
        {
            TableHeader h = new TableHeader(Dir.Trim(), Name.Trim(), 0, 0, 0, 0, 0);
            return h.Path;
        }

        public static string TempName()
        {
            return Header.TempName();
        }

    }

}
