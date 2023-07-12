using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Forms;

namespace Files
{
    /*
     * This file is part of Zach, Inc. Files.
     *
     * Zach, Inc. Files is free software: you can redistribute it and/or modify
     * it under the terms of the GNU General Public License as published by
     * the Free Software Foundation, either version 3 of the License, or
     * (at your option) any later version.
     *  
     * Zach, Inc. Files is distributed in the hope that it will be useful,
     * but WITHOUT ANY WARRANTY; without even the implied warranty of
     * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
     * GNU General Public License for more details.
     *  
     * You should have received a copy of the GNU General Public License
     * along with Zach, Inc. Files.  If not, see <http://www.gnu.org/licenses/>.
     */
    /// <summary>
    /// Extension methods for List Views
    /// </summary>
    [SuppressMessage("IntelliSenseCorrection", "IDE1006")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression")]
    public static class ListViewExtensions
    {
        /// <summary>
        /// Sets the double buffered property of a list view to the specified value
        /// </summary>
        /// <param name="listView">The List view</param>
        /// <param name="doubleBuffered">Double Buffered or not</param>
        public static void SetDoubleBuffered(this ListView listView, bool doubleBuffered = true)
        {
            listView
                .GetType()
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(listView, doubleBuffered, null);
        }

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        public static void SetTheme(this ListView listView, string theme = "explorer")
        {
            SetWindowTheme(listView.Handle, theme, null);
        }

    }
}
