using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Gidran;
using Equus.Calabrese;
using Equus.Shire;
using Equus.Horse;
using Equus.Fjord;

namespace Equus.HScript
{

    public sealed class MatrixVisitor : HScriptParserBaseVisitor<MNode>
    {

        private MNode _parent;

        public MatrixVisitor(Workspace Home, MemoryStruct LocalMemory, ExpressionVisitor Evaluator)
            : base()
        {
            this.Home = Home;
            this.Local = LocalMemory;
            this.Evaluator = Evaluator;
        }

        public ExpressionVisitor Evaluator
        {
            get;
            private set;
        }

        public Workspace Home
        {
            get;
            private set;
        }

        public MemoryStruct Local
        {
            get;
            private set;
        }

        public MNode MasterNode
        {
            get { return this._parent; }
        }

        // Overrides //
        public override MNode VisitMatrixMinus(HScriptParser.MatrixMinusContext context)
        {
            MNode m = new MNodeMinus(this._parent);
            m.AddChildNode(this.Visit(context.matrix_expression()));
            this._parent = m;
            return m;
        }

        public override MNode VisitMatrixInvert(HScriptParser.MatrixInvertContext context)
        {
            MNode m = new MNodeInverse(this._parent);
            m.AddChildNode(this.Visit(context.matrix_expression()));
            this._parent = m;
            return m;
        }

        public override MNode VisitMatrixTranspose(HScriptParser.MatrixTransposeContext context)
        {
            MNode m = new MNodeTranspose(this._parent);
            m.AddChildNode(this.Visit(context.matrix_expression()));
            this._parent = m;
            return m;
        }

        public override MNode VisitMatrixTrueMul(HScriptParser.MatrixTrueMulContext context)
        {
            MNode m = new MNodeMatrixMultiply(this._parent);
            m.AddChildNode(this.Visit(context.matrix_expression()[0]));
            m.AddChildNode(this.Visit(context.matrix_expression()[1]));
            this._parent = m;
            return m;
        }

        public override MNode VisitMatrixMulDiv(HScriptParser.MatrixMulDivContext context)
        {

            MNode m;
            if (context.op.Type == HScriptParser.MUL)
                m = new MNodeMultiply(this._parent);
            else if (context.op.Type == HScriptParser.DIV)
                m = new MNodeDivide(this._parent);
            else
                m = new MNodeCheckDivide(this._parent);

            m.AddChildNode(this.Visit(context.matrix_expression()[0]));
            m.AddChildNode(this.Visit(context.matrix_expression()[1]));
            this._parent = m;
            return m;

        }

        public override MNode VisitMatrixMulDivLeft(HScriptParser.MatrixMulDivLeftContext context)
        {

            FNode node = this.Evaluator.ToNode(context.expression());
            MNode m;
            // Third parameter here: 0 == scalar is on left side (A + B[]), 1 == scalar is on right side (A[] + B)
            if (context.op.Type == HScriptParser.MUL)
                m = new MNodeMultiplyScalar(this._parent, node, 0);
            else if (context.op.Type == HScriptParser.DIV)
                m = new MNodeDivideScalar(this._parent, node, 0);
            else
                m = new MNodeCheckDivideScalar(this._parent, node, 0);

            m.AddChildNode(this.Visit(context.matrix_expression()));
            this._parent = m;
            return m;

        }

        public override MNode VisitMatrixMulDivRight(HScriptParser.MatrixMulDivRightContext context)
        {

            FNode node = this.Evaluator.ToNode(context.expression());
            MNode m;
            // Third parameter here: 0 == scalar is on left side (A + B[]), 1 == scalar is on right side (A[] + B)
            if (context.op.Type == HScriptParser.MUL)
                m = new MNodeMultiplyScalar(this._parent, node, 1);
            else if (context.op.Type == HScriptParser.DIV)
                m = new MNodeDivideScalar(this._parent, node, 1);
            else
                m = new MNodeCheckDivideScalar(this._parent, node, 1);

            m.AddChildNode(this.Visit(context.matrix_expression()));
            this._parent = m;
            return m;

        }

