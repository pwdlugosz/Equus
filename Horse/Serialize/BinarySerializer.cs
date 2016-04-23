using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Equus.Shire;

namespace Equus.Horse
{

    public static class BinarySerializer
    {

        /*
         * Binary serialization:
         * Cells: affinity, nullness, data (if not null)
         * Records: record count as int, each cell
         * RecordSets:
         *      -- Header
         *      -- Columns (Header has the correct count)
         *      -- Sort key (Header has the correct count)
         *      -- Records (Header has the correct count)
         * 
         */

        private static int _DISK_READS = 0;
        private static int _DISK_WRITES = 0;

        public static int DiskReads
        {
            get { return _DISK_READS; }
        }

        public static int DiskWrites
        {
            get { return _DISK_WRITES; }
        }

        public static int DiskCalls
        {
            get { return _DISK_READS + _DISK_WRITES; }
        }

        public static void ResetCounters()
        {
            _DISK_READS = 0;
            _DISK_WRITES = 0;
        }

        // Read / write cells //
        private static void WriteCell(BinaryWriter Writer, Cell C)
        {

            // Write the affinity //
            Writer.Write((byte)C.AFFINITY);

            // Write nullness //
            Writer.Write(C.NULL);

            // If we are null, then exit
            // for security reasons, we do not want to write any ghost data if the cell is null //
            if (C.IsNull) return;

            // Write data //
            switch (C.Affinity)
            {

                case CellAffinity.BOOL: Writer.Write(C.BOOL);
                    return;

                case CellAffinity.INT: Writer.Write(C.INT);
                    return;

                case CellAffinity.DOUBLE: Writer.Write(C.DOUBLE);
                    return;

                case CellAffinity.DATE_TIME: Writer.Write(C.INT);
                    return;

                case CellAffinity.STRING:
                    Writer.Write(C.STRING.Length);
                    Writer.Write(C.STRING.ToCharArray());
                    return;

                case CellAffinity.BLOB:
                    Writer.Write(C.BLOB.Length);
                    Writer.Write(C.BLOB);
                    return;

            }

        }

        private static Cell ReadCell(BinaryReader Reader)
        {

            // Read the affinity //
            CellAffinity a = (CellAffinity)Reader.ReadByte();

            // Read nullness //
            bool b = (Reader.ReadByte() == 1);

            // If we are null, then exit
            // for security reasons, we do not want to write any ghost data if the cell is null //
            if (b == true)
                return new Cell(a);

            // Write data //
            switch (a)
            {

                case CellAffinity.BOOL: return new Cell(Reader.ReadBoolean());

                case CellAffinity.INT:
                    return new Cell(Reader.ReadInt64());

                case CellAffinity.DOUBLE:
                    return new Cell(Reader.ReadDouble());

                case CellAffinity.DATE_TIME:
                    return new Cell(DateTime.FromBinary(Reader.ReadInt64()));

                case CellAffinity.STRING:
                    int len = Reader.ReadInt32();
                    string s = new string(Reader.ReadChars(len));
                    return new Cell(s, false);

                case CellAffinity.BLOB:
                    int hlen = Reader.ReadInt32();
                    byte[] h = Reader.ReadBytes(hlen);
                    return new Cell(h);

            }
            return new Cell(a);

        }

        // Read / write records //
        private static void WriteRecord(BinaryWriter Writer, Record R)
        {

            // Write count //
            Writer.Write(R.Count);

            // Write each cell //
            for (int i = 0; i < R.Count; i++)
                BinarySerializer.WriteCell(Writer, R[i]);

        }

        private static Record ReadRecord(BinaryReader Reader, int Length)
        {

            // Get count //
            //int n = Reader.ReadInt32();
            Cell[] c = new Cell[Length];

            // Get cells //
            for (int i = 0; i < Length; i++)
                c[i] = BinarySerializer.ReadCell(Reader);

            return new Record(c);

        }

        // Read / write record lists //
        private static void WriteRecords(BinaryWriter Writer, List<Record> Cache)
        {

            // Do NOT write the record count; assume the reader knows what the record count is //
            foreach (Record r in Cache)
                BinarySerializer.WriteRecord(Writer, r);

        }

        private static void WriteHeaders(BinaryWriter Writer, List<Header> Cache)
        {
            List<Record> NewCache = Cache.ConvertAll((x) => { return new Record(x.BaseArray); });
            WriteRecords(Writer, NewCache);
        }

        private static List<Record> ReadRecords(BinaryReader Reader, long Count, int Length)
        {

            // Build a cache //
            List<Record> t = new List<Record>();

            // Loop through //
            for (int i = 0; i < Count; i++)
            {
                t.Add(BinarySerializer.ReadRecord(Reader, Length));
            }

            return t;

        }

        private static List<Header> ReadHeaders(BinaryReader Reader, long Count, int Length)
        {

            // Build a cache //
            List<Header> t = new List<Header>();

            // Loop through //
            for (int i = 0; i < Count; i++)
            {
                t.Add(new Header(BinarySerializer.ReadRecord(Reader, Length)));
            }

            return t;

        }

        // Read / write RecodSets //
        private static void WriteRecordSet(BinaryWriter Writer, RecordSet Data)
        {

            /*
             * Write:
             *      Header
             *      Schema
             *      SortKey
             *      Record Collection
             */

            // Update the data //
            Data.Update();

            // Write header //
            BinarySerializer.WriteRecord(Writer, Data.Header);

            // Write columns //
            BinarySerializer.WriteRecords(Writer, Data.Columns._Cache);

            // Write sort key //
            BinarySerializer.WriteRecord(Writer, Data.SortBy.ToRecord());

            // Write cache //
            BinarySerializer.WriteRecords(Writer, Data._Cache);

        }

        private static RecordSet ReadRecordSet(BinaryReader Reader)
        {

            /*
             * Read:
             *      Header
             *      Schema
             *      SortKey
             *      Record Collection
             */

            // Read header //
            Header h = new Header(BinarySerializer.ReadRecord(Reader, 10));

            // Read schema //
            Schema s = new Schema(BinarySerializer.ReadRecords(Reader, h.ColumnCount, 4));

            // Read key //
            Key k = new Key(BinarySerializer.ReadRecord(Reader, (int)h.KeyCount));

            // Read record cache //
            List<Record> l = BinarySerializer.ReadRecords(Reader, h.RecordCount, s.Count);

            // Return recordset //
            return new RecordSet(s, h, l, k);

        }

        private static void WriteTable(BinaryWriter Writer, Table Data)
        {

            /*
             * Write:
             *      Header
             *      Schema
             *      SortKey
             *      Record Collection
             */

            // Update //
            Data.Update();

            // Write header //
            BinarySerializer.WriteRecord(Writer, Data.Header);

            // Write columns //
            BinarySerializer.WriteRecords(Writer, Data.Columns._Cache);

            // Write sort key //
            BinarySerializer.WriteRecord(Writer, Data.SortBy.ToRecord());

            // Write cache //
            BinarySerializer.WriteRecords(Writer, Data.ReferenceTable._Cache);

        }

