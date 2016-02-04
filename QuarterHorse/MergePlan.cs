using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Shire;
using Equus.Gidran;
using Equus.Nokota;
using Equus.Andalusian;

namespace Equus.QuarterHorse
{

    public enum MergeAlgorithm : byte
    {

        SortMerge = 0,
        HashTable = 1,
        NestedLoop = 2

    }

    public enum MergeMethod : byte
    {

        Inner = 0,
        Left = 10,
        Right = 20,
        Full = 30,
        AntiLeft = 40,
        AntiRight = 50,
        AntiInner = 60,
        Cross = 70

    }

    internal static class MergeAlgorithmHelper
    {

        private const double NESTED_LOOP_RATIO = 0.05;

        internal static MergeAlgorithm Optimize(RecordSet T1, Key J1, RecordSet T2, Key J2)
        {

            double n1 = (double)T1.Count;
            double n2 = (double)T2.Count;

            double p = Math.Min(n1, n2) / Math.Max(n1, n2);

            // Test for sort merge //
            if (Key.EqualsStrict(T1.SortBy, J1) && Key.EqualsStrict(T2.SortBy, J2)) return MergeAlgorithm.SortMerge;

            // Test for nested loop //
            if (p <= NESTED_LOOP_RATIO) return MergeAlgorithm.NestedLoop;

            // Otherwise //
            return MergeAlgorithm.HashTable;

        }

        internal static MergeAlgorithm ParseAlgorithm(string Text)
        {

            switch (Text.ToUpper().Trim())
            {
                case "NESTED_LOOP":
                case "NL":
                case "N": return MergeAlgorithm.NestedLoop;
                case "SORT_MERGE":
                case "SM":
                case "S": return MergeAlgorithm.SortMerge;
                case "HASH_TABLE":
                case "HT":
                case "H": return MergeAlgorithm.HashTable;
                default: return MergeAlgorithm.HashTable;
            }

        }

        internal static MergeMethod ParseMethod(string Text)
        {

            switch (Text.Trim().ToUpper())
            {
                case "INNER":
                case "I": return MergeMethod.Inner;
                case "LEFT":
                case "L": return MergeMethod.Left;
                case "RIGHT":
                case "R": return MergeMethod.Right;
                case "FULL":
                case "F": return MergeMethod.Full;
                case "ANTI_LEFT":
                case "AL": return MergeMethod.AntiLeft;
                case "ANTI_RIGHT":
                case "AR": return MergeMethod.AntiRight;
                case "ANTI_INNER":
                case "AI": return MergeMethod.AntiInner;
                case "CROSS":
                case "C": return MergeMethod.Cross;
                default: return MergeMethod.Inner;
            }

        }

    }

    public static class MergeFunctions
    {

        private static void CheckSort(DataSet T1, Key J1, DataSet T2, Key J2)
        {

            // Check R1 //
            if (!Key.EqualsStrict(T1.SortBy, J1))
                T1.Sort(J1);

            // Check R2 //
            if (!Key.EqualsStrict(T2.SortBy, J2))
                T2.Sort(J2);

        }

        public static Schema Build(Schema S1, string Alias1, Schema S2, string Alias2, string Delim)
        {
            Schema s = new Schema();
            for (int i = 0; i < S1.Count; i++)
                s.Add(Alias1 + Delim + S1.ColumnName(i), S1.ColumnAffinity(i), S1.ColumnNull(i), S1.ColumnSize(i));
            for (int i = 0; i < S2.Count; i++)
                s.Add(Alias2 + Delim + S2.ColumnName(i), S2.ColumnAffinity(i), S2.ColumnNull(i), S2.ColumnSize(i));
            return s;
        }

        // Cross Join //
        public static void CrossJoin(RecordWriter Output, FNodeSet Fields, Predicate Where, DataSet Data1, DataSet Data2,
            StaticRegister Memory1, StaticRegister Memory2)
        {

            // Cursors //
            RecordReader Reader1 = Data1.OpenReader();
            
            // Table One Loop //
            while (!Reader1.EndOfData)
            {

                Memory1.Assign(Reader1.ReadNext());

                // Table Two Loop //
                RecordReader Reader2 = Data2.OpenReader();
                while (!Reader2.EndOfData)
                {

                    Memory2.Assign(Reader2.ReadNext());
                    if (Where.Render())
                        Output.Insert(Fields.Evaluate());

                }

            }

        }

