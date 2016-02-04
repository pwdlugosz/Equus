using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Calabrese
{

    public static class FNodeAnalysis
    {

        // These next two tell us if the node is just a simple field or static value //
        public static bool IsFlatFieldNode(FNode Node)
        {
            if (Node.Affinity == FNodeAffinity.FieldRefNode && Node.IsTerminal) return true;
            return false;
        }

        public static bool IsFlatValueNode(FNode Node)
        {
            if (Node.Affinity == FNodeAffinity.FieldRefNode && Node.IsTerminal) return true;
            return false;
        }

        // These tell us if the nodes contain certain types //
        public static bool ContainsFieldNodes(FNode Node)
        {

            if (Node.Affinity == FNodeAffinity.FieldRefNode) return true;
            if (Node.Affinity == FNodeAffinity.ValueNode) return false;

            bool b = false;
            foreach (FNode n in Node.Children)
            {
                b = b || ContainsFieldNodes(n);
                if (b == true) return true;
            }

            return false;

        }

        public static bool ContainsValueNodes(FNode Node)
        {

            if (Node.Affinity == FNodeAffinity.FieldRefNode) return false;
            if (Node.Affinity == FNodeAffinity.ValueNode) return true;

            bool b = false;
            foreach (FNode n in Node.Children)
            {
                b = b || ContainsValueNodes(n);
                if (b == true) return true;
            }

            return false;

        }

        public static bool ContainsResultNodes(FNode Node)
        {

            if (Node.Affinity != FNodeAffinity.ResultNode) return false;
            
            foreach (FNode n in Node.Children)
                if (n.IsResult) return true;

            return false;

        }

        public static bool ContainsResultNode(FNode Node, string Signiture)
        {
            return AllResultNodes(Node, Signiture).Count == 0;
        }

        public static bool ContainsResultNode(FNode Node, CellFunction Func)
        {
            return ContainsResultNode(Node, Func.NameSig);
        }

        public static int FieldNodeCount(FNode Node)
        {

            if (Node.Affinity == FNodeAffinity.FieldRefNode) return 1;
            if (Node.Affinity == FNodeAffinity.ValueNode) return 0;

            int Counter = 0;
            foreach (FNode n in Node.Children)
                Counter += FieldNodeCount(n);

            return Counter;

        }

        public static int ValueNodeCount(FNode Node)
        {

            if (Node.Affinity == FNodeAffinity.FieldRefNode) return 0;
            if (Node.Affinity == FNodeAffinity.ValueNode) return 1;

            int Counter = 0;
            foreach (FNode n in Node.Children)
                Counter += ValueNodeCount(n);

            return Counter;

        }

        public static int ResultNodeCount(FNode Node)
        {

            if (Node.Affinity == FNodeAffinity.FieldRefNode) return 0;
            if (Node.Affinity == FNodeAffinity.ValueNode) return 0;

            int Counter = 1;
            foreach (FNode n in Node.Children)
                Counter += ResultNodeCount(n);

            return Counter;

        }

        // Decompiling Methods //
        public static List<FNode> Decompile(FNode Node)
        {

            // Create the master list //
            List<FNode> master = new List<FNode>();

            // Add the master node //
            //master.Add(Node);

            // Recursive method //
            DecompileHelper(Node, master);

            // Return //
            return master;

        }

        private static void DecompileHelper(FNode Node, List<FNode> AllNodes)
        {

            // Add all the child nodes //
            AllNodes.Add(Node);
            
            // Go through each child node and call this function //
            foreach (FNode n in Node.Children)
                DecompileHelper(n, AllNodes);

        }

        public static List<FNode> AllNodes(FNode Node, FNodeAffinity Affinity)
        {

            // All nodes that are fields //
            return Decompile(Node).Where((x) => { return x.Affinity == Affinity; }).ToList();

        }

        public static List<T> Convert<T>(List<FNode> Nodes) where T : FNode
        {
            List<T> nodes = Nodes.ConvertAll<T>((x) => { return x as T; });
            return nodes;
        }

        public static List<FNodePointer> AllPointers(FNode Node)
        {
            List<FNode> all_pointers = AllNodes(Node, FNodeAffinity.PointerNode);
            List<FNodePointer> converted_pointers = Convert<FNodePointer>(all_pointers);
            return converted_pointers;
        }

        public static List<string> AllPointersRefs(FNode Node)
        {
            return AllPointers(Node).ConvertAll<string>((node) => { return node.PointerName; });
        }

        public static List<FNodeResult> AllResultNodes(FNode Node, string Name)
        {

            List<FNode> all_nodes = AllNodes(Node, FNodeAffinity.ResultNode);
            return Convert<FNodeResult>(all_nodes).Where((x) => { return StringComparer.OrdinalIgnoreCase.Compare(x.InnerFunction.NameSig, Name) == 0;}).ToList();

        }

        public static Key AllFields(FNode Node)
        {

            List<FNode> all_nodes = AllNodes(Node, FNodeAffinity.ResultNode);
            List<FNodeFieldRef> fields = Convert<FNodeFieldRef>(all_nodes);
            Key k = new Key();
            foreach (FNodeFieldRef n in fields)
                k.Add(n.Index);
            return k;

        }

        public static FNodeSet AllFields(FNode Node, Schema Columns)
        {
            Key k = AllFields(Node);
            return new FNodeSet(Schema.Split(Columns, k));
        }

        public static List<Cell> AllCellValues(FNode Node)
        {

            List<FNode> all_nodes = AllNodes(Node, FNodeAffinity.ResultNode);
            List<FNodeValue> Values = Convert<FNodeValue>(all_nodes);
            
            List<Cell> data = new List<Cell>();
            foreach (FNodeValue n in Values)
                data.Add(n.InnerValue);
            return data;

        }

        public static List<string> AllCellFunctionNames(FNode Node)
        {

            List<FNode> all_nodes = AllNodes(Node, FNodeAffinity.ResultNode);
            List<FNodeResult> fields = Convert<FNodeResult>(all_nodes);

            List<string> data = new List<string>();
            foreach (FNodeResult n in fields)
                data.Add(n.InnerFunction.NameSig);
            return data;

        }

        public static List<int> AllFieldRefs(FNode Node)
        {

            List<FNode> nodes = FNodeAnalysis.Decompile(Node);
            List<int> refs = new List<int>();
            foreach (FNode n in nodes)
            {

                if (n is FNodeFieldRef)
                    refs.Add((n as FNodeFieldRef).Index);

            }
            return refs;

        }

        // Add, remove, replace //
        public static void ReplaceNode(FNode OriginalNode, FNode ReplaceWithNode)
        {

            /*
             * Basic premise is to replace a node in the tree with another node:
             * -- If the node has children, need to point the kids to the new parent node
             * -- If the node is a child, need to point its parent to the new node
             * 
             */

            // Point Children to the new node //
            if (!OriginalNode.IsTerminal)
            {
                foreach (FNode n in OriginalNode.Children)
                    n.ParentNode = ReplaceWithNode;
            }

            // Point parent to new node //
            if (!OriginalNode.IsMaster)
            {
                for (int i = 0; i < OriginalNode.ParentNode.Children.Count; i++)
                {
                    if (OriginalNode.ParentNode.Children[i] == OriginalNode)
                        OriginalNode.ParentNode.Children[i] = ReplaceWithNode;
                }
            }


        }

        public static void AddNode(FNodeResult NewParentNode, params FNode[] Nodes)
        {
            foreach (FNode n in Nodes)
                NewParentNode.AddChildNode(n);
        }

        public static void Disconnect(FNode Node)
        {
            if (Node.IsMaster) return;
            Node.ParentNode.Children.Remove(Node);
        }

        // Generational Methods //
        /// <summary>
        /// Traverses the tree to get the master parent node (parent node that has no parents)
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static FNode EveNode(FNode Node)
        {

            while (!Node.IsMaster)
                Node = Node.ParentNode;
            return Node;

        }

        /// <summary>
        /// Gets all nodes (including the one passed) that share the same parent node as the node passed
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static List<FNode> SiblingNodes(FNode Node)
        {

            if (Node.IsMaster || Node.Affinity != FNodeAffinity.ResultNode) return null;

            return new List<FNode>(Node.ParentNode.Children);

        }

        /// <summary>
        /// Provides all the terminal nodes for a given node
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static List<FNode> AllTerminalNodes(FNode Node)
        {
            return Decompile(Node).Where((n) => { return n.IsTerminal; }).ToList();
        }

        /// <summary>
        /// Returns any node that has a terminal child node
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static List<FNode> AllParentTerminalNodes(FNode Node)
        {
            List<FNode> nodes = new List<FNode>();
            if (Node == null) return nodes;
            if (Node.IsTerminal) return nodes;
            foreach(FNode n in AllTerminalNodes(Node))
            {
                if (!nodes.Contains(n.ParentNode))
                    nodes.Add(n);
            }
            return nodes;
        }

        /// <summary>
        /// Checks if 'Node' is a direct or distant child of eve
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="Eve"></param>
        /// <returns></returns>
        public static bool IsDecendent(FNode Node, FNode Eve)
        {
            return Decompile(Eve).Contains(Node, new FNodeEquality());
        }

        public static bool ContainsPointerRef(FNode Node, string Pointer)
        {

            if (Node.Affinity == FNodeAffinity.PointerNode)
                return (Node as FNodePointer).PointerName == Pointer;

            if (Node.Affinity != FNodeAffinity.ResultNode)
                return false;

            foreach (FNode n in Node.Children)
            {
                if (ContainsPointerRef(n, Pointer))
                    return true;
            }

            return false;

        }

        // Printing methods //
        private static string Tree(FNode N, StringBuilder SB, int Level)
        {

            SB.Append(new string(' ', Level * 2));
            SB.Append(Level);
            SB.Append(" : ");
            SB.Append(N.ToString());
            SB.Append(" : ");
            if (N.Affinity == FNodeAffinity.PointerNode)
                SB.Append("<Pointer>");
            else
                SB.Append(N.ReturnAffinity().ToString());
            SB.AppendLine();
            foreach (FNode x in N.Children)
                Tree(x, SB, Level + 1);

            return SB.ToString();

        }

        public static string Tree(FNode N)
        {
            int l = 0;
            StringBuilder sb = new StringBuilder();
            return Tree(N, sb, l);
        }

        private static string TreeLite(FNode N, StringBuilder SB, int Level)
        {

            SB.Append(new string(' ', Level * 2));
            SB.Append(Level);
            SB.Append(" : ");
            SB.Append(N.ToString());
            SB.AppendLine();
            foreach (FNode x in N.Children)
                TreeLite(x, SB, Level + 1);

            return SB.ToString();

        }

        public static string TreeLite(FNode N)
        {
            int l = 0;
            StringBuilder sb = new StringBuilder();
            return TreeLite(N, sb, l);
        }

        // 'Is' Methods //
        public static bool IsEqNode(FNode Node)
        {
            if (!Node.IsResult) return false;
            if ((Node as FNodeResult).InnerFunction.NameSig == CellFunctionFactory.LookUp("==").NameSig) return true;
            return false;
        }

        public static bool IsAndNode(FNode Node)
        {
            if (!Node.IsResult) return false;
            if ((Node as FNodeResult).InnerFunction.NameSig == CellFunctionFactory.LookUp("and").NameSig) return true;
            return false;
        }

        public static bool IsOrXorIfCaseNode(FNode Node)
        {

            if (!Node.IsResult) return false;
            string t = (Node as FNodeResult).InnerFunction.NameSig;
            string[] s = { CellFunctionFactory.LookUp("or").NameSig, CellFunctionFactory.LookUp("xor").NameSig, CellFunctionFactory.LookUp("if").NameSig };
            return s.Contains(t);

        }

        public static bool IsBinaryBoolean(FNode Node)
        {

            if (Node.Children.Count != 2)
                return false;

            return Node.Children[0].ReturnAffinity() == CellAffinity.BOOL && Node.Children[1].ReturnAffinity() == CellAffinity.BOOL;

        }

        public static bool IsBinaryBooleanAnd(FNode Node)
        {
            return IsAndNode(Node) && IsBinaryBoolean(Node);
        }

        public static bool IsAndTree(FNode Node)
        {

            // If the node does not have a boolean return value //
            if (Node.ReturnAffinity() != CellAffinity.BOOL)
                return false;

            // Get all the binary boolean nodes //
            List<FNode> nodes = FNodeAnalysis.Decompile(Node).Where((b) => { return FNodeAnalysis.IsBinaryBoolean(b); }).ToList();

            // Get all the non-AND nodes //
            int NonAndCount = nodes.Where((b) => { return !FNodeAnalysis.IsAndNode(b); }).Count();

            // If the count is not zero, then we cannot decompile //
            return NonAndCount == 0;

        }

        public static bool IsFieldToFieldEqaulity(FNode Node)
        {

            // Check if result //
            if (!Node.IsResult) 
                return false;
            
            // Convert to result, then check //
            FNodeResult r = Node as FNodeResult;

            // Check that the node is 'EE' //
            if (!IsEqNode(Node)) 
                return false;

            // Check that there are only two arguements //
            if (Node.Children.Count != 2) 
                return false;
            
            // Check that both kids are field nodes //
            if (Node.Children[0].Affinity == FNodeAffinity.FieldRefNode 
                && Node.Children[1].Affinity == FNodeAffinity.FieldRefNode) 
                return true;
            
            
            return false;


        }

        // Prints //
        public static void Print(List<FNode> Nodes)
        {
            foreach (FNode n in Nodes)
                Comm.WriteLine(n.ToString());
        }

        // Datamining support //
        public static FNodeSet Split(FNode Node, string FunctionName)
        {

            // Build Tree //
            FNodeSet tree = new FNodeSet();
            if (Node.Affinity != FNodeAffinity.ResultNode || Node.ToString() != FunctionName)
            {
                tree.Add(Node);
                return tree;
            }

            Stack<FNode> nodes = new Stack<FNode>();
            nodes.Push(Node);

            while (nodes.Count != 0)
            {

                FNode t = nodes.Pop();

                if (t.ToString() == FunctionName)
                {
                    foreach (FNode u in t.Children)
                        nodes.Push(u);
                }
                else
                {
                    tree.Add(t);
                }

            }

            return tree;

        }

        public static FNode BindAllPointersToStatic(FNode Node, Cell StaticValue)
        {

            // Create a clone //
            FNode t = Node.CloneOfMe();

            // Check if the clone is just a pointer //
            if (Node.Affinity == FNodeAffinity.PointerNode)
                return new FNodeValue(Node.ParentNode, StaticValue);

            // If the node has no children, return //
            if (Node.IsTerminal)
                return t;

            // If a more complex tree ... //
            List<FNodePointer> refs = FNodeAnalysis.AllPointers(t);

            // Walk the decompiled tree and remove all pointers //
            foreach (FNodePointer n in refs)
            {
                FNodeAnalysis.ReplaceNode(n, new FNodeValue(n.ParentNode, StaticValue));
            }
            return t;

        }

        public static FNodeSet BindAllPointersToStatic(FNodeSet Node, Cell StaticValue)
        {

            FNodeSet q = new FNodeSet();
            for (int i = 0; i < Node.Count; i++)
            {
                q.Add(BindAllPointersToStatic(Node[i], StaticValue));
            }

            return q;

        }


    }

    
   
}
