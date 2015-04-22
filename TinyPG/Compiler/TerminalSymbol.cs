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
    using System.Text.RegularExpressions;

    public class TerminalSymbol : Symbol
    {
        public TerminalSymbol()
            : this("Terminal_" + ++Counter, "")
        { }

        public TerminalSymbol(string name)
            : this(name, string.Empty)
        { }

        public TerminalSymbol(string name, string pattern)
        {
            this.Name = name;
            this.Expression = new Regex(pattern, RegexOptions.Compiled);
        }

        public TerminalSymbol(string name, Regex expression)
        {
            this.Name = name;
            this.Expression = expression;
        }

        public Regex Expression { get; private set; }

        public override string PrintProduction()
        {
            return Helper.Outline(this.Name, 0, " -> " + this.Expression + ";", 4);
        }
    }
}