        private static Table ReadTable(BinaryReader Reader)
        {

            /*
             * Read:
             *      Header
             *      Schema
             *      SortKey
             *      Record Collection
             */

            // Read header //
            TableHeader h = new TableHeader(BinarySerializer.ReadRecord(Reader, 10));

            // Read schema //
            Schema s = new Schema(BinarySerializer.ReadRecords(Reader, h.ColumnCount, 4));

            // Read key //
            Key k = new Key(BinarySerializer.ReadRecord(Reader, (int)h.KeyCount));

            // Read record cache //
            List<Record> l = BinarySerializer.ReadRecords(Reader, h.Size, 2);

            // Return recordset //
            return new Table(h, s, l, k);

        }

        // Unionsets //
        /*
        public static void FlushRecordUnion(RecordUnion Data)
        {

            // Update the data //
            Data.Update();

            // Create a file stream //
            using (FileStream fs = File.Create(Data.Header.Path))
            {

                // Create a binary stream to write over the data //
                using (BinaryWriter br = new BinaryWriter(fs))
                {

                    // Write the header //
                    BinarySerializer.WriteRecord(br, Data.Header);

                    // Write every record set //
                    foreach (RecordSet t in Data.Sets)
                        BinarySerializer.WriteRecordSet(br, t);

                }
                // End BinaryStream

            }
            // End FileStream

            // Increment writes //
            _DISK_WRITES++;

        }

        public static RecordUnion BufferRecordUnion(string FullPath)
        {

            // Open a stream //
            byte[] b = File.ReadAllBytes(FullPath);

            // Create the union //
            RecordUnion ru;

            // Open a memory stream //
            using (MemoryStream ms = new MemoryStream(b))
            {

                // Open a reader //
                using (BinaryReader br = new BinaryReader(ms))
                {

                    // Read the header //
                    Header h = new Header(BinarySerializer.ReadRecord(br));

                    // Create the union //
                    ru = new RecordUnion(h);

                    // Read all the record sets into memory //
                    for (int i = 0; i < h.RecordCount; i++)
                    {

                        // Read from stream //
                        RecordSet rs = BinarySerializer.ReadRecordSet(br);

                        // Add the recordset to the union //
                        ru.Add(rs.Header.Name, rs);

                    }

                }

            }

            // Increment Reads //
            _DISK_READS++;

            return ru;

        }

        public static RecordUnion BufferRecordUnion(Header H)
        {
            return BufferRecordUnion(H.Path);
        }
        */

        // ---------------------------------------------------------------- //
        private static int WriteCell(byte[] Hash, Cell C, int Location)
        {

            Hash[Location] = (byte)C.AFFINITY;
            Location++;
            Hash[Location] = C.NULL;
            Location++;

            if (C.NULL == 1)
                return Location;

            // Bool //
            if (C.AFFINITY == CellAffinity.BOOL)
            {
                Hash[Location] = (C.BOOL == true ? (byte)1 : (byte)0);
            }

            // BLOB //
            if (C.AFFINITY == CellAffinity.BLOB)
            {

                Hash[Location] = C.B4;
                Hash[Location + 1] = C.B5;
                Hash[Location + 2] = C.B6;
                Hash[Location + 3] = C.B7;
                Location += 4;
                for (int i = 0; i < C.BLOB.Length; i++)
                {
                    Hash[Location + i] = C.BLOB[i];
                }
                return Location + C.BLOB.Length;

            }

            // STRING //
            if (C.AFFINITY == CellAffinity.STRING)
            {

                Hash[Location] = C.B4;
                Hash[Location + 1] = C.B5;
                Hash[Location + 2] = C.B6;
                Hash[Location + 3] = C.B7;
                Location += 4;
                for (int i = 0; i < C.STRING.Length; i++)
                {
                    Hash[Location + i * 2] = (byte)(C.STRING[i]);
                    Hash[Location + i * 2 + 1] = (byte)(C.STRING[i] >> 8);
                }
                return Location + C.STRING.Length * 2;

            }

            // Long, Double, Date //
            Hash[Location] = C.B0;
            Hash[Location + 1] = C.B1;
            Hash[Location + 2] = C.B2;
            Hash[Location + 3] = C.B3;
            Hash[Location + 4] = C.B4;
            Hash[Location + 5] = C.B5;
            Hash[Location + 6] = C.B6;
            Hash[Location + 7] = C.B7;
            return Location + 8;

        }

        private static void WriteCellSafe(BinaryWriter Writer, Cell C)
        {

            // Write the affinity //
            Writer.Write((byte)C.AFFINITY);

            // Write nullness //
            Writer.Write(C.NULL);

            // If we are null, then exit
            // for security reasons, we do not want to write any ghost data if the cell is null //
            if (C.IsNull) 
                return;

            // Bool //
            if (C.AFFINITY == CellAffinity.BOOL)
            {
                Writer.Write(C.BOOL == true ? (byte)1 : (byte)0);
                return;
            }

            // BLOB //
            if (C.AFFINITY == CellAffinity.BLOB)
            {

                C.INT_B = C.BLOB.Length;
                Writer.Write(C.B4);
                Writer.Write(C.B5);
                Writer.Write(C.B6);
                Writer.Write(C.B7);

                for (int i = 0; i < C.BLOB.Length; i++)
                {
                    Writer.Write(C.BLOB[i]);
                }

                return;

            }

            // STRING //
            if (C.AFFINITY == CellAffinity.STRING)
            {

                C.INT_B = C.STRING.Length;
                Writer.Write(C.B4);
                Writer.Write(C.B5);
                Writer.Write(C.B6);
                Writer.Write(C.B7);
                for (int i = 0; i < C.STRING.Length; i++)
                {
                    byte c1 = (byte)(C.STRING[i] >> 8);
                    byte c2 = (byte)(C.STRING[i] & 255);
                    Writer.Write(c1);
                    Writer.Write(c2);
                }
                return;

            }

            // Double, int, date //
            Writer.Write(C.B0);
            Writer.Write(C.B1);
            Writer.Write(C.B2);
            Writer.Write(C.B3);
            Writer.Write(C.B4);
            Writer.Write(C.B5);
            Writer.Write(C.B6);
            Writer.Write(C.B7);

        }

        private static void WriteRecordSafe(BinaryWriter Writer, Record R)
        {

            // Write count //
            //Writer.Write(R.Count);

            // Write each cell //
            for (int i = 0; i < R.Count; i++)
                BinarySerializer.WriteCellSafe(Writer, R[i]);

        }

        private static void WriteRecordsSafe(BinaryWriter Writer, List<Record> Cache)
        {

            // Do NOT write the record count; assume the reader knows what the record count is //
            foreach (Record r in Cache)
                BinarySerializer.WriteRecordSafe(Writer, r);

        }

