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

    /*
     * A map-reduce job requires the following classes:
     *      
     *      MRJob<M>, where M is MapNode: holds all the map nodes and the single reducer
     *      MapNode: Provides support for processing one or more chunks; there will be one map node per a core, NOT per a chunk
     *      Reducer<M> where M is a MapNode: Provides support for consolidating all MapNodes into one object
     * 
     * A map-reduce job has the following steps:
     *      (1). Plan Phase: decides how many map nodes it needs, then constructs one per a thread; it also partitions the table into chunks
     *              and decides which core gets which chunk
     *              -- Note, this step is NOT exposed to HorseScript
     *              -- This step is completed by the MRJob class
     *      (2). Map Phase: concurrently executes each map over each chunk
     *              -- Note, this is done in parallel, and the user must define what goes on here in HorseScript
     *              -- This step is completed by each MapNode
     *      (3). Reduce Phase: consolidates all maps (optional)
     *              -- Note, this is optional and not done in parallel, and the user must define what goes on here in HorseScript
     *      (4). Finalize Phase: finalizes the job, potentially yielding an object
     *              -- Note, this is optional and not done in parallel, and the user must define what goes on here in HorseScript
     *              -- If the reducer is not used, one cannot use the finalizer
     * 
     * Example: Ordinary least squares over a table with 24 chunks on a machine with 4 cores
     *      (1). Plan Phase: Partition the table into 4 sets of 6 chunks, then build 4 map objects
     *      (2). Map Phase: over each table do the following
     *          -- XtX
     *          -- XtY
     *      (3). Reduce phase
     *          -- Master.XtX += Node.XtX
     *          -- Master.XtY += Node.XtY
     *      (4). Finalize phase
     *          -- Beta = (XtX)^-1 XtY
     * 
     * 
     * 
     */

    public abstract class MapNode
    {

        public MapNode(int ID)
        {
            this.ID = ID;
            this.IsClosed = false;
        }

        public int ID
        {
            get;
            private set;
        }

        public bool IsClosed
        {
            get;
            protected set;
        }

        public abstract void Execute(RecordSet Chunk);

        public void Execute(TablePartitioner Factory)
        {

            while (Factory.CanRequest(this.ID))
            {

                RecordSet rs = Factory.RequestNext(this.ID);

                if (rs == null)
                    return;

                this.Execute(rs);

            }

        }

        public virtual void Close()
        {
            this.IsClosed = true;
        }

    }



}
