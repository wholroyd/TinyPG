namespace TinyPG.Compiler
{
    using System.Collections.Generic;

    public class Directives : List<Directive>
    {
        public Directive this[string name]
        {
            get { return this.Find(name); }
        }

        public bool Exists(Directive directive)
        {
            return this.Exists(d => d.Name == directive.Name);
        }

        public Directive Find(string name)
        {
            return this.Find(d => d.Name == name);
        }
    }
}