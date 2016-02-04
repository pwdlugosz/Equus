using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Calabrese
{

    public class FNodePointer : FNode
    {

        private string _NameID;
        private CellAffinity _Type;
        private int _Size;

        public FNodePointer(FNode Parent, string RefName, CellAffinity Type, int Size)
            : base(Parent, FNodeAffinity.PointerNode)
        {
            this._Type = Type;
            this._NameID = RefName;
            this._name = RefName;
            this._Size = Schema.FixSize(Type, Size);
        }

        public override Cell Evaluate()
        {
            throw new Exception(string.Format("Cannot evaluate pointer nodes; Name '{0}'", _NameID));
        }

        public override CellAffinity ReturnAffinity()
        {
            return this._Type;
        }

        public override string ToString()
        {
            return this.Affinity.ToString() + " : " + _NameID;
        }

        public override int GetHashCode()
        {
            return this._NameID.GetHashCode() ^ FNode.HashCode(this._Cache);
        }

        public override string Unparse(Schema S)
        {
            return this._Type.ToString().ToUpper() + "." + this._NameID.ToString();
        }

        public override FNode CloneOfMe()
        {
            return new FNodePointer(this.ParentNode, this._NameID, this._Type, this._Size);
        }

        public override int DataSize()
        {
            return this._Size;
        }

        public string PointerName
        {
            get { return _NameID; }
        }

    }


}