        private static void WriteHeadersSafe(BinaryWriter Writer, List<Header> Cache)
        {
            List<Record> NewCache = Cache.ConvertAll((x) => { return new Record(x.BaseArray); });
            WriteRecordsSafe(Writer, NewCache);
        }

        private static void WriteRecordSetSafe(BinaryWriter Writer, RecordSet Data)
        {

            /*
             * Write:
             *      Header
             *      Schema
             *      SortKey
             *      Record Collection
             */

            // Update the data //
            Data.Update();

            // Write header //
            BinarySerializer.WriteRecordSafe(Writer, Data.Header);

            // Write columns //
            BinarySerializer.WriteRecordsSafe(Writer, Data.Columns._Cache);

            // Write sort key //
            BinarySerializer.WriteRecordSafe(Writer, Data.SortBy.ToRecord());

            // Write cache //
            BinarySerializer.WriteRecordsSafe(Writer, Data._Cache);

        }

        private static void FlushRecordSetSafe(RecordSet Data)
        {

            //Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>> DISK_HIT_EXTENT: {0}", Data.Name);

            // Check if attached //
            if (Data.IsAttached == false)
                throw new Exception("BinarySerializer.FlushRecordSet: RecordSet passed is not attached");

            // Update the header //
            Data.Update();

            // Create a file stream //
            using (FileStream fs = File.Create(Data.Header.Path))
            {

                // Create a binary stream to write over the data //
                using (BinaryWriter br = new BinaryWriter(fs))
                {

                    // Write the record set //
                    BinarySerializer.WriteRecordSetSafe(br, Data);

                }

            }

            // Increment writes //
            _DISK_WRITES++;

        }

        // ---------------------------------------------------------------- //
        private static void WriteCellSafe2(Stream Writer, Cell C)
        {

            // Write the affinity //
            Writer.WriteByte((byte)C.AFFINITY);

            // Write nullness //
            Writer.WriteByte(C.NULL);

            // If we are null, then exit
            // for security reasons, we do not want to write any ghost data if the cell is null //
            if (C.IsNull)
                return;

            // Bool //
            if (C.AFFINITY == CellAffinity.BOOL)
            {
                Writer.WriteByte(C.BOOL == true ? (byte)1 : (byte)0);
                return;
            }

            // BLOB //
            if (C.AFFINITY == CellAffinity.BLOB)
            {

                C.INT_B = C.BLOB.Length;
                Writer.WriteByte(C.B4);
                Writer.WriteByte(C.B5);
                Writer.WriteByte(C.B6);
                Writer.WriteByte(C.B7);

                for (int i = 0; i < C.BLOB.Length; i++)
                {
                    Writer.WriteByte(C.BLOB[i]);
                }

                return;

            }

            // STRING //
            if (C.AFFINITY == CellAffinity.STRING)
            {

                C.INT_B = C.STRING.Length;
                Writer.WriteByte(C.B4);
                Writer.WriteByte(C.B5);
                Writer.WriteByte(C.B6);
                Writer.WriteByte(C.B7);
                for (int i = 0; i < C.STRING.Length; i++)
                {
                    byte c1 = (byte)(C.STRING[i] >> 8);
                    byte c2 = (byte)(C.STRING[i] & 255);
                    Writer.WriteByte(c1);
                    Writer.WriteByte(c2);
                }
                return;

            }

            // Double, int, date //
            Writer.WriteByte(C.B0);
            Writer.WriteByte(C.B1);
            Writer.WriteByte(C.B2);
            Writer.WriteByte(C.B3);
            Writer.WriteByte(C.B4);
            Writer.WriteByte(C.B5);
            Writer.WriteByte(C.B6);
            Writer.WriteByte(C.B7);

        }

        private static void WriteRecordSafe2(Stream Writer, Record R)
        {

            // Write count //
            //Writer.Write(R.Count);

            // Write each cell //
            for (int i = 0; i < R.Count; i++)
                BinarySerializer.WriteCellSafe2(Writer, R[i]);

        }

        private static void WriteRecordsSafe2(Stream Writer, List<Record> Cache)
        {

            // Do NOT write the record count; assume the reader knows what the record count is //
            foreach (Record r in Cache)
                BinarySerializer.WriteRecordSafe2(Writer, r);

        }

        private static void WriteHeadersSafe2(Stream Writer, List<Header> Cache)
        {
            List<Record> NewCache = Cache.ConvertAll((x) => { return new Record(x.BaseArray); });
            WriteRecordsSafe2(Writer, NewCache);
        }

        private static void WriteRecordSetSafe2(Stream Writer, RecordSet Data)
        {

            /*
             * Write:
             *      Header
             *      Schema
             *      SortKey
             *      Record Collection
             */

            // Update the data //
            Data.Update();

            // Write header //
            BinarySerializer.WriteRecordSafe2(Writer, Data.Header);

            // Write columns //
            BinarySerializer.WriteRecordsSafe2(Writer, Data.Columns._Cache);

            // Write sort key //
            BinarySerializer.WriteRecordSafe2(Writer, Data.SortBy.ToRecord());

            // Write cache //
            BinarySerializer.WriteRecordsSafe2(Writer, Data._Cache);

        }

        private static void FlushRecordSetSafe2(RecordSet Data)
        {

            //Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>> DISK_HIT_EXTENT: {0}", Data.Name);

            // Check if attached //
            if (Data.IsAttached == false)
                throw new Exception("BinarySerializer.FlushRecordSet: RecordSet passed is not attached");

            // Update the header //
            Data.Update();

            // Estimate the size //
            int size = EstimateSize(Data);

            // Create a file stream //
            using (FileStream fs = File.Create(Data.Header.Path, size))
            {

                // Write the record set //
                BinarySerializer.WriteRecordSetSafe2(fs, Data);

            }

            // Increment writes //
            _DISK_WRITES++;

        }

