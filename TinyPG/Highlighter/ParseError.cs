namespace TinyPG.Highlighter
{
    using System;

    [Serializable]
    public class ParseError
    {
        private string file;
        private string message;
        private int code;
        private int line;
        private int col;
        private int pos;
        private int length;

        public string File { get { return this.file; } }
        public int Code { get { return this.code; } }
        public int Line { get { return this.line; } }
        public int Column { get { return this.col; } }
        public int Position { get { return this.pos; } }
        public int Length { get { return this.length; } }
        public string Message { get { return this.message; } }

        // just for the sake of serialization
        public ParseError()
        {
        }

        public ParseError(string message, int code, ParseNode node) : this(message, code, node.Token)
        {
        }

        public ParseError(string message, int code, Token token) : this(message, code, token.File, token.Line, token.Column, token.StartPos, token.Length)
        {
        }

        public ParseError(string message, int code, string file = "", int line = 0, int col = 0, int pos = 0, int length = 0)
        {
            this.file = file;
            this.message = message;
            this.code = code;
            this.line = line;
            this.col = col;
            this.pos = pos;
            this.length = length;
        }
    }
}