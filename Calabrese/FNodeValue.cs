using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Calabrese
{

    public class FNodeValue : FNode
    {

        private Cell _value;
        
        public FNodeValue(FNode Parent, Cell Value)
            : base(Parent, FNodeAffinity.ValueNode)
        {
            this._value = Value;
        }

        public override Cell Evaluate()
        {
            return _value;
        }

        public override CellAffinity ReturnAffinity()
        {
            return _value.Affinity;
        }

        public override string ToString()
        {
            return this.Affinity.ToString() + " : " + _value.ToString();
        }

        public override int GetHashCode()
        {
            return this._value.GetHashCode() ^ FNode.HashCode(this._Cache);
        }

        public override string Unparse(Schema S)
        {
            return this._value.ToString();
        }

        public override FNode CloneOfMe()
        {
            return new FNodeValue(this.ParentNode, this._value);
        }

        public override int DataSize()
        {
            return this._value.Size;
        }

        public Cell InnerValue
        {
            get { return this._value; }
        }

    }

}
