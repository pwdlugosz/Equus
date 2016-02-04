using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Mustang
{

    public abstract class Reducer<M> where M : MapNode
    {

        public Reducer()
        {
        }

        public abstract void Consume(M Node);

        public virtual void ConsumeAll(IEnumerable<M> Nodes)
        {

            foreach (M n in Nodes)
            {
                this.Consume(n);
            }

        }

    }

}
