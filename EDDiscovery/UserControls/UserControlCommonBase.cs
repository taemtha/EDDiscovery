﻿/*
 * Copyright © 2016 - 2017 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EliteDangerousCore.DB;
using EliteDangerousCore;

namespace EDDiscovery.UserControls
{
    public class UserControlCommonBase : UserControl
    {
        // Displaynumbers.. 
        // 0 = first instance of control on main tabs
        // 100.. = second on instance of control on main tabs up to 199.
        // 1000 = Bottom history, 1001 = bottom right history, 1002 = middle right history, 1003 = top right history (travelhistorycontrol.cs)
        // 1 = first pop out window of type, 2 = second, etc (popoutcontrol, PopOut func) up to 99.

        // When a grid is open, displaynumber for its children is set by CreatePanel in UserControlContainerGrid
        // 1050 + grid displaynumber * 100 + index of type in grid
        // so, if a pop out grid of second instance, = 2, would get 1050+2000+index of type in grid
        // each grid gets 100 unique numbers, so 100 instances of each type in grid
        // this works for recursive grids to a enough for 158real world purposes.

        public int displaynumber { get; protected set; }
        public EDDiscoveryForm discoveryform { get; protected set; }
        public UserControlCursorType uctg { get; protected set; }

        protected int DisplayNumberOfGridInstance(int numopenedinside)
        { return 1050 + displaynumber * 100 + numopenedinside; }

        // in calling order..
        public void Init(EDDiscoveryForm ed, UserControlCursorType thc, int dn) // with display number
        {
            discoveryform = ed;
            displaynumber = dn;
            uctg = thc;
            Init();
        }    

        public virtual void Init() { }    // start up, give discovery form and cursor - for inherited classes
        public virtual void LoadLayout() { }        // then a chance to load a layout
        public virtual void InitialDisplay() { }    // then after the themeing, do the initial display
        public virtual void Closing() { }           // close it

        public virtual void ChangeCursorType(UserControlCursorType thc) { }     // optional, cursor has changed

        public virtual Color ColorTransparency { get { return Color.Transparent; } }        // override to say support transparency, and what colour you want.
        public virtual void SetTransparency(bool on, Color curcol) { }                      // set on/off transparency of components.

        public void SetControlText(string s)            // used to set heading text in either the form of the tabstrip
        {
            if (this.Parent is ExtendedControls.TabStrip)
                ((ExtendedControls.TabStrip)(this.Parent)).SetControlText(s);
            else if (this.Parent is Forms.UserControlForm)
                ((Forms.UserControlForm)(this.Parent)).SetControlText(s);
        }

        public bool IsTransparent
        {
            get
            {
                if (this.Parent is Forms.UserControlForm)
                    return ((Forms.UserControlForm)(this.Parent)).IsTransparent;
                else
                    return false;
            }
        }



        #region Resize

        public bool inresizeduetoexpand = false;                                            // FUNCTIONS to allow a form to grow temporarily.  Does not work when inside the panels

        public void RequestTemporaryMinimumSize(Size w)         // w is client area
        {
            if (this.Parent is Forms.UserControlForm)
            {
                inresizeduetoexpand = true;
                ((Forms.UserControlForm)(this.Parent)).RequestTemporaryMinimiumSize(w);
                inresizeduetoexpand = false;
            }
        }

        public void RequestTemporaryResizeExpand(Size w)        // by this client size
        {
            if (this.Parent is Forms.UserControlForm)
            {
                inresizeduetoexpand = true;
                ((Forms.UserControlForm)(this.Parent)).RequestTemporaryResizeExpand(w);
                inresizeduetoexpand = false;
            }
        }

        public void RequestTemporaryResize(Size w)              // w is client area
        {
            if (this.Parent is Forms.UserControlForm)
            {
                inresizeduetoexpand = true;
                ((Forms.UserControlForm)(this.Parent)).RequestTemporaryResize(w);
                inresizeduetoexpand = false;
            }
        }

        public void RevertToNormalSize()                        // and to revert
        {
            if (this.Parent is Forms.UserControlForm)
            {
                inresizeduetoexpand = true;
                ((Forms.UserControlForm)(this.Parent)).RevertToNormalSize();
                inresizeduetoexpand = false;
            }
        }

        public bool IsInTemporaryResize                         // have we grown?
        { get
            {
                return (this.Parent is Forms.UserControlForm) ? ((Forms.UserControlForm)(this.Parent)).istemporaryresized : false;
            }
        }

        #endregion

        #region DGV Column helpers - used to save/size the DGV of user controls dynamically.

        public void DGVLoadColumnLayout(DataGridView dgv, string root)
        {
            if (SQLiteConnectionUser.keyExists(root + "1"))        // if stored values, set back to what they were..
            {
                for (int i = 0; i < dgv.Columns.Count; i++)
                {
                    string k = root + (i + 1).ToString();
                    int w = SQLiteDBClass.GetSettingInt(k, -1);
                    if (w >= 10)        // in case something is up (min 10 pixels)
                        dgv.Columns[i].Width = w;
                    //System.Diagnostics.Debug.WriteLine("Load {0} {1} {2} {3}", Name, k, w, dgv.Columns[i].Width);
                }
            }
        }

        public void DGVSaveColumnLayout(DataGridView dgv, string root)
        {
            for (int i = 0; i < dgv.Columns.Count; i++)
            {
                string k = root + (i + 1).ToString();
                SQLiteDBClass.PutSettingInt(k, dgv.Columns[i].Width);
                //System.Diagnostics.Debug.WriteLine("Save {0} {1} {2}", Name, k, dgv.Columns[i].Width);
            }
        }

        #endregion
    }

    // Any UCs wanting to be a cursor, must implement this interface

    public delegate void ChangedSelection(int rowno, int colno, bool doubleclick, bool note);
    public delegate void ChangedSelectionHE(HistoryEntry he, HistoryList hl);

    public interface UserControlCursorType
    {
        event ChangedSelection OnChangedSelection;   // After a change of selection by the user, or after a OnHistoryChanged, or after a sort.
        event ChangedSelectionHE OnTravelSelectionChanged;   // as above, different format, for certain older controls
        void FireChangeSelection(); // fire a change sel event to everyone
        void GotoPosByJID(long jid);    // goto a pos by JID
        HistoryEntry GetCurrentHistoryEntry { get; }    // whats your current entry, null if not
    }
}
