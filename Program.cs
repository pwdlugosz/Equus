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
using Equus.Gidran;

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

            Program.CommandRun(args);

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


    }



}
