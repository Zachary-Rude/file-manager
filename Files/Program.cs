using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Files
{
    static class Program
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
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string pathName = null;
            if (args != null && args.Length > 0)
                pathName = args[0];
            Application.Run(new Form1(pathName));
        }
    }
}
