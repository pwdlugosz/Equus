﻿using System;
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

    public sealed class MNodeDivide : MNode
    {

        public MNodeDivide(MNode Parent)
            : base(Parent)
        {
        }

        public override CellMatrix Evaluate()
        {
            return this[0].Evaluate() / this[1].Evaluate();
        }

        public override CellAffinity ReturnAffinity()
        {
            return this[0].ReturnAffinity();
        }

        public override MNode CloneOfMe()
        {
            MNode node = new MNodeDivide(this.ParentNode);
            foreach (MNode m in this._Cache)
                node.AddChildNode(m.CloneOfMe());
            return node;
        }

    }

}