        // Nest Loop Generic Joins //
        private static void NestedLoopInnerJoin(RecordWriter Output, FNodeSet Fields, Predicate Where, DataSet Data1, DataSet Data2,
            StaticRegister Memory1, StaticRegister Memory2)
        {

            // Cursors //
            RecordReader Reader1 = Data1.OpenReader();
            
            // Table One Loop //
            while (!Reader1.EndOfData)
            {

                Memory1.Assign(Reader1.ReadNext());

                // Table Two Loop //
                RecordReader Reader2 = Data2.OpenReader();
                while (!Reader2.EndOfData)
                {

                    Memory2.Assign(Reader2.ReadNext());
                    if (Where.Render())
                        Output.Insert(Fields.Evaluate());

                }

            }

        }

        private static void NestedLoopLeftJoin(RecordWriter Output, FNodeSet Fields, Predicate Where, DataSet Data1, DataSet Data2,
            StaticRegister Memory1, StaticRegister Memory2, bool AntiJoin)
        {

            // Cursors //
            RecordReader Reader1 = Data1.OpenReader();
            bool match = false;

            // Table One Loop //
            while (!Reader1.EndOfData)
            {

                Memory1.Assign(Reader1.ReadNext());

                // Table Two Loop //
                RecordReader Reader2 = Data2.OpenReader();
                match = false;
                while (!Reader2.EndOfData)
                {
                    Memory2.Assign(Reader2.ReadNext());
                    if (Where.Render())
                    {

                        if (AntiJoin == false)
                            Output.Insert(Fields.Evaluate());
                        match = true;
                    }
                }

                Memory2.Assign(Reader2.SourceSchema.NullRecord);
                if (!match)
                {
                    Output.Insert(Fields.Evaluate());
                }

            }

        }

        private static void NestedLoopRightJoin(RecordWriter Output, FNodeSet Fields, Predicate Where, DataSet Data1, DataSet Data2,
            StaticRegister Memory1, StaticRegister Memory2, bool AntiJoin)
        {

            // Cursors //
            RecordReader Reader2 = Data2.OpenReader();
            bool match = false;

            // Table Two Loop //
            while (!Reader2.EndOfData)
            {

                Memory2.Assign(Reader2.ReadNext());

                // Table One Loop //
                RecordReader Reader1 = Data1.OpenReader();
                match = false;
                while (!Reader1.EndOfData)
                {
                    Memory1.Assign(Reader1.ReadNext());
                    if (Where.Render())
                    {
                        if (AntiJoin == false)
                            Output.Insert(Fields.Evaluate());
                        match = true;
                    }
                }

                Memory1.Assign(Reader1.SourceSchema.NullRecord);
                if (!match)
                {
                    Output.Insert(Fields.Evaluate());
                }

            }

        }

        public static void NestedLoop(MergeMethod JM, RecordWriter Output, FNodeSet Fields, Predicate Where, DataSet Data1, DataSet Data2,
            StaticRegister Memory1, StaticRegister Memory2)
        {

            switch (JM)
            {

                case MergeMethod.Cross:
                    CrossJoin(Output, Fields, Where, Data1, Data2, Memory1, Memory2);
                    break;
                case MergeMethod.Inner:
                    NestedLoopInnerJoin(Output, Fields, Where, Data1, Data2, Memory1, Memory2);
                    break;
                case MergeMethod.Left:
                    NestedLoopLeftJoin(Output, Fields, Where, Data1, Data2, Memory1, Memory2, false);
                    break;
                case MergeMethod.Right:
                    NestedLoopRightJoin(Output, Fields, Where, Data1, Data2, Memory1, Memory2, false);
                    break;
                case MergeMethod.Full:
                    NestedLoopLeftJoin(Output, Fields, Where, Data1, Data2, Memory1, Memory2, false);
                    NestedLoopRightJoin(Output, Fields, Where, Data1, Data2, Memory1, Memory2, true);
                    break;
                case MergeMethod.AntiLeft:
                    NestedLoopLeftJoin(Output, Fields, Where, Data1, Data2, Memory1, Memory2, true);
                    break;
                case MergeMethod.AntiRight:
                    NestedLoopRightJoin(Output, Fields, Where, Data1, Data2, Memory1, Memory2, true);
                    break;
                case MergeMethod.AntiInner:
                    NestedLoopLeftJoin(Output, Fields, Where, Data1, Data2, Memory1, Memory2, true);
                    NestedLoopRightJoin(Output, Fields, Where, Data1, Data2, Memory1, Memory2, true);
                    break;

            }
        }

