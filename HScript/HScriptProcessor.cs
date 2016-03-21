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

        private bool _CanRun = true;
        private List<string> _CompileErrorMessages;

        public HScriptProcessor(Workspace UseHome)
        {
            this.Home = UseHome;
            this.Commands = new List<HScriptParser.CommandContext>();
            this._CompileErrorMessages = new List<string>();
        }

        public Workspace Home
        {
            get;
            private set;
        }

        public List<HScriptParser.CommandContext> Commands
        {
            get;
            set;
        }

        internal void LoadCommandStack(string Script)
        {

            // Clear the current stack //
            this.Commands.Clear();
            this._CompileErrorMessages.Clear();

            // Create a token stream and do lexal analysis //
            AntlrInputStream TextStream = new AntlrInputStream(Script);
            HScriptLexer HorseLexer = new HScriptLexer(TextStream);

            // Parse the script //
            CommonTokenStream HorseTokenStream = new CommonTokenStream(HorseLexer);
            HScriptParser HorseParser = new HScriptParser(HorseTokenStream);
            HorseParser.RemoveErrorListeners();
            HorseParser.AddErrorListener(new ParserErrorListener());
            
            // Create an executer object //
            CommandVisitor processor = new CommandVisitor(this.Home);

            // Load the call stack //
            try
            {
                foreach (HScriptParser.CommandContext context in HorseParser.compile_unit().command_set().command())
                {
                    this.Commands.Add(context);
                }
            }
            catch (Exception e)
            {
                this._CompileErrorMessages.Add(e.Message);
            }

        }

        internal void RenderCommand(HScriptParser.CommandContext Context, CommandVisitor Processor)
        {

            // Consume //
            CommandPlan plan = Processor.Visit(Context);
            

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

        public void Execute(string Script)
        {

            // Load the command stack //
            this.LoadCommandStack(Script);

            // Check to see if there were parser errors //
            if (this._CompileErrorMessages.Count != 0)
            {
                this.Home.IO.AppendBuffer("Parsing Error");
                foreach (string s in this._CompileErrorMessages)
                    this.Home.IO.AppendBuffer('\t' + s);
                this.Home.IO.AppendBuffer("Process terminated");
                this.Home.IO.FlushStringBuffer();
                return;
            }

            // Create an executer object //
            CommandVisitor processor = new CommandVisitor(this.Home);
            
            // Run ... //
            foreach(HScriptParser.CommandContext ctx in this.Commands)
            {

                this.RenderCommand(ctx, processor);

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
