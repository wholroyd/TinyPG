namespace TinyPG.Compiler
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class Token
    {
        private string file;
        private int line;
        private int column;
        private int startpos;
        private int endpos;
        private string text;
        private object value;

        // contains all prior skipped symbols
        private List<Token> skipped;

        public string File { 
            get { return this.file; } 
            set { this.file = value; }
        }

        public int Line { 
            get { return this.line; } 
            set { this.line = value; }
        }

        public int Column {
            get { return this.column; } 
            set { this.column = value; }
        }

        public int StartPos { 
            get { return this.startpos;} 
            set { this.startpos = value; }
        }

        public int Length { 
            get { return this.endpos - this.startpos;} 
        }

        public int EndPos { 
            get { return this.endpos;} 
            set { this.endpos = value; }
        }

        public string Text { 
            get { return this.text;} 
            set { this.text = value; }
        }

        public List<Token> Skipped { 
            get { return this.skipped;} 
            set { this.skipped = value; }
        }
        public object Value { 
            get { return this.value;} 
            set { this.value = value; }
        }

        [XmlAttribute]
        public TokenType Type;

        public Token()
            : this(0, 0)
        {
        }

        public Token(int start, int end)
        {
            this.Type = TokenType._UNDETERMINED_;
            this.startpos = start;
            this.endpos = end;
            this.Text = ""; // must initialize with empty string, may cause null reference exceptions otherwise
            this.Value = null;
        }

        public void UpdateRange(Token token)
        {
            if (token.StartPos < this.startpos) this.startpos = token.StartPos;
            if (token.EndPos > this.endpos) this.endpos = token.EndPos;
        }

        public override string ToString()
        {
            if (this.Text != null)
                return this.Type + " '" + this.Text + "'";
            return this.Type.ToString();
        }
    }
}