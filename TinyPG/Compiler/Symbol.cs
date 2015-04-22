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
    public abstract class Symbol
    {
        protected static int Counter = 0;

        protected Symbol()
        {
            this.Attributes = new SymbolAttributes();
        }

        public SymbolAttributes Attributes { get; private set; }

        // an attached piece of sourcecode
        public string CodeBlock { get; set; }

        // the name of the symbol
        public string Name { get; protected set; }

        public Rule Rule { get; set; }

        public abstract string PrintProduction();
    }
}
