using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Shire;
using Equus.QuarterHorse;
using Equus.Nokota;
using Equus.Andalusian;
using Equus.HScript;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Equus.HScript
{

    public static class ScriptHelper
    {

        public static Dictionary<string, FNode> BuildParameters(ExpressionVisitor Evaluator, HScriptParser.Bind_element_setContext context)
        {

            Dictionary<string, FNode> parameters = new Dictionary<string, FNode>();
            if (context == null)
                return parameters;
            
            foreach (HScriptParser.Bind_elementContext ctx in context.bind_element())
            {

                string key = ctx.SCALAR().GetText();
                bool is_dynamic = 
                    (ctx.K_STATIC() == null) 
                    ? true 
                    : false;
                FNode value = 
                    (is_dynamic) 
                    ? Evaluator.ToNode(ctx.expression()) 
                    : new FNodeValue(null, new Cell(ctx.expression().GetText(), false));
                parameters.Add(key, value);

            }
            return parameters;

        }

        public static string BindParamters(Dictionary<string, FNode> Parameters, string Script)
        {

            StringBuilder sb = new StringBuilder(Script);
            foreach (KeyValuePair<string, FNode> kv in Parameters)
                sb.Replace(kv.Key, kv.Value.Evaluate().valueSTRING);
            return sb.ToString();

        }

        /*
        public static void AppendCallStack(HScriptProcessor Caller, ExpressionVisitor Evaluator, HScriptParser.Inline_scriptContext context)
        {

            // Get the parameters //
            Dictionary<string, FNode> parms = BuildParameters(Evaluator, context.bind_element_set());

            // Get the script //
            string script = Evaluator.ToNode(context.expression()).Evaluate().valueSTRING;

            // Bind parameters //
            script = BindParamters(parms, script);
            Console.WriteLine(script);

            // Append the stack //
            Caller.LoadCallStack(script);

        }
        */

    }


}