        // ---------------------------------------------------------------- //
        private static int WriteCellSafe3(byte[] Mem, int Location, Cell C)
        {

            // Write the affinity //
            Mem[Location] = ((byte)C.AFFINITY);
            Location++;

            // Write nullness //
            Mem[Location] = C.NULL;
            Location++;

            // If we are null, then exit
            // for security reasons, we do not want to write any ghost data if the cell is null //
            if (C.IsNull)
                return Location;

            // Bool //
            if (C.AFFINITY == CellAffinity.BOOL)
            {
                Mem[Location] = (C.BOOL == true ? (byte)1 : (byte)0);
                Location++;
                return Location;
            }

            // BLOB //
            if (C.AFFINITY == CellAffinity.BLOB)
            {

                C.INT_B = C.BLOB.Length;
                Mem[Location] = (C.B4);
                Mem[Location + 1] = (C.B5);
                Mem[Location + 2] = (C.B6);
                Mem[Location + 3] = (C.B7);
                Location += 4;

                for (int i = 0; i < C.BLOB.Length; i++)
                {
                    Mem[Location + i] = C.BLOB[i];
                }

                Location += C.BLOB.Length;
                return Location;

            }

            // STRING //
            if (C.AFFINITY == CellAffinity.STRING)
            {

                C.INT_B = C.STRING.Length;
                Mem[Location] = (C.B4);
                Mem[Location + 1] = (C.B5);
                Mem[Location + 2] = (C.B6);
                Mem[Location + 3] = (C.B7);
                Location += 4;

                for (int i = 0; i < C.STRING.Length; i++)
                {
                    byte c1 = (byte)(C.STRING[i] >> 8);
                    byte c2 = (byte)(C.STRING[i] & 255);
                    Mem[Location] = c1;
                    Location++;
                    Mem[Location] = c2;
                    Location++;
                }
                return Location;

            }

            // Double, int, date //
            Mem[Location] = C.B0;
            Mem[Location + 1] = C.B1;
            Mem[Location + 2] = C.B2;
            Mem[Location + 3] = C.B3;
            Mem[Location + 4] = C.B4;
            Mem[Location + 5] = C.B5;
            Mem[Location + 6] = C.B6;
            Mem[Location + 7] = C.B7;

            return Location + 8;

        }

        private static int WriteRecordSafe3(byte[] Mem, int Location, Record R)
        {

            // Write each cell //
            for (int i = 0; i < R.Count; i++)
            {
                Location = BinarySerializer.WriteCellSafe3(Mem, Location, R[i]);
            }

            return Location;

        }

        private static int WriteRecordsSafe3(byte[] Mem, int Location, List<Record> Cache)
        {

            // Do NOT write the record count; assume the reader knows what the record count is //
            foreach (Record r in Cache)
            {
                Location = BinarySerializer.WriteRecordSafe3(Mem, Location, r);
            }

            return Location;

        }

        private static int WriteHeadersSafe3(byte[] Mem, int Location, List<Header> Cache)
        {
            List<Record> NewCache = Cache.ConvertAll((x) => { return new Record(x.BaseArray); });
            return WriteRecordsSafe3(Mem, Location, NewCache);
        }

        private static int WriteRecordSetSafe3(byte[] Mem, int Location, RecordSet Data)
        {

            /*
             * Write:
             *      Header
             *      Schema
             *      SortKey
             *      Record Collection
             */

            // Update the data //
            Data.Update();

            // Write header //
            Location = BinarySerializer.WriteRecordSafe3(Mem, Location, Data.Header);

            // Write columns //
            Location = BinarySerializer.WriteRecordsSafe3(Mem, Location, Data.Columns._Cache);

            // Write sort key //
            Location = BinarySerializer.WriteRecordSafe3(Mem, Location, Data.SortBy.ToRecord());

            // Write cache //
            Location = BinarySerializer.WriteRecordsSafe3(Mem, Location, Data._Cache);

            return Location;

        }

        private static void FlushRecordSetSafe3(RecordSet Data)
        {

            //Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>> DISK_HIT_EXTENT: {0}", Data.Name);

            // Check if attached //
            if (Data.IsAttached == false)
                throw new Exception("BinarySerializer.FlushRecordSet: RecordSet passed is not attached");

            // Update the header //
            Data.Update();

            // Estimate the size //
            int size = EstimateSize(Data);

            // Memory stack //
            byte[] memory = new byte[size];

            // Write the data to memory //
            int location = BinarySerializer.WriteRecordSetSafe3(memory, 0, Data);

            // Create a file stream //
            using (FileStream fs = File.Create(Data.Header.Path, size))
            {
                fs.Write(memory, 0, location);
            }

            // Increment writes //
            _DISK_WRITES++;

        }

        // ---------------------------------------------------------------- //
        private static int WriteCellSafe4(byte[] Mem, int Location, Cell C)
        {

            // Write the affinity //
            Mem[Location] = ((byte)C.AFFINITY);
            Location++;

            // Write nullness //
            Mem[Location] = C.NULL;
            Location++;

            // If we are null, then exit
            // for security reasons, we do not want to write any ghost data if the cell is null //
            if (C.IsNull)
                return Location;

            // Bool //
            if (C.AFFINITY == CellAffinity.BOOL)
            {
                Mem[Location] = (C.BOOL == true ? (byte)1 : (byte)0);
                Location++;
                return Location;
            }

            // BLOB //
            if (C.AFFINITY == CellAffinity.BLOB)
            {

                C.INT_B = C.BLOB.Length;
                Mem[Location] = (C.B4);
                Mem[Location + 1] = (C.B5);
                Mem[Location + 2] = (C.B6);
                Mem[Location + 3] = (C.B7);
                Location += 4;

                for (int i = 0; i < C.BLOB.Length; i++)
                {
                    Mem[Location + i] = C.BLOB[i];
                }

                Location += C.BLOB.Length;
                return Location;

            }

            // STRING //
            if (C.AFFINITY == CellAffinity.STRING)
            {

                C.INT_B = C.STRING.Length;
                Mem[Location] = (C.B4);
                Mem[Location + 1] = (C.B5);
                Mem[Location + 2] = (C.B6);
                Mem[Location + 3] = (C.B7);
                Location += 4;

                for (int i = 0; i < C.STRING.Length; i++)
                {
                    byte c1 = (byte)(C.STRING[i] >> 8);
                    byte c2 = (byte)(C.STRING[i] & 255);
                    Mem[Location] = c1;
                    Location++;
                    Mem[Location] = c2;
                    Location++;
                }
                return Location;

            }

            // Double, int, date //
            Mem[Location] = C.B0;
            Mem[Location + 1] = C.B1;
            Mem[Location + 2] = C.B2;
            Mem[Location + 3] = C.B3;
            Mem[Location + 4] = C.B4;
            Mem[Location + 5] = C.B5;
            Mem[Location + 6] = C.B6;
            Mem[Location + 7] = C.B7;

            return Location + 8;

        }