        public override MNode VisitMatrixAddSub(HScriptParser.MatrixAddSubContext context)
        {

            MNode m;
            if (context.op.Type == HScriptParser.PLUS)
                m = new MNodeAdd(this._parent);
            else
                m = new MNodeSubtract(this._parent);

            m.AddChildNode(this.Visit(context.matrix_expression()[0]));
            m.AddChildNode(this.Visit(context.matrix_expression()[1]));
            this._parent = m;
            return m;

        }

        public override MNode VisitMatrixAddSubLeft(HScriptParser.MatrixAddSubLeftContext context)
        {

            FNode node = this.Evaluator.ToNode(context.expression());
            MNode m;
            // Third parameter here: 0 == scalar is on left side (A + B[]), 1 == scalar is on right side (A[] + B)
            if (context.op.Type == HScriptParser.PLUS)
                m = new MNodeAddScalar(this._parent, node, 0);
            else
                m = new MNodeSubtractScalar(this._parent, node, 0);

            m.AddChildNode(this.Visit(context.matrix_expression()));
            this._parent = m;
            return m;

        }

        public override MNode VisitMatrixAddSubRight(HScriptParser.MatrixAddSubRightContext context)
        {

            FNode node = this.Evaluator.ToNode(context.expression());
            MNode m;
            // Third parameter here: 0 == scalar is on left side (A + B[]), 1 == scalar is on right side (A[] + B)
            if (context.op.Type == HScriptParser.PLUS)
                m = new MNodeAddScalar(this._parent, node, 1);
            else
                m = new MNodeSubtractScalar(this._parent, node, 1);

            m.AddChildNode(this.Visit(context.matrix_expression()));
            this._parent = m;
            return m;

        }

        public override MNode VisitMatrixLookup(HScriptParser.MatrixLookupContext context)
        {
            string name = context.matrix_name().IDENTIFIER().GetText();
            if (this.Local.Arrays.Exists(name))
                return new MNodeHeap(this._parent, this.Local.Arrays, this.Local.Arrays.GetPointer(name));
            else if (this.Home.GlobalHeap.Arrays.Exists(name))
                return new MNodeHeap(this._parent, this.Home.GlobalHeap.Arrays, this.Home.GlobalHeap.Arrays.GetPointer(name));
            throw new Exception(string.Format("Matrix '{0}' not found", name));
        }

        public override MNode VisitMatrixLiteral(HScriptParser.MatrixLiteralContext context)
        {

            int Cols = context.matrix_literal().vector_literal().Count;
            int Rows = context.matrix_literal().vector_literal()[0].expression().Count;
            CellAffinity affinity = Evaluator.ToNode(context.matrix_literal().vector_literal()[0].expression()[0]).ReturnAffinity();
            CellMatrix matrix = new CellMatrix(Rows, Cols, affinity);
            
            for (int i = 0; i < Rows; i++)
            {
                
                for (int j = 0; j < Cols; j++)
                {

                    matrix[i,j] = this.Evaluator.ToNode(context.matrix_literal().vector_literal()[j].expression()[i]).Evaluate();

                }

            }

            return new MNodeLiteral(this._parent, matrix);

        }

        public override MNode VisitMatrixIdent(HScriptParser.MatrixIdentContext context)
        {

            int rank = (int)this.Evaluator.ToNode(context.expression()).Evaluate().INT;
            CellAffinity type = VisitorHelper.GetAffinity(context.type());

            return new MNodeIdentity(this.MasterNode, rank, type);

        }

        public override MNode VisitMatrixParen(HScriptParser.MatrixParenContext context)
        {
            return this.Visit(context.matrix_expression());
        }

        public MNode ToMatrix(HScriptParser.Matrix_expressionContext context)
        {
            return this.Visit(context);
        }

    }

}
