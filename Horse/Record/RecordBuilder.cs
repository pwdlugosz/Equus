using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Horse
{

    /// <summary>
    /// Akin to StringBuilder, this is a dynamic record builder
    /// </summary>
    public sealed class RecordBuilder
    {

        private List<Cell> _cache;

        /// <summary>
        /// Creates an empty RecordBuilder
        /// </summary>
        public RecordBuilder()
        {
            this._cache = new List<Cell>();
        }

        /// <summary>
        /// Creates a RecordBuilder and loads it with data
        /// </summary>
        /// <param name="Data">The cells to load the builder with</param>
        public RecordBuilder(params Cell[] Data)
            :this()
        {
            this.Add(Data);
        }

        /// <summary>
        /// Adds a cell to the right of the record
        /// </summary>
        /// <param name="Data">The cell to add</param>
        public void Add(Cell Data)
        {
            this._cache.Add(Data);
        }

        /// <summary>
        /// Adds a collection of cells to the data
        /// </summary>
        /// <param name="Data">The cells to add</param>
        public void Add(IEnumerable<Cell> Data)
        {
            foreach (Cell c in Data)
                this.Add(c);
        }

        /// <summary>
        /// Adds a collection of cells, derived from the record, to the data
        /// </summary>
        /// <param name="Data">The cells to add</param>
        public void Add(Record Data)
        {
            this.Add(Data.BaseArray);
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">An element which will be cast as a HORSE.INT type, which is a .NET long</param>
        public void Add(sbyte Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">An element which will be cast as a HORSE.INT type, which is a .NET long</param>
        public void Add(short Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">An element which will be cast as a HORSE.INT type, which is a .NET long</param>
        public void Add(int Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">An element which will be cast as a HORSE.INT type, which is a .NET long</param>
        public void Add(long Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">An element which will be cast as a HORSE.INT type, which is a .NET long</param>
        public void Add(byte Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">An element which will be cast as a HORSE.INT type, which is a .NET long</param>
        public void Add(ushort Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">An element which will be cast as a HORSE.INT type, which is a .NET long</param>
        public void Add(uint Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">An element which will be cast as a HORSE.INT type, which is a .NET long</param>
        public void Add(ulong Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">A boolean element</param>
        public void Add(bool Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">An element which will be cast as a HORSE.DOUBLE type, which is a .NET double</param>
        public void Add(float Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">An element which will be cast as a HORSE.DOUBLE type, which is a .NET double</param>
        public void Add(double Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">A date element</param>
        public void Add(DateTime Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">A string element</param>
        public void Add(string Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Adds an element to the right of the data
        /// </summary>
        /// <param name="Data">A byte array element</param>
        public void Add(byte[] Data)
        {
            this.Add(new Cell(Data));
        }

        /// <summary>
        /// Renders the current collection of cells into a record
        /// </summary>
        /// <returns>A record</returns>
        public Record ToRecord()
        {
            return new Record(this._cache.ToArray());
        }

    }

}
