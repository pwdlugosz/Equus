using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Equus.Horse;

namespace Equus.Calabrese
{

    public class CellFunction
    {

        protected string _sig = "";
        protected int _params = 0;
        protected Func<Cell[], Cell> _action;
        protected Func<CellAffinity[], CellAffinity> _rtype;
        protected Func<string[], Schema, string> _StringBuilder;
        protected int _size = -1;

        public CellFunction(string Sig, int Params, Func<Cell[], Cell> Action, Func<CellAffinity[], CellAffinity> RType, Func<string[], Schema, string> SBuilder)
        {
            this._sig = Sig;
            this._params = Params;
            this._action = Action;
            this._rtype = RType;
            this._StringBuilder = SBuilder;
        }

        public CellFunction(string Sig, int Params, Func<Cell[], Cell> Action, Func<CellAffinity[], CellAffinity> RType)
            : this(Sig, Params, Action, RType, CellFunction.FunctionStringBuilder(Sig))
        {
        }

        public CellFunction(string Sig, int Params, Func<Cell[], Cell> Action, CellAffinity RType)
            : this(Sig, Params, Action, CellFunction.FuncHierValue(RType))
        {
        }

        public CellFunction(string Sig, int Params, Func<Cell[], Cell> Action, CellAffinity RType, Func<string[], Schema, string> SBuilder)
            : this(Sig, Params, Action, CellFunction.FuncHierValue(RType), SBuilder)
        {

        }

        public CellFunction(string Sig, int Params, Func<Cell[], Cell> Action, Func<string[], Schema, string> SBuilder)
            : this(Sig, Params, Action, CellFunction.FuncHierStandard(), SBuilder)
        {

        }

        public CellFunction(string Sig, int Params, Func<Cell[], Cell> Action)
            : this(Sig, Params, Action, CellFunction.FuncHierStandard())
        {
        }

        public string NameSig
        {
            get { return _sig; }
        }

        public int ParamCount
        {
            get { return this._params; }
            set { this._params = value; }
        }

        public void SetSize(int Size)
        {
            this._size = Size;
        }

        public virtual Cell Evaluate(params Cell[] Data)
        {
            return _action(Data);
        }

        public virtual double Evaluate(params double[] Data)
        {
            return 0;
        }

        public virtual CellAffinity ReturnAffinity(params CellAffinity[] Data)
        {
            return _rtype(Data);
        }

        public override int GetHashCode()
        {
            return this._sig.GetHashCode();
        }

        public string Unparse(string[] Text, Schema Columns)
        {
            return this._StringBuilder(Text, Columns);
        }

        public virtual int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            if (Type == CellAffinity.BOOL)
                return 1;
            if (Type == CellAffinity.DATE_TIME || Type == CellAffinity.DOUBLE || Type == CellAffinity.INT)
                return 8;
            if (this._size != -1)
                return this._size;
            return Sizes.Length == 0 ? Schema.FixSize(Type,-1) : Sizes.First();
        }

