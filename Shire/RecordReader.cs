using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Shire
{

    public class RecordReader : IDisposable
    {

        protected const int DEFAULT_POINTER = 0;
	    protected const int INCREMENTER = 1;

	    protected int _ptrRecord = DEFAULT_POINTER;
	    protected RecordSet _Data;
	    protected bool _IsFiltered = false;
        protected Predicate _Where;
        protected Schema _columns;
	
	    // Constructor //
	    public RecordReader(RecordSet From, Predicate Where)
	    {
		
		    this._ptrRecord = DEFAULT_POINTER;
		    this._Data = From;
		    this._Where = Where;
            
            // Assign the where to a register pointing to 'this' //
            StreamRegister reg = new StreamRegister(this);
            this._Where.Node.AssignRegister(reg);
            
            // Fix the default //
            if (!Where.Default)
		    {
			    this._IsFiltered = true;
                while (!this.CheckFilter && !this.EndOfData)
                    this.Advance();
		    }

            // This is used to handle the writer class that inherits the reader //
            if (From != null)
                this._columns = From.Columns;
	    }

	    public RecordReader(RecordSet From)
            :this(From, Predicate.TrueForAll)
	    {
	    }
	
	    // Properties //
        public virtual bool EndOfData
	    {
            get
            {
                return this._ptrRecord >= this._Data.Count;
            }
	    }

        public virtual bool BeginingOfData
        {
            get
            {
                return this._ptrRecord == 0;
            }
        }

        protected virtual bool CheckFilter
        {
            get
            {
                if (this.EndOfData == true) return false;
                return this._Where.Render();
            }
        }

        public virtual int Position
        {
            get { return this._ptrRecord; }
        }

        public virtual long SetID
        {
            get 
            {
                if (this._Data.IsAttached) return this._Data.Header.ID;
                return 0;
            }
        }

        public virtual Schema SourceSchema
        {
            get { return this._columns; }
        }

        public virtual RecordSet BaseData
        {
            get { return this._Data; }
        }

	    // Methods //
        protected virtual void UnFilteredAdvance()
	    {
		    this._ptrRecord += INCREMENTER;
	    }

        protected virtual void FilteredAdvance()
	    {
            // Break if end of stream //
            if (this.EndOfData == true)
                return;
            // While the filter is false, advance, but advance at lease once //
            do
                this.UnFilteredAdvance();
            while (this.CheckFilter == false && this.EndOfData == false);
	    }

        public virtual void Advance()
	    {
		
		    if (this._IsFiltered == false)
                this.UnFilteredAdvance();
		    else
                this.FilteredAdvance();
		
	    }

        public virtual void Advance(int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                if (this.EndOfData) return;
                this.Advance();
            }
        }

        protected virtual void UnFilteredRevert()
        {
            this._ptrRecord--;
            if (this._ptrRecord < 0)
                this._ptrRecord = 0;
        }

        protected virtual void FilteredRevert()
        {
            // Break if end of stream //
            if (this.BeginingOfData == true)
                return;
            // While the filter is false, advance, but advance at lease once //
            do
                this.UnFilteredRevert();
            while (this.CheckFilter == false && this.BeginingOfData == false);
        }

        public virtual void Revert()
        {
            if (this._IsFiltered == false)
                this.UnFilteredRevert();
            else
                this.FilteredRevert();
        }

        public virtual void Revert(int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                if (this.BeginingOfData) return;
                this.Revert();
            }
        }

        // Reads //
        public virtual Record Read()
	    {
            return this._Data[this._ptrRecord];
	    }

        public virtual Record ReadNext()
	    {
		    Record cr = this.Read();
		    this.Advance();
		    return cr;
	    }

        // Implementations //
        void IDisposable.Dispose()
        {

        }

    }

}
