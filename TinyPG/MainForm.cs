// Copyright 2008 - 2010 Herre Kuijpers - <herre.kuijpers@gmail.com>
//
// This source file(s) may be redistributed, altered and customized
// by any means PROVIDING the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------

namespace TinyPG
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;
    using System.Xml;

    using TinyPG.CodeGenerators;
    using TinyPG.Compiler;
    using TinyPG.Controls;
    using TinyPG.Controls.DockExtender;
    using TinyPG.Debug;
    using TinyPG.Highlighter;

    using ParseTree = TinyPG.Compiler.ParseTree;

    public partial class MainForm : Form
    {
        #region member declarations
        // checks the syntax/semantics while editing on a seperate thread
        private SyntaxChecker _checker;

        // autocomplete popup form
        private AutoComplete _codecomplete;

        // the compiler used to evaluate the input
        private Compiler.Compiler _compiler;
        private AutoComplete _directivecomplete;
        // manages docking and floating of panels
        private DockExtender _dockExtender;

        Grammar _grammar;


        // the current file the user is editing
        private string _grammarFile;

        // scanner to be used by the highlighter, declare here
        // so we can modify the scanner properies at runtime if needed
        private Highlighter.Scanner _highlighterScanner;

        // used to make the input pane floating/draggable
        IFloaty _inputFloaty;

        // indicates if text/grammar has changed
        private bool _isDirty;
        // marks erronious text with little waves
        // this is used in combination with the checker
        private TextMarker _marker;

        // used to make the output pane floating/draggable
        IFloaty _outputFloaty;
        // keep this event handler reference in a seperate object, so it can be
        // unregistered on closing. this is required because the checker runs on a seperate thread
        EventHandler _syntaxUpdateChecker;

        // timer that will fire if the changed text requires evaluating
        private System.Windows.Forms.Timer _textChangedTimer;

        // responsible for text highlighting
        private TextHighlighter _textHighlighter;
        #endregion

        #region Initialization
        public MainForm()
        {
            this.InitializeComponent();
            this._isDirty = false;
            this._compiler = null;
            this._grammarFile = null;

            this.Disposed += this.MainFormDisposed;
        }

        private void MainFormLoad(object sender, EventArgs e)
        {
            this.headerEvaluator.Activate(this.textInput);
            this.headerEvaluator.CloseClick += this.HeaderEvaluatorCloseClick;
            this.headerOutput.Activate(this.tabOutput);
            this.headerOutput.CloseClick += this.HeaderOutputCloseClick;

            this._dockExtender = new DockExtender(this);
            this._inputFloaty = this._dockExtender.Attach(this.panelInput, this.headerEvaluator, this.splitterBottom);
            this._outputFloaty = this._dockExtender.Attach(this.panelOutput, this.headerOutput, this.splitterRight);

            this._inputFloaty.Docking += this.InputFloatyDocking;
            this._outputFloaty.Docking += this.InputFloatyDocking;
            this._inputFloaty.Hide();
            this._outputFloaty.Hide();

            this.textOutput.Text = AssemblyInfo.ProductName + " v" + Application.ProductVersion + "\r\n";
            this.textOutput.Text += AssemblyInfo.CopyRightsDetail + "\r\n\r\n";


            this._marker = new TextMarker(this.textEditor);
            this._checker = new SyntaxChecker(this._marker); // run the syntax checker on seperate thread

            // create the syntax update checker event handler and remember its reference
            this._syntaxUpdateChecker = this.CheckerUpdateSyntax;
            this._checker.UpdateSyntax += this._syntaxUpdateChecker; // listen for events
            var thread = new Thread(this._checker.Start);
            thread.Start();

            this._textChangedTimer = new System.Windows.Forms.Timer();
            this._textChangedTimer.Tick += this.TextChangedTimerTick;

            // assign the auto completion function to this editor
            // autocomplete form will take care of the rest
            this._codecomplete = new AutoComplete(this.textEditor);
            this._codecomplete.Enabled = false;
            this._directivecomplete = new AutoComplete(this.textEditor);
            this._directivecomplete.Enabled = false;
            this._directivecomplete.WordList.Items.Add("@ParseTree");
            this._directivecomplete.WordList.Items.Add("@Parser");
            this._directivecomplete.WordList.Items.Add("@Scanner");
            this._directivecomplete.WordList.Items.Add("@TextHighlighter");
            this._directivecomplete.WordList.Items.Add("@TinyPG");
            this._directivecomplete.WordList.Items.Add("Generate");
            this._directivecomplete.WordList.Items.Add("Language");
            this._directivecomplete.WordList.Items.Add("Namespace");
            this._directivecomplete.WordList.Items.Add("OutputPath");
            this._directivecomplete.WordList.Items.Add("TemplatePath");

            // setup the text highlighter (= text coloring)
            this._highlighterScanner = new Highlighter.Scanner();
            this._textHighlighter = new TextHighlighter(this.textEditor, this._highlighterScanner, new Highlighter.Parser(this._highlighterScanner));
            this._textHighlighter.SwitchContext += this.TextHighlighterSwitchContext;

            this.LoadConfig();

            if (this._grammarFile == null)
            {
                this.NewGrammar();
            }
        }
        #endregion Initialization

        #region Control events
        void CheckerUpdateSyntax(object sender, EventArgs e)
        {
            if (this.InvokeRequired && !this.IsDisposed)
            {
                this.Invoke(new EventHandler(this.CheckerUpdateSyntax), new object[] { sender, e });
                return;
            }

            this._marker.MarkWords();

            if (this._checker.Grammar == null) return;
            if (this._codecomplete.Visible) return;

            lock (this._checker.Grammar)
            {
                var startAdded = false;
                this._codecomplete.WordList.Items.Clear();
                foreach (var s in this._checker.Grammar.Symbols)
                {
                    this._codecomplete.WordList.Items.Add(s.Name);
                    if (s.Name == "Start")
                        startAdded = true;
                }

                if (!startAdded)
                {
                    this._codecomplete.WordList.Items.Add("Start");
                }
            }
        }

        void InputFloatyDocking(object sender, EventArgs e)
        {
            this.textEditor.BringToFront();
        }

        /// <summary>
        /// a context switch is raised when the caret of the editor moves from one section to
        /// another. the sections are defined by the highlighter parser. E.g. if the caret moves
        /// from the COMMENTBLOCK to a CODEBLOCK token, the contextswitch is raised.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TextHighlighterSwitchContext(object sender, ContextSwitchEventArgs e)
        {
            switch (e.NewContext.Token.Type)
            {
                case Highlighter.TokenType.DOTNET_COMMENTBLOCK:
                case Highlighter.TokenType.DOTNET_COMMENTLINE:
                case Highlighter.TokenType.DOTNET_STRING:
                case Highlighter.TokenType.GRAMMARSTRING:
                case Highlighter.TokenType.DIRECTIVESTRING:
                case Highlighter.TokenType.GRAMMARCOMMENTBLOCK:
                case Highlighter.TokenType.GRAMMARCOMMENTLINE:
                    this._codecomplete.Enabled = false; // disable autocompletion if user is editing in any of these blocks
                    this._directivecomplete.Enabled = false;
                    break;
                default:
                    switch (e.NewContext.Parent.Token.Type)
                    {
                        case Highlighter.TokenType.GrammarBlock:
                            this._directivecomplete.Enabled = false;
                            this._codecomplete.Enabled = true; //allow autocompletion
                            break;
                        case Highlighter.TokenType.DirectiveBlock:
                            this._codecomplete.Enabled = false;
                            this._directivecomplete.Enabled = true; //allow directives autocompletion
                            break;
                        default:
                            this._codecomplete.Enabled = false;
                            this._directivecomplete.Enabled = false;
                            break;
                    }
                    break;
            }
        }

        #endregion Control events

        #region Form events
        private void AboutTinyParserGeneratorToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.AboutTinyPg();
        }

        private void CodeblocksToolStripMenuItemClick(object sender, EventArgs e)
        {
            NotepadViewFile(AppDomain.CurrentDomain.BaseDirectory + @"Examples\simple expression2.tpg");
        }

        private void ExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }

        private void ExpressionEvaluatorToolStripMenuItem1Click(object sender, EventArgs e)
        {
            NotepadViewFile(AppDomain.CurrentDomain.BaseDirectory + @"Examples\simple expression1.tpg");
        }

        private void ExpressionEvaluatorToolStripMenuItemClick(object sender, EventArgs e)
        {
            this._inputFloaty.Show();
            this.textInput.Focus();
        }

        void HeaderEvaluatorCloseClick(object sender, EventArgs e)
        {
            this._inputFloaty.Hide();
        }

        void HeaderOutputCloseClick(object sender, EventArgs e)
        {
            this._outputFloaty.Hide();
        }

        void MainFormDisposed(object sender, EventArgs e)
        {
            // unregister event handler.
            this._checker.UpdateSyntax -= this._syntaxUpdateChecker; // listen for events

            this._checker.Dispose();
            this._marker.Dispose();
        }
        private void MenuToolsGenerateClick(object sender, EventArgs e)
        {

            this._outputFloaty.Show();
            this.tabOutput.SelectedIndex = 0;

            this.CompileGrammar();

            if (this._compiler != null && this._compiler.Errors.Count == 0)
            {
                // save the grammar when compilation was successful
                this.SaveGrammar(this._grammarFile);
            }

        }

        private void NewToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (this._isDirty) this.SaveGrammarAs();

            this.NewGrammar();
        }

        private void OpenToolStripMenuItemClick(object sender, EventArgs e)
        {
            var newgrammarfile = this.OpenGrammar();
            if (newgrammarfile == null) return;

            if (this._isDirty && this._grammarFile != null)
            {
                var r = MessageBox.Show(this, "You will lose current changes, continue?", "Lose changes", MessageBoxButtons.OKCancel);
                if (r == DialogResult.Cancel) return;
            }

            this._grammarFile = newgrammarfile;
            this.LoadGrammarFile();
            this.SaveConfig();
        }

        private void OutputToolStripMenuItemClick(object sender, EventArgs e)
        {
            this._outputFloaty.Show();
            this.tabOutput.SelectedIndex = 0;
        }

        private void ParseToolStripMenuItemClick(object sender, EventArgs e)
        {
            this._inputFloaty.Show();
            this._outputFloaty.Show();
            if (this.tabOutput.SelectedIndex != 0 && this.tabOutput.SelectedIndex != 1) this.tabOutput.SelectedIndex = 0;

            this.EvaluateExpression();
        }


        private void ParsetreeToolStripMenuItemClick(object sender, EventArgs e)
        {
            this._outputFloaty.Show();
            this.tabOutput.SelectedIndex = 1;
        }

        private void RegexToolToolStripMenuItemClick(object sender, EventArgs e)
        {
            this._outputFloaty.Show();
            this.tabOutput.SelectedIndex = 2;
        }

        private void SaveAsToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.SaveGrammarAs();
            this.SaveConfig();
        }

        private void SaveToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this._grammarFile))
            {
                this.SaveGrammarAs();
            }
            else
            {
                this.SaveGrammar(this._grammarFile);
            }

            this.SaveConfig();
        }

        private void TabOutputSelected(object sender, TabControlEventArgs e)
        {
            this.headerOutput.Text = e.TabPage.Text;
        }

        void TextChangedTimerTick(object sender, EventArgs e)
        {
            this._textChangedTimer.Stop();

            this.textEditor.Invalidate();
            this._checker.Check(this.textEditor.Text);
        }

        private void TextEditorEnter(object sender, EventArgs e)
        {
            this.SetStatusbar();
        }

        private void TextEditorLeave(object sender, EventArgs e)
        {
            this.SetStatusbar();
        }

        private void TextEditorSelectionChanged(object sender, EventArgs e)
        {
            this.SetStatusbar();
        }

        private void TextEditorTextChanged(object sender, EventArgs e)
        {
            if (this._textHighlighter.IsHighlighting)
                return;

            this._marker.Clear();
            this._textChangedTimer.Stop();
            this._textChangedTimer.Interval = 3000;
            this._textChangedTimer.Start();

            if (!this._isDirty)
            {
                this._isDirty = true;
                this.SetFormCaption();
            }

        }
        private void TextInputEnter(object sender, EventArgs e)
        {
            this.SetStatusbar();
        }

        private void TextInputLeave(object sender, EventArgs e)
        {
            this.SetStatusbar();
        }

        private void TextInputSelectionChanged(object sender, EventArgs e)
        {
            this.SetStatusbar();
        }

        private void TextOutputLinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                if (e.LinkText == "www.codeproject.com")
                {
                    Process.Start("http://www.codeproject.com/script/Articles/MemberArticles.aspx?amid=2192187");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void TheTinyPgGrammarHighlighterV12ToolStripMenuItemClick(object sender, EventArgs e)
        {
            NotepadViewFile(AppDomain.CurrentDomain.BaseDirectory + @"Examples\GrammarHighlighter.tpg");
        }

        private void TheTinyPgGrammarToolStripMenuItemClick(object sender, EventArgs e)
        {
            NotepadViewFile(AppDomain.CurrentDomain.BaseDirectory + @"Examples\BNFGrammar v1.1.tpg");
        }

        private void TheTinyPgGrammarV10ToolStripMenuItemClick(object sender, EventArgs e)
        {
            NotepadViewFile(AppDomain.CurrentDomain.BaseDirectory + @"Examples\BNFGrammar v1.0.tpg");
        }

        private void TvParsetreeAfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null)
                return;

            var ipn = e.Node.Tag as IParseNode;
            if (ipn == null) return;

            this.textInput.Select(ipn.IToken.StartPos, ipn.IToken.EndPos - ipn.IToken.StartPos);
            this.textInput.ScrollToCaret();
        }
        private void ViewParserToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.ViewFile("Parser");
        }

        private void ViewParseTreeCodeToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.ViewFile("ParseTree");
        }

        private void ViewScannerToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.ViewFile("Scanner");
        }
        #endregion Form events

        #region Processing functions

        private static void NotepadViewFile(string filename)
        {
            try
            {
                Process.Start("Notepad.exe", filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void AboutTinyPg()
        {
            var about = new StringBuilder();

            //// http://www.codeproject.com/script/Articles/MemberArticles.aspx?amid=2192187

            about.AppendLine(AssemblyInfo.ProductName + " v" + Application.ProductVersion);
            about.AppendLine(AssemblyInfo.CopyRightsDetail);
            about.AppendLine();
            about.AppendLine("For more information about the author");
            about.AppendLine("or TinyPG visit www.codeproject.com");

            this._outputFloaty.Show();
            this.tabOutput.SelectedIndex = 0;
            this.textOutput.Text = about.ToString();

        }

        private void CompileGrammar()
        {

            if (string.IsNullOrEmpty(this._grammarFile)) this.SaveGrammarAs();

            if (string.IsNullOrEmpty(this._grammarFile))
                return;

            this._compiler = new Compiler.Compiler();
            var output = new StringBuilder();

            // clear tree
            this.tvParsetree.Nodes.Clear();

            var prog = new Program(this.ManageParseError, output);
            var starttimer = DateTime.Now;
            this._grammar = prog.ParseGrammar(this.textEditor.Text, this._grammarFile);

            if (this._grammar != null)
            {
                this.SetHighlighterLanguage(this._grammar.Directives["TinyPG"]["Language"]);

                if (prog.BuildCode(this._grammar, this._compiler))
                {
                    var span = DateTime.Now.Subtract(starttimer);
                    output.AppendLine("Compilation successful in " + span.TotalMilliseconds + "ms.");
                }
            }

            this.textOutput.Text = output.ToString();
            this.textOutput.Select(this.textOutput.Text.Length, 0);
            this.textOutput.ScrollToCaret();

        }

        private void EvaluateExpression()
        {
            this.textOutput.Text = "Parsing expression...\r\n";
            try
            {

                if (this._isDirty || this._compiler == null || !this._compiler.IsCompiled) this.CompileGrammar();

                if (string.IsNullOrEmpty(this._grammarFile))
                    return;

                // save the grammar when compilation was successful
                if (this._compiler != null && this._compiler.Errors.Count == 0) this.SaveGrammar(this._grammarFile);

                var result = new CompilerResult();
                if (this._compiler.IsCompiled)
                {
                    result = this._compiler.Run(this.textInput.Text, this.textInput);

                    //textOutput.Text = result.ParseTree.PrintTree();
                    this.textOutput.Text += result.Output;
                    ParseTreeViewer.Populate(this.tvParsetree, result.ParseTree);
                }
            }
            catch (Exception exc)
            {
                this.textOutput.Text += "An exception occured compiling the assembly: \r\n" + exc.Message + "\r\n" + exc.StackTrace;
            }

        }

        private void LoadConfig()
        {
            try
            {
                var configfile = AppDomain.CurrentDomain.BaseDirectory + "TinyPG.config";

                if (!File.Exists(configfile))
                    return;

                var doc = new XmlDocument();
                doc.Load(configfile);
                this.openFileDialog.InitialDirectory = doc["AppSettings"]["OpenFilePath"].Attributes[0].Value;
                this.saveFileDialog.InitialDirectory = doc["AppSettings"]["SaveFilePath"].Attributes[0].Value;
                this._grammarFile = doc["AppSettings"]["GrammarFile"].Attributes[0].Value;

                if (string.IsNullOrEmpty(this._grammarFile)) this.NewGrammar();
                else this.LoadGrammarFile();
            }
            catch (Exception)
            {
            }
        }

        private void LoadGrammarFile()
        {
            if (this._grammarFile == null) return;
            if (!File.Exists(this._grammarFile))
            {
                this._grammarFile = null; // file does not exist anymore
                return;
            }

            var folder = new FileInfo(this._grammarFile).DirectoryName;
            Directory.SetCurrentDirectory(folder);

            this.textEditor.Text = File.ReadAllText(this._grammarFile);
            this.textEditor.ClearUndo();
            this.CompileGrammar();
            this.textOutput.Text = "";
            this.textEditor.Focus();
            this.SetStatusbar();
            this._textHighlighter.ClearUndo();
            this._isDirty = false;
            this.SetFormCaption();
            this.textEditor.Select(0, 0);
            this._checker.Check(this.textEditor.Text);


        }

        private void ManageParseError(ParseTree tree, StringBuilder output)
        {
            foreach (var error in tree.Errors)
                output.AppendLine(string.Format("({0},{1}): {2}", error.Line, error.Column, error.Message));

            output.AppendLine("Semantic errors in grammar found.");
            this.textEditor.Select(tree.Errors[0].Position, tree.Errors[0].Length > 0 ? tree.Errors[0].Length : 1);
        }

        private void NewGrammar()
        {
            this._grammarFile = null;
            this._isDirty = false;

            var text = "//" + AssemblyInfo.ProductName + " v" + Application.ProductVersion + "\r\n";
            text += "//" + AssemblyInfo.CopyRightsDetail + "\r\n\r\n";
            this.textEditor.Text = text;
            this.textEditor.ClearUndo();

            this.textOutput.Text = AssemblyInfo.ProductName + " v" + Application.ProductVersion + "\r\n";
            this.textOutput.Text += AssemblyInfo.CopyRightsDetail + "\r\n\r\n";

            this.SetFormCaption();
            this.SaveConfig();

            this.textEditor.Select(this.textEditor.Text.Length, 0);

            this._isDirty = false;
            this._textHighlighter.ClearUndo();
            this.SetFormCaption();
            this.SetStatusbar();

        }

        private string OpenGrammar()
        {
            var r = this.openFileDialog.ShowDialog(this);

            return r == DialogResult.OK 
                ? this.openFileDialog.FileName 
                : null;
        }

        private void SaveConfig()
        {
            var configfile = AppDomain.CurrentDomain.BaseDirectory + "TinyPG.config";
            XmlAttribute attr;
            var doc = new XmlDocument();
            XmlNode settings = doc.CreateElement("AppSettings", "TinyPG");
            doc.AppendChild(settings);

            XmlNode node = doc.CreateElement("OpenFilePath", "TinyPG");
            settings.AppendChild(node);
            node = doc.CreateElement("SaveFilePath", "TinyPG");
            settings.AppendChild(node);
            node = doc.CreateElement("GrammarFile", "TinyPG");
            settings.AppendChild(node);

            attr = doc.CreateAttribute("Value");
            settings["OpenFilePath"].Attributes.Append(attr);
            if (File.Exists(this.openFileDialog.FileName))
                attr.Value = new FileInfo(this.openFileDialog.FileName).Directory.FullName;

            attr = doc.CreateAttribute("Value");
            settings["SaveFilePath"].Attributes.Append(attr);
            if (File.Exists(this.saveFileDialog.FileName))
                attr.Value = new FileInfo(this.saveFileDialog.FileName).Directory.FullName;

            attr = doc.CreateAttribute("Value");
            attr.Value = this._grammarFile;
            settings["GrammarFile"].Attributes.Append(attr);

            doc.Save(configfile);
        }

        private void SaveGrammar(string filename)
        {
            if (String.IsNullOrEmpty(filename)) return;

            this._grammarFile = filename;

            var folder = new FileInfo(this._grammarFile).DirectoryName;
            Directory.SetCurrentDirectory(folder);

            var text = this.textEditor.Text.Replace("\n", "\r\n");
            File.WriteAllText(filename, text);
            this._isDirty = false;
            this.SetFormCaption();
        }

        private void SaveGrammarAs()
        {
            var r = this.saveFileDialog.ShowDialog(this);
            if (r == DialogResult.OK)
            {
                this.SaveGrammar(this.saveFileDialog.FileName);
            }

        }

        private void SetFormCaption()
        {
            this.Text = "@TinyPG - a Tiny Parser Generator .Net";
            if ((this._grammarFile == null) || (!File.Exists(this._grammarFile)))
            {
                if (this._isDirty) this.Text += " *";
                return;
            }

            var name = new FileInfo(this._grammarFile).Name;
            this.Text += " [" + name + "]";
            if (this._isDirty) this.Text += " *";
        }

        /// <summary>
        /// this is where some of the magic happens
        /// to highlight specific C# code or VB code, the language specific keywords are swapped
        /// that is, the DOTNET regexps are overwritten by either the c# or VB regexps
        /// </summary>
        /// <param name="language"></param>
        private void SetHighlighterLanguage(string language)
        {
            lock (TextHighlighter.treelock)
            {
                switch (CodeGeneratorFactory.GetSupportedLanguage(language))
                {
                    case SupportedLanguage.VBNet:
                        this._highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_STRING] = this._highlighterScanner.Patterns[Highlighter.TokenType.VB_STRING];
                        this._highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_SYMBOL] = this._highlighterScanner.Patterns[Highlighter.TokenType.VB_SYMBOL];
                        this._highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_COMMENTBLOCK] = this._highlighterScanner.Patterns[Highlighter.TokenType.VB_COMMENTBLOCK];
                        this._highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_COMMENTLINE] = this._highlighterScanner.Patterns[Highlighter.TokenType.VB_COMMENTLINE];
                        this._highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_KEYWORD] = this._highlighterScanner.Patterns[Highlighter.TokenType.VB_KEYWORD];
                        this._highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_NONKEYWORD] = this._highlighterScanner.Patterns[Highlighter.TokenType.VB_NONKEYWORD];
                        break;
                    default:
                        this._highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_STRING] = this._highlighterScanner.Patterns[Highlighter.TokenType.CS_STRING];
                        this._highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_SYMBOL] = this._highlighterScanner.Patterns[Highlighter.TokenType.CS_SYMBOL];
                        this._highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_COMMENTBLOCK] = this._highlighterScanner.Patterns[Highlighter.TokenType.CS_COMMENTBLOCK];
                        this._highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_COMMENTLINE] = this._highlighterScanner.Patterns[Highlighter.TokenType.CS_COMMENTLINE];
                        this._highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_KEYWORD] = this._highlighterScanner.Patterns[Highlighter.TokenType.CS_KEYWORD];
                        this._highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_NONKEYWORD] = this._highlighterScanner.Patterns[Highlighter.TokenType.CS_NONKEYWORD];
                        break;
                }
                this._textHighlighter.HighlightText();
            }

        }

        private void SetStatusbar()
        {
            if (this.textEditor.Focused)
            {
                var pos = this.textEditor.SelectionStart;
                this.statusPos.Text = pos.ToString(CultureInfo.InvariantCulture);
                this.statusCol.Text = (pos - this.textEditor.GetFirstCharIndexOfCurrentLine() + 1).ToString(CultureInfo.InvariantCulture);
                this.statusLine.Text = (this.textEditor.GetLineFromCharIndex(pos) + 1).ToString(CultureInfo.InvariantCulture);

            }
            else if (this.textInput.Focused)
            {
                var pos = this.textInput.SelectionStart;
                this.statusPos.Text = pos.ToString(CultureInfo.InvariantCulture);
                this.statusCol.Text = (pos - this.textInput.GetFirstCharIndexOfCurrentLine() + 1).ToString(CultureInfo.InvariantCulture);
                this.statusLine.Text = (this.textInput.GetLineFromCharIndex(pos) + 1).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                this.statusPos.Text = "-";
                this.statusCol.Text = "-";
                this.statusLine.Text = "-";
            }
        }

        private void ViewFile(string filetype)
        {
            try
            {
                if (this._isDirty || this._compiler == null || !this._compiler.IsCompiled) this.CompileGrammar();

                if (this._grammar == null)
                    return;

                var generator = CodeGeneratorFactory.CreateGenerator(filetype, this._grammar.Directives["TinyPG"]["Language"]);
                var folder = this._grammar.GetOutputPath() + generator.FileName;
                Process.Start(folder);
            }
            catch (Exception)
            {
            }
        }
        #endregion

        void CopyAction(object sender, EventArgs e)
        {
            this.textEditor.Copy();
        }

        private void CopyToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.CopyAction(sender, e);
        }

        void CutAction(object sender, EventArgs e)
        {
            this.textEditor.Cut();
        }

        private void CutToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.CutAction(sender, e);
        }

        void PasteAction(object sender, EventArgs e)
        {
            this.textEditor.Paste();
        }

        private void PasteToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.PasteAction(sender, e);
        }

        private void TextEditorMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && this.textEditor.SelectedText != "")
            {   //click event
                //MessageBox.Show("you got it!");
                var contextMenu = new ContextMenu();
                var menuItem = new MenuItem("Cut");
                menuItem.Click += this.CutAction;
                contextMenu.MenuItems.Add(menuItem);
                menuItem = new MenuItem("Copy");
                menuItem.Click += this.CopyAction;
                contextMenu.MenuItems.Add(menuItem);
                menuItem = new MenuItem("Paste");
                menuItem.Click += this.PasteAction;
                contextMenu.MenuItems.Add(menuItem);

                this.textEditor.ContextMenu = contextMenu;
            }
        }

        private void textOutput_LinkClicked(object sender, LinkClickedEventArgs e)
        {

        }

        private void tvParsetree_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void tabOutput_Selected(object sender, TabControlEventArgs e)
        {

        }

        private void numberedRichTextBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
