using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Mustang
{

    public abstract class MapFactory<M> where M : MapNode
    {

        public MapFactory()
        {
        }

        public abstract M BuildNew(int PartitionID);

    }

}
