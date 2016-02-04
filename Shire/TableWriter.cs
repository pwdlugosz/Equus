using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Shire
{

    public class TableWriter : RecordWriter
    {

        private Table _ParentData;
        private int _ptrData = 0;

        public TableWriter(Table Data, Predicate Having)
            : base(Data.PopLastOrGrow(), Having)
        {
            this._ParentData = Data;
            this._ptrData = 0;
            this._columns = Data.Columns;
        }

        public TableWriter(Table Data)
            : this(Data, Predicate.TrueForAll)
        {
        }

        // Properties //
        public override bool EndOfData
        {
            get
            {
                return this._ptrRecord >= this._Data.Count && this._ptrData >= this._ParentData.Count;
            }
        }
        
        public override Schema SourceSchema
        {
            get
            {
                return _ParentData.Columns;
            }
        }

        public bool EndOfExtent
        {
            get
            {
                return this._ptrRecord >= this._Data.Count;
            }
        }

        public bool EndOfCache
        {
            get
            {
                return this._ptrData >= this._ParentData.Count;
            }
        }

        public int ExtentPosition
        {
            get { return this._ptrData; }
        }

        // Methods //
        protected override void UnFilteredAdvance()
        {

            this._ptrRecord += INCREMENTER;

            // If we are at the end of the extent, but not the cache, buffer the next extent //
            if (this.EndOfExtent && !this.EndOfData)
            {

                // Advance and pop //
                this._ptrData++;
                this._Data = this._ParentData.PopAt(this._ptrData);

                // Reset the record pointer //
                this._ptrRecord = 0;

            }


        }

        public override void Insert(Record Data)
        {

            // Check to see if the base data is full, if so, push back to the parent and ask the parent for a new extent //
            if (this._Data.IsFull)
            {

                // Pass the data back to the parent //
                this._ParentData.Push(this._Data);

                // Ask the parent to allocate a new record set //
                this._Data = this._ParentData.Grow();

                // Increment our position //
                this._ptrData++;

            }

            base.Insert(Data);

        }

        public override void Close()
        {
            this._ParentData.CursorClose(this._Data);
        }

    }

}
