using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Horse
{

    /// <summary>
    /// Generates a sort key from text passed. The text is in the format 'T1, ... , TN'. TN is either a column index or name, and has a suffix of either 'ASC', 'DESC' or missing.
    /// </summary>
    public sealed class SortKeyFactory
    {

        private List<char> _FieldDelims;
        private List<char> _AscDescDelims;
        private List<string> _AscTokens;
        private List<string> _DescTokens;

        /// <summary>
        /// Create a new instance of the sort key factory
        /// </summary>
        public SortKeyFactory()
        {

            this._FieldDelims = new List<char>();
            this._AscDescDelims = new List<char>();
            this._AscTokens = new List<string>();
            this._DescTokens = new List<string>();

        }

        /// <summary>
        /// Adds a deliminator for the fields in the key string
        /// </summary>
        /// <param name="Token">Delim to add</param>
        public void AddFieldDelim(char Token)
        {

            if (this._FieldDelims.Contains(Token))
                return;
            this._FieldDelims.Add(Token);

        }

        /// <summary>
        /// Adds one or more tokens to the delim collection for the fields in the key string
        /// </summary>
        /// <param name="Tokens">The collection of tokens to add</param>
        public void AddFieldDelims(params char[] Tokens)
        {

            foreach (char t in Tokens)
                this.AddFieldDelim(t);

        }

        /// <summary>
        /// Adds a delim for the filed DELIM AscOrDesc suffix
        /// </summary>
        /// <param name="Token">The token char to add</param>
        public void AddAscDescDelim(char Token)
        {

            if (this._AscDescDelims.Contains(Token))
                return;
            this._AscDescDelims.Add(Token);

        }

        /// <summary>
        /// Adds one or more delims for the filed DELIM AscOrDesc suffix
        /// </summary>
        /// <param name="Token">The tokens to add</param>
        public void AddAscDescDelims(params char[] Tokens)
        {
            foreach (char c in Tokens)
                this.AddAscDescDelim(c);
        }

        /// <summary>
        /// Adds a token that indicates an 'ASCENDING' sort affinity
        /// </summary>
        /// <param name="Token">The string text</param>
        public void AddAscToken(string Token)
        {
            if (this._AscTokens.Contains(Token, StringComparer.OrdinalIgnoreCase))
                return;
            this._AscTokens.Add(Token);
        }

        /// <summary>
        /// Adds one or more tokens that indicate an 'ASCENDING' sort affinity
        /// </summary>
        /// <param name="Token">The strings tokens</param>
        public void AddAscTokens(params string[] Tokens)
        {
            foreach (string t in Tokens)
                this.AddAscToken(t);
        }

        /// <summary>
        /// Adds a token that indicates an 'DESCENDING' sort affinity
        /// </summary>
        /// <param name="Token">The string text</param>
        public void AddDescToken(string Token)
        {
            if (this._DescTokens.Contains(Token, StringComparer.OrdinalIgnoreCase))
                return;
            this._DescTokens.Add(Token);
        }

        /// <summary>
        /// Adds one or more tokens that indicate an 'DESCENDING' sort affinity
        /// </summary>
        /// <param name="Token">The strings tokens</param>
        public void AddDescTokens(params string[] Tokens)
        {
            foreach (string t in Tokens)
                this.AddDescToken(t);
        }

        /// <summary>
        /// Renders a string into a key
        /// </summary>
        /// <param name="Columns">The schema that will be used in the key generation</param>
        /// <param name="Text">The key string to be parsed</param>
        /// <returns>A key with defined sort affinities</returns>
        public Key Render(Schema Columns, string Text)
        {

            Columns = Columns ?? new Schema();

            string[] tokens = Text.Split(this._FieldDelims.ToArray());

            Key k = new Key();

            foreach (string t in tokens)
            {

                string[] temp = t.Split(this._AscDescDelims.ToArray(),StringSplitOptions.RemoveEmptyEntries);
                
                // Get the field name //
                string field_or_index = temp[0];
                int idx = Columns.ColumnIndex(field_or_index);
                if (idx == -1)
                {
                    if (!int.TryParse(field_or_index, out idx))
                        throw new Exception("Element passed is neither a field or an index: " + field_or_index);
                }

                // Get the affinity //
                string asc_or_desc = (temp.Length < 2) ? "\0" : temp[1];
                KeyAffinity sort_type = KeyAffinity.Ascending;
                if (this._AscTokens.Contains(asc_or_desc, StringComparer.OrdinalIgnoreCase))
                    sort_type = KeyAffinity.Ascending;
                else if (this._DescTokens.Contains(asc_or_desc, StringComparer.OrdinalIgnoreCase))
                    sort_type = KeyAffinity.Descending;

                k.Add(idx, sort_type);

            }

            return k;

        }

        // Statics //
        /// <summary>
        /// Creates a default factor with:
        ///     ',' as the field delim
        ///     ' ', 'tab', 'newline' as the ascending-descending deliminators
        ///     'asc' and 'ascending' as the ascending strings
        ///     'desc' and 'descedning' as the descending strings
        /// Note: the ascending and descending comparisons ignore cases
        /// </summary>
        public static SortKeyFactory Default
        {
            
            get
            {

                SortKeyFactory skf = new SortKeyFactory();
                skf.AddFieldDelims(',');
                skf.AddAscDescDelims(' ', '\t', '\n');
                skf.AddAscTokens("asc", "ascending");
                skf.AddDescTokens("desc", "descending");
                return skf;

            }

        }

    }


}
