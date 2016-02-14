using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Calabrese
{

    public class Predicate
    {

        private FNode _Node;
        private bool _Default = false;

        public Predicate(FNode Node)
        {
            if (Node.ReturnAffinity() != CellAffinity.BOOL)
                throw new Exception(string.Format("Node passed does not return boolean : {0}", Node.ReturnAffinity()));
            this._Node = Node;
        }

        internal bool Default
        {
            get { return _Default; }
        }

        public FNode Node
        {
            get { return this._Node; }
        }

        public bool Render()
        {
            return this._Node.Evaluate().valueBOOL;
        }

        public void AssignRegister(Shire.Register Memory)
        {
            _Node.AssignRegister(Memory);
        }

        public Predicate NOT
        {
            get
            {
                FNode node = this._Node.CloneOfMe();
                FNode t = new FNodeResult(node.ParentNode, new CellUniNot());
                t.AddChildNode(node);
                return new Predicate(t);
            }
        }

        public string UnParse(Schema Columns)
        {
            return this._Node.Unparse(Columns);
        }

        public Predicate CloneOfMe()
        {
            return new Predicate(this._Node.CloneOfMe());
        }

        public static Predicate TrueForAll
        {
            get 
            {
                Predicate p = new Predicate(new FNodeValue(null, new Cell(true)));
                p._Default = true;
                return p; 
            }
        }

        public static Predicate FalseForAll
        {
            get { return new Predicate(new FNodeValue(null, new Cell(false))); }
        }

    }

    public static class PredicateFactory
    {

        public static Predicate Equals(FNode Left, FNode Right)
        {
            FNodeResult node = new FNodeResult(null, CellFunctionFactory.LookUp("=="));
            node.AddChildNode(Left);
            node.AddChildNode(Right);
            return new Predicate(node);
        }

        public static Predicate NotEquals(FNode Left, FNode Right)
        {
            FNodeResult node = new FNodeResult(null, CellFunctionFactory.LookUp("!="));
            node.AddChildNode(Left);
            node.AddChildNode(Right);
            return new Predicate(node);
        }

        public static Predicate IsNull(FNode N)
        {
            FNodeResult node = new FNodeResult(null, CellFunctionFactory.LookUp("isnull"));
            node.AddChildNode(N);
            return new Predicate(node);
        }

        public static Predicate IsNotNull(FNode N)
        {
            FNodeResult node = new FNodeResult(null, CellFunctionFactory.LookUp("isnotnull"));
            node.AddChildNode(N);
            return new Predicate(node);
        }

        public static Predicate LessThan(FNode Left, FNode Right)
        {
            FNodeResult node = new FNodeResult(null, CellFunctionFactory.LookUp("<"));
            node.AddChildNode(Left);
            node.AddChildNode(Right);
            return new Predicate(node);
        }

        public static Predicate GreaterThan(FNode Left, FNode Right)
        {
            FNodeResult node = new FNodeResult(null, CellFunctionFactory.LookUp(">"));
            node.AddChildNode(Left);
            node.AddChildNode(Right);
            return new Predicate(node);
        }

        public static Predicate LessThanOrEquals(FNode Left, FNode Right)
        {
            FNodeResult node = new FNodeResult(null, CellFunctionFactory.LookUp("<="));
            node.AddChildNode(Left);
            node.AddChildNode(Right);
            return new Predicate(node);
        }

        public static Predicate GreaterThanOrEquals(FNode Left, FNode Right)
        {
            FNodeResult node = new FNodeResult(null, CellFunctionFactory.LookUp(">="));
            node.AddChildNode(Left);
            node.AddChildNode(Right);
            return new Predicate(node);
        }

        public static Predicate Between(FNode Compare, FNode Lower, FNode Upper)
        {
            FNode LT = LessThan(Compare, Upper).Node;
            FNode GT = GreaterThan(Compare, Lower).Node;
            return And(LT, GT);
        }
        
        public static Predicate BetweenIN(FNode Compare, FNode Lower, FNode Upper)
        {
            FNode LT = LessThanOrEquals(Compare, Upper).Node;
            FNode GT = GreaterThanOrEquals(Compare, Lower).Node;
            return And(LT, GT);
        }

        public static Predicate And(params FNode[] Nodes)
        {
            FNodeResult and = new FNodeResult(null, new AndMany());
            and.AddChildren(Nodes);
            return new Predicate(and);
        }

        public static Predicate Or(params FNode[] Nodes)
        {
            FNodeResult or = new FNodeResult(null, new AndMany());
            or.AddChildren(Nodes);
            return new Predicate(or);
        }


    }
    
}
