namespace TinyPG.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [Serializable]
    [XmlInclude(typeof(ParseTree))]
    public class ParseNode
    {
        protected string text;
        protected List<ParseNode> nodes;
        
        public List<ParseNode> Nodes { get {return this.nodes;} }
        
        [XmlIgnore] // avoid circular references when serializing
        public ParseNode Parent;
        public Token Token; // the token/rule

        [XmlIgnore] // skip redundant text (is part of Token)
        public string Text { // text to display in parse tree 
            get { return this.text;} 
            set { this.text = value; }
        } 

        public virtual ParseNode CreateNode(Token token, string text)
        {
            ParseNode node = new ParseNode(token, text);
            node.Parent = this;
            return node;
        }

        protected ParseNode(Token token, string text)
        {
            this.Token = token;
            this.text = text;
            this.nodes = new List<ParseNode>();
        }

        protected object GetValue(ParseTree tree, TokenType type, int index)
        {
            return this.GetValue(tree, type, ref index);
        }

        protected object GetValue(ParseTree tree, TokenType type, ref int index)
        {
            object o = null;
            if (index < 0) return o;

            // left to right
            foreach (ParseNode node in this.nodes)
            {
                if (node.Token.Type == type)
                {
                    index--;
                    if (index < 0)
                    {
                        o = node.Eval(tree);
                        break;
                    }
                }
            }
            return o;
        }

        /// <summary>
        /// this implements the evaluation functionality, cannot be used directly
        /// </summary>
        /// <param name="tree">the parsetree itself</param>
        /// <param name="paramlist">optional input parameters</param>
        /// <returns>a partial result of the evaluation</returns>
        internal object Eval(ParseTree tree, params object[] paramlist)
        {
            object Value = null;

            switch (this.Token.Type)
            {
                case TokenType.Start:
                    Value = this.EvalStart(tree, paramlist);
                    break;
                case TokenType.Directive:
                    Value = this.EvalDirective(tree, paramlist);
                    break;
                case TokenType.NameValue:
                    Value = this.EvalNameValue(tree, paramlist);
                    break;
                case TokenType.ExtProduction:
                    Value = this.EvalExtProduction(tree, paramlist);
                    break;
                case TokenType.Attribute:
                    Value = this.EvalAttribute(tree, paramlist);
                    break;
                case TokenType.Params:
                    Value = this.EvalParams(tree, paramlist);
                    break;
                case TokenType.Param:
                    Value = this.EvalParam(tree, paramlist);
                    break;
                case TokenType.Production:
                    Value = this.EvalProduction(tree, paramlist);
                    break;
                case TokenType.Rule:
                    Value = this.EvalRule(tree, paramlist);
                    break;
                case TokenType.Subrule:
                    Value = this.EvalSubrule(tree, paramlist);
                    break;
                case TokenType.ConcatRule:
                    Value = this.EvalConcatRule(tree, paramlist);
                    break;
                case TokenType.Symbol:
                    Value = this.EvalSymbol(tree, paramlist);
                    break;

                default:
                    Value = this.Token.Text;
                    break;
            }
            return Value;
        }

        protected virtual object EvalStart(ParseTree tree, params object[] paramlist)
        {
            return "Could not interpret input; no semantics implemented.";
        }

        protected virtual object EvalDirective(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalNameValue(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalExtProduction(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalAttribute(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalParams(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalParam(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalProduction(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalRule(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalSubrule(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalConcatRule(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalSymbol(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }


    }
}