namespace TinyPG.Compiler
{
    using System.Collections.Generic;

    public class Symbols : List<Symbol>
    {
        public bool Exists(Symbol symbol)
        {
            return this.Exists(s => s.Name == symbol.Name);
        }

        public Symbol Find(string name)
        {
            return this.Find(s => s != null && s.Name == name);
        }
    }
}