// Copyright 2008 - 2010 Herre Kuijpers - <herre.kuijpers@gmail.com>
//
// This source file(s) may be redistributed, altered and customized
// by any means PROVIDING the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------

namespace TinyPG.Controls.DockExtender
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    /// <summary>
    /// this class contains basically all the logic for making a control floating and dockable
    /// note that it is an internal class, only it's IFloaty interface is exposed to the client
    /// </summary>
    internal sealed class Floaty : Form, IFloaty
    {
        #region a teeny weeny tiny bit of API functions used
        private const int WM_NCLBUTTONDBLCLK = 0x00A3;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MOVE = 0xF010;

        // NOTE: I don't like using API's in .Net... so I try to avoid them if possible.
        // this time there was no way around it.

        // this function is used to be able to send some very specific (uncommon) messages
        // to the floaty forms. It is used particularly to switch between start dragging a docked panel
        // to dragging a floaty form.
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(int hWnd, int Msg, int wParam, int lParam);
        #endregion private members

        #region private members

        // this is the orgigional state of the panel. This state is used to reset a control to its
        // origional state if it was floating
        private DockState _dockState;

        // this is a flag that indicates if a control can start floating
        private bool _startFloating;

        // indicates if the container is floating or docked
        private bool _isFloating; 

        // this is the dockmananger that manages this floaty.
        private DockExtender _dockExtender;

        private bool _dockOnHostOnly;
        private bool _dockOnInside;

        #endregion private members

        #region initialization
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="DockExtender">requires the DockExtender</param>
        public Floaty(DockExtender DockExtender)
        {
            this._dockExtender = DockExtender;
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Floaty
            // 
            this.ClientSize = new Size(178, 122);
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.Name = "Floaty";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.ResumeLayout(false);
            this._dockOnInside = true;
            this._dockOnHostOnly = true; // keep it simple for now
        }

        #endregion initialization

        #region properties
        internal DockState DockState 
        {
            get { return this._dockState; }
        }

        public bool DockOnHostOnly
        {
            get { return this._dockOnHostOnly; }
            set { this._dockOnHostOnly = value; }
        }

        public bool DockOnInside
        {
            get { return this._dockOnInside; }
            set { this._dockOnInside = value; }
        }
        
        #endregion properties

        #region overrides
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCLBUTTONDBLCLK) // doubleclicked on border, so reset.
            {
                this.DockFloaty();
            }
            base.WndProc(ref m);
        }

        protected override void OnResizeBegin(EventArgs e)
        {
            base.OnResizeBegin(e);
        }

        protected override void OnResize(EventArgs e)
        {
            
            base.OnResize(e);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            
            if (this._dockExtender.Overlay.Visible && this._dockExtender.Overlay.DockHostControl != null) //ok found new docking position
            {
                this._dockState.OrgDockingParent = this._dockExtender.Overlay.DockHostControl;
                this._dockState.OrgBounds = this._dockState.Container.RectangleToClient(this._dockExtender.Overlay.Bounds);
                this._dockState.OrgDockStyle = this._dockExtender.Overlay.Dock;
                this._dockExtender.Overlay.Hide();
                this.DockFloaty(); // dock the container
            }
            this._dockExtender.Overlay.DockHostControl = null;
            this._dockExtender.Overlay.Hide();
            base.OnResizeEnd(e);
        }

        protected override void OnMove(EventArgs e)
        {
            if (this.IsDisposed) return;

            Point pt = Cursor.Position;
            Point pc = this.PointToClient(pt);
            if (pc.Y < -21 || pc.Y > 0) return;
            if (pc.X < -1 || pc.X > this.Width) return;

            Control t = this._dockExtender.FindDockHost(this, pt);
            if (t == null) 
            {
                this._dockExtender.Overlay.Hide();
            }
            else
            {
                this.SetOverlay(t, pt);
            }
            base.OnMove(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide(); // hide but don't close
            base.OnClosing(e);
        }

        #endregion overrides

        #region public methods (implements IFloaty)
        // override base method, this control only allows one way of showing.
        public new void Show()
        {
            if (!this.Visible && this._isFloating)
                base.Show(this._dockState.OrgDockHost);

            this._dockState.Container.Show();

            if (this._dockState.Splitter != null)
                this._dockState.Splitter.Show();
        }

        public new void Hide()
        {
            if (this.Visible)
                base.Hide();

            this._dockState.Container.Hide();
            if (this._dockState.Splitter != null)
                this._dockState.Splitter.Hide();
        }

        // this this member
        public new void Show(IWin32Window win)
        {
            this.Show();
        }

        public new void Dock()
        {
            if (!this._isFloating) return;
            this.DockFloaty();

        }

        public void Float()
        {
            if (this._isFloating) return;
            this.Text = this._dockState.Handle.Text;

            Point pt = this._dockState.Container.PointToScreen(new Point(0,0));
            Size sz = this._dockState.Container.Size;
            if (this._dockState.Container.Equals(this._dockState.Handle))
            {
                sz.Width += 18;
                sz.Height += 28;
            }
            if (sz.Width > 600) sz.Width = 600;
            if (sz.Height > 600) sz.Height = 600;

            this._dockState.OrgDockingParent = this._dockState.Container.Parent;
            this._dockState.OrgBounds = this._dockState.Container.Bounds;
            this._dockState.OrgDockStyle = this._dockState.Container.Dock;
            this._dockState.Handle.Hide();
            this._dockState.Container.Parent = this;
            this._dockState.Container.Dock = DockStyle.Fill;

            if (this._dockState.Splitter != null)
            {
                this._dockState.Splitter.Visible = false; // hide splitter
                this._dockState.Splitter.Parent = this;
            }
            this.Bounds = new Rectangle(pt, sz);
            this._isFloating = true;
            this.Show();
        }


        #endregion

        #region helper functions - this contains most of the logic

        /// <summary>
        /// determines the client area of the control. The area of docked controls are excluded
        /// </summary>
        /// <param name="c">the control to which to determine the client area</param>
        /// <returns>returns the docking area in screen coordinates</returns>
        private Rectangle GetDockingArea(Control c)
        {
            Rectangle r = c.Bounds;
           
            if (c.Parent != null)
                r = c.Parent.RectangleToScreen(r);

            Rectangle rc = c.ClientRectangle;

            int borderwidth = (r.Width - rc.Width) / 2;
            r.X += borderwidth;
            r.Y += (r.Height - rc.Height) - borderwidth;

            if (!this._dockOnInside)
            {
                rc.X += r.X;
                rc.Y += r.Y;
                return rc;
            }

            foreach (Control cs in c.Controls)
            {
                if (!cs.Visible) continue;
                switch (cs.Dock)
                {
                    case DockStyle.Left:
                        rc.X += cs.Width;
                        rc.Width -= cs.Width;
                        break;
                    case DockStyle.Right:
                            rc.Width -= cs.Width;
                        break;
                    case DockStyle.Top:
                        rc.Y += cs.Height;
                        rc.Height -= cs.Height;
                        break;
                    case DockStyle.Bottom:
                            rc.Height -= cs.Height;
                        break;
                    default:
                        break;
                }
            }
            rc.X += r.X;
            rc.Y += r.Y;

            //Console.WriteLine("Client = " + c.Name + " " + rc.ToString());

            return rc;
        }

        /// <summary>
        /// This method will check if the overlay needs to be displayed or not
        /// for display it will position the overlay
        /// </summary>
        /// <param name="c"></param>
        /// <param name="p">position of cursor in screen coordinates</param>
        private void SetOverlay(Control c, Point pc)
        {

            Rectangle r = this.GetDockingArea(c);
            Rectangle rc = r;

            //determine relative coordinates
            float rx = (pc.X - r.Left) / (float)(r.Width);
            float ry = (pc.Y - r.Top) / (float)(r.Height);

            //Console.WriteLine("Moving over " + c.Name + " " +  rx.ToString() + "," + ry.ToString());

            this._dockExtender.Overlay.Dock = DockStyle.None; // keep floating

            // this section determines when the overlay is to be displayed.
            // it depends on the position of the mouse cursor on the client area.
            // the overlay is currently only shown if the mouse is moving over either the Northern, Western, 
            // Southern or Eastern parts of the client area.
            // when the mouse is in the center or in the NE, NW, SE or SW, no overlay preview is displayed, hence
            // allowing the user to dock the container.

            // dock to left, checks the Western area
            if (rx > 0 && rx < ry && rx < 0.25 && ry < 0.75 && ry > 0.25)
            {
                r.Width = r.Width / 2;
                if (r.Width > this.Width)
                    r.Width = this.Width;

                this._dockExtender.Overlay.Dock = DockStyle.Left; // dock to left
            }

            // dock to the right, checks the Easter area
            if (rx < 1 && rx > ry && rx > 0.75 && ry < 0.75 && ry > 0.25)
            {
                r.Width = r.Width / 2;
                if (r.Width > this.Width)
                    r.Width = this.Width;
                r.X = rc.X + rc.Width - r.Width;
                this._dockExtender.Overlay.Dock = DockStyle.Right;
            }

            // dock to top, checks the Northern area
            if (ry > 0 && ry < rx && ry < 0.25 && rx < 0.75 && rx > 0.25)
            {
                r.Height = r.Height / 2;
                if (r.Height > this.Height)
                    r.Height = this.Height;
                this._dockExtender.Overlay.Dock = DockStyle.Top;
            }

            // dock to the bottom, checks the Southern area
            if (ry < 1 && ry > rx && ry > 0.75 && rx < 0.75 && rx > 0.25)
            {
                r.Height = r.Height / 2;
                if (r.Height > this.Height)
                    r.Height = this.Height;
                r.Y = rc.Y + rc.Height - r.Height;
                this._dockExtender.Overlay.Dock = DockStyle.Bottom;
            }
            if (this._dockExtender.Overlay.Dock != DockStyle.None)
                this._dockExtender.Overlay.Bounds = r;
            else
                this._dockExtender.Overlay.Hide();

            if (!this._dockExtender.Overlay.Visible && this._dockExtender.Overlay.Dock != DockStyle.None)
            {
                this._dockExtender.Overlay.DockHostControl = c;
                this._dockExtender.Overlay.Show(this._dockState.OrgDockHost);
                this.BringToFront();
            }
        }

        internal void Attach(DockState dockState)
        {
            // track the handle's mouse movements
            this._dockState = dockState;
            this.Text = this._dockState.Handle.Text;
            this._dockState.Handle.MouseMove += this.Handle_MouseMove;
            this._dockState.Handle.MouseHover += this.Handle_MouseHover;
            this._dockState.Handle.MouseLeave += this.Handle_MouseLeave;
        }

        /// <summary>
        /// makes the docked control floatable in this Floaty form
        /// </summary>
        /// <param name="dockState"></param>
        /// <param name="offsetx"></param>
        /// <param name="offsety"></param>
        private void MakeFloatable(DockState dockState, int offsetx, int offsety)
        {
            Point ps = Cursor.Position;
            this._dockState = dockState;
            this.Text = this._dockState.Handle.Text;

            Size sz = this._dockState.Container.Size;
            if (this._dockState.Container.Equals(this._dockState.Handle))
            {
                sz.Width += 18;
                sz.Height += 28;
            }
            if (sz.Width > 600) sz.Width = 600;
            if (sz.Height > 600) sz.Height = 600;



            this._dockState.OrgDockingParent = this._dockState.Container.Parent;
            this._dockState.OrgBounds = this._dockState.Container.Bounds;
            this._dockState.OrgDockStyle = this._dockState.Container.Dock;
            //_dockState.OrgDockingParent.Controls.Remove(_dockState.Container);
            //Controls.Add(_dockState.Container);
            this._dockState.Handle.Hide();
            this._dockState.Container.Parent = this;
            this._dockState.Container.Dock = DockStyle.Fill;
            //_dockState.Handle.Visible = false; // hide it for now
            if (this._dockState.Splitter != null)
            {
                this._dockState.Splitter.Visible = false; // hide splitter
                this._dockState.Splitter.Parent = this;
            }
            // allow redraw of floaty and container
            //Application.DoEvents();  

            // this is kind of tricky
            // disable the mousemove events of the handle
            SendMessage(this._dockState.Handle.Handle.ToInt32(), WM_LBUTTONUP, 0, 0);
            ps.X -= offsetx;
            ps.Y -= offsety;


            this.Bounds = new Rectangle(ps, sz);
            this._isFloating = true;
            this.Show();
            // enable the mousemove events of the new floating form, start dragging the form immediately
            
            SendMessage(this.Handle.ToInt32(), WM_SYSCOMMAND, SC_MOVE | 0x02, 0);
        }

        /// <summary>
        /// this will dock the floaty control
        /// </summary>
        private void DockFloaty()
        {
            // bring dockhost to front first to prevent flickering
            this._dockState.OrgDockHost.TopLevelControl.BringToFront();
            this.Hide();
            this._dockState.Container.Visible = false; // hide it temporarely
            this._dockState.Container.Parent = this._dockState.OrgDockingParent;
            this._dockState.Container.Dock = this._dockState.OrgDockStyle;
            this._dockState.Container.Bounds = this._dockState.OrgBounds;
            this._dockState.Handle.Visible = true; // show handle again
            this._dockState.Container.Visible = true; // it's good, show it

            if (this._dockOnInside)
                this._dockState.Container.BringToFront(); // set to front

            //show splitter
            if (this._dockState.Splitter != null && this._dockState.OrgDockStyle != DockStyle.Fill && this._dockState.OrgDockStyle != DockStyle.None)
            {
                this._dockState.Splitter.Parent = this._dockState.OrgDockingParent;
                this._dockState.Splitter.Dock = this._dockState.OrgDockStyle;
                this._dockState.Splitter.Visible = true; // show splitter

                if (this._dockOnInside)
                    this._dockState.Splitter.BringToFront();
                else
                    this._dockState.Splitter.SendToBack();
            }

            if (!this._dockOnInside)
                this._dockState.Container.SendToBack(); // set to back

            this._isFloating = false;

            if (this.Docking != null)
                this.Docking.Invoke(this, new EventArgs());
        }

        private void DetachHandle()
        {
            this._dockState.Handle.MouseMove -= this.Handle_MouseMove;
            this._dockState.Handle.MouseHover -= this.Handle_MouseHover;
            this._dockState.Handle.MouseLeave -= this.Handle_MouseLeave;
            this._dockState.Container = null;
            this._dockState.Handle = null;
        }

        #endregion helper functions

        #region Container Handle tracking methods
        void Handle_MouseHover(object sender, EventArgs e)
        {
            this._startFloating = true;
        }

        void Handle_MouseLeave(object sender, EventArgs e)
        {
            this._startFloating = false;
        }

        void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this._startFloating)
            {
                Point ps = this._dockState.Handle.PointToScreen(new Point(e.X, e.Y));
                this.MakeFloatable(this._dockState, e.X, e.Y);
            }
        }
        #endregion Container Handle tracking methods


        #region events

        public event EventHandler Docking;

        #endregion
    }
}
