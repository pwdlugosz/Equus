using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Shire;

namespace Equus.Calabrese
{

    public static class FNodeFactory
    {

        // Value //
        public static FNode Value(bool Value)
        {
            return new FNodeValue(null, new Cell(Value));
        }

        public static FNode Value(long Value)
        {
            return new FNodeValue(null, new Cell(Value));
        }

        public static FNode Value(double Value)
        {
            return new FNodeValue(null, new Cell(Value));
        }

        public static FNode Value(DateTime Value)
        {
            return new FNodeValue(null, new Cell(Value));
        }

        public static FNode Value(string Value)
        {
            return new FNodeValue(null, new Cell(Value));
        }

        public static FNode Value(byte[] Value)
        {
            return new FNodeValue(null, new Cell(Value));
        }

        public static FNode Value(int Value)
        {
            return new FNodeValue(null, new Cell(Value));
        }

        public static FNode Value(CellAffinity Value)
        {
            return new FNodeValue(null, new Cell(Value));
        }

        public static FNode Value(Cell Value)
        {
            return new FNodeValue(null, Value);
        }

        // Fields //
        public static FNode Field(int Index, CellAffinity Type, int Size, Register Memory)
        {
            return new FNodeFieldRef(null, Index, Type, Size, Memory);
        }

        public static FNode Field(int Index, CellAffinity Type)
        {
            return Field(Index, Type, Schema.FixSize(Type, -1), null);
        }

        public static FNode Field(Schema Columns, string Name, Register Memory)
        {
            int idx = Columns.ColumnIndex(Name);
            return Field(idx, Columns.ColumnAffinity(idx), Columns.ColumnSize(idx), Memory);
        }

        public static FNode Field(Schema Columns, string Name)
        {
            return Field(Columns, Name, null);
        }

        // Functions //
        public static FNode Add(FNode Left, FNode Right)
        {
            FNode t = new FNodeResult(Left.ParentNode, CellFunctionFactory.LookUp(FunctionNames.OP_ADD));
            t.AddChildren(Left, Right);
            return t;
        }

        public static FNode Subtract(FNode Left, FNode Right)
        {
            FNode t = new FNodeResult(Left.ParentNode, CellFunctionFactory.LookUp(FunctionNames.OP_SUB));
            t.AddChildren(Left, Right);
            return t;
        }

        public static FNode Multiply(FNode Left, FNode Right)
        {
            FNode t = new FNodeResult(Left.ParentNode, CellFunctionFactory.LookUp(FunctionNames.OP_MUL));
            t.AddChildren(Left, Right);
            return t;
        }

        public static FNode Divide(FNode Left, FNode Right)
        {
            FNode t = new FNodeResult(Left.ParentNode, CellFunctionFactory.LookUp(FunctionNames.OP_DIV));
            t.AddChildren(Left, Right);
            return t;
        }

        public static FNode Modulo(FNode Left, FNode Right)
        {
            FNode t = new FNodeResult(Left.ParentNode, CellFunctionFactory.LookUp(FunctionNames.OP_MOD));
            t.AddChildren(Left, Right);
            return t;
        }

        public static FNode StichtAnd(IEnumerable<FNode> Nodes)
        {

            if (Nodes.Count() == 0) return null;

            if (Nodes.Count() == 1) return Nodes.First();

            FNode node = Nodes.First();

            for (int i = 1; i < Nodes.Count(); i++)
                node = LinkAnd(node, Nodes.ElementAt(i));
            
            return node;

        }

        public static FNode LinkAnd(FNode Left, FNode Right)
        {

            FNodeResult node = new FNodeResult(null, CellFunctionFactory.LookUp("and"));
            node.AddChildNode(Left);
            node.AddChildNode(Right);
            return node;

        }

    }

}
