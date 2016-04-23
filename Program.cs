using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Equus.Horse;
using Equus.Shire;
using Equus.QuarterHorse;
using Equus.Calabrese;
using Equus.HScript;
using Equus.Thoroughbred.ARizenTalent;
using Equus.Thoroughbred.ManOWar;
using Equus.Gidran;
using Equus.Nokota;
using System.Drawing;
using System.Xml.Linq;

/*
 * Mustang : core data libraries
 * Shire : core query libraries
 * QuarterHorse : leafnode, predicate, functions
 * 
 * 
 * 
 * 
 * 
 */

namespace Equus
{
    class Program
    {

        static void Main(string[] args)
        {

            string Notifier = "";
            Stopwatch sw = new Stopwatch();
            sw.Start();
            long bytes = GC.GetTotalMemory(false);

            //double[][] data = new double[4][];

            //data[0] = new double[] {0D, 0D, 0D};
            //data[1] = new double[] {1D, 0D, 1D};
            //data[2] = new double[] {0D, 1D, 1D};
            //data[3] = new double[] {1D, 1D, 0D};

            //Matrix m = new Matrix(4, 3);
            //m[0] = data[0];
            //m[1] = data[1];
            //m[2] = data[2];
            //m[3] = data[3];

            //Thoroughbred.WarAdmiral.LinkUpdate rule = Thoroughbred.WarAdmiral.LinkUpdateFactory.Generate("b;0.70;0.0");
            //Thoroughbred.WarAdmiral.LinkUpdate rule = Thoroughbred.WarAdmiral.LinkUpdateFactory.Generate("m");
            //Thoroughbred.WarAdmiral.LinkUpdate rule = Thoroughbred.WarAdmiral.LinkUpdateFactory.Generate("r");
            //Thoroughbred.WarAdmiral.LinkUpdate rule = Thoroughbred.WarAdmiral.LinkUpdateFactory.Generate("r+");
            //Thoroughbred.WarAdmiral.LinkUpdate rule = Thoroughbred.WarAdmiral.LinkUpdateFactory.Generate("r-");
            //Thoroughbred.WarAdmiral.LinkUpdate rule = Thoroughbred.WarAdmiral.LinkUpdateFactory.Generate("ir+");
            //Thoroughbred.WarAdmiral.LinkUpdate rule = Thoroughbred.WarAdmiral.LinkUpdateFactory.Generate("ir-");
            //Thoroughbred.WarAdmiral.LinkUpdate rule = Thoroughbred.WarAdmiral.LinkUpdateFactory.Generate("test");

            //Thoroughbred.WarAdmiral.Network net = new Thoroughbred.WarAdmiral.Network(rule, data);

            //net.AddDataLayer(new int[] { 0, 1 }, true);
            //net.AddHiddenLayer(2, true, new Numerics.BinarySigmoid());
            //net.AddOutputLayer(new int[] { 2 }, new Numerics.BinarySigmoid());
            //net.Construct();

            //net.Render();
            //net.PrintPredictions(data);
            //Console.WriteLine(net.Statistics());

            //Console.WriteLine("--------------------------------------");
            //Thoroughbred.ManOWar.SimpleNetwork sim = new SimpleNetwork();
            //sim.TrainOne(m);

            //sim.PrintGradients();

            Workspace space = new Workspace(@"C:\Users\pwdlu_000\Documents\Equus\X_Data\Temp_Database\");
            HScriptProcessor runner = new HScriptProcessor(space);
            string script = File.ReadAllText(@"C:\Users\pwdlu_000\Documents\Equus\Equus\HScript\TestScript.txt");
            runner.Execute(script);

            bytes = GC.GetTotalMemory(false) - bytes;
            Console.WriteLine("Disk Reads: {0} ::::: Disk Writes: {1}", BinarySerializer.DiskReads, BinarySerializer.DiskWrites);
            Console.WriteLine(":::::: Complete {0} : {1}kb ::::::", sw.Elapsed.ToString(), Math.Round(((double)bytes) / (1024.00), 2));
            Notifier = Console.ReadLine();

        }

        public static string CommRunFile = "RUNF";
        public static string CommRunCommand = "RUNC";
        public static string CommExit = "EXIT";
        public static string CommOkGo = "OKGO";

        public static void CommandRun(string[] args)
        {

            // Communicate //
            Console.WriteLine("Welcome to Horse!");
            string temp = null;
            if (args.Length == 0)
            {
                Console.WriteLine("Please enter a temp directory:");
                temp = Console.ReadLine();
            }
            else
            {
                temp = args[0];
            }

            // Run each command //
            Workspace space = new Workspace(temp);
            HScriptProcessor runner = new HScriptProcessor(space);
            int runs = 0;

            string Command = CommOkGo;
            while (Command != CommExit)
            {

                Console.WriteLine("Enter Horse Command:");
                Command = Console.ReadLine();
                while (Command.Length < 4)
                {
                    Console.WriteLine("Command is invalid. Please enter a new command");
                    Command = Console.ReadLine();
                }

                string MajorCommand = Command.Substring(0, 4).ToUpper();
                string MinorCommand = Command.Substring(4, Command.Length - 4).Trim();

                // Check for exit //
                if (MajorCommand == CommExit)
                {
                    return;
                }
                // File run //
                else if (MajorCommand == CommRunFile)
                {
                    string file = File.ReadAllText(MinorCommand);
                    runner.Execute(file);
                }
                // Command line run //
                else if (MajorCommand == CommRunCommand)
                {
                    runner.Execute(MinorCommand);
                }
                // Invalid command //
                else
                {
                    Console.WriteLine("Command '{0}' is not valid", MajorCommand);
                }
                runs++;

            }

        }

        public static void TestSerializer()
        {

            //RecordSet rs = new RecordSet(@"C:\Users\pwdlu_000\Documents\Equus\X_Data\Temp_Database\", "Test1", new Schema("t1 bool, t2 int, t3 double, t4 date, t5 string.16, t6 blob.16"));
            Table t = new Table(@"C:\Users\pwdlu_000\Documents\Equus\X_Data\Temp_Database\", "Test2", new Schema("t1 bool, t2 int, t3 double, t4 date, t5 string.16, t6 blob.16"), 500);
            RecordWriter w = t.OpenWriter();

            for (int i = 0; i < 5000; i++)
            {
                Cell a = new Cell(true);
                Cell b = new Cell(100L);
                Cell c = new Cell(3.14D);
                Cell d = new Cell(DateTime.Now);
                Cell e = new Cell("Hello World1");
                Cell f = new Cell(Guid.NewGuid().ToByteArray());
                Record r = Record.Stitch(a, b, c, d, e, f);
                w.Insert(r);
            }
            w.Close();

            Table x = BinarySerializer.BufferTable(t.Header.Path);
            RecordReader uc = x.OpenReader();

            while (!uc.EndOfData)
            {
                Console.WriteLine(uc.ReadNext());
            }


            //BinarySerializer.FlushRecordSetSafe4(rs);

            //Stopwatch tx = Stopwatch.StartNew();
            //for (int i = 0; i < 1000; i++)
            //{
            //    //BinarySerializer.FlushRecordSet(rs);
            //    //BinarySerializer.FlushRecordSetSafe(rs);
            //    //BinarySerializer.FlushRecordSetSafe2(rs);
            //    //BinarySerializer.FlushRecordSetSafe3(rs);
            //    BinarySerializer.FlushRecordSetSafe4(rs);
            //}
            //tx.Stop();
            //Console.WriteLine(tx.Elapsed);

            //Stopwatch tx = Stopwatch.StartNew();
            //for (int i = 0; i < 1000; i++)
            //{
            //    RecordSet ts = BinarySerializer.BufferRecordSet(rs.Header.Path);
            //    RecordSet ts = BinarySerializer.BufferRecordSetSafe(rs.Header.Path);
            //    RecordSet ts = BinarySerializer.BufferRecordSetSafe2(rs.Header.Path);
            //}
            //tx.Stop();
            //Console.WriteLine(tx.Elapsed);
            //RecordSet qs = BinarySerializer.BufferRecordSetSafe2(rs.Header.Path);
            //qs.Print(10);

            //RecordSet ts = BinarySerializer.BufferRecordSetSafe2(rs.Header.Path);
            //ts.Print(10);


        }

        // Finds the integer square root of a positive number  
        public static long Root(long num)
        {

            if (0 == num) { return 0; }   
            long n = (num / 2) + 1;       
            long n1 = (n + (num / n)) / 2;
            while (n1 < n)
            {
                n = n1;
                n1 = (n + (num / n)) / 2;
            }   
            return n;

        }

        public static bool IsSquare(long num)
        {
            long root = Root(num);
            return (num - root * root) == 0;
        }

        public static void RSATest()
        {

            long p = 179424673;
            long q = 275604541;
            
            long t = p * q;

            Console.WriteLine(Factor(p * q, 3, 2));
            return;

            //List<long> primes = GetPrimes(t, 1);
            //foreach (long l in primes)
            //    Console.WriteLine("PRIME : {0}", l);
            //return;

            long p_var = Root(t);
            long c = p_var / 10 + 1;
            long exit = (long)(int.MaxValue) / 4;
            long learning_rate = Root(Root(t));
            long lag_error = long.MaxValue;
            
            Console.WriteLine("ROOT {0}", exit);

            for (int i = 0; i < 10000000; i++)
            {

                long q_var = p_var + c;
                long e = (t - p_var * q_var);
                long c_dx = p_var;
                long p_dx = 2 * p_var * c;

                if (lag_error < e)
                    learning_rate = learning_rate * 1200 / 1000;
                else
                    learning_rate = learning_rate / 2;
                if (learning_rate < 1000)
                    learning_rate = 1000;

                c += Math.Sign(e) * learning_rate;
                p_var = t / (c + p_var);
                
                //Console.WriteLine("Error {0} : pq {1} : t {2} p {3} : q {4} : exit {5}", e, p_var * q_var, t, p_var, p_var + c, exit);
                if (Math.Abs(e) < exit)
                    break;

                if (c < 0)
                    c = -c;
                if (p_var < 0)
                    p_var = -p_var;

                lag_error = e;

            }

            long l = Factor(t, p_var, 2);
            Console.WriteLine(l);


        }

        public static long Factor(long PQ, long P_Start, long Spin)
        {

            long p = P_Start;
            long q = PQ / P_Start;
            long error = PQ - p * q;

            while (error != 0)
            {

                if (error < 0)
                    p -= Spin;
                else
                    p += Spin;

                q = PQ / p;
                error = PQ - p * q;

            }

            return p;

        }

        public static List<long> GetPrimes(long n, long start, long spin)
        {

            List<long> storage = new List<long>();
            while (n > 1)
            {
                long i = start;
                while (true)
                {
                    //Console.WriteLine(i);
                    if (IsPrime(i))
                    {
                        if (n % i == 0)
                        {
                            n /= i;
                            storage.Add(i);
                            break;
                        }
                    }
                    i += spin;
                }
            }
            return storage;
        }

        public static bool IsPrime(long n)
        {
            
            if (n <= 1) 
                return false;

            if (n < 6)
                return (n == 2 || n == 3);

            if (((n + 1) % 6 != 0) && ((n-1) % 6 != 0))
                return false;

            for (long i = 6; i <= (long)Math.Sqrt(n) + 1; i++)
                if (n % i == 0) 
                    return false;
            return true;

        }

        public void NeuralTest()
        {


            //NeuralNetworkFactory factory = new NeuralNetworkFactory(Numerics.ScalarFunctionFactory.Select("BinarySigmoid"), new NodeReductionLinear());
            //factory.AddDataLayer(true, new Key(0, 1));
            //factory.AddHiddenLayer(true, 2);
            //factory.AddPredictionLayer(new Key(2));
            //NeuralNetwork net = factory.Construct();

            //Matrix m = new Matrix(4, 3);
            //m[0] = new double[] { 0D, 0D, 0D };
            //m[1] = new double[] { 1D, 0D, 1D };
            //m[2] = new double[] { 0D, 1D, 1D };
            //m[3] = new double[] { 1D, 1D, 0D };

            //net.Seed = 128;
            //net.Epochs = 10000;
            //NeuralRule rule = RuleFactory.Construct("bprop", net.Responses);
            ////NeuralRule rule = RuleFactory.Construct("rprop", net.Responses);
            ////NeuralRule rule = RuleFactory.Construct("rprop+", net.Responses);
            ////NeuralRule rule = RuleFactory.Construct("rprop-", net.Responses);
            ////NeuralRule rule = RuleFactory.Construct("irprop+", net.Responses);
            ////NeuralRule rule = RuleFactory.Construct("irprop-", net.Responses);
            ////NeuralRule rule = RuleFactory.Construct("mprop", net.Responses);
            ////NeuralRule rule = RuleFactory.Construct("qprop", net.Responses);
            ////NeuralRule rule = RuleFactory.Construct("hprop", net.Responses);

            //net.Render();

            //net.PrintPredictions(m);
            //Console.WriteLine(net.Statistics());


        }



    }

    public static class OCRHelper
    {

        public static Record GetCircleRecord(string FilePath)
        {

            Bitmap map = new Bitmap((FilePath));
            Record r = Lipizzan.Exchange.MonoChromeBitmapToRecord(map);
            RecordBuilder rb = new RecordBuilder();
            rb.Add(FilePath);
            rb.Add(1D);
            rb.Add(0D);
            rb.Add(0D);
            rb.Add(r);

            return rb.ToRecord();
        
        }

        public static Record GetTriangleRecord(string FilePath)
        {

            Bitmap map = new Bitmap((FilePath));
            Record r = Lipizzan.Exchange.MonoChromeBitmapToRecord(map);
            RecordBuilder rb = new RecordBuilder();
            rb.Add(FilePath);
            rb.Add(0D);
            rb.Add(1D);
            rb.Add(0D);
            rb.Add(r);

            return rb.ToRecord();

        }

        public static Record GetSquareRecord(string FilePath)
        {

            Bitmap map = new Bitmap((FilePath));
            Record r = Lipizzan.Exchange.MonoChromeBitmapToRecord(map);
            RecordBuilder rb = new RecordBuilder();
            rb.Add(FilePath);
            rb.Add(0D);
            rb.Add(0D);
            rb.Add(1D);
            rb.Add(r);

            return rb.ToRecord();

        }

    }


}