        // Sort Merge - Collection Map  //
        private static void SortMergeInnerJoin(RecordWriter Output, FNodeSet Fields, Predicate Where, DataSet Data1, DataSet Data2, Key Equality1, Key Equality2,
            StaticRegister Memory1, StaticRegister Memory2)
        {

            // Check sort //
            CheckSort(Data1, Equality1, Data2, Equality2);

            // function variables //
            int c = 0;
            RecordReader c1 = Data1.OpenReader();
            RecordReader c2 = Data2.OpenReader();
            
            // main loop //
            while (!c1.EndOfData && !c2.EndOfData)
            {

                // get the compare //
                Record r1 = c1.Read();
                Record r2 = c2.Read();
                c = Record.Compare(r1, Equality1, r2, Equality2);
                Memory1.Assign(r1);
                Memory2.Assign(r2);

                // RS1 < RS2 //
                if (c < 0)
                {
                    c1.Advance();
                }
                // RS1 > RS2 //
                else if (c > 0)
                {
                    c2.Advance();
                }
                // RS1 == RS2 //
                else
                {

                    int k = 0;
                    while (c == 0)
                    {

                        // Add the record //
                        Record t = Fields.Evaluate();
                        if (Where.Render())
                            Output.Insert(t);

                        // Advance p2 //
                        k++;
                        c2.Advance();
                        if (c2.EndOfData) break;
                        r2 = c2.Read();
                        Memory2.Assign(r2);

                        // Break if the new c != 0 //
                        c = Record.Compare(r1, Equality1, r2, Equality2);
                        if (c != 0) 
                            break;

                    }
                    c2.Revert(k);
                    c1.Advance();

                }

            }

        }

        private static void SortMergeLeftJoin(RecordWriter Output, FNodeSet Fields, Predicate Where, DataSet Data1, DataSet Data2, Key Equality1, Key Equality2,
            StaticRegister Memory1, StaticRegister Memory2, bool AntiJoin)
        {

            // Check sort //
            CheckSort(Data1, Equality1, Data2, Equality2);

            // function variables //
            int c = 0;
            RecordReader c1 = Data1.OpenReader();
            RecordReader c2 = Data2.OpenReader();
            
            // main loop //
            while (!c1.EndOfData && !c2.EndOfData)
            {

                // get the compare //
                Record r1 = c1.Read();
                Record r2 = c2.Read();
                c = Record.Compare(r1, Equality1, r2, Equality2);
                Memory1.Assign(r1);
                Memory2.Assign(r2);

                // RS1 < RS2 //
                if (c < 0)
                {
                    if (Where.Render())
                    {
                        Memory2.Assign(Data2.Columns.NullRecord);
                        Output.Insert(Fields.Evaluate());
                    }
                    c1.Advance();
                }
                // RS1 > RS2 //
                else if (c > 0)
                {
                    c2.Advance();
                }
                // RS1 == RS2 and AntiJoin //
                else if (AntiJoin)
                {
                    c1.Advance();
                }
                // RS1 == RS2 //
                else
                {

                    int k = 0;
                    while (c == 0)
                    {

                        // Add the record //
                        Record t = Fields.Evaluate();
                        if (Where.Render())
                            Output.Insert(t);

                        // Advance p2 //
                        k++;
                        c2.Advance();
                        if (c2.EndOfData) 
                            break;
                        r2 = c2.Read();
                        Memory2.Assign(r2);

                        // Break if the new c != 0 //
                        c = Record.Compare(r1, Equality1, r2, Equality2);
                        if (c != 0) 
                            break;

                    }
                    c2.Revert(k);
                    c1.Advance();

                }

            }

            Memory2.Assign(Data2.Columns.NullRecord);
            while (!c1.EndOfData)
            {

                Memory1.Assign(c1.ReadNext());
                if (Where.Render())
                    Output.Insert(Fields.Evaluate());
                
            }

        }

