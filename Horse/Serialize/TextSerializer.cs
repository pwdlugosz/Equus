using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Equus.Shire;

namespace Equus.Horse
{

    public static class TextSerializer
    {

        // Text IO //
        public static void FlushText(string FullPath, RecordReader R, char Delim, bool Headers)
        {

            // Build a stream //
            using (StreamWriter sw = new StreamWriter(FullPath))
            {

                // Write the name string //
                if (Headers)
                    sw.WriteLine(R.SourceSchema.ToNameString(Delim));

                // Write all records //
                while (!R.EndOfData)
                {
                    string value = R.ReadNext().ToString(Delim);
                    if (!R.EndOfData)
                        sw.WriteLine(value);
                    else
                        sw.Write(value);
                }

            }

        }

        public static void BufferText(string FullPath, RecordWriter W, int Skip, char[] Delim, char Escape)
        {

            // Read text //
            using (StreamReader sr = new StreamReader(FullPath))
            {

                // Handle headers //
                int ticks = 0;
                while (ticks < Skip)
                {
                    ticks++;
                    sr.ReadLine();
                }

                // Loop //
                while (sr.EndOfStream == false)
                {
                    W.Insert(Splitter.ToRecord(sr.ReadLine(), W.SourceSchema, Delim, Escape));
                }

            }

        }

        // Text serialization //
        public static string ToString(RecordSet Data, Key K, char ColumnDelim, char RowDelim)
        {

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < Data.Count; i++)
            {
                sb.Append(Data[i].ToString(K, ColumnDelim));
                if (i != Data.Count - 1) sb.Append(RowDelim);
            }
            return sb.ToString();

        }

        public static string ToString(RecordSet Data, Key K, char ColumnDelim)
        {
            return ToString(Data, K, ColumnDelim, '\n');
        }

        public static string ToString(RecordSet Data, Key K)
        {
            return ToString(Data, K, ',');
        }

        public static string ToString(RecordSet Data, char ColumnDelim, char RowDelim)
        {
            Key k = Key.Build(Data.Columns.Count);
            return ToString(Data, k, ColumnDelim, RowDelim);
        }

        public static string ToString(RecordSet Data, char ColumnDelim)
        {
            return ToString(Data, ColumnDelim, '\n');
        }

        public static string ToString(RecordSet Data)
        {
            return ToString(Data, ',');
        }

    }
}
