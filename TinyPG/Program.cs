// Copyright 2008 - 2010 Herre Kuijpers - <herre.kuijpers@gmail.com>
//
// This source file(s) may be redistributed, altered and customized
// by any means PROVIDING the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------
using System;
using System.IO;
using System.Windows.Forms;
using TinyPG.Compiler;
using System.Text;

namespace TinyPG
{
    public class Program
    {
        private readonly StringBuilder _output;

        private readonly OnParseErrorDelegate _parseErrorDelegate;

        public Program(OnParseErrorDelegate parseErrorDelegate, StringBuilder output)
        {
            this._parseErrorDelegate = parseErrorDelegate;
            this._output = output;
        }

        public delegate void OnParseErrorDelegate(ParseTree tree, StringBuilder output);

        public enum ExitCode
        {
            Success = 0,
            InvalidFilename = 1,
            UnknownError = 10
        }

        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                var grammarFilePath = Path.GetFullPath(args[0]);
                var output = new StringBuilder(string.Empty);
                if (!File.Exists(grammarFilePath))
                {
                    output.Append("Specified file " + grammarFilePath + " does not exists");
                    Console.WriteLine(output.ToString());
                    return (int)ExitCode.InvalidFilename;
                }

                // As stated in documentation current directory is the one of the TPG file.
                Directory.SetCurrentDirectory(Path.GetDirectoryName(grammarFilePath));

                var starttimer = DateTime.Now;

                var prog = new Program(ManageParseError, output);
                var grammar = prog.ParseGrammar(File.ReadAllText(grammarFilePath), Path.GetFileName(grammarFilePath));

                if (grammar != null && prog.BuildCode(grammar, new Compiler.Compiler()))
                {
                    var span = DateTime.Now.Subtract(starttimer);
                    output.AppendLine("Compilation successful in " + span.TotalMilliseconds + "ms.");
                }

                Console.WriteLine(output.ToString());
            }
            else
            {
                Application.ThreadException += ApplicationThreadException;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }

            return (int)ExitCode.Success;
        }

        public bool BuildCode(Grammar grammar, Compiler.Compiler compiler)
        {

            this._output.AppendLine("Building code...");
            compiler.Compile(grammar);
            if (!compiler.IsCompiled)
            {
                foreach (var err in compiler.Errors)
                    this._output.AppendLine(err);
                this._output.AppendLine("Compilation contains errors, could not compile.");
            }

            new GeneratedFilesWriter(grammar).Generate(false);

            return compiler.IsCompiled;
        }

        public Grammar ParseGrammar(string input, string grammarFile)
        {
            Grammar grammar = null;
            var scanner = new Scanner();
            var parser = new Parser(scanner);

            var tree = parser.Parse(input, grammarFile, new GrammarTree());

            if (tree.Errors.Count > 0)
            {
                this._parseErrorDelegate(tree, this._output);
            }
            else
            {
                grammar = (Grammar)tree.Eval();
                grammar.Preprocess();

                if (tree.Errors.Count == 0)
                {
                    this._output.AppendLine(grammar.PrintGrammar());
                    this._output.AppendLine(grammar.PrintFirsts());

                    this._output.AppendLine("Parse successful!\r\n");
                }
            }
            return grammar;
        }

        private static void ManageParseError(ParseTree tree, StringBuilder output)
        {
            foreach (var error in tree.Errors)
                output.AppendLine(string.Format("({0},{1}): {2}", error.Line, error.Column, error.Message));

            output.AppendLine("Semantic errors in grammar found.");
        }

        static void ApplicationThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show("An unhandled exception occured: " + e.Exception.Message);
        }
    }
}