        // Statics //
        internal static Func<string[], Schema, string> FunctionStringBuilder(string Name)
        {
            return (Text, Columns) =>
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Name);
                sb.Append("(");
                for (int i = 0; i < Text.Length; i++)
                {
                    sb.Append(Text[i]);
                    if (i != Text.Length - 1) sb.Append(",");
                }
                sb.Append(")");
                return sb.ToString();
            };

        }

        internal static Func<string[], Schema, string> BinOppStringBuilder(string Opp)
        {
            return (Text, Columns) =>
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("(");
                sb.Append(Text[0]);
                sb.Append(Opp);
                sb.Append(Text[1]);
                sb.Append(")");
                return sb.ToString();
            };

        }

        internal static Func<string[], Schema, string> UniOppStringBuilder(string Opp)
        {
            return (Text, Columns) =>
            {
                StringBuilder sb = new StringBuilder();
                //sb.Append("(");
                sb.Append(Opp);
                sb.Append(Text[0]);
                //sb.Append(")");
                return sb.ToString();
            };

        }

        internal static Func<CellAffinity[], CellAffinity> FuncHierStandard()
        {
            return (X) => { return CellAffinityHelper.Highest(X); };
        }

        internal static Func<CellAffinity[], CellAffinity> FuncHierValue(CellAffinity A)
        {
            return (X) => { return A; };
        }

    }

    // Used for +, -, !, NOT unary opperations
    #region UniOpperations

    public abstract class CellUnaryOpperation : CellFunction
    {

        public CellUnaryOpperation(string Name, string Token)
            : base(Name, 1, null, CellFunction.FuncHierStandard() , UniOppStringBuilder(Token))
        {
        }

    }

    public sealed class CellUniPlus : CellUnaryOpperation
    {

        public CellUniPlus()
            : base(FunctionNames.UNI_PLUS, "+")
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return +Data[0];
        }

        public override double Evaluate(params double[] Data)
        {
            return +Data[0];
        }

    }

    public sealed class CellUniMinus : CellUnaryOpperation
    {

        public CellUniMinus()
            : base(FunctionNames.UNI_MINUS, "-")
        { }

        public override Cell Evaluate(params Cell[] Data)
        {
            return -Data[0];
        }

        public override double Evaluate(params double[] Data)
        {
            return -Data[0];
        }

    }

    public sealed class CellUniNot : CellUnaryOpperation
    {

        public CellUniNot()
            : base(FunctionNames.UNI_NOT, "!")
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return !Data[0];
        }

    }

    public sealed class CellUniAutoInc : CellUnaryOpperation
    {

        public CellUniAutoInc()
            : base(FunctionNames.UNI_AUTO_INC, "++")
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0]++;
        }

        public override double Evaluate(params double[] Data)
        {
            return Data[0] + 1;
        }

    }

    public sealed class CellUniAutoDec : CellUnaryOpperation
    {

        public CellUniAutoDec()
            : base(FunctionNames.UNI_AUTO_DEC, "--")
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0]--;
        }

        public override double Evaluate(params double[] Data)
        {
            return Data[0]-1;
        }

    }

    #endregion

    // Used for +, -, *, /, %
    #region BinaryOpperations

    public abstract class CellBinaryOpperation : CellFunction
    {

        public CellBinaryOpperation(string Name, string Token)
            : base(Name, 2, null, CellFunction.FuncHierStandard(), BinOppStringBuilder(Token))
        {
        }



    }

    public sealed class CellBinPlus : CellBinaryOpperation
    {

        public CellBinPlus()
            : base(FunctionNames.OP_ADD, "+")
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] + Data[1];
        }

        public override double Evaluate(params double[] Data)
        {
            return Data[0] + Data[1];
        }

    }

    public sealed class CellBinMinus : CellBinaryOpperation
    {

        public CellBinMinus()
            : base(FunctionNames.OP_SUB, "-")
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] - Data[1];
        }
        
        public override double Evaluate(params double[] Data)
        {
            return Data[0] - Data[1];
        }

    }

    public sealed class CellBinMult : CellBinaryOpperation
    {

        public CellBinMult()
            : base(FunctionNames.OP_MUL, "*")
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] * Data[1];
        }

        public override double Evaluate(params double[] Data)
        {
            return Data[0] * Data[1];
        }

    }

    public sealed class CellBinDiv : CellBinaryOpperation
    {

        public CellBinDiv()
            : base(FunctionNames.OP_DIV, "/")
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] / Data[1];
        }

        public override double Evaluate(params double[] Data)
        {
            return Data[0] / Data[1];
        }

    }

    public sealed class CellBinDiv2 : CellBinaryOpperation
    {

        public CellBinDiv2()
            : base(FunctionNames.OP_DIV2, "/?")
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.CheckDivide(Data[0], Data[1]);
        }

        public override double Evaluate(params double[] Data)
        {
            return Data[1] == 0 ? 0 : Data[0] / Data[1];
        }

    }

    public sealed class CellBinMod : CellBinaryOpperation
    {

        public CellBinMod()
            : base(FunctionNames.OP_MOD, "%")
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] % Data[1];
        }

        public override double Evaluate(params double[] Data)
        {
            return Data[0] % Data[1];
        }

    }

    #endregion

    // Used for ==, !=, <, <=, >, >=, AND, OR, XOR
    #region BooleanOpperations

    public abstract class CellBooleanOpperation : CellFunction
    {

        public CellBooleanOpperation(string Name, string Token)
            : base(Name, 2, null, CellFunction.FuncHierValue(CellAffinity.BOOL), BinOppStringBuilder(Token))
        {
        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return 1;
        }

    }

    public sealed class CellBoolEQ : CellBinaryOpperation
    {

        public CellBoolEQ()
            : base(FunctionNames.BOOL_EQ, "==")
        {
            this._rtype = (x) => { return CellAffinity.BOOL; };
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] == Data[1] ? Cell.TRUE : Cell.FALSE;
        }

    }

    public sealed class CellBoolNEQ : CellBinaryOpperation
    {

        public CellBoolNEQ()
            : base(FunctionNames.BOOL_NEQ, "!=")
        {
            this._rtype = (x) => { return CellAffinity.BOOL; };
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] != Data[1] ? Cell.TRUE : Cell.FALSE;
        }

    }

    public sealed class CellBoolLT : CellBinaryOpperation
    {

        public CellBoolLT()
            : base(FunctionNames.BOOL_LT, "<")
        {
            this._rtype = (x) => { return CellAffinity.BOOL; };
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] < Data[1] ? Cell.TRUE : Cell.FALSE;
        }

    }

    public sealed class CellBoolLTE : CellBinaryOpperation
    {

        public CellBoolLTE()
            : base(FunctionNames.BOOL_LTE, "<=")
        {
            this._rtype = (x) => { return CellAffinity.BOOL; };
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] <= Data[1] ? Cell.TRUE : Cell.FALSE;
        }

    }

    public sealed class CellBoolGT : CellBinaryOpperation
    {

        public CellBoolGT()
            : base(FunctionNames.BOOL_GT, ">")
        {
            this._rtype = (x) => { return CellAffinity.BOOL; };
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] > Data[1] ? Cell.TRUE : Cell.FALSE;
        }

    }

    public sealed class CellBoolGTE : CellBinaryOpperation
    {

        public CellBoolGTE()
            : base(FunctionNames.BOOL_GTE, ">=")
        {
            this._rtype = (x) => { return CellAffinity.BOOL; };
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] >= Data[1] ? Cell.TRUE : Cell.FALSE;
        }

    }

    #endregion

    // Fixed parameter known return type //
    #region FixedKnownFunctions

    public abstract class CellFuncFixedKnown : CellFunction
    {

        public CellFuncFixedKnown(string Name, int Params, CellAffinity RType)
            : base(Name, Params, null, RType, CellFunction.FunctionStringBuilder(Name))
        {
        }

    }

    public sealed class CellFuncFKYear : CellFuncFixedKnown
    {

        public CellFuncFKYear()
            : base(FunctionNames.FUNC_YEAR, 1, CellAffinity.INT)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Year(Data[0]);
        }

    }

    public sealed class CellFuncFKMonth : CellFuncFixedKnown
    {

        public CellFuncFKMonth()
            : base(FunctionNames.FUNC_MONTH, 1, CellAffinity.INT)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Month(Data[0]);
        }

    }

    public sealed class CellFuncFKDay : CellFuncFixedKnown
    {

        public CellFuncFKDay()
            : base(FunctionNames.FUNC_DAY, 1, CellAffinity.INT)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Day(Data[0]);
        }

    }
    
    public sealed class CellFuncFKHour : CellFuncFixedKnown
    {

        public CellFuncFKHour()
            : base(FunctionNames.FUNC_HOUR, 1, CellAffinity.INT)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Hour(Data[0]);
        }

    }

    public sealed class CellFuncFKMinute : CellFuncFixedKnown
    {

        public CellFuncFKMinute()
            : base(FunctionNames.FUNC_MINUTE, 1, CellAffinity.INT)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Minute(Data[0]);
        }

    }

    public sealed class CellFuncFKSecond : CellFuncFixedKnown
    {

        public CellFuncFKSecond()
            : base(FunctionNames.FUNC_SECOND, 1, CellAffinity.INT)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Second(Data[0]);
        }

    }

    public sealed class CellFuncFKMillisecond : CellFuncFixedKnown
    {

        public CellFuncFKMillisecond()
            : base(FunctionNames.FUNC_MILLISECOND, 1, CellAffinity.INT)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Millisecond(Data[0]);
        }

    }

    public sealed class CellFuncFKTicks : CellFuncFixedKnown
    {

        public CellFuncFKTicks()
            : base(FunctionNames.FUNC_TIMESPAN, 1, CellAffinity.STRING)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            if (Data[0].Affinity != CellAffinity.INT)
                return new Cell(Data[0].AFFINITY);
            return new Cell(new TimeSpan(Data[0].INT).ToString());
        }

    }

    public sealed class CellFuncFKSubstring : CellFuncFixedKnown
    {

        public CellFuncFKSubstring()
            : base(FunctionNames.FUNC_SUBSTR, 3, CellAffinity.STRING)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Substring(Data[0], Data[1].INT, Data[2].INT);
        }

    }

    public sealed class CellFuncFKLeft : CellFuncFixedKnown
    {

        public CellFuncFKLeft()
            : base(FunctionNames.FUNC_SLEFT, 2, CellAffinity.STRING)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Left(Data[0], Data[1].INT);
        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return Sizes.First();
        }

    }

    public sealed class CellFuncFKRight : CellFuncFixedKnown
    {

        public CellFuncFKRight()
            : base(FunctionNames.FUNC_SRIGHT, 2, CellAffinity.STRING)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Right(Data[0], Data[1].INT);
        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return Sizes.First();
        }

    }

    public sealed class CellFuncFKReplace : CellFuncFixedKnown
    {

        public CellFuncFKReplace()
            : base(FunctionNames.FUNC_REPLACE, 3, CellAffinity.STRING)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Replace(Data[0], Data[1], Data[2]);
        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return Sizes.First();
        }

    }

    public sealed class CellFuncFKLength : CellFuncFixedKnown
    {

        public CellFuncFKLength()
            : base(FunctionNames.FUNC_LENGTH, 1, CellAffinity.INT)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            Cell c = new Cell(8);
            if (Data[0].AFFINITY == CellAffinity.STRING)
                c.INT = (long)Data[0].STRING.Length;
            else if (Data[0].AFFINITY == CellAffinity.BLOB)
                c.INT = (long)Data[0].BLOB.Length;
            return c;
        }

    }

    public sealed class CellFuncFKIsNull : CellFuncFixedKnown
    {

        public CellFuncFKIsNull()
            : base(FunctionNames.FUNC_IS_NULL, 1, CellAffinity.BOOL)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0].NULL == 1 ? Cell.TRUE : Cell.FALSE;
        }

    }

    public sealed class CellFuncFKIsNotNull : CellFuncFixedKnown
    {

        public CellFuncFKIsNotNull()
            : base(FunctionNames.FUNC_IS_NOT_NULL, 1, CellAffinity.BOOL)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0].NULL != 1 ? Cell.TRUE : Cell.FALSE;
        }

    }

    public sealed class CellFuncFKTypeOf : CellFuncFixedKnown
    {

        public CellFuncFKTypeOf()
            : base(FunctionNames.FUNC_TYPEOF, 1, CellAffinity.INT)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return new Cell((long)Data[0].AFFINITY);
        }

    }

    public sealed class CellFuncFKSTypeOf : CellFuncFixedKnown
    {

        public CellFuncFKSTypeOf()
            : base(FunctionNames.FUNC_STYPEOF, 1, CellAffinity.STRING)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return new Cell(Data[0].AFFINITY.ToString());
        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return 6; // DOUBLE is the longest type name
        }

    }

    public sealed class CellFuncFKRound : CellFuncFixedKnown
    {

        public CellFuncFKRound()
            : base(FunctionNames.FUNC_ROUND, 2, CellAffinity.DOUBLE)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            if (Data[0].AFFINITY != CellAffinity.DOUBLE)
                return Cell.NULL_DOUBLE;
            Data[0].DOUBLE = Math.Round(Data[0].DOUBLE, (int)Data[1].INT);
            return Data[0];
        }

    }

    public sealed class CellFuncFKToUTF16 : CellFuncFixedKnown
    {

        public CellFuncFKToUTF16()
            : base(FunctionNames.FUNC_TO_UTF16, 1, CellAffinity.STRING)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            if (Data[0].AFFINITY != CellAffinity.BLOB || Data[0].NULL == 1)
                return Cell.NULL_STRING;

            Data[0].STRING = Cell.ByteArrayToUTF16String(Data[0].BLOB);
            Data[0].AFFINITY = CellAffinity.STRING;

            return Data[0];

        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return Sizes.First() / 2; // divide by 2 because two bytes == 1 char
        }

    }

    public sealed class CellFuncFKToUTF8 : CellFuncFixedKnown
    {

        public CellFuncFKToUTF8()
            : base(FunctionNames.FUNC_TO_UTF8, 1, CellAffinity.STRING)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            if (Data[0].AFFINITY != CellAffinity.BLOB || Data[0].NULL == 1)
                return Cell.NULL_STRING;

            Data[0].STRING = ASCIIEncoding.UTF8.GetString(Data[0].BLOB);
            Data[0].AFFINITY = CellAffinity.STRING;

            return Data[0];

        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return Sizes.First(); 
        }

    }

    public sealed class CellFuncFKToHEX : CellFuncFixedKnown
    {

        public CellFuncFKToHEX()
            : base(FunctionNames.FUNC_TO_HEX, 1, CellAffinity.STRING)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            if (Data[0].AFFINITY != CellAffinity.BLOB || Data[0].NULL == 1)
                return Cell.NULL_STRING;

            Data[0].STRING = Cell.HEX_LITERARL + BitConverter.ToString(Data[0].BLOB).Replace("-","");
            Data[0].AFFINITY = CellAffinity.STRING;

            return Data[0];

        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return Sizes.First() * 2 + 2; // 1 byte = 2 chars + 2 for '0x'
        }

    }

    public sealed class CellFuncFKFromUTF16 : CellFuncFixedKnown
    {

        public CellFuncFKFromUTF16()
            : base(FunctionNames.FUNC_FROM_UTF16, 1, CellAffinity.BLOB)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            if (Data[0].AFFINITY != CellAffinity.STRING || Data[0].NULL == 1)
                return Cell.NULL_BLOB;

            Data[0].BLOB = ASCIIEncoding.BigEndianUnicode.GetBytes(Data[0].STRING);
            Data[0].AFFINITY = CellAffinity.BLOB;

            return Data[0];

        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return Sizes.First() * 2; // 1 byte = 2 chars
        }

    }

    public sealed class CellFuncFKFromUTF8 : CellFuncFixedKnown
    {

        public CellFuncFKFromUTF8()
            : base(FunctionNames.FUNC_FROM_UTF8, 1, CellAffinity.BLOB)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            if (Data[0].AFFINITY != CellAffinity.STRING || Data[0].NULL == 1)
                return Cell.NULL_BLOB;

            Data[0].BLOB = ASCIIEncoding.UTF8.GetBytes(Data[0].STRING);
            Data[0].AFFINITY = CellAffinity.BLOB;

            return Data[0];

        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return Sizes.First(); // 1 byte = 1 chars for utf 8
        }

    }

    public sealed class CellFuncFKFromHEX : CellFuncFixedKnown
    {

        public CellFuncFKFromHEX()
            : base(FunctionNames.FUNC_FROM_HEX, 1, CellAffinity.BLOB)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            if (Data[0].AFFINITY != CellAffinity.STRING || Data[0].NULL == 1)
                return Cell.NULL_BLOB;

            Data[0] = Cell.ByteParse(Data[0].STRING);

            return Data[0];

        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return Sizes.First();
        }

    }

    public sealed class CellFuncFKNormal : CellFuncFixedKnown
    {

        public CellFuncFKNormal()
            : base(FunctionNames.FUNC_NDIST, 1, CellAffinity.DOUBLE)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            Cell c = Data[0];
            if (c.AFFINITY != CellAffinity.DOUBLE)
                c.AFFINITY = CellAffinity.DOUBLE;

            c.DOUBLE = Equus.Numerics.SpecialFunctions.NormalCDF(c.DOUBLE);

            return c;

        }

    }

    public sealed class CellFuncFKThreadID : CellFuncFixedKnown
    {

        public CellFuncFKThreadID()
            : base(FunctionNames.FUNC_THREAD_ID, 0, CellAffinity.INT)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return new Cell(Environment.CurrentManagedThreadId);
        }

    }

    #endregion

    // Fixed parameter, returns type of first argument //
    #region FixedVariableFunctions

    public abstract class CellFuncFixedVariable : CellFunction
    {

        public CellFuncFixedVariable(string Name, int Params)
            : base(Name, Params, null, CellFunction.FuncHierStandard(), CellFunction.FunctionStringBuilder(Name))
        {
        }

    }

    public sealed class CellFuncFVLog : CellFuncFixedVariable
    {

        public CellFuncFVLog()
            : base(FunctionNames.FUNC_LOG, 1)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Log(Data[0]);
        }

        public override double Evaluate(params double[] Data)
        {
            return Math.Log(Data[0]);
        }

    }

    public sealed class CellFuncFVExp : CellFuncFixedVariable
    {

        public CellFuncFVExp()
            : base(FunctionNames.FUNC_EXP, 1)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Exp(Data[0]);
        }

        public override double Evaluate(params double[] Data)
        {
            return Math.Exp(Data[0]);
        }

    }

    public sealed class CellFuncFVPower : CellFuncFixedVariable
    {

        public CellFuncFVPower()
            : base(FunctionNames.FUNC_POWER, 2)
        {
            this._StringBuilder = CellFunction.BinOppStringBuilder("^");
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Power(Data[0], Data[1]);
        }

        public override double Evaluate(params double[] Data)
        {
            return Math.Pow(Data[0], Data[1]);
        }

    }

    public sealed class CellFuncFVSin : CellFuncFixedVariable
    {

        public CellFuncFVSin()
            : base(FunctionNames.FUNC_SIN, 1)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Sin(Data[0]);
        }

        public override double Evaluate(params double[] Data)
        {
            return Math.Sin(Data[0]);
        }

    }

    public sealed class CellFuncFVCos : CellFuncFixedVariable
    {

        public CellFuncFVCos()
            : base(FunctionNames.FUNC_COS, 1)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Cos(Data[0]);
        }

        public override double Evaluate(params double[] Data)
        {
            return Math.Cos(Data[0]);
        }

    }

    public sealed class CellFuncFVTan : CellFuncFixedVariable
    {

        public CellFuncFVTan()
            : base(FunctionNames.FUNC_TAN, 1)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Tan(Data[0]);
        }

        public override double Evaluate(params double[] Data)
        {
            return Math.Tan(Data[0]);
        }

    }

    public sealed class CellFuncFVSinh : CellFuncFixedVariable
    {

        public CellFuncFVSinh()
            : base(FunctionNames.FUNC_SINH, 1)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Sinh(Data[0]);
        }

        public override double Evaluate(params double[] Data)
        {
            return Math.Sinh(Data[0]);
        }

    }

    public sealed class CellFuncFVCosh : CellFuncFixedVariable
    {

        public CellFuncFVCosh()
            : base(FunctionNames.FUNC_COSH, 1)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Cosh(Data[0]);
        }

        public override double Evaluate(params double[] Data)
        {
            return Math.Cosh(Data[0]);
        }

    }

    public sealed class CellFuncFVTanh : CellFuncFixedVariable
    {

        public CellFuncFVTanh()
            : base(FunctionNames.FUNC_TANH, 1)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Tanh(Data[0]);
        }

        public override double Evaluate(params double[] Data)
        {
            return Math.Tanh(Data[0]);
        }

    }

    public sealed class CellFuncFVLogit : CellFuncFixedVariable
    {

        public CellFuncFVLogit()
            : base(FunctionNames.FUNC_LOGIT, 1)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            switch (Data[0].AFFINITY)
            {
                case CellAffinity.DOUBLE:
                    Data[0].DOUBLE = 1 / (1 + Math.Exp(-Data[0].DOUBLE));
                    break;
                case CellAffinity.INT:
                    Data[0].INT = (long)(1 / (1 + Math.Exp(-Data[0].valueDOUBLE)));
                    break;
                default:
                    Data[0].NULL = 1;
                    break;
            }
            return Data[0];

        }

        public override double Evaluate(params double[] Data)
        {
            return 1 / (1 + Math.Exp(-Data[0]));
        }

    }

    public sealed class CellFuncFVIfNull : CellFuncFixedVariable
    {

        public CellFuncFVIfNull()
            : base(FunctionNames.FUNC_IF_NULL, 2)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            if (Data[0].NULL != 1)
                return Data[0];
            else if (Data[1].AFFINITY == Data[0].AFFINITY)
                return Data[1];
            else
                return Cell.Cast(Data[1], Data[0].AFFINITY);
        }

    }

    public sealed class CellFuncFVAND : CellFuncFixedVariable
    {

        public CellFuncFVAND()
            : base(FunctionNames.FUNC_AND, 2)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] & Data[1];
        }

    }

    public sealed class CellFuncFVOR : CellFuncFixedVariable
    {

        public CellFuncFVOR()
            : base(FunctionNames.FUNC_OR, 2)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] | Data[1];
        }

    }

    public sealed class CellFuncFVXOR : CellFuncFixedVariable
    {

        public CellFuncFVXOR()
            : base(FunctionNames.FUNC_XOR, 2)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Data[0] ^ Data[1];
        }

    }

    public sealed class CellFuncFVSMax : CellFuncFixedVariable
    {

        public CellFuncFVSMax()
            : base(FunctionNames.FUNC_SMAX, -1)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Max(Data);
        }

    }

    public sealed class CellFuncFVSMin : CellFuncFixedVariable
    {

        public CellFuncFVSMin()
            : base(FunctionNames.FUNC_SMIN, -1)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return Cell.Min(Data);
        }

    }

    #endregion

    // Crypto Hashes //
    #region CryptoFunctions

    public abstract class CellFuncCryptHash : CellFunction
    {

        private HashAlgorithm _hasher;

        public CellFuncCryptHash(string Name, HashAlgorithm Algorithm)
            : base(Name, 1, null, CellAffinity.BLOB, CellFunction.FunctionStringBuilder(Name))
        {
            this._hasher = Algorithm;
        }

        public HashAlgorithm InnerHasher
        {
            get { return this._hasher; }
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            byte[] b = this._hasher.ComputeHash(Data[0].valueBLOB);
            return new Cell(b);
        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return 16;
        }

    }

    public sealed class CellFuncCHMD5 : CellFuncCryptHash
    {

        public CellFuncCHMD5()
            : base(FunctionNames.HASH_MD5, new MD5CryptoServiceProvider())
        {
        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return 16; // md5 has a hash size of 16 bytes
        }

    }

    public sealed class CellFuncCHSHA1 : CellFuncCryptHash
    {

        public CellFuncCHSHA1()
            : base(FunctionNames.HASH_SHA1, new SHA1CryptoServiceProvider())
        {
        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return 20; // sha1 has a hash size of 20 bytes
        }

    }

    public sealed class CellFuncCHSHA256 : CellFuncCryptHash
    {

        public CellFuncCHSHA256()
            : base(FunctionNames.HASH_SHA256, new SHA256CryptoServiceProvider())
        {
        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return 32; // sha256 has a hash size of 32 bytes
        }

    }

    #endregion

    // Special //
    #region SpecialFunctions

    public sealed class CellDateBuild : CellFunction
    {

        public CellDateBuild()
            : base(FunctionNames.SPECIAL_DATE_BUILD, -1, null, CellAffinity.DATE_TIME, CellFunction.FunctionStringBuilder(FunctionNames.SPECIAL_DATE_BUILD))
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            if (!(Data.Length == 3 || Data.Length == 6 || Data.Length == 7))
                throw new ArgumentException(string.Format("Invalid argument legnth passed : {0}", Data.Length));

            int year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0, millisecond = 0;

            // Get the Year, Month, Day //
            if (Data.Length == 3 || Data.Length == 6 || Data.Length == 7)
            {
                year = (int)Data[0].valueINT;
                month = (int)Data[1].valueINT;
                day = (int)Data[2].valueINT;
            }

            // Hour, Minute, Second //
            if (Data.Length == 6 || Data.Length == 7)
            {
                hour = (int)Data[3].valueINT;
                minute = (int)Data[4].valueINT;
                second = (int)Data[5].valueINT;
            }

            // Millisecond //
            if (Data.Length == 7)
            {
                millisecond = (int)Data[6].valueINT;
            }

            DateTime t = new DateTime(year, month, day, hour, minute, second, millisecond);

            return new Cell(t);

        }

    }

    public sealed class CellFuncIf : CellFunction
    {

        public CellFuncIf()
            : base(FunctionNames.SPECIAL_IF, 3, null, CellFunction.FuncHierStandard(), CellFunction.FunctionStringBuilder(FunctionNames.SPECIAL_IF))
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            if (Data[0].BOOL)
                return Data[1];
            else if (Data[1].AFFINITY == Data[2].AFFINITY)
                return Data[2];
            else
                return Cell.Cast(Data[2], Data[1].AFFINITY);
        }

    }

    public sealed class CellFuncKeyChange : CellFunction
    {

        private Cell[] _cache;
        private Cell _FirstCall = new Cell(-1);
        private Cell _NoChange = new Cell(0);
        private Cell _Change = new Cell(1);

        public CellFuncKeyChange()
            : base(FunctionNames.MUTABLE_KEY_CHANGE, -1, null, CellAffinity.INT, CellFunction.FunctionStringBuilder(FunctionNames.MUTABLE_KEY_CHANGE))
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            /*
             * -1 == first call of the key change function
             *  0 == no key change
             *  1 == the key has changed
             */

            if (this._cache == null)
                return this._FirstCall;

            int n = Math.Min(this._cache.Length, Data.Length);
            for (int i = 0; i < n; i++)
            {
                if (this._cache[i] != Data[i])
                    return this._Change;
            }
            return this._NoChange;

        }

    }

    public sealed class CellFuncCase : CellFunction
    {

        private List<FNode> _Whens;
        private List<FNode> _Thens;
        private FNode _Else;
        
        public CellFuncCase(List<FNode> Whens, List<FNode> Thens, FNode ELSE)
            : base(FunctionNames.SPECIAL_CASE, -1, null, CellFunction.FuncHierStandard(), null)
        {

            if (Thens.Count != Whens.Count)
                throw new Exception("When and then statements have different counts");

            this._Whens = Whens;
            this._Thens = Thens;
            this._Else = ELSE ?? new FNodeValue(null, new Cell(Thens.First().ReturnAffinity()));
            this._StringBuilder = CellFuncCase.CaseStringBuilder(this);
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            for (int i = 0; i < this._Whens.Count; i++)
            {

                if (this._Whens[i].Evaluate().valueBOOL)
                    return this._Thens[i].Evaluate();

            }

            return this._Else.Evaluate();

        }

        public static Func<string[], Schema, string> CaseStringBuilder(CellFuncCase Function)
        {

            Func<string[], Schema, string> func = (x,y) =>
            {

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("CASE ");
                for(int i = 0; i < Function._Whens.Count; i++)
                {
                    sb.AppendLine(string.Format("WHEN {0} THEN {1} ", Function._Whens[i].Unparse(y), Function._Thens[i].Unparse(y)));
                }

                sb.AppendLine(string.Format("ELSE {0} ", Function._Else.Unparse(y)));

                sb.AppendLine("END");

                return sb.ToString();

            };

            return func;

        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            if (Type == CellAffinity.STRING || Type == CellAffinity.BLOB)
                return Sizes.Max();
            return Schema.FixSize(Type, -1);
        }

    }

    #endregion

    // Mutable or instance level functions //
    #region MutableFunctions

    public sealed class CellRandom : CellFunction
    {

        private Random _r = null;
        
        public CellRandom()
            : base(FunctionNames.MUTABLE_RAND, -1, null, CellAffinity.DOUBLE)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            if (_r == null)
            {
                int seed = CellRandom.RandomSeed;
                if (Data.Length != 0)
                    seed = (int)Data[0].valueINT;
                _r = new Random(seed);
            }

            return new Cell(_r.NextDouble());

        }

        /// <summary>
        /// Returns a random seed variable not based on Enviorment.Ticks; this function makes it extemely unlikely that the same seed will be returned twice,
        /// which is a risk with Enviorment.Ticks if the function is called many times in a row.
        /// </summary>
        internal static int RandomSeed
        {

            get
            {

                Guid g = Guid.NewGuid();
                byte[] bits = g.ToByteArray();
                HashAlgorithm sha256 = SHA256CryptoServiceProvider.Create();
                for (int i = 0; i < (int)bits[3] * (int)bits[6]; i++)
                {
                    bits = sha256.ComputeHash(bits);
                }
                int seed = BitConverter.ToInt32(bits, 8);
                if (seed < 0)
                    return seed = -seed;
                
                return seed;

            }

        }

    }

    public sealed class CellRandomInt : CellFunction
    {

        private Random _r = null;

        public CellRandomInt()
            : base(FunctionNames.MUTABLE_RANDINT, -1, null, CellAffinity.INT)
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            if (_r == null)
            {
                int seed = CellRandom.RandomSeed;
                if (Data.Length != 0)
                    seed = (int)Data[0].INT;
                _r = new Random(seed);
            }

            if (Data.Length == 2)
                return new Cell(this._r.Next((int)Data[0].INT, (int)Data[1].INT));
            else if (Data.Length == 3)
                return new Cell(this._r.Next((int)Data[1].INT, (int)Data[2].INT));

            return new Cell(this._r.Next());

        }

    }

    #endregion

    // Window Functions //
    #region WindowFunctions

    public static class CellCollectionFunctions
    {

        /// <summary>
        /// Record: 0 = sum weights, 1 = sum data, 2 = sum data squared
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="WEIGHT"></param>
        /// <returns></returns>
        internal static Record Univariate(IEnumerable<Cell> Data, IEnumerable<Cell> Weight)
        {

            // If the counts are different, then throw an exceptions //
            if (Data.Count() != Weight.Count())
                throw new Exception(string.Format("WEIGHT and Table have different lengths {0} : {1}", Weight.Count(), Data.Count()));

            // Define variables //
            Cell w, x;
            Record r = Record.Stitch(Cell.ZeroValue(Weight.First().Affinity), Cell.ZeroValue(Data.First().Affinity), Cell.ZeroValue(Data.First().Affinity));
            for (int i = 0; i < Data.Count(); i++)
            {
                w = Weight.ElementAt(i);
                x = Data.ElementAt(i);
                if (!x.IsNull && !w.IsNull)
                {
                    r[0] += w;
                    r[1] += x * w;
                    r[2] += x * x * w;
                }
            }
            return r;

        }

        /// <summary>
        /// Record: 0 = weight, 1 = sum data x, 2 = sum data x squared, 3 = sum data y, 4 = sum data y squared, 5 = sum data x * y
        /// </summary>
        /// <param name="XData"></param>
        /// <param name="YData"></param>
        /// <param name="WEIGHT"></param>
        /// <returns></returns>
        internal static Record Bivariate(IEnumerable<Cell> XData, IEnumerable<Cell> YData, IEnumerable<Cell> Weight)
        {

            // If the counts are different, then throw an exceptions //
            if (XData.Count() != Weight.Count() || YData.Count() != Weight.Count())
                throw new Exception(string.Format("WEIGHT and Table have different lengths {0} : {1} : {2}", Weight.Count(), XData.Count(), YData.Count()));

            // Define variables //
            Cell x, y, w;
            Record r = Record.Stitch(Cell.ZeroValue(Weight.First().Affinity), Cell.ZeroValue(XData.First().Affinity), Cell.ZeroValue(XData.First().Affinity),
                Cell.ZeroValue(YData.First().Affinity), Cell.ZeroValue(YData.First().Affinity), Cell.ZeroValue(XData.First().Affinity));
            for (int i = 0; i < XData.Count(); i++)
            {
                x = XData.ElementAt(i);
                y = YData.ElementAt(i);
                w = Weight.ElementAt(i);
                if (!x.IsNull && !y.IsNull && !w.IsNull)
                {
                    r[0] += w;
                    r[1] += x * w;
                    r[2] += x * x * w;
                    r[3] += y * w;
                    r[4] += y * y * w;
                    r[5] += x * y * w;
                }
            }
            return r;
        }
        
        public static Cell Average(IEnumerable<Cell> Data, IEnumerable<Cell> Weight)
        {
            // Record: 0 = sum weights, 1 = sum data, 2 = sum data squared
            Record r = Univariate(Data, Weight);
            return r[1] / r[0];
        }

        public static Cell Variance(IEnumerable<Cell> Data, IEnumerable<Cell> Weight)
        {
            // Record: 0 = sum weights, 1 = sum data, 2 = sum data squared
            Record r = Univariate(Data, Weight);
            Cell m = r[1] / r[0];
            return r[2] / r[0] - m * m;
        }

        public static Cell SDeviation(IEnumerable<Cell> Data, IEnumerable<Cell> Weight)
        {
            return Cell.Sqrt(Variance(Data, Weight));
        }

        public static Cell Covariance(IEnumerable<Cell> XData, IEnumerable<Cell> YData, IEnumerable<Cell> Weight)
        {
            // Record: 0 = weight, 1 = sum data x, 2 = sum data x squared, 3 = sum data y, 4 = sum data y squared, 5 = sum data x * y
            Record r = Bivariate(XData, YData, Weight);
            Cell avgx = r[1] / r[0], avgy = r[3] / r[0];
            return r[5] / r[0] - avgx * avgy;
        }

        public static Cell Correlation(IEnumerable<Cell> XData, IEnumerable<Cell> YData, IEnumerable<Cell> Weight)
        {
            // Record: 0 = weight, 1 = sum data x, 2 = sum data x squared, 3 = sum data y, 4 = sum data y squared, 5 = sum data x * y
            Record r = Bivariate(XData, YData, Weight);
            Cell avgx = r[1] / r[0], avgy = r[3] / r[0];
            Cell stdx = Cell.Sqrt(r[2] / r[0] - avgx * avgx), stdy = Cell.Sqrt(r[4] / r[0] - avgy * avgy);
            Cell covar = r[5] / r[0] - avgx * avgy;
            return covar / (stdx * stdy);
        }

        public static Cell Slope(IEnumerable<Cell> XData, IEnumerable<Cell> YData, IEnumerable<Cell> Weight)
        {
            // Record: 0 = weight, 1 = sum data x, 2 = sum data x squared, 3 = sum data y, 4 = sum data y squared, 5 = sum data x * y
            Record r = Bivariate(XData, YData, Weight);
            Cell avgx = r[1] / r[0], avgy = r[3] / r[0];
            Cell stdx = Cell.Sqrt(r[2] / r[0] - avgx * avgx);
            Cell covar = r[5] / r[0] - avgx * avgy;
            return covar / (stdx * stdx);
        }

        public static Cell Intercept(IEnumerable<Cell> XData, IEnumerable<Cell> YData, IEnumerable<Cell> Weight)
        {
            // Record: 0 = weight, 1 = sum data x, 2 = sum data x squared, 3 = sum data y, 4 = sum data y squared, 5 = sum data x * y
            Record r = Bivariate(XData, YData, Weight);
            Cell avgx = r[1] / r[0], avgy = r[3] / r[0];
            Cell stdx = Cell.Sqrt(r[2] / r[0] - avgx * avgx);
            Cell covar = r[5] / r[0] - avgx * avgy;
            return avgy - avgx * covar / (stdx * stdx);
        }


    }

    public abstract class CellMovingUni : CellFunction
    {

        private int _LagCount = -1;
        private Queue<Cell> _Xcache;
        private Queue<Cell> _Wcache;

        private const int OFFSET_LAG = 0;
        private const int OFFSET_DATA_X = 1;
        private const int OFFSET_DATA_W = 2;
        
        public CellMovingUni(string Name)
            : base(Name, -1, null, CellAffinity.DOUBLE)
        {
            this._Xcache = new Queue<Cell>();
            this._Wcache = new Queue<Cell>();
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            // Check the lag count //
            if (this._LagCount == -1) 
                this._LagCount = (int)Data[OFFSET_LAG].INT;

            // Accumulate and enque the data //
            this._Xcache.Enqueue(Data[OFFSET_DATA_X]);

            // Accumulate weights //
            if (Data.Length == 3)
                this._Wcache.Enqueue(Data[OFFSET_DATA_W]);
            else
                this._Wcache.Enqueue(Cell.OneValue(Data[OFFSET_DATA_X].Affinity));

            // Check for no accumulation //
            Cell x;
            if (this._Xcache.Count != this._LagCount)
            {
                x = new Cell(Data[OFFSET_DATA_X].Affinity);
            }
            else
            {
                x = this.Motion(this._Xcache, this._Wcache);
                this._Xcache.Dequeue();
                this._Wcache.Dequeue();
            }

            return x;

        }

        public abstract Cell Motion(Queue<Cell> Data, Queue<Cell> Weight);

    }

    public abstract class CellMovingBi : CellFunction
    {

        private int _LagCount = -1;
        private Queue<Cell> _Xcache;
        private Queue<Cell> _Ycache;
        private Queue<Cell> _Wcache;

        private const int OFFSET_LAG = 0;
        private const int OFFSET_DATA_X = 1;
        private const int OFFSET_DATA_Y = 2;
        private const int OFFSET_DATA_W = 3;

        public CellMovingBi(string Name)
            : base(Name, -1, null, CellAffinity.DOUBLE)
        {
            this._Xcache = new Queue<Cell>();
            this._Ycache = new Queue<Cell>();
            this._Wcache = new Queue<Cell>();
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            // Check the lag count //
            if (this._LagCount == -1)
                this._LagCount = (int)Data[OFFSET_LAG].INT;

            // Accumulate and enque the data //
            this._Xcache.Enqueue(Data[OFFSET_DATA_X]);
            this._Ycache.Enqueue(Data[OFFSET_DATA_Y]);

            // Accumulate weights //
            if (Data.Length == 4)
                this._Wcache.Enqueue(Data[OFFSET_DATA_W]);
            else
                this._Wcache.Enqueue(Cell.OneValue(Data[OFFSET_DATA_X].Affinity));

            // Check for no accumulation //
            Cell x;
            if (this._Xcache.Count != this._LagCount)
            {
                x = new Cell(Data[OFFSET_DATA_X].Affinity);
            }
            else
            {
                x = this.Motion(this._Xcache, this._Ycache, this._Wcache);
                this._Xcache.Dequeue();
                this._Ycache.Dequeue();
                this._Wcache.Dequeue();
            }

            return x;

        }

        public abstract Cell Motion(Queue<Cell> XData, Queue<Cell> YData, Queue<Cell> Weight);

    }

    public sealed class CellMAvg : CellMovingUni
    {

        public CellMAvg()
            : base(FunctionNames.MUTABLE_MAVG)
        {
        }

        public override Cell Motion(Queue<Cell> Data, Queue<Cell> Weight)
        {
            return CellCollectionFunctions.Average(Data, Weight);
        }

    }

    public sealed class CellMVar : CellMovingUni
    {

        public CellMVar()
            : base(FunctionNames.MUTABLE_MVAR)
        {
        }

        public override Cell Motion(Queue<Cell> Data, Queue<Cell> Weight)
        {
            return CellCollectionFunctions.Variance(Data, Weight);
        }

    }

    public sealed class CellMStdev : CellMovingUni
    {

        public CellMStdev()
            : base(FunctionNames.MUTABLE_MSTDEV)
        {
        }

        public override Cell Motion(Queue<Cell> Data, Queue<Cell> Weight)
        {
            return CellCollectionFunctions.SDeviation(Data, Weight);
        }

    }

    public sealed class CellMCovar : CellMovingBi
    {

        public CellMCovar()
            : base(FunctionNames.MUTABLE_MCOVAR)
        {
        }

        public override Cell Motion(Queue<Cell> XData, Queue<Cell> YData, Queue<Cell> Weight)
        {
            return CellCollectionFunctions.Covariance(XData, YData, Weight);
        }

    }

    public sealed class CellMCorr : CellMovingBi
    {

        public CellMCorr()
            : base(FunctionNames.MUTABLE_MCORR)
        {
        }

        public override Cell Motion(Queue<Cell> XData, Queue<Cell> YData, Queue<Cell> Weight)
        {
            return CellCollectionFunctions.Correlation(XData, YData, Weight);
        }

    }

    public sealed class CellMIntercept : CellMovingBi
    {

        public CellMIntercept()
            : base(FunctionNames.MUTABLE_MINTERCEPT)
        {
        }

        public override Cell Motion(Queue<Cell> XData, Queue<Cell> YData, Queue<Cell> Weight)
        {
            return CellCollectionFunctions.Intercept(XData, YData, Weight);
        }

    }

    public sealed class CellMSlope : CellMovingBi
    {

        public CellMSlope()
            : base(FunctionNames.MUTABLE_MSLOPE)
        {
        }

        public override Cell Motion(Queue<Cell> XData, Queue<Cell> YData, Queue<Cell> Weight)
        {
            return CellCollectionFunctions.Slope(XData, YData, Weight);
        }

    }

    #endregion

    // Single value functions //
    #region VolatileFunctions

    public sealed class CellGUID : CellFunction
    {

        public CellGUID()
            : base(FunctionNames.VOLATILE_GUID, 0, null, CellAffinity.BLOB, CellFunction.FunctionStringBuilder(FunctionNames.VOLATILE_GUID))
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return new Cell(Guid.NewGuid().ToByteArray());
        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return 16; // GUIDs are 16 bytes
        }

    }

    public sealed class CellTicks : CellFunction
    {

        public CellTicks()
            : base(FunctionNames.VOLATILE_TICKS, 0, null, CellAffinity.INT, CellFunction.FunctionStringBuilder(FunctionNames.VOLATILE_TICKS))
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return new Cell((long)Environment.TickCount);
        }

    }

    public sealed class CellNow : CellFunction
    {

        public CellNow()
            : base(FunctionNames.VOLATILE_NOW, 0, null, CellAffinity.DATE_TIME, CellFunction.FunctionStringBuilder(FunctionNames.VOLATILE_NOW))
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            return new Cell(DateTime.Now);
        }

    }

    #endregion

    // Optimization helpers hidden form Percheron //
    #region HiddenFunctions

    public sealed class AndMany : CellFunction
    {

        public AndMany()
            : base(FunctionNames.FUNC_AND_MANY, -1, null, CellAffinity.BOOL, FunctionStringBuilder(FunctionNames.FUNC_AND_MANY))
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            bool b = true;
            foreach (Cell c in Data)
            {
                b = b && c.BOOL;
                if (!b) return new Cell(false);
            }
            
            return new Cell(true);

        }

    }

    public sealed class OrMany : CellFunction
    {

        public OrMany()
            : base(FunctionNames.FUNC_OR_MANY, -1, null, CellAffinity.BOOL, FunctionStringBuilder(FunctionNames.FUNC_OR_MANY))
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            bool b = false;
            foreach (Cell c in Data)
            {
                b = b || c.BOOL;
                if (b) return new Cell(true);
            }

            return new Cell(false);

        }

    }

    public sealed class AddMany : CellFunction
    {

        public AddMany()
            : base(FunctionNames.FUNC_ADD_MANY, -1, null, CellFunction.FuncHierStandard(), FunctionStringBuilder(FunctionNames.FUNC_ADD_MANY))
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            Cell s = Data[0];
            for (int i = 1; i < Data.Length; i++)
                s += Data[i];

            return s;

        }

        public override double Evaluate(params double[] Data)
        {

            double s = 0;
            foreach(double d in Data)
                s += d;

            return s;

        }

    }

    public sealed class ProductMany : CellFunction
    {

        public ProductMany()
            : base(FunctionNames.FUNC_PRODUCT_MANY, -1, null, CellFunction.FuncHierStandard(), FunctionStringBuilder(FunctionNames.FUNC_PRODUCT_MANY))
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {

            Cell s = Data[0];
            for (int i = 1; i < Data.Length; i++)
                s *= Data[i];

            return s;

        }
        
        public override double Evaluate(params double[] Data)
        {

            double s = 0;
            foreach (double d in Data)
                s *= d;

            return s;

        }

    }

    #endregion

    // File IO Functions //
    #region FileIO

    public sealed class CellFuncIOText : CellFunction
    {

        public CellFuncIOText()
            : base(FunctionNames.FUNC_FILE_IO_TEXT, 1, null, CellAffinity.STRING, FunctionStringBuilder(FunctionNames.FUNC_FILE_IO_TEXT))
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            string file_name = Data[0].STRING;
            if (!System.IO.File.Exists(file_name))
                return Cell.NULL_STRING;
            
            // Do it this way rather than 'Cell c = new Cell(System.IO.File.ReadAllText(file_name)), so we can avoid the string length cap
            Cell c = new Cell("");
            c.STRING = System.IO.File.ReadAllText(file_name);
            return c;
        }

        public override int ReturnSize(CellAffinity Type, params int[] Sizes)
        {
            return base.ReturnSize(Type, Sizes);
        }

    }

    public sealed class CellFuncIOBytes : CellFunction
    {

        public CellFuncIOBytes()
            : base(FunctionNames.FUNC_FILE_IO_BYTES, 1, null, CellAffinity.BLOB, FunctionStringBuilder(FunctionNames.FUNC_FILE_IO_BYTES))
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            string file_name = Data[0].STRING;
            if (!System.IO.File.Exists(file_name))
                return Cell.NULL_BLOB;

            // Do it this way rather than 'Cell c = new Cell(System.IO.File.ReadAllBytes(file_name)), so we can avoid the blob length cap
            Cell c = new Cell(new byte[0]);
            c.BLOB = System.IO.File.ReadAllBytes(file_name);
            return c;
        }

    }

    public sealed class CellFuncFileSize : CellFunction
    {

        public CellFuncFileSize()
            : base(FunctionNames.FUNC_FILE_SIZE, 1, null, CellAffinity.INT, CellFunction.FunctionStringBuilder(FunctionNames.FUNC_FILE_SIZE))
        {
        }

        public override Cell Evaluate(params Cell[] Data)
        {
            string path = Data[0].valueSTRING;
            try
            {
                if (!System.IO.File.Exists(path))
                    return Cell.NULL_INT;
            }
            catch
            {
                return Cell.NULL_INT;
            }

            System.IO.FileInfo fi = new System.IO.FileInfo(path);
            return new Cell(fi.Length);

        }

    }

    #endregion

    // Static Classes //
    public static class FunctionNames
    {

        public const string UNI_PLUS = "uniplus";
        public const string UNI_MINUS = "uniminus";
        public const string UNI_NOT = "uninot";
        public const string UNI_AUTO_INC = "autoinc";
        public const string UNI_AUTO_DEC = "autodec";

        public const string OP_ADD = "add";
        public const string OP_SUB = "subract";
        public const string OP_MUL = "multiply";
        public const string OP_DIV = "divide";
        public const string OP_DIV2 = "divide2";
        public const string OP_MOD = "modulo";

        public const string BOOL_EQ = "equals";
        public const string BOOL_NEQ = "notequals";
        public const string BOOL_LT = "lessthan";
        public const string BOOL_LTE = "lessthanorequalto";
        public const string BOOL_GT = "greaterthan";
        public const string BOOL_GTE = "greaterthanorequalto";
        
        public const string FUNC_YEAR = "year";
        public const string FUNC_MONTH = "month";
        public const string FUNC_DAY = "day";
        public const string FUNC_HOUR = "hour";
        public const string FUNC_MINUTE = "minute";
        public const string FUNC_SECOND = "second";
        public const string FUNC_MILLISECOND = "millisecond";
        public const string FUNC_TIMESPAN = "timespan";
        public const string FUNC_SUBSTR = "substr";
        public const string FUNC_SLEFT = "sleft";
        public const string FUNC_SRIGHT = "sright";
        public const string FUNC_REPLACE = "replace";
        public const string FUNC_LENGTH = "length";
        public const string FUNC_IS_NULL = "isnull";
        public const string FUNC_IS_NOT_NULL = "isnotnull";
        public const string FUNC_TYPEOF = "typeof";
        public const string FUNC_STYPEOF = "stypeof";
        public const string FUNC_ROUND = "round";
        public const string FUNC_TO_UTF16 = "toutf16";
        public const string FUNC_TO_UTF8 = "toutf8";
        public const string FUNC_TO_HEX = "tohex";
        public const string FUNC_FROM_UTF16 = "fromutf16";
        public const string FUNC_FROM_UTF8 = "fromutf8";
        public const string FUNC_FROM_HEX = "fromhex";
        public const string FUNC_NDIST = "ndist";
        public const string FUNC_THREAD_ID = "threadid";
        
        // Differentiable Functions //
        public const string FUNC_LOG = "log";
        public const string FUNC_EXP = "exp";
        public const string FUNC_POWER = "power";
        public const string FUNC_SIN = "sin";
        public const string FUNC_COS = "cos";
        public const string FUNC_TAN = "tan";
        public const string FUNC_SINH = "sinh";
        public const string FUNC_COSH = "cosh";
        public const string FUNC_TANH = "tanh";
        public const string FUNC_LOGIT = "logit";
        public const string FUNC_SMAX = "smax";
        public const string FUNC_SMIN = "smin";

        public const string FUNC_IF_NULL = "ifnull";
        public const string FUNC_AND = "and";
        public const string FUNC_OR = "or";
        public const string FUNC_XOR = "xor";

        public const string HASH_MD5 = "md5";
        public const string HASH_SHA1 = "sha1";
        public const string HASH_SHA256 = "sha256";

        public const string MUTABLE_LAG = "lag";
        public const string MUTABLE_LAGN = "lagn";
        public const string MUTABLE_ROWID = "rowid";
        public const string MUTABLE_SEQUENCE = "sequence";
        public const string MUTABLE_RAND = "rand";
        public const string MUTABLE_RANDINT = "randint";

        public const string MUTABLE_MAVG = "mavg";
        public const string MUTABLE_MVAR = "mvar";
        public const string MUTABLE_MSTDEV = "mstdev";
        public const string MUTABLE_MCOVAR = "mcovar";
        public const string MUTABLE_MCORR = "mcorr";
        public const string MUTABLE_MINTERCEPT = "mintercept";
        public const string MUTABLE_MSLOPE = "mslope";

        public const string MUTABLE_KEY_CHANGE = "key_change";

        public const string VOLATILE_GUID = "guid";
        public const string VOLATILE_NOW = "now";
        public const string VOLATILE_TICKS = "ticks";

        public const string FUNC_AND_MANY = "andmany";
        public const string FUNC_OR_MANY = "ormany";
        public const string FUNC_ADD_MANY = "addmany";
        public const string FUNC_PRODUCT_MANY =  "productmany";

        public const string FUNC_FILE_IO_TEXT = "fileiotext";
        public const string FUNC_FILE_IO_BYTES = "fileiobytes";
        public const string FUNC_FILE_SIZE = "filesize";

        public const string TOKEN_UNI_PLUS = "u+";
        public const string TOKEN_UNI_MINUS = "u-";
        public const string TOKEN_UNI_NOT = "!";
        public const string TOKEN_UNI_AUTO_INC = "++";
        public const string TOKEN_UNI_AUTO_DEC = "--";

        public const string TOKEN_OP_ADD = "+";
        public const string TOKEN_OP_SUB = "-";
        public const string TOKEN_OP_MUL = "*";
        public const string TOKEN_OP_DIV = "/";
        public const string TOKEN_OP_DIV2 = "/?";
        public const string TOKEN_OP_MOD = "%";

        public const string TOKEN_BOOL_EQ = "==";
        public const string TOKEN_BOOL_NEQ = "!=";
        public const string TOKEN_BOOL_LT = "<";
        public const string TOKEN_BOOL_LTE = "<=";
        public const string TOKEN_BOOL_GT = ">";
        public const string TOKEN_BOOL_GTE = ">=";
        public const string TOKEN_FUNC_IF_NULL = "??";

        public const string SPECIAL_IF = "if";
        public const string SPECIAL_CASE = "case";
        public const string SPECIAL_DATE_BUILD = "date_build";

        

    }

    public static class CellFunctionFactory
    {

        private static Dictionary<string, Func<CellFunction>> FUNCTION_TABLE = new Dictionary<string, Func<CellFunction>>(StringComparer.OrdinalIgnoreCase)
        {

            { FunctionNames.TOKEN_UNI_PLUS, () => { return new CellUniPlus();}},
            { FunctionNames.UNI_PLUS, () => { return new CellUniPlus();}},
            { FunctionNames.TOKEN_UNI_MINUS, () => { return new CellUniMinus();}},
            { FunctionNames.UNI_MINUS, () => { return new CellUniMinus();}},
            { FunctionNames.TOKEN_UNI_NOT, () => { return new CellUniNot();}},
            { FunctionNames.UNI_NOT, () => { return new CellUniNot();}},
            { FunctionNames.TOKEN_UNI_AUTO_INC, () => { return new CellUniAutoInc();}},
            { FunctionNames.UNI_AUTO_INC, () => { return new CellUniAutoInc();}},
            { FunctionNames.TOKEN_UNI_AUTO_DEC, () => { return new CellUniAutoDec();}},
            { FunctionNames.UNI_AUTO_DEC, () => { return new CellUniAutoDec();}},

            { FunctionNames.TOKEN_OP_ADD, () => { return new CellBinPlus();}},
            { FunctionNames.OP_ADD, () => { return new CellBinPlus();}},
            { FunctionNames.TOKEN_OP_SUB, () => { return new CellBinMinus();}},
            { FunctionNames.OP_SUB, () => { return new CellBinMinus();}},
            { FunctionNames.TOKEN_OP_MUL, () => { return new CellBinMult();}},
            { FunctionNames.OP_MUL, () => { return new CellBinMult();}},
            { FunctionNames.TOKEN_OP_DIV, () => { return new CellBinDiv();}},
            { FunctionNames.OP_DIV, () => { return new CellBinDiv();}},
            { FunctionNames.TOKEN_OP_DIV2, () => { return new CellBinDiv2();}},
            { FunctionNames.OP_DIV2, () => { return new CellBinDiv2();}},
            { FunctionNames.TOKEN_OP_MOD, () => { return new CellBinMod();}},
            { FunctionNames.OP_MOD, () => { return new CellBinMod();}},
            
            { FunctionNames.TOKEN_BOOL_EQ, () => { return new CellBoolEQ();}},
            { FunctionNames.BOOL_EQ, () => { return new CellBoolEQ();}},
            { FunctionNames.TOKEN_BOOL_NEQ, () => { return new CellBoolNEQ();}},
            { FunctionNames.BOOL_NEQ, () => { return new CellBoolNEQ();}},
            { FunctionNames.TOKEN_BOOL_LT, () => { return new CellBoolLT();}},
            { FunctionNames.BOOL_LT, () => { return new CellBoolLT();}},
            { FunctionNames.TOKEN_BOOL_LTE, () => { return new CellBoolLTE();}},
            { FunctionNames.BOOL_LTE, () => { return new CellBoolLTE();}},
            { FunctionNames.TOKEN_BOOL_GT, () => { return new CellBoolGT();}},
            { FunctionNames.BOOL_GT, () => { return new CellBoolGT();}},
            { FunctionNames.TOKEN_BOOL_GTE, () => { return new CellBoolGTE();}},
            { FunctionNames.BOOL_GTE, () => { return new CellBoolGTE();}},

            { FunctionNames.FUNC_AND, () => { return new CellFuncFVAND();}},
            { FunctionNames.FUNC_OR, () => { return new CellFuncFVOR();}},
            { FunctionNames.FUNC_XOR, () => { return new CellFuncFVXOR();}},
            
            { FunctionNames.FUNC_YEAR, () => { return new CellFuncFKYear();}},
            { FunctionNames.FUNC_MONTH, () => { return new CellFuncFKMonth();}},
            { FunctionNames.FUNC_DAY, () => { return new CellFuncFKDay();}},
            { FunctionNames.FUNC_HOUR, () => { return new CellFuncFKHour();}},
            { FunctionNames.FUNC_MINUTE, () => { return new CellFuncFKMinute();}},
            { FunctionNames.FUNC_SECOND, () => { return new CellFuncFKSecond();}},
            { FunctionNames.FUNC_MILLISECOND, () => { return new CellFuncFKMillisecond();}},
            { FunctionNames.FUNC_TIMESPAN, () => { return new CellFuncFKTicks();}},
            
            { FunctionNames.FUNC_SUBSTR, () => { return new CellFuncFKSubstring();}},
            { FunctionNames.FUNC_SLEFT, () => { return new CellFuncFKLeft();}},
            { FunctionNames.FUNC_SRIGHT, () => { return new CellFuncFKRight();}},
            { FunctionNames.FUNC_REPLACE, () => { return new CellFuncFKReplace();}},
            { FunctionNames.FUNC_LENGTH, () => { return new CellFuncFKLength();}},
            { FunctionNames.FUNC_IS_NULL, () => { return new CellFuncFKIsNull();}},
            { FunctionNames.FUNC_IS_NOT_NULL, () => { return new CellFuncFKIsNotNull();}},
            { FunctionNames.FUNC_TYPEOF, () => { return new CellFuncFKTypeOf();}},
            { FunctionNames.FUNC_STYPEOF, () => { return new CellFuncFKSTypeOf();}},
            { FunctionNames.FUNC_ROUND, () => { return new CellFuncFKRound();}},
            
            { FunctionNames.FUNC_TO_UTF16, () => { return new CellFuncFKToUTF16();}},
            { FunctionNames.FUNC_TO_UTF8, () => { return new CellFuncFKToUTF8();}},
            { FunctionNames.FUNC_TO_HEX, () => { return new CellFuncFKToHEX();}},
            { FunctionNames.FUNC_FROM_UTF16, () => { return new CellFuncFKFromUTF16();}},
            { FunctionNames.FUNC_FROM_UTF8, () => { return new CellFuncFKFromUTF8();}},
            { FunctionNames.FUNC_FROM_HEX, () => { return new CellFuncFKFromHEX();}},
            { FunctionNames.FUNC_NDIST, () => { return new CellFuncFKNormal();}},
            { FunctionNames.FUNC_THREAD_ID, () => { return new CellFuncFKThreadID();}},

            { FunctionNames.FUNC_LOG, () => { return new CellFuncFVLog();}},
            { FunctionNames.FUNC_EXP, () => { return new CellFuncFVExp();}},
            { FunctionNames.FUNC_POWER, () => { return new CellFuncFVPower();}},
            { FunctionNames.FUNC_SIN, () => { return new CellFuncFVSin();}},
            { FunctionNames.FUNC_COS, () => { return new CellFuncFVCos();}},
            { FunctionNames.FUNC_TAN, () => { return new CellFuncFVTan();}},
            { FunctionNames.FUNC_SINH, () => { return new CellFuncFVSinh();}},
            { FunctionNames.FUNC_COSH, () => { return new CellFuncFVCosh();}},
            { FunctionNames.FUNC_TANH, () => { return new CellFuncFVTanh();}},
            { FunctionNames.FUNC_LOGIT, () => { return new CellFuncFVLogit();}},
            { FunctionNames.TOKEN_FUNC_IF_NULL, () => { return new CellFuncFVIfNull();}},
            { FunctionNames.FUNC_IF_NULL, () => { return new CellFuncFVIfNull();}},
            { FunctionNames.FUNC_SMIN, () => { return new CellFuncFVSMin();}},
            { FunctionNames.FUNC_SMAX, () => { return new CellFuncFVSMax();}},
            
            { FunctionNames.SPECIAL_IF, () => { return new CellFuncIf();}},
            { FunctionNames.SPECIAL_DATE_BUILD, () => { return new CellDateBuild();}},
            
            { FunctionNames.HASH_MD5, () => { return new CellFuncCHMD5();}},
            { FunctionNames.HASH_SHA1, () => { return new CellFuncCHSHA1();}},
            { FunctionNames.HASH_SHA256, () => { return new CellFuncCHSHA256();}},
            
            //{ FunctionNames.MUTABLE_LAG, () => { return new CellLag();}},
            //{ FunctionNames.MUTABLE_LAGN, () => { return new CellLagN();}},
            //{ FunctionNames.MUTABLE_ROWID, () => { return new CellRowID();}},
            //{ FunctionNames.MUTABLE_SEQUENCE, () => { return new CellSequence();}},
            { FunctionNames.MUTABLE_RAND, () => { return new CellRandom();}},
            { FunctionNames.MUTABLE_RANDINT, () => { return new CellRandomInt();}},
            
            { FunctionNames.MUTABLE_MAVG, () => { return new CellMAvg();}},
            { FunctionNames.MUTABLE_MVAR, () => { return new CellMVar();}},
            { FunctionNames.MUTABLE_MSTDEV, () => { return new CellMStdev();}},
            { FunctionNames.MUTABLE_MCOVAR, () => { return new CellMCovar();}},
            { FunctionNames.MUTABLE_MCORR, () => { return new CellMCorr();}},
            { FunctionNames.MUTABLE_MINTERCEPT, () => { return new CellMIntercept();}},
            { FunctionNames.MUTABLE_MSLOPE, () => { return new CellMSlope();}},
            
            { FunctionNames.VOLATILE_GUID, () => { return new CellGUID();}},
            { FunctionNames.VOLATILE_TICKS, () => { return new CellTicks();}},
            { FunctionNames.VOLATILE_NOW, () => { return new CellNow();}},

            { FunctionNames.FUNC_ADD_MANY, () => { return new AddMany();}},
            { FunctionNames.FUNC_PRODUCT_MANY, () => { return new ProductMany();}},
            { FunctionNames.FUNC_AND_MANY, () => { return new AndMany();}},
            { FunctionNames.FUNC_OR_MANY, () => { return new OrMany();}},

            { FunctionNames.MUTABLE_KEY_CHANGE, () => { return new CellFuncKeyChange();}},

            { FunctionNames.FUNC_FILE_SIZE, () => { return new CellFuncFileSize();}},
            { FunctionNames.FUNC_FILE_IO_BYTES, () => { return new CellFuncIOBytes();}},
            { FunctionNames.FUNC_FILE_IO_TEXT, () => { return new CellFuncIOText();}},


        };

        public static bool Exists(string FunctionName)
        {
            return FUNCTION_TABLE.ContainsKey(FunctionName);
        }

        public static CellFunction LookUp(string FunctionName)
        {
            return FUNCTION_TABLE[FunctionName].Invoke(); 
        }

        public static void Print()
        {
            foreach (KeyValuePair<string, Func<CellFunction>> kv in FUNCTION_TABLE)
                Comm.WriteLine(kv.Key);
        }

    }

}
