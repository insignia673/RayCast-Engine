using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RayCastGame.Utils
{
    public static class Input
    {
        public static Point Mouse { get; set; }

        //public readonly static Dictionary<string, bool> MOUSEINPUT = new Dictionary<string, bool>
        //{
        //    { "Left", false},
        //    { "Right", false}
        //};
        public readonly static Dictionary<Keys, bool> KEYINPUT = new Dictionary<Keys, bool>
        {
            { Keys.A, false },
            { Keys.S, false },
            { Keys.D, false },
            { Keys.W, false },
            { Keys.Right, false },
            { Keys.Left, false },
            { Keys.ShiftKey, false },
            { Keys.Escape, false }
        };
    }
}
