namespace TinyPG.Controls.DockExtender
{
    using System;

    /// <summary>
    /// this is the publicly exposed interface of the floating window (floaty)
    /// add more methods/properties here for your own needs, so these are exposed to the client
    /// the main goal is to keep the floaty form internal
    /// </summary>
    public interface IFloaty
    {
        /// <summary>
        /// show the floaty 
        /// </summary>
        void Show();

        /// <summary>
        /// hide the floaty
        /// </summary>
        void Hide();

        /// <summary>
        /// dock the floaty (works only if it is floating)
        /// </summary>
        void Dock();

        /// <summary>
        /// float the floaty (works only if it is docked)
        /// </summary>
        void Float();

        /// <summary>
        /// set a caption for the floaty
        /// </summary>
        String Text {get; set; }

        /// <summary>
        /// indicates if a floaty may dock only on the host docking control (e.g. the form)
        /// and not inside other floaties
        /// </summary>
        bool DockOnHostOnly { get; set; }

        /// <summary>
        /// indicates if a floaty may dock on the inside or on the outside of a form/control
        /// default is true
        /// </summary>
        bool DockOnInside { get; set; }

        event EventHandler Docking;
    }
}