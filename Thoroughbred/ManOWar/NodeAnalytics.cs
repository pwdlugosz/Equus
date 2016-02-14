using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Thoroughbred.ManOWar
{

    public static class NodeAnalytics
    {

        public static List<NeuralNode> AllChildren(NeuralNode Node)
        {
            List<NeuralNode> nodes = new List<NeuralNode>();
            DecompileHelper(Node, nodes);
            return nodes;
        }

        private static void DecompileHelper(NeuralNode Node, List<NeuralNode> Nodes)
        {

            foreach (NodeLink n in Node.Children)
            {
                if (!Nodes.Contains(n.Child))
                {
                    Nodes.Add(n.Child);
                    DecompileHelper(n.Child, Nodes);
                }
            }

        }

        public static bool IsCircular(NeuralNode Node)
        {
            return AllChildren(Node).Contains(Node);
        }

        public static bool AnyCircular(NeuralNode Node)
        {
            List<NeuralNode> Nodes = AllChildren(Node);
            foreach (NeuralNode n in Nodes)
                if (IsCircular(n)) return true;
            return IsCircular(Node);
        }

    }

}
