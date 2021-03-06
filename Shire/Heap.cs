﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Gidran;

namespace Equus.Shire
{

    public class Heap<T>
    {

        protected Dictionary<string, int> _RefSet;
        protected List<T> _Heap;

        public Heap()
        {
            _RefSet = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _Heap = new List<T>();
        }

        // Properties //
        public int Count
        {
            get { return this._RefSet.Count; }
        }

        public T this[string Name]
        {

            get
            {
                return this._Heap[this._RefSet[Name]];
            }

            set
            {
                this._Heap[this._RefSet[Name]] = value;
            }

        }

        public T this[int Pointer]
        {

            get
            {
                return this._Heap[Pointer];
            }

            set
            {
                this._Heap[Pointer] = value;
            }

        }

        // Methods //
        public bool Exists(string Name)
        {
            return this._RefSet.ContainsKey(Name);
        }

        public int GetPointer(string Name)
        {
            return this._RefSet[Name];
        }

        public void Allocate(string Name, T Value)
        {
            if (this.Exists(Name))
                throw new Exception(string.Format("Cannot allocate '{0}', an allocation with that name already exists", Name));
            this._RefSet.Add(Name, this._Heap.Count);
            this._Heap.Add(Value);
        }

        public void Deallocate(string Name)
        {

            if (this.Exists(Name))
            {
                int ptr = this.GetPointer(Name);
                this._RefSet.Remove(Name);
                this[ptr] = default(T);
            }

        }

        public void Reallocate(string Name, T Value)
        {
            this.Deallocate(Name);
            this.Allocate(Name, Value);
        }

        public void Vacum()
        {

            List<T> NewHeap = new List<T>();

            int NewPointer = 0;

            foreach (KeyValuePair<string, int> kv in this._RefSet)
            {

                // Add a value to the new heap //
                NewHeap.Add(this._Heap[kv.Value]);

                // Reset the pointer //
                this._RefSet[kv.Key] = NewPointer;

                // Increment the pointer //
                NewPointer++;

            }

            // Point the new heap //
            this._Heap = NewHeap;

        }

        public string Name(int Pointer)
        {
            return this._RefSet.Keys.ToArray()[Pointer];
        }

        public Dictionary<string, T> Entries
        {
            get
            {
                Dictionary<string, T> values = new Dictionary<string, T>();
                foreach (KeyValuePair<string, int> kv in this._RefSet)
                    values.Add(kv.Key, this[kv.Value]);
                return values;
            }
        }

    }

    public sealed class CellHeap : Heap<Cell>
    {

        public CellHeap()
            : base()
        {
        }

    }

    public sealed class MemoryStruct
    {

        public CellHeap Scalars;
        public Heap<CellMatrix> Arrays;

        public MemoryStruct(CellHeap UseScalars, Heap<CellMatrix> UseArrays, bool UseIsLocal)
        {
            this.Scalars = UseScalars;
            this.Arrays = UseArrays;
            
        }

        public MemoryStruct(bool UseIsLocal)
            : this(new CellHeap(), new Heap<CellMatrix>(), UseIsLocal)
        {
        }

        public bool IsLocal
        {
            get;
            private set;
        }

    }

}
