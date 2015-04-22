namespace TinyPG.Compiler
{
    using System.Collections.Generic;

    public class Directive : Dictionary<string, string>
    {
        public Directive(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }
}