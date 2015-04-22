namespace TinyPG.Controls.DockExtender
{
    using System.Collections.Generic;
    using System.Windows.Forms;

    /// <summary>
    /// define a Floaty collection used for enumerating all defined floaties
    /// </summary>
    public class Floaties : List<IFloaty>
    {
        public IFloaty Find(Control container)
        {
            foreach (Floaty f in this)
            {
                if (f.DockState.Container.Equals(container))
                    return f;
            }
            return null;
        }
    }
}