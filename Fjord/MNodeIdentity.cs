using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Gidran;
using Equus.Horse;
using Equus.Shire;
using Equus.Calabrese;

namespace Equus.Fjord
{
    public sealed class MNodeIdentity : MNode
    {

        private int _Size;
        private CellAffinity _Type;

        public MNodeIdentity(MNode Parent, int Size, CellAffinity Type)
            : base(Parent)
        {
            this._Size = Size;
            this._Type = Type;
        }

        public override CellMatrix Evaluate()
        {
            return CellMatrix.Identity(this._Size, this._Type);
        }

        public override CellAffinity ReturnAffinity()
        {
            return this._Type;
        }

        public override MNode CloneOfMe()
        {
            return new MNodeIdentity(this.ParentNode, this._Size, this._Type);
        }

    }

}
