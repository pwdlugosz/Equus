using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Nokota;
using Equus.Shire;

namespace Equus.Mustang
{

    public sealed class MRJob<M> where M : MapNode
    {

        private List<M> _Nodes;
        private Reducer<M> _Reducer;
        private DataSet _Data;
        private int _Partitions = Environment.ProcessorCount;
        private TablePartitioner _PartitionElements;

        public MRJob(DataSet Data, Reducer<M> Reducer, MapFactory<M> Factory, int Partitions)
        {

            // Set the reducer and data //
            this._Reducer = Reducer;
            this._Data = Data;
            this._Nodes = new List<M>();

            /* Check the partitions:
             *  -- if Data is either a recordset or a table with one extent, then use a small partitioner
             *  -- otherwise, use a large partitioner but limit the partitions to the extent count
             * 
             */
            if (Data.ExtentCount == 1)
            {

                this._PartitionElements = new SmallTablePartitioner(Data.PopAt(0), Partitions);
                this._Partitions = Partitions;

            }
            else
            {

                if (Data.ExtentCount < Partitions)
                    Partitions = Data.ExtentCount;

                this._PartitionElements = new LargeTablePartitioner(Data.ToBigRecordSet, Partitions);
                this._Partitions = Partitions;

            }

            // Go through and load up on map nodes //
            for (int i = 0; i < this._Partitions; i++)
            {
                M node = Factory.BuildNew(i);
                this._Nodes.Add(node);
            }

        }

        // Threads //
        public int Partitions
        {

            get
            {
                return this._Partitions;
            }

        }

        // Render //
        private Task[] RenderCollection()
        {

            // Set up the task collection //
            List<Task> tasks = new List<Task>();

            // Create a set of tasks //
            foreach (M m in this._Nodes)
            {

                // Build the task //
                //Task q = Task.Factory.StartNew(() => { m.Execute(this._PartitionElements); });
                Task r = new Task(() => { m.Execute(this._PartitionElements); });

                // Add it to the set //
                tasks.Add(r);

            }

            // Return //
            return tasks.ToArray();

        }

        public void ExecuteMapsConcurrently()
        {

            // Create a set of tasks //
            Task[] tasks = RenderCollection();

            // Run each task //
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (Task t in tasks)
            {

                // Start the task //
                t.Start();

            }

            // Tell each task to wait //
            Task.WaitAll(tasks);

            sw.Stop();
            Console.WriteLine("MapReduce Runtime: {0}", sw.Elapsed);
            // Close each mapping node //
            foreach (MapNode m in this._Nodes)
            {
                m.Close();
            }

            // We are done //

        }

        public void ExecuteMapsSequentially()
        {

            // Run each node //
            foreach (M node in this._Nodes)
            {
                node.Execute(this._PartitionElements);
            }

            // Close each mapping node //
            //foreach (MapNode m in this._Nodes)
            //{
            //    m.Close();
            //}

        }

        public void ReduceMaps()
        {
            this._Reducer.ConsumeAll(this._Nodes);
        }

    }

}
