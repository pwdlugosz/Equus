using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Calabrese
{

    internal static class FNodeGradient
    {

        internal static bool Compact = true;
        private static bool Debug = false;

        /// <summary>
        /// Calculates the gradient (first derivative) of a node with respect to a parameter node passed (pointer node).
        /// This method calls FNodeCompacter.CompactNode if the class level static variable 'Compact' is true (by default it is set to true).
        /// The gradient calculation leaves a lot of un-needed expressions that could be cancled out.
        /// </summary>
        /// <param name="Node">The node to calculate the gradient over</param>
        /// <param name="X">The parameter we are differentiating with respect to</param>
        /// <returns>A node representing a gradient</returns>
        internal static FNode Gradient(FNode Node, FNodePointer X)
        {

            // The node is a pointer node //
            if (Node.Affinity == FNodeAffinity.PointerNode)
            {
                if ((Node as FNodePointer).PointerName == X.PointerName)
                    return new FNodeValue(Node.ParentNode, Cell.OneValue(X.ReturnAffinity()));
                else
                    return new FNodeValue(Node.ParentNode, Cell.ZeroValue(X.ReturnAffinity()));
            }

            // The node is not a function node //
            if (Node.Affinity != FNodeAffinity.ResultNode)
                return new FNodeValue(Node.ParentNode, Cell.ZeroValue(Node.ReturnAffinity()));

            // Check if the node, which we now know is a function, has X as a decendant //
            if (!FNodeAnalysis.IsDecendent(X, Node))
                return new FNodeValue(Node.ParentNode, Cell.ZeroValue(X.ReturnAffinity()));

            // Otherwise we have to do work :( //

            // Get the name signiture //
            string name_sig = (Node as FNodeResult).InnerFunction.NameSig;

            // Go through each differentiable function //
            FNode t = null;
            switch (name_sig)
            {

                case FunctionNames.UNI_PLUS:
                    t = GradientOfUniPlus(Node, X);
                    break;
                case FunctionNames.UNI_MINUS:
                    t = GradientOfUniMinus(Node, X);
                    break;

                case FunctionNames.OP_ADD:
                    t = GradientOfAdd(Node, X);
                    break;
                case FunctionNames.OP_SUB:
                    t = GradientOfSubtract(Node, X);
                    break;
                case FunctionNames.OP_MUL:
                    t = GradientOfMultiply(Node, X);
                    break;
                case FunctionNames.OP_DIV:
                    t = GradientOfDivide(Node, X);
                    break;

                case FunctionNames.FUNC_LOG:
                    t = GradientOfLog(Node, X);
                    break;
                case FunctionNames.FUNC_EXP:
                    t = GradientOfExp(Node, X);
                    break;
                case FunctionNames.FUNC_POWER:
                    t = GradientOfPowerLower(Node, X);
                    break;

                case FunctionNames.FUNC_SIN:
                    t = GradientOfSin(Node, X);
                    break;
                case FunctionNames.FUNC_COS:
                    t = GradientOfCos(Node, X);
                    break;
                case FunctionNames.FUNC_TAN:
                    t = GradientOfTan(Node, X);
                    break;

                case FunctionNames.FUNC_SINH:
                    t = GradientOfSinh(Node, X);
                    break;
                case FunctionNames.FUNC_COSH:
                    t = GradientOfCosh(Node, X);
                    break;
                case FunctionNames.FUNC_TANH:
                    t = GradientOfTanh(Node, X);
                    break;

                case FunctionNames.FUNC_LOGIT:
                    t = GradientOfLogit(Node, X);
                    break;

                case FunctionNames.FUNC_NDIST:
                    t = GradientOfNDIST(Node, X);
                    break;
                
                default:
                    throw new Exception(string.Format("Function is not differentiable : {0}", name_sig));
            }

            if (Compact)
                t = FNodeCompacter.CompactNode(t);

            return t;

        }

        /// <summary>
        /// Calculates the gradient (first derivative) of a node with respect to a parameter node passed (pointer node).
        /// This method calls FNodeCompacter.CompactNode if the class level static variable 'Compact' is true (by default it is set to true).
        /// The gradient calculation leaves a lot of un-needed expressions that could be cancled out.
        /// </summary>
        /// <param name="Node">The node to calculate the gradient over</param>
        /// <param name="PointerRef">The parameter name we are differentiating with respect to</param>
        /// <returns>A node representing a gradient</returns>
        internal static FNode Gradient(FNode Node, string PointerRef)
        {
            return Gradient(Node, new FNodePointer(null, PointerRef, CellAffinity.DOUBLE, 8));
        }

        /// <summary>
        /// Calculates the gradient (first derivative) of a node with respect to a parameter node passed (pointer node).
        /// This method calls FNodeCompacter.CompactNode if the class level static variable 'Compact' is true (by default it is set to true).
        /// The gradient calculation leaves a lot of un-needed expressions that could be cancled out.
        /// </summary>
        /// <param name="Tree">The node to calculate the gradient over</param>
        /// <param name="PointerRef">The parameter names we are differentiating with respect to; must have the same number of elements as the Tree parameter</param>
        /// <returns>A tree representing all gradients</returns>
        internal static FNodeSet Gradient(FNodeSet Tree, string[] PointerRefs)
        {

            if (Tree.Count != PointerRefs.Length)
                throw new Exception(string.Format("Tree and pointers have different counts {0} : {1}", Tree.Count, PointerRefs.Length));

            FNodeSet tree = new FNodeSet();

            for (int i = 0; i < Tree.Count; i++)
                tree.Add(PointerRefs[i], Gradient(Tree[i], PointerRefs[i]));

            return tree;

        }

        /// <summary>
        /// Calculates the gradients (first derivative) of a node with respect to all parameters present.
        /// This method calls FNodeCompacter.CompactNode if the class level static variable 'Compact' is true (by default it is set to true).
        /// The gradient calculation leaves a lot of un-needed expressions that could be cancled out.
        /// </summary>
        /// <param name="Node">The node to calculate the gradient over</param>
        /// <returns>A node representing a gradient</returns>
        internal static FNodeSet Gradient(FNode Node)
        {

            string[] variables = FNodeAnalysis.AllPointersRefs(Node).Distinct().ToArray();
            FNodeSet tree = new FNodeSet();
            foreach (string n in variables)
                tree.Add(n, Gradient(Node, n));

            return tree;

        }

        // F(X) = -G(X), F'(X) = -G'(X)
        private static FNode GradientOfUniMinus(FNode Node, FNodePointer X)
        {
            if(Debug) Comm.WriteLine("GRADIENT : UNI_MINUS");
            FNodeResult t = new FNodeResult(Node.ParentNode, new CellUniMinus());
            t.AddChildNode(Gradient(Node.Children[0], X));
            return t;
        }

        // F(X) = +G(X), F'(X) = G'(X)
        private static FNode GradientOfUniPlus(FNode Node, FNodePointer X)
        {
            if (Debug) Comm.WriteLine("GRADIENT : UNI_PLUS");
            return Gradient(Node.Children[0], X);
        }

        // F(X) = G(X) + H(X), F'(X) = G'(X) + H'(X)
        private static FNode GradientOfAdd(FNode Node, FNodePointer X)
        {
            if (Debug) Comm.WriteLine("GRADIENT : ADD");
            FNodeResult t = new FNodeResult(Node.ParentNode, new CellBinPlus());
            t.AddChildNode(Gradient(Node.Children[0], X));
            t.AddChildNode(Gradient(Node.Children[1], X));
            return t;
        }

        // F(X) = G(X) - H(X), F'(X) = G'(X) - H'(X)
        private static FNode GradientOfSubtract(FNode Node, FNodePointer X)
        {
            if (Debug) Comm.WriteLine("GRADIENT : SUB");
            FNodeResult t = new FNodeResult(Node.ParentNode, new CellBinMinus());
            t.AddChildNode(Gradient(Node.Children[0], X));
            t.AddChildNode(Gradient(Node.Children[1], X));
            return t;
        }

        // F(X) = G(X) * H(X), F'(X) = G'(X) * H(X) + G(X) * H'(X)
        private static FNode GradientOfMultiply(FNode Node, FNodePointer X)
        {

            if (Debug) Comm.WriteLine("GRADIENT : MULT");
            // G'(X) * H(X) //
            FNodeResult t = new FNodeResult(Node.ParentNode, new CellBinMult());
            t.AddChildNode(Gradient(Node.Children[0], X));
            t.AddChildNode(Node.Children[1]);

            // G(X) * H'(X) //
            FNodeResult u = new FNodeResult(Node.ParentNode, new CellBinMult());
            u.AddChildNode(Node.Children[0]);
            u.AddChildNode(Gradient(Node.Children[1], X));

            // Final Node //
            FNodeResult v = new FNodeResult(Node.ParentNode, new CellBinPlus());
            v.AddChildNode(t);
            v.AddChildNode(u);

            return v;
        }

        // F(X) = G(X) / H(X), F'(X) = (G'(X) * H(X) - G(X) * H'(X)) / (H(X) * H(X))
        private static FNode GradientOfDivide(FNode Node, FNodePointer X)
        {

            if (Debug) Comm.WriteLine("GRADIENT : DIVIDE");

            // Need to handle the case where H is not a function of X //
            if (!FNodeAnalysis.ContainsPointerRef(Node[1], X.PointerName))
            {
                FNode a = Gradient(Node[0], X);
                return FNodeFactory.Divide(a, Node[1]);
            }

            // G'(X) * H(X) //
            FNodeResult t = new FNodeResult(Node.ParentNode, new CellBinMult());
            t.AddChildNode(Gradient(Node.Children[0], X));
            t.AddChildNode(Node.Children[1]);

            // G(X) * H'(X) //
            FNodeResult u = new FNodeResult(Node.ParentNode, new CellBinMult());
            u.AddChildNode(Node.Children[0]);
            u.AddChildNode(Gradient(Node.Children[1], X));

            // G'(X) * H(X) - G(X) * H'(X) //
            FNodeResult v = new FNodeResult(Node.ParentNode, new CellBinMinus());
            v.AddChildNode(t);
            v.AddChildNode(u);

            // H(X) * H(X) //
            FNodeResult w = new FNodeResult(Node.ParentNode, new CellFuncFVPower());
            w.AddChildNode(Node.Children[1]);
            w.AddChildNode(new FNodeValue(null, new Cell(2.00)));

            // Final Node //
            FNodeResult x = new FNodeResult(Node.ParentNode, new CellBinDiv());
            x.AddChildNode(v);
            x.AddChildNode(w);

            return x;
        }

        // F(X) = exp(G(X)), F'(X) = exp(G(X)) * G'(X), or simplified F(X) * G'(X) 
        private static FNode GradientOfExp(FNode Node, FNodePointer X)
        {

            // F(X) * G'(X) //
            FNodeResult t = new FNodeResult(Node.ParentNode, new CellBinMult());
            t.AddChildNode(Node.CloneOfMe());
            t.AddChildNode(Gradient(Node.Children[0], X));

            return t;
        }

        // F(X) = log(G(X)), F'(X) = 1 / G(X) * G'(X)
        private static FNode GradientOfLog(FNode Node, FNodePointer X)
        {

            // 1 / G(X) //
            FNodeResult t = new FNodeResult(Node.ParentNode, new CellFuncFVPower());
            t.AddChildNode(Node.Children[0]);
            t.AddChildNode(new FNodeValue(null, new Cell(-1.00)));

            // 1 / G(X) //
            FNodeResult u = new FNodeResult(Node.ParentNode, new CellBinMult());
            u.AddChildNode(t);
            u.AddChildNode(Gradient(t.Children[0], X));

            return u;

        }

        // F(X) = sin(G(X)), F'(X) = cos(G(X)) * G'(X)
        private static FNode GradientOfSin(FNode Node, FNodePointer X)
        {

            // cos(G(X)) //
            FNodeResult t = new FNodeResult(Node.ParentNode, new CellFuncFVCos());
            t.AddChildNode(Node.Children[0]);

            // 1 / G(X) //
            FNodeResult u = new FNodeResult(Node.ParentNode, new CellBinMult());
            u.AddChildNode(t);
            u.AddChildNode(Gradient(t.Children[0], X));

            return u;

        }

        // F(X) = cos(G(X)), F'(X) = -sin(G(X)) * G'(X)
        private static FNode GradientOfCos(FNode Node, FNodePointer X)
        {

            // sin(G(X)) //
            FNodeResult t = new FNodeResult(Node.ParentNode, new CellFuncFVSin());
            t.AddChildNode(Node.Children[0]);

            // -sin(G(X)) //
            FNodeResult u = new FNodeResult(t, new CellUniMinus());
            u.AddChildNode(t);

            // -sin(G(X)) * G'(X) //
            FNodeResult v = new FNodeResult(Node.ParentNode, new CellBinMult());
            v.AddChildNode(u);
            v.AddChildNode(Gradient(t.Children[0], X));

            return v;

        }

        // F(X) = tan(X), F'(X) = Power(cos(x) , -2.00)
        private static FNode GradientOfTan(FNode Node, FNodePointer X)
        {

            // cos(G(X)) //
            FNodeResult t = new FNodeResult(null, new CellFuncFVCos());
            t.AddChildNode(Node.Children[0]);

            // power(cos(G(x)),2) //
            FNodeResult u = new FNodeResult(t, new CellFuncFVPower());
            u.AddChildNode(t);
            u.AddChildNode(new FNodeValue(null, new Cell(-2.00)));

            // power(cos(G(x)),2) * G'(X) //
            FNodeResult v = new FNodeResult(Node.ParentNode, new CellBinMult());
            v.AddChildNode(u);
            v.AddChildNode(Gradient(t.Children[0], X));

            return u;

        }

        // F(X) = sinh(G(X)), F'(X) = cosh(G(X)) * G'(X)
        private static FNode GradientOfSinh(FNode Node, FNodePointer X)
        {

            // cosh(G(X)) //
            FNodeResult t = new FNodeResult(Node.ParentNode, new CellFuncFVCosh());
            t.AddChildNode(Node.Children[0]);

            // 1 / G(X) //
            FNodeResult u = new FNodeResult(Node.ParentNode, new CellBinMult());
            u.AddChildNode(t);
            u.AddChildNode(Gradient(Node.Children[0], X));

            return u;

        }

        // F(X) = cosh(G(X)), F'(X) = sinh(G(X)) * G'(X)
        private static FNode GradientOfCosh(FNode Node, FNodePointer X)
        {

            // sinh(G(X)) //
            FNodeResult t = new FNodeResult(Node.ParentNode, new CellFuncFVSinh());
            t.AddChildNode(Node.Children[0]);

            // sinh(G(X)) * G'(X) //
            FNodeResult u = new FNodeResult(Node.ParentNode, new CellBinMult());
            u.AddChildNode(t);
            u.AddChildNode(Gradient(Node.Children[0], X));

            return u;

        }

        // F(X) = tanh(X), F'(X) = Power(cosh(x) , -2.00)
        private static FNode GradientOfTanh(FNode Node, FNodePointer X)
        {

            // cosh(G(X)) //
            FNodeResult t = new FNodeResult(null, new CellFuncFVCosh());
            t.AddChildNode(Node.Children[0]);

            // power(cosh(G(x)),2) //
            FNodeResult u = new FNodeResult(t, new CellFuncFVPower());
            u.AddChildNode(t);
            u.AddChildNode(new FNodeValue(null, new Cell(-2.00)));

            // power(cosh(G(x)),2) * G'(X) //
            FNodeResult v = new FNodeResult(Node.ParentNode, new CellBinMult());
            v.AddChildNode(u);
            v.AddChildNode(Gradient(Node.Children[0], X));

            return u;

        }

        // F(X) = power(G(X), N), F'(X) = power(G(X), N - 1) * G'(X) * N, or G'(X) if N == 1, N must not be a decendant of X
        private static FNode GradientOfPowerLower(FNode Node, FNodePointer X)
        {

            // Throw an exception if X is decendant of N, in otherwords F(X) = Power(G(X), H(X))
            if (FNodeAnalysis.IsDecendent(X, Node.Children[1]))
                throw new Exception(string.Format("Cannot differentiate the power function with the form: Power(G(X), H(X)); H(X) cannot have a relation to X"));

            // Build 'N-1' //
            FNode n_minus_one = new FNodeResult(Node.ParentNode, new CellBinMinus());
            n_minus_one.AddChildNode(Node.Children[1]);
            n_minus_one.AddChildNode(new FNodeValue(null, new Cell(1.00)));

            // Get Power(G(X), N-1) //
            FNode power_gx_n_minus_one = new FNodeResult(Node.ParentNode, new CellFuncFVPower());
            power_gx_n_minus_one.AddChildNode(Node.Children[0]);
            power_gx_n_minus_one.AddChildNode(n_minus_one);

            // Get Power(G(X), N-1) * G'(X) //
            FNode t = new FNodeResult(Node.ParentNode, new CellBinMult());
            t.AddChildNode(power_gx_n_minus_one);
            t.AddChildNode(Gradient(Node.Children[0], X));

            // Get Power(G(X), N-1) * G'(X) * N //
            FNode u = new FNodeResult(Node.ParentNode, new CellBinMult());
            u.AddChildNode(t);
            u.AddChildNode(Node.Children[1]);

            return u;

        }

        // F(X) = power(Y, G(X)), F'(X) = LOG(Y) * power(Y, G(X)) * G'(X)
        private static FNode GradientOfPowerUpper(FNode Node, FNodePointer X)
        {

            // Throw an exception if X is decendant of Y, in otherwords F(X) = Power(G(X), H(X))
            if (FNodeAnalysis.IsDecendent(X, Node.Children[1]))
                throw new Exception(string.Format("Cannot differentiate the power function with the form: Power(G(X), H(X)); G(X) cannot have a relation to X"));

            // LOG(Y) //
            FNode log_y = new FNodeResult(null, new CellFuncFVLog());
            log_y.AddChildNode(Node.Children[0]);

            // Get Power(Y, G(X)) * LOG(Y) //
            FNode pow_f_dx = new FNodeResult(Node.ParentNode, new CellBinMult());
            pow_f_dx.AddChildNode(Node.CloneOfMe());
            pow_f_dx.AddChildNode(log_y);

            // Get Power(G(X), N-1) * G'(X) //
            FNode t = new FNodeResult(Node.ParentNode, new CellBinMult());
            t.AddChildNode(pow_f_dx);
            t.AddChildNode(Gradient(Node.Children[1], X));

            return t;

        }

        // F(X) = 1 / (1 + exp(-X)) = logit(X), F'(X) = logit(X) * (1 - logit(X))
        private static FNode GradientOfLogit(FNode Node, FNodePointer X)
        {

            // Logit, create two to avoid incest in the node tree //
            FNode t = X.CloneOfMe();
            FNode u = X.CloneOfMe();
            FNode v = FNodeFactory.Value(1D);

            return t * (v - u);

        }

        // F(X) = NDIST, F'(X) = 0.398942 * EXP(-0.50 * X * X) 
        private static FNode GradientOfNDIST(FNode Node, FNodePointer X)
        {

            // Build -0.5 * X * X //
            FNode t = FNodeFactory.Value(-0.5) * Node[0] * Node[0]; // - 0.5 * X^2
            FNode u = (new FNodeResult(t, new CellFuncFVExp())); // exp(-0.5 * X^2)
            u.AddChildNode(t);
            FNode v = u * FNodeFactory.Value(0.398942); // 1/sqrt(2pi)
            FNode w = v * Gradient(Node[0], X); // X' * exp(-0.5 * X^2) / sqrt(2 * pi)
            
            return v;

        }

    }


}