        private static void SortMergeRightJoin(RecordWriter Output, FNodeSet Fields, Predicate Where, DataSet Data1, DataSet Data2, Key Equality1, Key Equality2,
            StaticRegister Memory1, StaticRegister Memory2, bool AntiJoin)
        {

            // Check sort //
            CheckSort(Data1, Equality1, Data2, Equality2);

            // function variables //
            int c = 0;
            RecordReader c1 = Data1.OpenReader();
            RecordReader c2 = Data2.OpenReader();
            
            // main loop //
            while (!c1.EndOfData && !c2.EndOfData)
            {

                // get the compare //
                Record r1 = c1.Read();
                Record r2 = c2.Read();
                c = Record.Compare(r1, Equality1, r2, Equality2);
                Memory1.Assign(r1);
                Memory2.Assign(r2);

                // RS1 < RS2 //
                if (c < 0)
                {
                    c1.Advance();
                }
                // RS1 > RS2 //
                else if (c > 0)
                {
                    if (Where.Render())
                    {
                        Memory1.Assign(Data1.Columns.NullRecord);
                        Output.Insert(Fields.Evaluate());
                    }
                    c2.Advance();
                }
                // RS1 == RS2 and AntiJoin //
                else if (AntiJoin)
                {
                    c2.Advance();
                }
                // RS1 == RS2 //
                else
                {

                    int k = 0;
                    while (c == 0)
                    {

                        // Add the record //
                        Output.Insert(Fields.Evaluate());

                        // Advance p2 //
                        k++;
                        c1.Advance();
                        if (c1.EndOfData) 
                            break;
                        r1 = c1.Read();
                        Memory1.Assign(r1);

                        // Break if the new c != 0 //
                        c = Record.Compare(r1, Equality1, r2, Equality2);
                        if (c != 0) 
                            break;

                    }
                    c1.Revert(k);
                    c2.Advance();

                }

            }

            Memory1.Assign(Data1.Columns.NullRecord);
            while (!c2.EndOfData)
            {
                Memory2.Assign(c2.ReadNext());
                if (Where.Render())
                    Output.Insert(Fields.Evaluate());
            }

        }

        public static void SortMerge(MergeMethod JM, RecordWriter Output, FNodeSet Fields, Predicate Where, DataSet Data1, DataSet Data2, Key Equality1, Key Equality2,
            StaticRegister Memory1, StaticRegister Memory2)
        {

            switch (JM)
            {

                case MergeMethod.Cross:
                    CrossJoin(Output, Fields, Where, Data1, Data2, Memory1, Memory2);
                    break;
                case MergeMethod.Inner:
                    SortMergeInnerJoin(Output, Fields, Where, Data1, Data2, Equality1, Equality2, Memory1, Memory2);
                    break;
                case MergeMethod.Left:
                    SortMergeLeftJoin(Output, Fields, Where, Data1, Data2, Equality1, Equality2, Memory1, Memory2, false);
                    break;
                case MergeMethod.Right:
                    SortMergeRightJoin(Output, Fields, Where, Data1, Data2, Equality1, Equality2, Memory1, Memory2, false);
                    break;
                case MergeMethod.Full:
                    SortMergeLeftJoin(Output, Fields, Where, Data1, Data2, Equality1, Equality2, Memory1, Memory2, false);
                    SortMergeRightJoin(Output, Fields, Where, Data1, Data2, Equality1, Equality2, Memory1, Memory2, true);
                    break;
                case MergeMethod.AntiLeft:
                    SortMergeLeftJoin(Output, Fields, Where, Data1, Data2, Equality1, Equality2, Memory1, Memory2, true);
                    break;
                case MergeMethod.AntiRight:
                    SortMergeRightJoin(Output, Fields, Where, Data1, Data2, Equality1, Equality2, Memory1, Memory2, true);
                    break;
                case MergeMethod.AntiInner:
                    SortMergeLeftJoin(Output, Fields, Where, Data1, Data2, Equality1, Equality2, Memory1, Memory2, true);
                    SortMergeRightJoin(Output, Fields, Where, Data1, Data2, Equality1, Equality2, Memory1, Memory2, true);
                    break;

            }
        }

