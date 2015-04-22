namespace TinyPG.Highlighter
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [Serializable]
    [XmlInclude(typeof(ParseTree))]
    public partial class ParseNode
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
                case TokenType.CommentBlock:
                    Value = this.EvalCommentBlock(tree, paramlist);
                    break;
                case TokenType.DirectiveBlock:
                    Value = this.EvalDirectiveBlock(tree, paramlist);
                    break;
                case TokenType.GrammarBlock:
                    Value = this.EvalGrammarBlock(tree, paramlist);
                    break;
                case TokenType.AttributeBlock:
                    Value = this.EvalAttributeBlock(tree, paramlist);
                    break;
                case TokenType.CodeBlock:
                    Value = this.EvalCodeBlock(tree, paramlist);
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

        protected virtual object EvalCommentBlock(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalDirectiveBlock(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalGrammarBlock(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalAttributeBlock(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }

        protected virtual object EvalCodeBlock(ParseTree tree, params object[] paramlist)
        {
            foreach (var node in this.Nodes)
                node.Eval(tree, paramlist);
            return null;
        }


    }
}