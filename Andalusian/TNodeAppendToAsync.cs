using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Andalusian
{
    
    public sealed class TNodeAppendToTableAsync : TNode
    {

        private Table _ParentData;
        private RecordSet _RecordCache;
        private FNodeSet _Fields;

        public TNodeAppendToTableAsync(TNode Parent, Table UseParentData, FNodeSet UseFields)
            : base(Parent)
        {

            if (UseParentData.Columns.GetHashCode() != UseFields.Columns.GetHashCode())
                throw new Exception("Output table and fields passed are not compatible");

            this._ParentData = UseParentData;
            this._RecordCache = new RecordSet(UseParentData.Columns);
            this._Fields = UseFields;

        }

        // Base Implementations //
        public override void Invoke()
        {

            // Sink the cache to the parent table //
            if (this._RecordCache.IsFull)
            {
                this._ParentData.Union(this._RecordCache);
                this._RecordCache = new RecordSet(this._ParentData.Columns);
            }

            // Add the record //
            this._RecordCache.Add(this._Fields.Evaluate());

        }

        public override void EndInvoke()
        {

            if (this._RecordCache.Count != 0)
            {
                this._ParentData.Union(this._RecordCache);
                BinarySerializer.Flush(this._ParentData);
            }

        }

        public override TNode CloneOfMe()
        {
            return new TNodeAppendToTableAsync(this.Parent, this._ParentData, this._Fields.CloneOfMe());
        }

    }

    public sealed class TNodeAppendToChunkAsync : TNode
    {

        private RecordSet _ParentData;
        private RecordSet _RecordCache;
        private FNodeSet _Fields;

        public TNodeAppendToChunkAsync(TNode Parent, RecordSet UseParentData, FNodeSet UseFields)
            : base(Parent)
        {

            if (UseParentData.Columns.GetHashCode() != UseFields.Columns.GetHashCode())
                throw new Exception("Output table and fields passed are not compatible");

            this._ParentData = UseParentData;
            this._RecordCache = new RecordSet(UseParentData.Columns);
            this._Fields = UseFields;

        }

        // Base Implementations //
        public override void Invoke()
        {

            // Sink the cache to the parent table //
            if (this._RecordCache.IsFull)
            {
                this._ParentData.Union(this._RecordCache);
                this._RecordCache = new RecordSet(this._ParentData.Columns);
            } 
            
            // Add the record //
            this._RecordCache.Add(this._Fields.Evaluate());

        }

        public override void EndInvoke()
        {

            if (this._RecordCache.Count != 0)
            {
                this._ParentData.Union(this._RecordCache);
            }
        }

        public override TNode CloneOfMe()
        {
            return new TNodeAppendToChunkAsync(this.Parent, this._ParentData, this._Fields.CloneOfMe());
        }


    }

}