        // Hash Table - Collection Map //
        private static DataSet BuildJoinHelper(DataSet Data1, DataSet Data2, MergeMethod JM)
        {

            // Create the predicate fields //
            Key joiner = new Key();
            int T1Count = Data1.Columns.Count;
            for (int i = 2; i < T1Count; i++)
                joiner.Add(i);

            // Memory Registers //
            StaticRegister mem1 = new StaticRegister(null);
            StaticRegister mem2 = new StaticRegister(null);

            // Build the output fields //
            FNodeSet keeper = new FNodeSet();
            keeper.Add(new FNodeFieldRef(null, 0, CellAffinity.INT, 8, mem1));
            keeper.Add(new FNodeFieldRef(null, 1, CellAffinity.INT, 8, mem1));
            keeper.Add(new FNodeFieldRef(null, 0, CellAffinity.INT, 8, mem2));
            keeper.Add(new FNodeFieldRef(null, 1, CellAffinity.INT, 8, mem2));

            // Create the hashing variables //
            string dir = (Data1.Directory != null ? Data1.Directory : Data2.Directory);
            string name = Header.TempName();
            Schema s = new Schema("set_id1 int, row_id1 int, set_id2 int, row_id2 int");

            // Write the join result to the data set //
            DataSet hash = DataSet.CreateOfType(Data1, dir, name, s, Data1.MaxRecords);
            RecordWriter brw = hash.OpenWriter();
            MergeFunctions.SortMerge(JM, brw, keeper, Predicate.TrueForAll, Data1, Data2, joiner, joiner, mem1, mem2);
            brw.Close();

            // Return //
            return hash;

        }

        public static void HashTable(MergeMethod JM, RecordWriter Output, FNodeSet Fields, Predicate Where, DataSet Data1, DataSet Data2, Key Equality1, Key Equality2,
            StaticRegister Memory1, StaticRegister Memory2)
        {

            // Build temp hash tables //
            DataSet h1 = IndexBuilder.Build(Data1, Equality1, Data1.Directory);
            DataSet h2 = IndexBuilder.Build(Data2, Equality2, Data2.Directory);

            // Combine has tables //
            DataSet hash = BuildJoinHelper(h1, h2, JM);

            // Exit if the hash table has no records //
            if (hash.IsEmpty)
            {
                DataSetManager.DropData(h1);
                DataSetManager.DropData(h2);
                DataSetManager.DropData(hash);
                return;
            }

            // Sort the table by the first and second set ids, keys 0 and 2 //
            hash.Sort(new Key(0, 2));

            // Open a reader //
            RecordReader ac = hash.OpenReader();

            // Define logic //
            int sid1 = (int)ac.Read()[0].INT;
            int sid2 = (int)ac.Read()[2].INT;
            int rid1 = 0;
            int rid2 = 0;
            bool isnull1 = false;
            bool isnull2 = false;

            // Create the temp variables //
            RecordSet ts1 = Data1.PopAt(sid1);
            RecordSet ts2 = Data2.PopAt(sid2);

            // Main loop //
            while (!ac.EndOfData)
            {

                // Read the record id //
                Record dr = ac.ReadNext();
                sid1 = (int)dr[0].INT;
                rid1 = (int)dr[1].INT;
                sid2 = (int)dr[2].INT;
                rid2 = (int)dr[3].INT;
                isnull1 = dr[0].IsNull;
                isnull2 = dr[2].IsNull;

                // Check if we need to re-buffer a shard //
                if (ts1.ID != sid1 && !isnull1)
                    ts1 = Data1.PopAt(sid1);
                if (ts2.ID != sid2 && !isnull2)
                    ts2 = Data2.PopAt(sid2);

                // Create the output record - table one //
                if (!isnull1)
                    Memory1.Assign(ts1[rid1]);
                else
                    Memory1.Assign(ts1.Columns.NullRecord);

                // Create the output record - table two //
                if (!isnull2)
                    Memory2.Assign(ts2[rid2]);
                else
                    Memory2.Assign(ts2.Columns.NullRecord);

                // Write the output record //
                Record t = Fields.Evaluate();
                if (Where.Render())
                    Output.Insert(t);

            }

            // Drop tables //
            DataSetManager.DropData(h1);
            DataSetManager.DropData(h2);
            DataSetManager.DropData(hash);

        }

