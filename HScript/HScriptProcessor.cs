using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
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

    public sealed class HScriptProcessor
    {

        public HScriptProcessor(Workspace UseHome)
        {
            this.Home = UseHome;
            this.CallStack = new Stack<HScriptParser.CommandContext>();
        }

        public Workspace Home
        {
            get;
            private set;
        }

        public Stack<HScriptParser.CommandContext> CallStack
        {
            get;
            set;
        }

        internal void LoadCallStack(string Script)
        {

            // Create a token stream and do lexal analysis //
            AntlrInputStream TextStream = new AntlrInputStream(Script);
            HScriptLexer HorseLexer = new HScriptLexer(TextStream);

            // Parse the script //
            CommonTokenStream HorseTokenStream = new CommonTokenStream(HorseLexer);
            HScriptParser HorseParser = new HScriptParser(HorseTokenStream);
            //HorseParser.RemoveErrorListeners();
            //HorseParser.AddErrorListener(new ParserErrorListener());
            
            // Create an executer object //
            CommandVisitor processor = new CommandVisitor(this.Home);

            // Create a temp structure to holde the context values //
            List<HScriptParser.CommandContext> cache = new List<HScriptParser.CommandContext>();

            // Load the call stack //
            try
            {
                foreach (HScriptParser.CommandContext context in HorseParser.compile_unit().command_set().command())
                    cache.Add(context);
            }
            catch (Exception e)
            {
                Comm.WriteLine(e.Message);
                throw e;
            }

            // Reverse the cache //
            cache.Reverse();

            // Append the cache to the call stack //
            foreach (HScriptParser.CommandContext ctx in cache)
                this.CallStack.Push(ctx);

        }

        public void Execute(string Script)
        {

            // Create an executer object //
            CommandVisitor processor = new CommandVisitor(this.Home);
            
            // Load the call stack //
            this.LoadCallStack(Script);

            // Run ... //
            while(this.CallStack.Count != 0)
            {

                // Load the context //
                HScriptParser.CommandContext ctx = this.CallStack.Pop();

                // Check if this is an execution //
                if (ctx.inline_script() != null)
                {

                    // Load the call stack //
                    ScriptHelper.AppendCallStack(this, new ExpressionVisitor(null, this.Home), ctx.inline_script());

                    // Pop the last value //
                    if (this.CallStack.Count != 0)
                        ctx = this.CallStack.Pop();
                    else // rare occasion when exec is the last call, but what we are executing is nothing
                        return;

                }

                // Consume //
                CommandPlan plan = processor.Visit(ctx);

                // Add the header to the plan buffer //
                if (!this.Home.SupressIO)
                    this.Home.IO.Communicate();

                // Execute //
                plan.Execute();

                // Communicate //
                if (!this.Home.SupressIO)
                {

                    // Append the write stack //
                    this.Home.IO.Communicate(plan.MessageText());

                    // Dump the buffer //
                    this.Home.IO.FlushStringBuffer();
                    this.Home.IO.FlushRecordBuffer();

                }

            }


        }

    }

    public sealed class ParserErrorListener : BaseErrorListener
    {

        public ParserErrorListener()
            : base()
        {
        }

        public override void ReportAmbiguity(Parser recognizer, Antlr4.Runtime.Dfa.DFA dfa, int startIndex, int stopIndex, bool exact, Antlr4.Runtime.Sharpen.BitSet ambigAlts, Antlr4.Runtime.Atn.ATNConfigSet configs)
        {
            base.ReportAmbiguity(recognizer, dfa, startIndex, stopIndex, exact, ambigAlts, configs);
        }

        public override void ReportAttemptingFullContext(Parser recognizer, Antlr4.Runtime.Dfa.DFA dfa, int startIndex, int stopIndex, Antlr4.Runtime.Sharpen.BitSet conflictingAlts, Antlr4.Runtime.Atn.SimulatorState conflictState)
        {
            base.ReportAttemptingFullContext(recognizer, dfa, startIndex, stopIndex, conflictingAlts, conflictState);
        }

        public override void ReportContextSensitivity(Parser recognizer, Antlr4.Runtime.Dfa.DFA dfa, int startIndex, int stopIndex, int prediction, Antlr4.Runtime.Atn.SimulatorState acceptState)
        {
            base.ReportContextSensitivity(recognizer, dfa, startIndex, stopIndex, prediction, acceptState);
        }

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            
            string message = string.Format("Invalid token '{0}' found on line '{1}' at position '{2}'", offendingSymbol.Text.ToString(), line, charPositionInLine);
            throw new HScriptException(message, e);

        }

    }

    public sealed class HScriptException : Exception
    {

        public HScriptException(string Message, Exception InnerException)
            : base(Message, InnerException)
        {
        }

        public HScriptException(string Message)
            : base(Message)
        {
        }

    }

}