        private static int WriteRecordSafe4(byte[] Mem, int Location, Record R)
        {

            // Write each cell //
            for (int j = 0; j < R.Count; j++)
            {

                Cell C = R[j];

                // Write the affinity //
                Mem[Location] = ((byte)C.AFFINITY);
                Location++;

                // Write nullness //
                Mem[Location] = C.NULL;
                Location++;

                // If we are null, then exit
                // for security reasons, we do not want to write any ghost data if the cell is null //
                if (C.NULL == 0)
                {

                    // Bool //
                    if (C.AFFINITY == CellAffinity.BOOL)
                    {
                        Mem[Location] = (C.BOOL == true ? (byte)1 : (byte)0);
                        Location++;
                    }

                    // BLOB //
                    else if (C.AFFINITY == CellAffinity.BLOB)
                    {

                        C.INT_B = C.BLOB.Length;
                        Mem[Location] = (C.B4);
                        Mem[Location + 1] = (C.B5);
                        Mem[Location + 2] = (C.B6);
                        Mem[Location + 3] = (C.B7);
                        Location += 4;

                        for (int i = 0; i < C.BLOB.Length; i++)
                        {
                            Mem[Location + i] = C.BLOB[i];
                        }

                        Location += C.BLOB.Length;

                    }

                    // STRING //
                    else if (C.AFFINITY == CellAffinity.STRING)
                    {

                        C.INT_B = C.STRING.Length;
                        Mem[Location] = (C.B4);
                        Mem[Location + 1] = (C.B5);
                        Mem[Location + 2] = (C.B6);
                        Mem[Location + 3] = (C.B7);
                        Location += 4;

                        for (int i = 0; i < C.STRING.Length; i++)
                        {
                            byte c1 = (byte)(C.STRING[i] >> 8);
                            byte c2 = (byte)(C.STRING[i] & 255);
                            Mem[Location] = c1;
                            Location++;
                            Mem[Location] = c2;
                            Location++;
                        }

                    }

                    // Double, int, date //
                    else
                    {

                        Mem[Location] = C.B0;
                        Mem[Location + 1] = C.B1;
                        Mem[Location + 2] = C.B2;
                        Mem[Location + 3] = C.B3;
                        Mem[Location + 4] = C.B4;
                        Mem[Location + 5] = C.B5;
                        Mem[Location + 6] = C.B6;
                        Mem[Location + 7] = C.B7;
                        Location += 8;
                    }

                }

            }

            return Location;

        }

        private static int WriteRecordsSafe4(byte[] Mem, int Location, List<Record> Cache)
        {

            // Do NOT write the record count; assume the reader knows what the record count is //
            foreach (Record R in Cache)
            {

                // Write each cell //
                for (int j = 0; j < R.Count; j++)
                {

                    Cell C = R[j];

                    // Write the affinity //
                    Mem[Location] = ((byte)C.AFFINITY);
                    Location++;

                    // Write nullness //
                    Mem[Location] = C.NULL;
                    Location++;

                    // If we are null, then exit
                    // for security reasons, we do not want to write any ghost data if the cell is null //
                    if (C.NULL == 0)
                    {

                        // Bool //
                        if (C.AFFINITY == CellAffinity.BOOL)
                        {
                            Mem[Location] = (C.BOOL == true ? (byte)1 : (byte)0);
                            Location++;
                        }

                        // BLOB //
                        else if (C.AFFINITY == CellAffinity.BLOB)
                        {

                            C.INT_B = C.BLOB.Length;
                            Mem[Location] = (C.B4);
                            Mem[Location + 1] = (C.B5);
                            Mem[Location + 2] = (C.B6);
                            Mem[Location + 3] = (C.B7);
                            Location += 4;

                            for (int i = 0; i < C.BLOB.Length; i++)
                            {
                                Mem[Location + i] = C.BLOB[i];
                            }

                            Location += C.BLOB.Length;

                        }

                        // STRING //
                        else if (C.AFFINITY == CellAffinity.STRING)
                        {

                            C.INT_B = C.STRING.Length;
                            Mem[Location] = (C.B4);
                            Mem[Location + 1] = (C.B5);
                            Mem[Location + 2] = (C.B6);
                            Mem[Location + 3] = (C.B7);
                            Location += 4;

                            for (int i = 0; i < C.STRING.Length; i++)
                            {
                                byte c1 = (byte)(C.STRING[i] >> 8);
                                byte c2 = (byte)(C.STRING[i] & 255);
                                Mem[Location] = c1;
                                Location++;
                                Mem[Location] = c2;
                                Location++;
                            }

                        }

                        // Double, int, date //
                        else
                        {

                            Mem[Location] = C.B0;
                            Mem[Location + 1] = C.B1;
                            Mem[Location + 2] = C.B2;
                            Mem[Location + 3] = C.B3;
                            Mem[Location + 4] = C.B4;
                            Mem[Location + 5] = C.B5;
                            Mem[Location + 6] = C.B6;
                            Mem[Location + 7] = C.B7;
                            Location += 8;
                        }

                    }

                }

            }

            return Location;

        }

        private static int WriteHeadersSafe4(byte[] Mem, int Location, List<Header> Cache)
        {
            List<Record> NewCache = Cache.ConvertAll((x) => { return new Record(x.BaseArray); });
            return WriteRecordsSafe4(Mem, Location, NewCache);
        }

        private static int WriteRecordSetSafe4(byte[] Mem, int Location, RecordSet Data)
        {

            /*
             * Write:
             *      Header
             *      Schema
             *      SortKey
             *      Record Collection
             */

            // Update the data //
            Data.Update();

            // Write header //
            Location = BinarySerializer.WriteRecordSafe4(Mem, Location, Data.Header);
            //Console.WriteLine("Header Terminator: {0}", Location);

            // Write columns //
            Location = BinarySerializer.WriteRecordsSafe4(Mem, Location, Data.Columns._Cache);
            //Console.WriteLine("Schema Terminator: {0}", Location);

            // Write sort key //
            Location = BinarySerializer.WriteRecordSafe4(Mem, Location, Data.SortBy.ToRecord());
            //Console.WriteLine("Keys Terminator: {0}", Location);

            // Write cache //
            Location = BinarySerializer.WriteRecordsSafe4(Mem, Location, Data._Cache);
            //Console.WriteLine("Data Terminator: {0}", Location);

            return Location;

        }

        private static int WriteTableSafe4(byte[] Mem, int Location, Table Data)
        {

            /*
             * Write:
             *      Header
             *      Schema
             *      SortKey
             *      Record Collection
             */

            // Update the data //
            Data.Update();

            // Write header //
            Location = BinarySerializer.WriteRecordSafe4(Mem, Location, Data.Header);
            //Console.WriteLine("Header Terminator: {0}", Location);

            // Write columns //
            Location = BinarySerializer.WriteRecordsSafe4(Mem, Location, Data.Columns._Cache);
            //Console.WriteLine("Schema Terminator: {0}", Location);

            // Write sort key //
            Location = BinarySerializer.WriteRecordSafe4(Mem, Location, Data.SortBy.ToRecord());
            //Console.WriteLine("Keys Terminator: {0}", Location);

            // Write cache //
            Location = BinarySerializer.WriteRecordsSafe4(Mem, Location, Data.ReferenceTable._Cache);
            //Console.WriteLine("Data Terminator: {0}", Location);

            return Location;

        }

        private static void FlushRecordSetSafe4(RecordSet Data)
        {

            //Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>> DISK_HIT_EXTENT: {0}", Data.Name);

            // Check if attached //
            if (Data.IsAttached == false)
                throw new Exception("BinarySerializer.FlushRecordSet: RecordSet passed is not attached");

            // Update the header //
            Data.Update();

            // Estimate the size //
            int size = EstimateSize(Data);

            // Memory stack //
            byte[] memory = new byte[size * 2];

            // Write the data to memory //
            int location = BinarySerializer.WriteRecordSetSafe4(memory, 0, Data);

            // Create a file stream //
            using (FileStream fs = File.Create(Data.Header.Path, size))
            {
                fs.Write(memory, 0, location);
            }

            // Increment writes //
            _DISK_WRITES++;

        }

