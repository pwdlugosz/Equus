﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Fjord;

namespace Equus.HScript.Parameters
{

    public sealed class HParameter
    {

        private HParameterAffinity _affinity;
        private DataSet _data;
        private FNode _expression;
        private FNodeSet _expression_set;
        private Lambda _lambda;
        private MNode _matrix;

        public HParameter(DataSet Value)
        {
            this._affinity = HParameterAffinity.DataSet;
            this._data = Value;
        }

        public HParameter(FNode Value)
        {
            this._affinity = HParameterAffinity.Expression;
            this._expression = Value;
        }

        public HParameter(FNodeSet Value)
        {
            this._affinity = HParameterAffinity.ExpressionSet;
            this._expression_set = Value;
        }

        public HParameter(Lambda Value)
        {
            this._affinity = HParameterAffinity.Lambda;
            this._lambda = Value;
        }

        public HParameter(MNode Value)
        {
            this._affinity = HParameterAffinity.Matrix;
            this._matrix = Value;
        }

        public HParameterAffinity Affinity
        {
            get { return this._affinity; }
        }

        public DataSet Data
        {
            get { return this._data; }
        }

        public FNode Expression
        {
            get { return this._expression; }
        }

        public FNodeSet ExpressionSet
        {
            get { return this._expression_set; }
        }

        public Lambda Lambda
        {
            get { return this._lambda; }
        }

        public MNode Matrix
        {
            get { return this._matrix; }
        }

    }

}