using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Calabrese
{

    public sealed class FNodeCompacter
    {

        private int _Ticks = 0;
        private int _Tocks = 0;
        private int _Cycles = 0;

        // Compact all //
        public FNode Compact(FNode Node)
        {

            // Clone the current node //
            //FNode t = Node.CloneOfMe();

            this._Tocks = 1;
            while (this._Tocks != 0)
            {

                // Reset the tock variables //
                this._Tocks = 0;

                // Compact the leaf node; note that we may need to do this again //
                Node = CompactUnit(Node);

                // Accumulate the ticks //
                this._Ticks += this._Tocks;

                // Accumulate the cycles //
                this._Cycles++;

            }

            // return the compacted node //
            return Node;

        }

        public int TotalCompacts
        {
            get { return this._Ticks; }
        }

        public int Cycles
        {
            get { return this._Cycles; }
        }

        private FNode CompactUnit(FNode Node)
        {

            for (int i = 0; i < Node.Children.Count; i++)
                Node.Children[i] = CompactUnit(Node.Children[i]);

            return CompactSingle(Node);

        }

        private FNode CompactSingle(FNode Node)
        {

            // The order we do these is optimized to reduce the number of tock loops //
            Node = CompactPower(Node);
            Node = CompactMultDivMod(Node);
            Node = CompactAddSub(Node);
            Node = CompactUni(Node);
            Node = CompactCancleOut(Node);
            Node = CompactStaticArguments(Node);

            return Node;

        }

        // A - A -> 0
        // A / A -> 1
        private FNode CompactCancleOut(FNode Node)
        {

            if (Node.Affinity != FNodeAffinity.ResultNode)
                return Node;

            FNodeResult x = (Node as FNodeResult);
            string name = x.InnerFunction.NameSig;

            // Check that the node is either - or / //
            if (name != FunctionNames.OP_SUB && name != FunctionNames.OP_DIV && name != FunctionNames.OP_DIV2)
                return Node;

            // Build an equality checker //
            IEqualityComparer<FNode> lne = new FNodeEquality();

            // Check if A == B //
            if (!lne.Equals(Node.Children[0], Node.Children[1]))
                return Node;

            // Check for A - A -> 0 //
            if (name == FunctionNames.OP_SUB)
                return new FNodeValue(Node.ParentNode, Cell.ZeroValue(CellAffinity.DOUBLE));

            // Check for A - A -> 0 //
            if (name == FunctionNames.OP_DIV || name == FunctionNames.OP_DIV2)
                return new FNodeValue(Node.ParentNode, Cell.OneValue(CellAffinity.DOUBLE));

            return Node;

        }

        // -(-A) -> A
        // !(!A) -> A
        // -c -> -c where c is a constant
        // !c -> !c where c is a constant
        private FNode CompactUni(FNode Node)
        {

            if (Node.Affinity != FNodeAffinity.ResultNode)
                return Node;

            FNodeResult x = (Node as FNodeResult);
            string name = x.InnerFunction.NameSig;

            // Check that the node is either -A, +A, !A //
            if (name != FunctionNames.UNI_MINUS && name != FunctionNames.UNI_PLUS && name != FunctionNames.UNI_NOT)
                return Node;

            // Check for the child being a constant //
            if (Node.Children[0].Affinity == FNodeAffinity.ValueNode)
            {
                Cell c = (Node.Children[0] as FNodeValue).InnerValue;
                if (name == FunctionNames.UNI_MINUS)
                    c = -c;
                if (name == FunctionNames.UNI_NOT)
                    c = !c;
                return new FNodeValue(Node.ParentNode, c);
            }

            // Check that A = F(B) //
            if (Node.Children[0].Affinity != FNodeAffinity.ResultNode)
                return Node;

            // Get the name of the function of the child node //
            string sub_name = (Node.Children[0] as FNodeResult).InnerFunction.NameSig;

            // Check for -(-A) //
            if (name == FunctionNames.UNI_MINUS && sub_name == FunctionNames.UNI_MINUS)
                return Node.Children[0].Children[0];

            // Check for !(!A) //
            if (name == FunctionNames.UNI_NOT && sub_name == FunctionNames.UNI_NOT)
                return Node.Children[0].Children[0];

            return Node;

        }

        // A + 0 or 0 + A or A - 0 -> A
        // 0 - A -> -A 
        // A + -B -> A - B
        private FNode CompactAddSub(FNode Node)
        {

            if (Node.Affinity != FNodeAffinity.ResultNode)
                return Node;

            FNodeResult x = (Node as FNodeResult);
            string name = x.InnerFunction.NameSig;

            if (name != FunctionNames.OP_ADD && name != FunctionNames.OP_SUB)
                return Node;

            // Look for A + 0 or A - 0 -> A //
            if (IsStaticZero(Node.Children[1]))
            {
                this._Tocks++;
                return Node.Children[0];
            }

            // Look for 0 + A -> A //
            if (IsStaticZero(Node.Children[0]) && name == FunctionNames.OP_ADD)
            {
                this._Tocks++;
                return Node.Children[1];
            }

            // Look for 0 - A -> -A //
            if (IsStaticZero(Node.Children[0]) && name == FunctionNames.OP_SUB)
            {
                this._Tocks++;
                FNode t = new FNodeResult(Node.ParentNode, CellFunctionFactory.LookUp(FunctionNames.UNI_MINUS));
                t.AddChildNode(Node.Children[1]);
                return t;
            }

            // Look for A + -B -> A - B //
            if (IsUniNegative(Node.Children[1]) && name == FunctionNames.OP_ADD)
            {
                this._Tocks++;
                FNode t = new FNodeResult(Node.ParentNode, CellFunctionFactory.LookUp(FunctionNames.OP_SUB));
                t.AddChildNode(Node.Children[0]);
                t.AddChildNode(Node.Children[1].Children[0]);
                return t;
            }

            // Look for -A + B -> B - A //
            if (IsUniNegative(Node.Children[0]) && name == FunctionNames.OP_ADD)
            {
                this._Tocks++;
                FNode t = new FNodeResult(Node.ParentNode, CellFunctionFactory.LookUp(FunctionNames.OP_SUB));
                t.AddChildNode(Node.Children[1]);
                t.AddChildNode(Node.Children[0].Children[0]);
                return t;
            }

            // Look for A - -B -> A + B //
            if (IsUniNegative(Node.Children[1]) && name == FunctionNames.OP_SUB)
            {
                this._Tocks++;
                FNode t = new FNodeResult(Node.ParentNode, CellFunctionFactory.LookUp(FunctionNames.OP_ADD));
                t.AddChildNode(Node.Children[0]);
                t.AddChildNode(Node.Children[1].Children[0]);
                return t;
            }

            return Node;

        }

        // A * 1 or 1 * A or A / 1 or A /? 1 or A % 1 -> A 
        // A * -1 or -1 * A or A / -1 or A /? -1 or A % -1 -> -A 
        // A * 0, 0 * A, 0 / A, 0 /? A, A /? 0, 0 % A -> 0 
        // A / 0, A % 0 -> null
        private FNode CompactMultDivMod(FNode Node)
        {

            if (Node.Affinity != FNodeAffinity.ResultNode)
                return Node;

            FNodeResult x = (Node as FNodeResult);
            string name = x.InnerFunction.NameSig;

            if (name != FunctionNames.OP_MUL
                && name != FunctionNames.OP_DIV
                && name != FunctionNames.OP_DIV2
                && name != FunctionNames.OP_MOD)
                return Node;

            // A * 1 or A / 1 or A /? 1 or A % 1 //
            if (IsStaticOne(Node.Children[1]))
            {
                this._Tocks++;
                return Node.Children[0];
            }

            // 1 * A //
            if (IsStaticOne(Node.Children[0]) && name == FunctionNames.OP_MUL)
            {
                this._Tocks++;
                return Node.Children[1];
            }

            // A * -1 or A / -1 or A /? -1 or A % -1 //
            if (IsStaticMinusOne(Node.Children[1]))
            {
                this._Tocks++;
                FNode t = new FNodeResult(Node.ParentNode, new CellUniMinus());
                t.AddChildNode(Node.Children[0]);
                return t;
            }

            // -1 * A //
            if (IsStaticMinusOne(Node.Children[0]) && name == FunctionNames.OP_MUL)
            {
                this._Tocks++;
                FNode t = new FNodeResult(Node.ParentNode, new CellUniMinus());
                t.AddChildNode(Node.Children[1]);
                return t;
            }

            // Look 0 * A, 0 / A, 0 /? A, 0 % A //
            if (IsStaticZero(Node.Children[0]))
            {
                this._Tocks++;
                return new FNodeValue(Node.ParentNode, new Cell(0.00));
            }

            // A * 0, A /? 0 //
            if (IsStaticZero(Node.Children[1]) && (name == FunctionNames.OP_MUL || name == FunctionNames.OP_DIV2))
            {
                this._Tocks++;
                return new FNodeValue(Node.ParentNode, new Cell(0.00));
            }

            // A / 0, A % 0 //
            if (IsStaticZero(Node.Children[1]) && (name == FunctionNames.OP_DIV || name == FunctionNames.OP_MOD))
            {
                this._Tocks++;
                return new FNodeValue(Node.ParentNode, Cell.NULL_DOUBLE);
            }

            return Node;

        }

        // 1 * 2 + 3 -> 5
        private FNode CompactStaticArguments(FNode Node)
        {

            if (ChildrenAreAllStatic(Node))
            {
                this._Tocks++;
                return new FNodeValue(Node.ParentNode, Node.Evaluate());
            }

            return Node;

        }

        // power(A,1) -> A
        // power(A,0) -> 1
        private FNode CompactPower(FNode Node)
        {

            if (Node.Affinity != FNodeAffinity.ResultNode)
                return Node;

            FNodeResult x = (Node as FNodeResult);
            string name = x.InnerFunction.NameSig;

            if (name != FunctionNames.FUNC_POWER)
                return Node;

            // Check the second argument of power(A,B) looking for B == 1 //
            if (IsStaticOne(Node.Children[1]))
                return Node.Children[0];

            // Check the second argumnet of power(A,B) looging for B == 0, if so return static 1.000, even power(0,0) = 1.000 //
            if (IsStaticZero(Node.Children[1]))
                return new FNodeValue(Node.ParentNode, new Cell(1.000));

            return Node;

        }

        // Helpers //
        public static bool IsStaticZero(FNode Node)
        {
            if (Node.Affinity == FNodeAffinity.ValueNode)
                return (Node as FNodeValue).InnerValue == Cell.ZeroValue(Node.ReturnAffinity());
            return false;
        }

        public static bool IsStaticOne(FNode Node)
        {
            if (Node.Affinity == FNodeAffinity.ValueNode)
                return (Node as FNodeValue).InnerValue == Cell.OneValue(Node.ReturnAffinity());
            return false;
        }

        public static bool IsStaticMinusOne(FNode Node)
        {
            if (Node.Affinity == FNodeAffinity.ValueNode)
                return (Node as FNodeValue).InnerValue == -Cell.OneValue(Node.ReturnAffinity());
            if (Node.Affinity == FNodeAffinity.ResultNode)
            {
                FNodeResult x = (Node as FNodeResult);
                if (x.InnerFunction.NameSig == FunctionNames.UNI_MINUS && IsStaticOne(x.Children[0]))
                    return true;
            }
            return false;
        }

        public static bool IsUniNegative(FNode Node)
        {
            if (Node.Affinity == FNodeAffinity.ResultNode)
            {
                FNodeResult x = (Node as FNodeResult);
                return x.InnerFunction.NameSig == FunctionNames.UNI_MINUS;
            }
            return false;
        }

        public static bool ChildrenAreAllStatic(FNode Node)
        {

            if (Node.IsTerminal)
                return false;

            foreach (FNode n in Node.Children)
            {
                if (n.Affinity != FNodeAffinity.ValueNode)
                    return false;
            }
            return true;

        }

        // Opperands //
        public static FNode CompactNode(FNode Node)
        {
            FNodeCompacter lnc = new FNodeCompacter();
            return lnc.Compact(Node);
        }

        public static FNodeSet CompactTree(FNodeSet Tree)
        {

            FNodeSet t = new FNodeSet();

            foreach (FNode n in Tree.Nodes)
                t.Add(CompactNode(n));

            return t;

        }

        /// <summary>
        /// Binds a leaf node to another node
        /// </summary>
        /// <param name="MainNode">The node containing a pointer node that will be bound</param>
        /// <param name="ParameterNode">The node that will be bound to the MainNode</param>
        /// <param name="PointerNodeName">The name of the pointer the ParameterNode will be replacing</param>
        /// <returns></returns>
        public static FNode Bind(FNode MainNode, FNode ParameterNode, string PointerNodeName)
        {

            // Clone the main node //
            FNode t = MainNode.CloneOfMe();

            // Decompile t //
            List<FNodePointer> refs = FNodeAnalysis.AllPointers(t);

            // Replace the pointer node with the parameter node //
            foreach (FNodePointer x in refs)
            {
                if (x.PointerName == PointerNodeName)
                    FNodeAnalysis.ReplaceNode(x, ParameterNode);
            }

            return t;

        }

    }


}