        private static void WriteTable(byte[] Mem, int Location, Table Data)
        {

            /*
             * Write:
             *      Header
             *      Schema
             *      SortKey
             *      Record Collection
             */

            // Update //
            Data.Update();

            // Write header //
            Location = BinarySerializer.WriteRecordSafe4(Mem, Location, Data.Header);

            // Write columns //
            Location = BinarySerializer.WriteRecordsSafe4(Mem, Location, Data.Columns._Cache);

            // Write sort key //
            Location = BinarySerializer.WriteRecordSafe4(Mem, Location, Data.SortBy.ToRecord());

            // Write cache //
            Location = BinarySerializer.WriteRecordsSafe4(Mem, Location, Data.ReferenceTable._Cache);

        }

        // ---------------------------------------------------------------- //
        private static Cell ReadCellSafe(BinaryReader Reader)
        {

            // Read the affinity //
            CellAffinity a = (CellAffinity)Reader.ReadByte();

            // Read nullness //
            bool b = (Reader.ReadByte() == 1);

            // If we are null, then exit
            // for security reasons, we do not want to write any ghost data if the cell is null //
            if (b == true)
                return new Cell(a);

            // Cell c //
            Cell c = new Cell(a);
            c.NULL = 0;

            if (a == CellAffinity.BOOL)
            {
                c.B0 = Reader.ReadByte();
                return c;
            }

            // BLOB //
            if (a == CellAffinity.BLOB)
            {

                c.B4 = Reader.ReadByte();
                c.B5 = Reader.ReadByte();
                c.B6 = Reader.ReadByte();
                c.B7 = Reader.ReadByte();
                byte[] blob = new byte[c.INT_B];
                for (int i = 0; i < blob.Length; i++)
                {
                    blob[i] = Reader.ReadByte();
                }
                c.BLOB = blob;
                return new Cell(blob);

            }

            // STRING //
            if (a == CellAffinity.STRING)
            {

                c.B4 = Reader.ReadByte();
                c.B5 = Reader.ReadByte();
                c.B6 = Reader.ReadByte();
                c.B7 = Reader.ReadByte();
                char[] chars = new char[c.INT_B];
                for (int i = 0; i < c.INT_B; i++)
                {
                    byte c1 = Reader.ReadByte();
                    byte c2 = Reader.ReadByte();
                    chars[i] = (char)(((int)c2) | (int)(c1 << 8));
                }
                return new Cell(new string(chars));

            }

            // Double, Ints, Dates //
            c.B0 = Reader.ReadByte();
            c.B1 = Reader.ReadByte();
            c.B2 = Reader.ReadByte();
            c.B3 = Reader.ReadByte();
            c.B4 = Reader.ReadByte();
            c.B5 = Reader.ReadByte();
            c.B6 = Reader.ReadByte();
            c.B7 = Reader.ReadByte();
            return c;

        }

        private static Record ReadRecordSafe(BinaryReader Reader, int Length)
        {

            // Get count //
            //int n = Reader.ReadInt32();
            Cell[] c = new Cell[Length];

            // Get cells //
            for (int i = 0; i < Length; i++)
                c[i] = BinarySerializer.ReadCellSafe(Reader);

            return new Record(c);

        }

        private static List<Record> ReadRecordsSafe(BinaryReader Reader, long Count, int Length)
        {

            // Build a cache //
            List<Record> t = new List<Record>();

            // Loop through //
            for (int i = 0; i < Count; i++)
            {
                t.Add(BinarySerializer.ReadRecordSafe(Reader, Length));
            }

            return t;

        }

        private static List<Header> ReadHeadersSafe(BinaryReader Reader, long Count, int Length)
        {

            // Build a cache //
            List<Header> t = new List<Header>();

            // Loop through //
            for (int i = 0; i < Count; i++)
            {
                t.Add(new Header(BinarySerializer.ReadRecordSafe(Reader, Length)));
            }

            return t;

        }

        private static RecordSet ReadRecordSetSafe(BinaryReader Reader)
        {

            /*
             * Read:
             *      Header
             *      Schema
             *      SortKey
             *      Record Collection
             */

            // Read header //
            Header h = new Header(BinarySerializer.ReadRecordSafe(Reader, 10));

            // Read schema //
            Schema s = new Schema(BinarySerializer.ReadRecordsSafe(Reader, h.ColumnCount, 4));

            // Read key //
            Key k = new Key(BinarySerializer.ReadRecordSafe(Reader, (int)h.KeyCount));

            // Read record cache //
            List<Record> l = BinarySerializer.ReadRecordsSafe(Reader, h.RecordCount, (int)h.ColumnCount);

            // Return recordset //
            return new RecordSet(s, h, l, k);

        }

        private static RecordSet BufferRecordSetSafe(string FullPath)
        {

            // Open a stream //
            byte[] b = File.ReadAllBytes(FullPath);
            RecordSet rs;

            using (MemoryStream ms = new MemoryStream(b))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    rs = BinarySerializer.ReadRecordSetSafe(br);
                }
            }

            // Increment Reads //
            _DISK_READS++;

