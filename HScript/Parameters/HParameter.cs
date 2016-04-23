using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Fjord;
using Equus.Shire;

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
        private CellHeap _heap;
        private int _ref;

        public HParameter(DataSet Value)
        {
            this._affinity = HParameterAffinity.DataSet;
            this._data = Value;
        }

        public HParameter(FNode Value)
        {
            this._affinity = HParameterAffinity.Expression;
            this._expression = Value;
            this._expression_set = new FNodeSet();
            this._expression_set.Add(Value); // in case a set of one was passed 
        }

        public HParameter(FNodeSet Value)
        {
            this._affinity = HParameterAffinity.ExpressionSet;
            this._expression_set = Value;
            this._expression = Value[0];
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

        public HParameter(CellHeap Heap, string Value)
        {
            this._heap = Heap;
            this._ref = this._heap.GetPointer(Value);
            this._affinity = HParameterAffinity.Scalar;
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

        public CellHeap Heap
        {
            get { return this._heap; }
        }

        public int HeapRef
        {
            get { return this._ref; }
        }

    }

}
