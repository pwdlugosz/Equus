using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Equus.Horse
{

    public static class DataSetManager
    {

        // Drops //
        public static void DropRecordSet(string FullPath)
        {
            if (!Exists(FullPath)) return;
            File.Delete(FullPath);
        }

        public static void DropRecordSet(Header H)
        {
            DropRecordSet(H.Path);
        }

        public static void DropRecordSet(RecordSet Data)
        {
            if (!Data.IsAttached) return;
            DropRecordSet(Data.Header);
        }

        public static void DropRecordSet(IEnumerable<Header> Headers)
        {
            foreach (Header h in Headers)
                DropRecordSet(h);
        }

        public static void DropTable(string FullPath)
        {

            // Check if exists //
            if (!Exists(FullPath)) return;

            // Open the table //
            Table brs = BinarySerializer.BufferTable(FullPath);

            // Drop each sub-table //
            DropRecordSet(brs.Headers);

            // Delete the current file //
            File.Delete(FullPath);

        }

        public static void DropTable(TableHeader H)
        {
            DropTable(H.Path);
        }

        public static void DropTable(Table Data)
        {
            DropTable(Data.Header);
        }

        public static void DropData(DataSet Data)
        {
            if (Data.IsBig)
                DataSetManager.DropTable(Data.ToBigRecordSet);
            else
                DataSetManager.DropRecordSet(Data.ToRecordSet);
        }

        // Renames //
        public static void RenameRecordSet(string FullPath, string NewName)
        {

            // Open the table //
            RecordSet rs = BinarySerializer.BufferRecordSet(FullPath);
            rs.Header.Name = NewName;

            // Close the table //
            BinarySerializer.FlushRecordSet(rs);

            // Drop the old table //
            DropRecordSet(FullPath);

        }

        public static void RenameRecordSet(Header H, string NewName)
        {
            RenameRecordSet(H.Path, NewName);
        }

        public static void RemameRecordSet(RecordSet Data, string NewName)
        {
            RenameRecordSet(Data.Header, NewName);
        }

        public static void RenameBigRecordSet(string FullPath, string NewDB, string NewName)
        {
            Table brs = BinarySerializer.BufferTable(FullPath);
            RenameBigRecordSet(brs, NewDB, NewName);
        }

        public static void RenameBigRecordSet(string FullPath, string NewName)
        {
            Table brs = BinarySerializer.BufferTable(FullPath);
            RenameBigRecordSet(brs, NewName);
        }

        public static void RenameBigRecordSet(TableHeader H, string NewDB, string NewName)
        {
            Table brs = BinarySerializer.BufferTable(H);
            DataSetManager.RenameBigRecordSet(brs, NewDB, NewName);
        }

        public static void RenameBigRecordSet(TableHeader H, string NewName)
        {
            Table brs = BinarySerializer.BufferTable(H);
            DataSetManager.RenameBigRecordSet(brs, NewName);
        }

        public static void RenameBigRecordSet(Table Data, string NewName)
        {
            RenameBigRecordSet(Data, Data.Header.Directory, NewName);
        }

        public static void RenameBigRecordSet(Table Data, string NewDB, string NewName)
        {

            // Get the full file path of the curernt named record set //
            string path = Data.Header.Path;

            // Change the name on the main table //
            Data.Header.Name = NewName;
            Data.Header.Directory = NewDB;

            // Go through each record in the file table: open, change the name, change the name in the file table, then flush //
            foreach (Header h in Data.Headers)
            {

                RecordSet rs = BinarySerializer.BufferRecordSet(h);
                rs.Header.Name = NewName;
                h.Name = NewName;
                h.Directory = NewDB;
                BinarySerializer.FlushRecordSet(rs);

            }

            BinarySerializer.FlushTable(Data);

            // Drop the old table //
            DataSetManager.DropTable(path);

        }

        public static void RenameData(DataSet Data, string NewName)
        {
            if (Data.IsBig)
                RenameBigRecordSet(Data.ToBigRecordSet, NewName);
            else
                RemameRecordSet(Data.ToRecordSet, NewName);
        }

        // Exists //
        public static bool Exists(string Path)
        {
            try
            {
                return File.Exists(Path);
            }
            catch
            {
                return false;
            }
        }

        public static bool Exists(Header Data)
        {
            return Exists(Data.Path);
        }

        public static bool Exists(RecordSet Data)
        {
            return Exists(Data.Header.Path);
        }

        public static bool Exists(TableHeader Data)
        {
            return Exists(Data.Path);
        }

        public static bool Exists(Table Data)
        {
            return Exists(Data.Header.Path);
        }


    }

}
