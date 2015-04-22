namespace TinyPG.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>
    /// this class implements the semantics of the parsetree
    /// it will create a Grammar with production rules
    /// </summary>
    public class GrammarNode : ParseTree
    {
        public override ParseNode CreateNode(Token token, string text)
        {
            var node = new GrammarNode(token, text) { Parent = this };
            return node;
        }

        protected GrammarNode()
        {
        }

        protected GrammarNode(Token token, string text)
        {
            this.Token = token;
            this.text = text;
            this.nodes = new List<ParseNode>();
        }

        /// <summary>
        /// EvalStart will first do a semantic check to see if symbols are declared correctly
        /// then it will also check for attributes and parse the directives
        /// after that it will complete the transformation to the grammar tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="paramlist"></param>
        /// <returns></returns>
        protected override object EvalStart(ParseTree tree, params object[] paramlist)
        {
            bool startFound = false;
            Grammar g = new Grammar();
            foreach (ParseNode n in this.Nodes)
            {
                if (n.Token.Type == TokenType.Directive)
                {
                    this.EvalDirective(tree, g, n);
                }

                if (n.Token.Type == TokenType.ExtProduction)
                {
                    if (n.Nodes[n.Nodes.Count - 1].Nodes[2].Nodes[0].Token.Type == TokenType.STRING)
                    {
                        TerminalSymbol terminal;
                        try
                        {
                            terminal = new TerminalSymbol(n.Nodes[n.Nodes.Count - 1].Nodes[0].Token.Text, n.Nodes[n.Nodes.Count - 1].Nodes[2].Nodes[0].Token.Text);
                            for (int i = 0; i < n.Nodes.Count - 1; i++)
                            {
                                if (n.Nodes[i].Token.Type == TokenType.Attribute) this.EvalAttribute(tree, g, terminal, n.Nodes[i]);
                            }

                        }
                        catch (Exception ex)
                        {
                            tree.Errors.Add(new ParseError("regular expression for '" + n.Nodes[n.Nodes.Count - 1].Nodes[0].Token.Text + "' results in error: " + ex.Message, 0x1020, n.Nodes[0]));
                            continue;
                        }

                        if (terminal.Name == "Start")
                            tree.Errors.Add(new ParseError("'Start' symbol cannot be a regular expression.", 0x1021, n.Nodes[0]));

                        if (g.Symbols.Find(terminal.Name) == null)
                            g.Symbols.Add(terminal);
                        else
                            tree.Errors.Add(new ParseError("Terminal already declared: " + terminal.Name, 0x1022, n.Nodes[0]));

                    }
                    else
                    {
                        var nts = new NonTerminalSymbol(n.Nodes[n.Nodes.Count - 1].Nodes[0].Token.Text);
                        if (g.Symbols.Find(nts.Name) == null)
                            g.Symbols.Add(nts);
                        else
                            tree.Errors.Add(new ParseError("Non terminal already declared: " + nts.Name, 0x1023, n.Nodes[0]));

                        for (int i = 0; i < n.Nodes.Count - 1; i++)
                        {
                            if (n.Nodes[i].Token.Type == TokenType.Attribute) this.EvalAttribute(tree, g, nts, n.Nodes[i]);
                        }

                        if (nts.Name == "Start")
                            startFound = true;
                    }
                }
            }

            if (!startFound)
            {
                tree.Errors.Add(new ParseError("The grammar requires 'Start' to be a production rule.", 0x0024));
                return g;
            }

            foreach (ParseNode n in this.Nodes)
            {
                if (n.Token.Type == TokenType.ExtProduction)
                {
                    n.Eval(tree, g);
                }
            }

            return g;
        }


        protected override object EvalDirective(ParseTree tree, params object[] paramlist)
        {
            Grammar g = (Grammar)paramlist[0];
            GrammarNode node = (GrammarNode)paramlist[1];
            string name = node.Nodes[1].Token.Text;

            switch (name)
            {
                case "TinyPG":
                case "Parser":
                case "Scanner":
                case "ParseTree":
                case "TextHighlighter":
                    if (g.Directives.Find(name) != null)
                    {
                        tree.Errors.Add(new ParseError("Directive '" + name + "' is already defined", 0x1030, node.Nodes[1]));
                        return null; ;
                    }
                    break;
                default:
                    tree.Errors.Add(new ParseError("Directive '" + name + "' is not supported", 0x1031, node.Nodes[1]));
                    break;
            }

            Directive directive = new Directive(name);
            g.Directives.Add(directive);

            foreach (ParseNode n in node.Nodes)
            {
                if (n.Token.Type == TokenType.NameValue) this.EvalNameValue(tree, g, directive, n);
            }

            return null;
        }

        protected override object EvalNameValue(ParseTree tree, params object[] paramlist)
        {
            Grammar grammer = (Grammar)paramlist[0];
            Directive directive = (Directive)paramlist[1];
            GrammarNode node = (GrammarNode)paramlist[2];

            string key = node.Nodes[0].Token.Text;
            string value = node.Nodes[2].Token.Text.Substring(1, node.Nodes[2].Token.Text.Length - 2);
            if (value.StartsWith("\""))
                value = value.Substring(1);

            directive[key] = value;

            var names = new List<string>(new[] { "Namespace", "OutputPath", "TemplatePath" });
            var languages = new List<string>(new[] { "c#", "cs", "csharp", "vb", "vb.net", "vbnet", "visualbasic" });
            switch (directive.Name)
            {
                case "TinyPG":
                    names.Add("Namespace");
                    names.Add("OutputPath");
                    names.Add("TemplatePath");
                    names.Add("Language");

                    if (key == "TemplatePath")
                        if (grammer.GetTemplatePath() == null)
                            tree.Errors.Add(new ParseError("Template path '" + value + "' does not exist", 0x1060, node.Nodes[2]));

                    if (key == "OutputPath")
                        if (grammer.GetOutputPath() == null)
                            tree.Errors.Add(new ParseError("Output path '" + value + "' does not exist", 0x1061, node.Nodes[2]));

                    if (key == "Language")
                        if (!languages.Contains(value.ToLower(CultureInfo.InvariantCulture)))
                            tree.Errors.Add(new ParseError("Language '" + value + "' is not supported", 0x1062, node.Nodes[2]));
                    break;
                case "Parser":
                case "Scanner":
                case "ParseTree":
                case "TextHighlighter":
                    names.Add("Generate");
                    names.Add("FileName");
                    break;
                default:
                    return null;
            }

            if (!names.Contains(key))
                tree.Errors.Add(new ParseError("Directive attribute '" + key + "' is not supported", 0x1034, node.Nodes[0]));

            return null;
        }

        protected override object EvalExtProduction(ParseTree tree, params object[] paramlist)
        {
            return this.Nodes[this.Nodes.Count - 1].Eval(tree, paramlist);
        }

        protected override object EvalAttribute(ParseTree tree, params object[] paramlist)
        {
            Grammar grammar = (Grammar)paramlist[0];
            Symbol symbol = (Symbol)paramlist[1];
            GrammarNode node = (GrammarNode)paramlist[2];

            if (symbol.Attributes.ContainsKey(node.Nodes[1].Token.Text))
            {
                tree.Errors.Add(new ParseError("Attribute already defined for this symbol: " + node.Nodes[1].Token.Text, 0x1039, node.Nodes[1]));
                return null;
            }

            symbol.Attributes.Add(node.Nodes[1].Token.Text, (object[])this.EvalParams(tree, node));
            switch (node.Nodes[1].Token.Text)
            {
                case "Skip":
                    if (symbol is TerminalSymbol)
                        grammar.SkipSymbols.Add(symbol);
                    else
                        tree.Errors.Add(new ParseError("Attribute for non-terminal rule not allowed: " + node.Nodes[1].Token.Text, 0x1035, node));
                    break;
                case "Color":
                    if (symbol is NonTerminalSymbol)
                        tree.Errors.Add(new ParseError("Attribute for non-terminal rule not allowed: " + node.Nodes[1].Token.Text, 0x1035, node));

                    if (symbol.Attributes["Color"].Length != 1 && symbol.Attributes["Color"].Length != 3)
                        tree.Errors.Add(new ParseError("Attribute " + node.Nodes[1].Token.Text + " has too many or missing parameters", 0x103A, node.Nodes[1]));

                    for (int i = 0; i < symbol.Attributes["Color"].Length; i++)
                    {
                        if (symbol.Attributes["Color"][i] is string)
                        {
                            tree.Errors.Add(new ParseError("Parameter " + node.Nodes[3].Nodes[i * 2].Nodes[0].Token.Text + " is of incorrect type", 0x103A, node.Nodes[3].Nodes[i * 2].Nodes[0]));
                            break;
                        }
                    }
                    break;
                case "IgnoreCase":
                    if (!(symbol is TerminalSymbol))
                        tree.Errors.Add(new ParseError("Attribute for non-terminal rule not allowed: " + node.Nodes[1].Token.Text, 0x1035, node));
                    break;
                case "FileAndLine":
                    if (symbol is TerminalSymbol)
                    {
                        grammar.SkipSymbols.Add(symbol);
                        grammar.FileAndLine = symbol;
                    }
                    else
                        tree.Errors.Add(new ParseError("Attribute for non-terminal rule not allowed: " + node.Nodes[1].Token.Text, 0x1035, node));
                    break;
                default:
                    tree.Errors.Add(new ParseError("Attribute not supported: " + node.Nodes[1].Token.Text, 0x1036, node.Nodes[1]));
                    break;
            }

            return symbol;
        }

        protected override object EvalParams(ParseTree tree, params object[] paramlist)
        {
            GrammarNode node = (GrammarNode)paramlist[0];
            if (node.Nodes.Count < 4) return null;
            if (node.Nodes[3].Token.Type != TokenType.Params) return null;

            GrammarNode parms = (GrammarNode)node.Nodes[3];
            List<object> objects = new List<object>();
            for (int i = 0; i < parms.Nodes.Count; i += 2)
            {
                objects.Add(this.EvalParam(tree, parms.Nodes[i]));
            }

            return objects.ToArray();
        }

        protected override object EvalParam(ParseTree tree, params object[] paramlist)
        {
            GrammarNode node = (GrammarNode)paramlist[0];
            try
            {
                switch (node.Nodes[0].Token.Type)
                {
                    case TokenType.STRING:
                        return node.Nodes[0].Token.Text;
                    case TokenType.INTEGER:
                        return Convert.ToInt32(node.Nodes[0].Token.Text);
                    case TokenType.HEX:
                        return long.Parse(node.Nodes[0].Token.Text.Substring(2), NumberStyles.HexNumber);
                    default:
                        tree.Errors.Add(new ParseError("Attribute parameter is not a valid value: " + node.Token.Text, 0x1037, node));
                        break;
                }
            }
            catch (Exception)
            {
                tree.Errors.Add(new ParseError("Attribute parameter is not a valid value: " + node.Token.Text, 0x1038, node));
            }

            return null;
        }

        protected override object EvalProduction(ParseTree tree, params object[] paramlist)
        {
            Grammar g = (Grammar)paramlist[0];
            if (this.Nodes[2].Nodes[0].Token.Type == TokenType.STRING)
            {
                TerminalSymbol term = g.Symbols.Find(this.Nodes[0].Token.Text) as TerminalSymbol;
                if (term == null)
                    tree.Errors.Add(new ParseError("Symbol '" + this.Nodes[0].Token.Text + "' is not declared. ", 0x1040, this.Nodes[0]));
            }
            else
            {
                NonTerminalSymbol nts = g.Symbols.Find(this.Nodes[0].Token.Text) as NonTerminalSymbol;
                if (nts == null)
                    tree.Errors.Add(new ParseError("Symbol '" + this.Nodes[0].Token.Text + "' is not declared. ", 0x1041, this.Nodes[0]));
                Rule r = (Rule)this.Nodes[2].Eval(tree, g, nts);
                if (nts != null)
                    nts.Rules.Add(r);

                if (this.Nodes[3].Token.Type == TokenType.CODEBLOCK)
                {
                    string codeblock = this.Nodes[3].Token.Text;
                    nts.CodeBlock = codeblock;
                    this.ValidateCodeBlock(tree, nts, this.Nodes[3]);

                    // beautify the codeblock format
                    codeblock = codeblock.Substring(1, codeblock.Length - 3).Trim();
                    nts.CodeBlock = codeblock;
                }
            }

            return g;
        }

        protected override object EvalRule(ParseTree tree, params object[] paramlist)
        {
            return this.Nodes[0].Eval(tree, paramlist);
        }

        protected override object EvalSubrule(ParseTree tree, params object[] paramlist)
        {
            if (this.Nodes.Count == 1) // single symbol
                return this.Nodes[0].Eval(tree, paramlist);

            Rule choiceRule = new Rule(RuleType.Choice);
            // i+=2 to skip to the | symbols
            for (int i = 0; i < this.Nodes.Count; i += 2)
            {
                Rule rule = (Rule)this.Nodes[i].Eval(tree, paramlist);
                choiceRule.Rules.Add(rule);
            }

            return choiceRule;
        }

        protected override object EvalConcatRule(ParseTree tree, params object[] paramlist)
        {
            if (this.Nodes.Count == 1) // single symbol
                return this.Nodes[0].Eval(tree, paramlist);

            Rule concatRule = new Rule(RuleType.Concat);
            for (int i = 0; i < this.Nodes.Count; i++)
            {
                Rule rule = (Rule)this.Nodes[i].Eval(tree, paramlist);
                concatRule.Rules.Add(rule);
            }

            return concatRule;
        }


        protected override object EvalSymbol(ParseTree tree, params object[] paramlist)
        {
            ParseNode last = this.Nodes[this.Nodes.Count - 1];
            if (last.Token.Type == TokenType.UNARYOPER)
            {
                Rule unaryRule;
                string oper = last.Token.Text.Trim();
                if (oper == "*") unaryRule = new Rule(RuleType.ZeroOrMore);
                else if (oper == "+") unaryRule = new Rule(RuleType.OneOrMore);
                else if (oper == "?") unaryRule = new Rule(RuleType.Option);
                else throw new NotImplementedException("unknown unary operator");

                if (this.Nodes[0].Token.Type == TokenType.BRACKETOPEN)
                {
                    Rule rule = (Rule)this.Nodes[1].Eval(tree, paramlist);
                    unaryRule.Rules.Add(rule);
                }
                else
                {
                    Grammar g = (Grammar)paramlist[0];
                    if (this.Nodes[0].Token.Type == TokenType.IDENTIFIER)
                    {

                        Symbol s = g.Symbols.Find(this.Nodes[0].Token.Text);
                        if (s == null)
                        {
                            tree.Errors.Add(new ParseError("Symbol '" + this.Nodes[0].Token.Text + "' is not declared. ", 0x1042, this.Nodes[0]));
                        }

                        Rule r = new Rule(s);
                        unaryRule.Rules.Add(r);
                    }
                }
                return unaryRule;
            }

            if (this.Nodes[0].Token.Type == TokenType.BRACKETOPEN)
            {
                // create subrule syntax tree
                return this.Nodes[1].Eval(tree, paramlist);
            }

            Grammar grammar = (Grammar)paramlist[0];
            Symbol symbol = grammar.Symbols.Find(this.Nodes[0].Token.Text);
            if (symbol == null)
            {
                tree.Errors.Add(new ParseError("Symbol '" + this.Nodes[0].Token.Text + "' is not declared.", 0x1043, this.Nodes[0]));
            }
            return new Rule(symbol);
        }

        /// <summary>
        /// validates whether $ variables are corresponding to valid symbols
        /// errors are added to the tree Error object.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="nts">non terminal and its production rule</param>
        /// <param name="node"></param>
        /// <returns>a formated codeblock</returns>
        private void ValidateCodeBlock(ParseTree tree, NonTerminalSymbol nts, ParseNode node)
        {
            if (nts == null) return;
            string codeblock = nts.CodeBlock;

            Regex var = new Regex(@"\$(?<var>[a-zA-Z_0-9]+)(\[(?<index>[^]]+)\])?", RegexOptions.Compiled);
            Symbols symbols = nts.DetermineProductionSymbols();
            MatchCollection matches = var.Matches(codeblock);

            foreach (Match match in matches)
            {
                Symbol s = symbols.Find(match.Groups["var"].Value);
                if (s == null)
                {
                    tree.Errors.Add(new ParseError("Variable $" + match.Groups["var"].Value + " cannot be matched.", 0x1016, node.Token.File, node.Token.StartPos + match.Groups["var"].Index, node.Token.StartPos + match.Groups["var"].Index, match.Groups["var"].Length));
                    break; // error situation
                }
            }
        }
    }
}