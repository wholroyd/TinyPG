namespace TinyPG.Debug
{
    using System.Collections.Generic;

    public interface IParseNode
    {
        IToken IToken { get; }
        List<IParseNode> INodes { get; }
        string Text { get; set; }
    }
}