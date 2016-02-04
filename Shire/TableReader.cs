using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Shire
{

    public class TableReader : RecordReader
    {

        private Table _ParentData;
        private int _ptrData = 0;

        // Constructor //
        public TableReader(Table From, Predicate Where)
            : base(From.PopFirstOrGrow())
        {
            
            this._ptrData = DEFAULT_POINTER;
            this._ParentData = From;
            this._columns = From.Columns;
            this._Where = Where;
            this._Where.AssignRegister(new StreamRegister(this));
            if (!Where.Default)
            {
                this._IsFiltered = true;
                while (!this.CheckFilter && !this.EndOfData)
                    this.Advance();
            }

        }

        public TableReader(Table From)
            : this(From, Predicate.TrueForAll)
        {
        }

        // Properties //
        public override bool EndOfData
        {
            get
            {
                return this.EndOfCache && this.EndOfExtent || this._ParentData.Count == 0;
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
                return this._ptrData >= this._ParentData.ExtentCount;
            }
        }

        public int ExtentPosition
        {
            get { return this._ptrData; }
        }

        public override bool BeginingOfData
        {
            get
            {
                return base.BeginingOfData && this._ptrData == 0;
            }
        }

        // Methods //
        protected override void UnFilteredAdvance()
        {

            base.UnFilteredAdvance();

            // If we are at the end of the extent, but not the cache, buffer the next extent //
            if (this.EndOfExtent && !this.EndOfCache)
            {

                // Increment Pointer //
                this._ptrData++;

                // Exit if at the end of the cache //
                if (!this.EndOfCache)
                {

                    // Advance and pop //
                    this._Data = this._ParentData.PopAt(this._ptrData);

                    // Reset the record pointer //
                    this._ptrRecord = 0;

                }

            }


        }

        protected override void UnFilteredRevert()
        {

            // If at zero and not the first shard //
            if (base.Position == 0 && this._ptrData != 0)
            {

                // Decrement the anthology pointer //
                this._ptrData--;

                // Buffer the prior shard //
                this._Data = this._ParentData.PopAt(this._ptrData);

                // Change the position to be at the end //
                this._ptrRecord = this._Data.Count - 1;

            }
            else
            {
                base.UnFilteredRevert();
            }
        }

    }

}
