using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Horse
{

    public static class Splitter
    {

        public static string[] Split(string Text, char[] Delim, char Escape, bool KeepDelims, string EmptyString)
        {

            if (Delim.Contains(Escape))
                throw new Exception("The deliminators cannot contain the escape token");

            List<string> TempArray = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool InEscape = false;

            // Go through each char in string //
            foreach (char c in Text)
            {

                // turn on escaping //
                if (c == Escape)
                    InEscape = (!InEscape);

                // Slipt //
                if (!InEscape)
                {

                    // We found a deliminator //
                    if (Delim.Contains(c))
                    {

                        string s = sb.ToString();

                        // Check the size of the current cache and add the string //
                        if (s.Length == 0)
                            TempArray.Add(EmptyString);
                        else
                            TempArray.Add(s);

                        // Check to see if we need to keep our delims //
                        if (KeepDelims)
                            TempArray.Add(c.ToString());

                        sb = new StringBuilder();

                    }
                    else if (c != Escape)
                    {
                        sb.Append(c);
                    }

                }// end the string building phase //
                else if (c != Escape)
                {
                    sb.Append(c);
                }

            }

            if (InEscape)
                throw new Exception("Unclosed escape sequence");

            // Now do clean up //
            string t = sb.ToString();
            if (t.Length != 0)
            {
                if (!(t.Length == 1 && Delim.Contains(t[0])) || KeepDelims) TempArray.Add(sb.ToString());
            }
            else if (Delim.Contains(Text.Last()))
            {
                TempArray.Add(EmptyString);
            }
            return TempArray.ToArray();

        }

        public static string[] Split(string Text, char Delim, char Escape, bool KeepDelims, string EmptyString)
        {
            return Split(Text, new char[] { Delim }, Escape, KeepDelims, EmptyString);
        }

        // Print //
        public static void Print(string[] Text, int Level)
        {
            Level = Math.Max(Level, 0);
            string s = new string(' ', Level);
            foreach (string t in Text)
            {
                Comm.WriteLine(s + t);
            }
        }

        public static void Print(string[] Text)
        {
            Print(Text, 0);
        }

        public static void Print(List<string[]> Text)
        {
            int level = 5;
            string space = "-----------------------------------------------";
            foreach (string[] a in Text)
            {
                Comm.WriteLine(space);
                Print(a, level);
            }
        }

        // Records //
        public static Record ToRecord(string Text, Schema Columns, char[] Delims, char Escape)
        {

            // Split the data //
            string[] t = Splitter.Split(Text, Delims, Escape, false, Cell.NULL_STRING_TEXT);

            // Check the length //
            if (t.Length != Columns.Count)
                throw new Exception(string.Format("Text has {0} fields, but schema has {1} fields", t.Length, Columns.Count));

            // Build the record //
            RecordBuilder rb = new RecordBuilder();
            for (int i = 0; i < t.Length; i++)
                rb.Add(Cell.Parse(t[i], Columns.ColumnAffinity(i)));
            
            return rb.ToRecord();

        }

        public static Record ToRecord(string Text, Schema Columns, char[] Delims)
        {
            return ToRecord(Text, Columns, Delims, char.MaxValue);
        }

    }

}
