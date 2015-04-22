namespace TinyPG.Highlighter
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [ComVisible(true)]
    public class ContextSwitchEventArgs : EventArgs
    {
        public readonly ParseNode PreviousContext;
        public readonly ParseNode NewContext;

        // Summary:
        //     Initializes a new instance of the System.EventArgs class.
        public ContextSwitchEventArgs(ParseNode prevContext, ParseNode nextContext)
        {
            this.PreviousContext = prevContext;
            this.NewContext = nextContext;
        }
    }
}