            return rs;

        }

        // ---------------------------------------------------------------- //
        private static int ReadCellSafe2(byte[] Mem, int Location, out Cell C)
        {

            // Read the affinity //
            CellAffinity a = (CellAffinity)Mem[Location];
            Location++;

            // Read nullness //
            bool b = (Mem[Location] == 1);
            Location++;

            // If we are null, then exit
            // for security reasons, we do not want to write any ghost data if the cell is null //
            if (b == true)
            {
                C = new Cell(a);
                return Location;
            }

            // Cell c //
            C = new Cell(a);
            C.NULL = 0;

            if (a == CellAffinity.BOOL)
            {
                C.B0 = Mem[Location];
                Location++;
                return Location;
            }

            // BLOB //
            if (a == CellAffinity.BLOB)
            {

                C.B4 = Mem[Location];
                C.B5 = Mem[Location + 1];
                C.B6 = Mem[Location + 2];
                C.B7 = Mem[Location + 3];
                Location += 4;
                byte[] blob = new byte[C.INT_B];
                for (int i = 0; i < blob.Length; i++)
                {
                    blob[i] = Mem[Location];
                    Location++;
                }
                C = new Cell(blob);
                return Location;

            }

            // STRING //
            if (a == CellAffinity.STRING)
            {

                C.B4 = Mem[Location];
                C.B5 = Mem[Location + 1];
                C.B6 = Mem[Location + 2];
                C.B7 = Mem[Location + 3];
                Location += 4;
                char[] chars = new char[C.INT_B];
                for (int i = 0; i < C.INT_B; i++)
                {
                    byte c1 = Mem[Location];
                    Location++;
                    byte c2 = Mem[Location];
                    Location++;
                    chars[i] = (char)(((int)c2) | (int)(c1 << 8));
                }
                C = new Cell(new string(chars));
                return Location;

            }

            // Double, Ints, Dates //
            C.B0 = Mem[Location];
            C.B1 = Mem[Location + 1];
            C.B2 = Mem[Location + 2];
            C.B3 = Mem[Location + 3];
            C.B4 = Mem[Location + 4];
            C.B5 = Mem[Location + 5];
            C.B6 = Mem[Location + 6];
            C.B7 = Mem[Location + 7];
            Location += 8;
            return Location;

        }

        private static int ReadRecordSafe2(byte[] Mem, int Location, int Length, out Record Datum)
        {

            // Array //
            Cell[] q = new Cell[Length];

            // Get cells //
            for (int j = 0; j < Length; j++)
            {

                Cell C;

                // Read the affinity //
                CellAffinity a = (CellAffinity)Mem[Location];
                Location++;

                // Read nullness //
                bool b = (Mem[Location] == 1);
                Location++;

                // If we are null, then exit
                // for security reasons, we do not want to write any ghost data if the cell is null //
                if (b == true)
                {
                    C = new Cell(a);
                }
                else
                {

                    // Cell c //
                    C = new Cell(a);
                    C.NULL = 0;

                    // BOOL //
                    if (a == CellAffinity.BOOL)
                    {
                        C.B0 = Mem[Location];
                        Location++;
                    }

                    // BLOB //
                    else if (a == CellAffinity.BLOB)
                    {

                        C.B4 = Mem[Location];
                        C.B5 = Mem[Location + 1];
                        C.B6 = Mem[Location + 2];
                        C.B7 = Mem[Location + 3];
                        Location += 4;
                        byte[] blob = new byte[C.INT_B];
                        for (int i = 0; i < blob.Length; i++)
                        {
                            blob[i] = Mem[Location];
                            Location++;
                        }
                        C = new Cell(blob);

                    }

                    // STRING //
                    else if (a == CellAffinity.STRING)
                    {

                        C.B4 = Mem[Location];
                        C.B5 = Mem[Location + 1];
                        C.B6 = Mem[Location + 2];
                        C.B7 = Mem[Location + 3];
                        Location += 4;
                        char[] chars = new char[C.INT_B];
                        for (int i = 0; i < C.INT_B; i++)
                        {
                            byte c1 = Mem[Location];
                            Location++;
                            byte c2 = Mem[Location];
                            Location++;
                            chars[i] = (char)(((int)c2) | (int)(c1 << 8));
                        }
                        C = new Cell(new string(chars));

                    }

                    // Double, Ints, Dates //
                    else
                    {
                        C.B0 = Mem[Location];
                        C.B1 = Mem[Location + 1];
                        C.B2 = Mem[Location + 2];
                        C.B3 = Mem[Location + 3];
                        C.B4 = Mem[Location + 4];
                        C.B5 = Mem[Location + 5];
                        C.B6 = Mem[Location + 6];
                        C.B7 = Mem[Location + 7];
                        Location += 8;
                    }

                }

                q[j] = C;

            }

            Datum = new Record(q);
            return Location;

        }

        private static int ReadRecordsSafe2(byte[] Mem, int Location, long Count, int Length, List<Record> Cache)
        {

            // Loop through //
            for (int k = 0; k < Count; k++)
            {

                // Array //
                Cell[] q = new Cell[Length];

                // Get cells //
                for (int j = 0; j < Length; j++)
                {

                    Cell C;

                    // Read the affinity //
                    CellAffinity a = (CellAffinity)Mem[Location];
                    Location++;

                    // Read nullness //
                    bool b = (Mem[Location] == 1);
                    Location++;

                    // If we are null, then exit
                    // for security reasons, we do not want to write any ghost data if the cell is null //
                    if (b == true)
                    {
                        C = new Cell(a);
                    }
                    else
                    {

                        // Cell c //
                        C = new Cell(a);
                        C.NULL = 0;

                        // BOOL //
                        if (a == CellAffinity.BOOL)
                        {
                            C.B0 = Mem[Location];
                            Location++;
                        }

                        // BLOB //
                        else if (a == CellAffinity.BLOB)
                        {

                            C.B4 = Mem[Location];
                            C.B5 = Mem[Location + 1];
                            C.B6 = Mem[Location + 2];
                            C.B7 = Mem[Location + 3];
                            Location += 4;
                            byte[] blob = new byte[C.INT_B];
                            for (int i = 0; i < blob.Length; i++)
                            {
                                blob[i] = Mem[Location];
                                Location++;
                            }
                            C = new Cell(blob);

                        }

                        // STRING //
                        else if (a == CellAffinity.STRING)
                        {

                            C.B4 = Mem[Location];
                            C.B5 = Mem[Location + 1];
                            C.B6 = Mem[Location + 2];
                            C.B7 = Mem[Location + 3];
                            Location += 4;
                            char[] chars = new char[C.INT_B];
                            for (int i = 0; i < C.INT_B; i++)
                            {
                                byte c1 = Mem[Location];
                                Location++;
                                byte c2 = Mem[Location];
                                Location++;
                                chars[i] = (char)(((int)c2) | (int)(c1 << 8));
                            }
                            C = new Cell(new string(chars));

                        }

                        // Double, Ints, Dates //
                        else
                        {
                            C.B0 = Mem[Location];
                            C.B1 = Mem[Location + 1];
                            C.B2 = Mem[Location + 2];
                            C.B3 = Mem[Location + 3];
                            C.B4 = Mem[Location + 4];
                            C.B5 = Mem[Location + 5];
                            C.B6 = Mem[Location + 6];
                            C.B7 = Mem[Location + 7];
                            Location += 8;
                        }

                    }

                    q[j] = C;

                }

                Cache.Add(new Record(q));

            }

            return Location;

        }

        private static int ReadHeadersSafe2(byte[] Mem, int Location, long Count, int Length, List<Header> Cache)
        {

            // Loop through //
            for (int k = 0; k < Count; k++)
            {

                // Array //
                Cell[] q = new Cell[Length];

                // Get cells //
                for (int j = 0; j < Length; j++)
                {

                    Cell C;

                    // Read the affinity //
                    CellAffinity a = (CellAffinity)Mem[Location];
                    Location++;

                    // Read nullness //
                    bool b = (Mem[Location] == 1);
                    Location++;

                    // If we are null, then exit
                    // for security reasons, we do not want to write any ghost data if the cell is null //
                    if (b == true)
                    {
                        C = new Cell(a);
                    }
                    else
                    {

                        // Cell c //
                        C = new Cell(a);
                        C.NULL = 0;

                        // BOOL //
                        if (a == CellAffinity.BOOL)
                        {
                            C.B0 = Mem[Location];
                            Location++;
                        }

                        // BLOB //
                        else if (a == CellAffinity.BLOB)
                        {

                            C.B4 = Mem[Location];
                            C.B5 = Mem[Location + 1];
                            C.B6 = Mem[Location + 2];
                            C.B7 = Mem[Location + 3];
                            Location += 4;
                            byte[] blob = new byte[C.INT_B];
                            for (int i = 0; i < blob.Length; i++)
                            {
                                blob[i] = Mem[Location];
                                Location++;
                            }
                            C = new Cell(blob);

                        }

                        // STRING //
                        else if (a == CellAffinity.STRING)
                        {

                            C.B4 = Mem[Location];
                            C.B5 = Mem[Location + 1];
                            C.B6 = Mem[Location + 2];
                            C.B7 = Mem[Location + 3];
                            Location += 4;
                            char[] chars = new char[C.INT_B];
                            for (int i = 0; i < C.INT_B; i++)
                            {
                                byte c1 = Mem[Location];
                                Location++;
                                byte c2 = Mem[Location];
                                Location++;
                                chars[i] = (char)(((int)c2) | (int)(c1 << 8));
                            }
                            C = new Cell(new string(chars));

                        }

                        // Double, Ints, Dates //
                        else
                        {
                            C.B0 = Mem[Location];
                            C.B1 = Mem[Location + 1];
                            C.B2 = Mem[Location + 2];
                            C.B3 = Mem[Location + 3];
                            C.B4 = Mem[Location + 4];
                            C.B5 = Mem[Location + 5];
                            C.B6 = Mem[Location + 6];
                            C.B7 = Mem[Location + 7];
                            Location += 8;
                        }

                    }

                    q[j] = C;

                }

                Cache.Add(new Header(new Record(q)));

            }

            return Location;

        }

        private static RecordSet ReadRecordSetSafe2(byte[] Mem, int Location)
        {

            /*
             * Read:
             *      Header
             *      Schema
             *      SortKey
             *      Record Collection
             */

            // Read header //
            Record rh;
            Location = ReadRecordSafe2(Mem, Location, 10, out rh);
            Header h = new Header(rh);
            
            // Read schema //
            List<Record> s_cache = new List<Record>();
            Location = BinarySerializer.ReadRecordsSafe2(Mem, Location, h.ColumnCount, 4, s_cache);
            Schema s = new Schema(s_cache);
            
            // Read key //
            Record rk;
            Location = ReadRecordSafe2(Mem, Location, (int)h.KeyCount, out rk);
            Key k = new Key(rk);
            
            // Read record cache //
            List<Record> d_cache = new List<Record>();
            Location = BinarySerializer.ReadRecordsSafe2(Mem, Location, (int)h.RecordCount, (int)h.ColumnCount, d_cache);
            
            // Return recordset //
            return new RecordSet(s, h, d_cache, k);

        }

        private static Table ReadTableSafe2(byte[] Mem, int Location)
        {

            /*
             * Read:
             *      Header
             *      Schema
             *      SortKey
             *      Record Collection
             */

            // Read header //
            Record rh;
            Location = ReadRecordSafe2(Mem, Location, 11, out rh);
            TableHeader h = new TableHeader(rh);

            // Read schema //
            List<Record> s_cache = new List<Record>();
            Location = BinarySerializer.ReadRecordsSafe2(Mem, Location, h.ColumnCount, 4, s_cache);
            Schema s = new Schema(s_cache);

            // Read key //
            Record rk;
            Location = ReadRecordSafe2(Mem, Location, (int)h.KeyCount, out rk);
            Key k = new Key(rk);

            // Read record cache //
            List<Record> d_cache = new List<Record>();
            Location = BinarySerializer.ReadRecordsSafe2(Mem, Location, (int)h.Size, 2, d_cache);

            // Return recordset //
            return new Table(h, s, d_cache, k);

        }

        private static RecordSet BufferRecordSetSafe2(string FullPath)
        {

            // Open a stream //
            byte[] b = File.ReadAllBytes(FullPath);
            RecordSet rs = ReadRecordSetSafe2(b, 0);

            // Increment Reads //
            _DISK_READS++;

            return rs;

        }

        // Regular Datasets //
        public static RecordSet BufferRecordSet(string FullPath)
        {
            return BinarySerializer.BufferRecordSetSafe2(FullPath);
        }

        public static RecordSet BufferRecordSet(Header H)
        {
            return BufferRecordSet(H.Path);
        }

        public static void FlushRecordSet(RecordSet Data)
        {
            BinarySerializer.FlushRecordSetSafe4(Data);
        }

        // Big Datasets //
        public static Table BufferTable(string FullPath)
        {

            // Open a stream //
            byte[] b = File.ReadAllBytes(FullPath);
            
            Table t = BinarySerializer.ReadTableSafe2(b, 0);

            // Increment Reads //
            _DISK_READS++;

            return t;

        }

        public static Table BufferTable(TableHeader H)
        {
            return BufferTable(H.Path);
        }

        public static void FlushTable(Table Data)
        {

            // Update the header //
            Data.Update();

            // Get the size //
            int size = EstimateSize(Data);
            
            // Memory //
            byte[] memory = new byte[size];

            // Create a file stream //
            using (FileStream fs = File.Create(Data.Header.Path))
            {
                int location = BinarySerializer.WriteTableSafe4(memory, 0, Data);
                fs.Write(memory, 0, location);
            }

            // Increment writes //
            _DISK_WRITES++;

        }

        // Generic Datasets //
        public static void Flush(DataSet D)
        {
            if (D.IsBig)
                FlushTable(D.ToBigRecordSet);
            else
                FlushRecordSet(D.ToRecordSet);
        }

        // Hashes //
        public static byte[] HashTable(Table Data, System.Security.Cryptography.HashAlgorithm Hasher)
        {

            byte[] b = new byte[Hasher.HashSize / 8];
            bool Begining = true;

            foreach (RecordSet rs in Data.Extents)
            {

                using (MemoryStream ms = new MemoryStream())
                {


                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {

                        // Write the value of the current hash if it is not on the first run //
                        if (Begining)
                            bw.Write(b);
                        Begining = false;

                        // Write the value of the data //
                        BinarySerializer.WriteRecords(bw, rs._Cache);

                        b = Hasher.ComputeHash(ms.ToArray());

                    }

                }

            }

            return b;

        }

        public static int EstimateSize(RecordSet Chunk)
        {

            int hsize = Header.RecordSize();
            int ssize = Schema.SchemaSchema().RecordLength * Chunk.Columns.Count;
            int ksize = Chunk.SortBy.Count * 8;
            int dsize = Chunk.Count * Chunk.Columns.RecordLength;
            return hsize + ssize + ksize + dsize;

        }

        public static int EstimateSize(Table Data)
        {

            int hsize = 4096;
            int ssize = Schema.SchemaSchema().RecordLength * Data.Columns.Count;
            int ksize = Data.SortBy.Count * 8;
            int dsize = Data.ExtentCount * 10 * 2;
            return hsize + ssize + ksize + dsize;

        }


    }


}
