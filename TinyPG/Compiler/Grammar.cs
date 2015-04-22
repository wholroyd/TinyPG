// Copyright 2008 - 2010 Herre Kuijpers - <herre.kuijpers@gmail.com>
//
// This source file(s) may be redistributed, altered and customized
// by any means PROVIDING the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------

namespace TinyPG.Compiler
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;

    public class Grammar
    {
        public Grammar()
        {
            this.Symbols = new Symbols();
            this.SkipSymbols = new Symbols();
            this.Directives = new Directives();
        }

        /// <summary>
        /// These are specific directives that should be applied to the grammar. This can be
        /// metadata, or information on how code should be generated.
        /// </summary>
        public Directives Directives { get; set; }

        /// <summary>
        /// The special symbol used to alter the internal file and line number tracking for correct
        /// error reporting.
        /// </summary>
        public Symbol FileAndLine { get; set; }

        /// <summary>
        /// Corresponds to the symbols that will be skipped during parsing e.g. commenting codeblocks
        /// </summary>
        public Symbols SkipSymbols { get; set; }

        /// <summary>
        /// Represents all terminal and nonterminal symbols in the grammar
        /// </summary>
        public Symbols Symbols { get; set; }

        public Symbols GetNonTerminals()
        {
            var symbols = new Symbols();
            foreach (var s in this.Symbols)
            {
                if (s is NonTerminalSymbol)
                    symbols.Add(s);
            }

            return symbols;
        }

        public string GetOutputPath()
        {
            var folder = Directory.GetCurrentDirectory() + @"\";
            var pathout = this.Directives["TinyPG"]["OutputPath"];
            folder = Path.IsPathRooted(pathout) 
                ? Path.GetFullPath(pathout) 
                : Path.GetFullPath(folder + pathout);

            var dir = new DirectoryInfo(folder + @"\");
            return dir.Exists 
                ? folder 
                : null;
        }

        public string GetTemplatePath()
        {
            var folder = AppDomain.CurrentDomain.BaseDirectory;
            var pathout = this.Directives["TinyPG"]["TemplatePath"];
            folder = Path.GetFullPath(Path.IsPathRooted(pathout) 
                ? pathout 
                : Path.Combine(folder, pathout));

            var dir = new DirectoryInfo(folder + @"\");
            return dir.Exists 
                ? folder
                : null;
        }

        public Symbols GetTerminals()
        {
            var symbols = new Symbols();
            foreach (var s in this.Symbols)
            {
                if (s is TerminalSymbol)
                    symbols.Add(s);
            }

            return symbols;
        }
        /// <summary>
        /// Once the grammar terminals and nonterminal production rules have been defined
        /// use the Compile method to analyse and check the grammar semantics.
        /// </summary>
        public void Preprocess()
        {
            this.SetupDirectives();

            this.DetermineFirsts();

            //LookAheadTree LATree = DetermineLookAheadTree();
            //Symbols nts = GetNonTerminals();
            //NonTerminalSymbol n = (NonTerminalSymbol)nts[0];
            //TerminalSymbol t = (TerminalSymbol) n.FirstTerminals[0];

            //Symbols Follow = new Symbols();
            //t.Rule.DetermineFirstTerminals(Follow, 1);
        }

        /*
        private LookAheadTree DetermineLookAheadTree()
        {
            LookAheadTree tree = new LookAheadTree();
            foreach (NonTerminalSymbol nts in GetNonTerminals())
            {
                tree.NonTerminal = nts;
                nts.DetermineLookAheadTree(tree);
                //nts.DetermineFirstTerminals();
                tree.PrintTree();
            }
            return tree;
        }
        */

        public string PrintFirsts()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\r\n/*\r\nFirst symbols:");
            foreach (NonTerminalSymbol s in this.GetNonTerminals())
            {
                var firsts = s.Name + ": ";
                foreach (TerminalSymbol t in s.FirstTerminals)
                    firsts += t.Name + ' ';
                sb.AppendLine(firsts);
            }

            sb.AppendLine("\r\nSkip symbols: ");
            var skips = "";
            foreach (TerminalSymbol s in this.SkipSymbols)
            {
                skips += s.Name + " ";
            }
            sb.AppendLine(skips);
            sb.AppendLine("*/");
            return sb.ToString();

        }

        public string PrintGrammar()
        {
            var sb = new StringBuilder();
            sb.AppendLine("//Terminals:");
            foreach (var s in this.GetTerminals())
            {
                var skip = this.SkipSymbols.Find(s.Name);
                if (skip != null)
                    sb.Append("[Skip] ");
                sb.AppendLine(s.PrintProduction());
            }

            sb.AppendLine("\r\n//Production lines:");
            foreach (var s in this.GetNonTerminals())
            {
                sb.AppendLine(s.PrintProduction());
            }

            return sb.ToString();
        }

        private void DetermineFirsts()
        {
            foreach (NonTerminalSymbol nts in this.GetNonTerminals())
            {
                nts.DetermineFirstTerminals();
            }
        }

        private void SetupDirectives()
        {

            var d = this.Directives.Find("TinyPG");
            if (d == null)
            {
                d = new Directive("TinyPG");
                this.Directives.Insert(0, d);
            }
            if (!d.ContainsKey("Namespace"))
                d["Namespace"] = "TinyPG"; // set default namespace
            if (!d.ContainsKey("OutputPath"))
                d["OutputPath"] = "./"; // write files to current path
            if (!d.ContainsKey("Language"))
                d["Language"] = "C#"; // set default language
            if (!d.ContainsKey("TemplatePath"))
            {
                switch (d["Language"].ToLower(CultureInfo.InvariantCulture))
                {
                    // set the default templates directory
                    case "visualbasic":
                    case "vbnet":
                    case "vb.net":
                    case "vb":
                        d["TemplatePath"] = AppDomain.CurrentDomain.BaseDirectory + @"Templates\VB\";
                        break;
                    default:
                        d["TemplatePath"] = AppDomain.CurrentDomain.BaseDirectory + @"Templates\C#\";
                        break;
                }
            }

            d = this.Directives.Find("Parser");
            if (d == null)
            {
                d = new Directive("Parser");
                this.Directives.Insert(1, d);
            }
            if (!d.ContainsKey("Generate"))
                d["Generate"] = "True"; // generate parser by default

            d = this.Directives.Find("Scanner");
            if (d == null)
            {
                d = new Directive("Scanner");
                this.Directives.Insert(1, d);
            }
            if (!d.ContainsKey("Generate"))
                d["Generate"] = "True"; // generate scanner by default

            d = this.Directives.Find("ParseTree");
            if (d == null)
            {
                d = new Directive("ParseTree");
                this.Directives.Add(d);
            }
            if (!d.ContainsKey("Generate"))
                d["Generate"] = "True"; // generate parsetree by default

            d = this.Directives.Find("TextHighlighter");
            if (d == null)
            {
                d = new Directive("TextHighlighter");
                this.Directives.Add(d);
            }
            if (!d.ContainsKey("Generate"))
                d["Generate"] = "False"; // do NOT generate a text highlighter by default
        }
    }
}