        // Main Join Functions //
        /// <summary>
        /// Allows the user to perform a join based on the equality predicate AND each predicate link via 'AND'
        /// </summary>
        /// <param name="Output"></param>
        /// <param name="JM"></param>
        /// <param name="JA"></param>
        /// <param name="T1"></param>
        /// <param name="J1"></param>
        /// <param name="T2"></param>
        /// <param name="J2"></param>
        /// <param name="CM"></param>
        public static void Join(MergeMethod JM, MergeAlgorithm JA, RecordWriter Output, FNodeSet Fields, Predicate Where, DataSet Data1, DataSet Data2, 
            Key Equality1, Key Equality2, StaticRegister Memory1, StaticRegister Memory2)
        {

            // Do some checks first //
            if (Where == null)
                Where = Predicate.TrueForAll;
            if (Equality1.Count != Equality2.Count)
                throw new Exception("Both join keys must have the same length");
            if (Equality1.Count == 0)
                JA = MergeAlgorithm.NestedLoop;

            // Nested loop; if the algorithm is nested loop and we have keys, we need to build a new where clause that has the equality predicates //
            FNodeResult nl_node = new FNodeResult(null, new AndMany());
            nl_node.AddChildNode(Where.Node.CloneOfMe());
            for (int i = 0; i < Equality1.Count; i++)
            {

                FNodeFieldRef left = new FNodeFieldRef(null, Equality1[i], Data1.Columns.ColumnAffinity(Equality1[i]), Data1.Columns.ColumnSize(Equality1[i]), Memory1);
                FNodeFieldRef right = new FNodeFieldRef(null, Equality2[i], Data2.Columns.ColumnAffinity(Equality2[i]), Data2.Columns.ColumnSize(Equality2[i]), Memory2);
                FNodeResult eq = new FNodeResult(null, new CellBoolEQ());
                eq.AddChildren(left, right);
                nl_node.AddChildNode(eq);

            }

            Predicate nl_where = (Equality1.Count == 0 ? Where : new Predicate(nl_node));

            // Switch //
            switch (JA)
            {
                case MergeAlgorithm.SortMerge:
                    SortMerge(JM, Output, Fields, Where, Data1, Data2, Equality1, Equality2, Memory1, Memory2);
                    break;
                case MergeAlgorithm.NestedLoop:
                    NestedLoop(JM, Output, Fields, nl_where, Data1, Data2, Memory1, Memory2);
                    break;
                default:
                    HashTable(JM, Output, Fields, Where, Data1, Data2, Equality1, Equality2, Memory1, Memory2);
                    break;
            }

        }

    }

    public sealed class MergePlan : CommandPlan
    {

        private MergeMethod _use_method;
        private MergeAlgorithm _use_algorithm;
        private RecordWriter _output;
        private FNodeSet _fields;
        private Predicate _where;
        private DataSet _data1;
        private DataSet _data2;
        private Key _key1;
        private Key _key2;
        private StaticRegister _mem1;
        private StaticRegister _mem2;

        public MergePlan(MergeMethod JM, MergeAlgorithm JA, RecordWriter Output, FNodeSet Fields, Predicate Where, DataSet Data1, DataSet Data2, 
            Key Equality1, Key Equality2, StaticRegister Memory1, StaticRegister Memory2)
            : base()
        {

            this._use_method = JM;
            this._use_algorithm = JA;
            this._output = Output;
            this._fields = Fields;
            this._where = Where;
            this._data1 = Data1;
            this._data2 = Data2;
            this._key1 = Equality1;
            this._key2 = Equality2;
            this._mem1 = Memory1;
            this._mem2 = Memory2;
            this.Name = "MERGE";

        }

        public override void Execute()
        {

            this.Message.AppendLine(string.Format("Merge '{0}' with '{1}'", this._data1.Name, this._data2.Name));
            this.Message.AppendLine(string.Format("Using the '{0}' method and the '{1}' algorithm", this._use_method, this._use_algorithm));

            this._timer = System.Diagnostics.Stopwatch.StartNew();
            MergeFunctions.Join(this._use_method, this._use_algorithm, this._output, this._fields, 
                this._where, this._data1, this._data2, this._key1, this._key2, this._mem1, this._mem2);
            
            this._output.Close();
            this._timer.Stop();

        }



    }

}
