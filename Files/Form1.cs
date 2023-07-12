using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualBasic.FileIO;

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
    [SuppressMessage("IntelliSenseCorrection", "IDE1006")]
    [SuppressMessage("IntelliSenseCorrection", "IDE0044")]
    public partial class Form1 : Form
    {
        string storedPath;
        string currentPath;
        string newPath;
        string oldPath;
        bool txtLocationJustEntered = false;
        List<string> listFiles = new List<string>();
        List<string> navigationHistory = new List<string>();
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        public static void SetTreeViewTheme(IntPtr treeHandle)
        {
            SetWindowTheme(treeHandle, "explorer", null);
        }

        public Form1()
        {
            InitializeComponent();
            this.MinimumSize = new Size(0, 0);
            this.Icon = Properties.Resources.filesicon;
            openWithToolStripMenuItem.Click += this.openWithToolStripMenuItem_Click;
            contextMenuStrip1.Renderer = new ToolStripAeroRenderer();
            navigateToFolder(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            lvFiles.Columns[0].Width = 165;
            lvFiles.Columns[1].Width = 105;
            lvFiles.Columns[3].Width = 70;
            lvFiles.View = Properties.Settings.Default.FolderView;
            menuItem7.Checked = Properties.Settings.Default.LargeIcon;
            menuItem8.Checked = Properties.Settings.Default.SmallIcon;
            menuItem9.Checked = Properties.Settings.Default.Details;
            menuItem14.Checked = Properties.Settings.Default.List;
            menuItem16.Checked = Properties.Settings.Default.Tile;
            lvFiles.SetDoubleBuffered();
            UpdateFileTree();
            enableBackForwardTimer.Start();
            SetTreeViewTheme(treeView1.Handle);
            lvFiles.SetTheme();
        }

        public Form1(string pathName) : this()
        {
            if (pathName == null)
                return;

            if (!Directory.Exists(pathName))
            {
                MessageBox.Show("The folder does not exist", "Cannot open folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                navigateToFolder(pathName);
                storedPath = txtLocation.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cannot open folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.OemQuestion:
                    if (!txtLocation.Focused)
                    {
                        txtLocation.Focus();
                    }
                    break;
                case Keys.F5:
                    menuItem21.PerformClick();
                    break;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void UpdateRecentFolderList()
        {
            if (menuItem19.MenuItems.Count > 0)
            {
                menuItem19.MenuItems.Clear();
                if (Properties.Settings.Default.RecentFolders.Count > 0)
                {
                    int index = 0;
                    var uniques = Properties.Settings.Default.RecentFolders.Cast<IEnumerable>();
                    var unique = uniques.Distinct();
                    foreach (string fldr in unique)
                    {
                        if (!string.IsNullOrEmpty(fldr) || !string.IsNullOrWhiteSpace(fldr))
                        {
                            index++;
                            MenuItem item = new MenuItem(fldr);
                            item.Click += recentFolder_Click;
                            item.Index = index;
                            menuItem19.MenuItems.Add(item);
                        }
                    }
                    MenuItem separator = new MenuItem("-");
                    menuItem19.MenuItems.Add(separator);
                }
            }
            menuItem19.MenuItems.Add(menuClear);
            if (Properties.Settings.Default.RecentFolders.Count > 0)
            {
                menuClear.Enabled = true;
            }
            else
            {
                menuClear.Enabled = false;
            }
        }

        /// <summary>
        /// Updates the in-app file system tree.
        /// </summary>
        [SuppressMessage("IntelliSenseCorrection", "IDE0017", Justification = "Allowing use of constructors followed by separate property assignments in order to maintain consistency and readability of code.")]
        private void UpdateFileTree()
        {
            // Clear file tree
            treeView1.Nodes.Clear();

            // Add "My Computer" ("This PC" on Windows 8+) to file tree
            TreeNode myComputerNode = new TreeNode(Environment.OSVersion.Version >= new Version(6, 2) ? "This PC" : "My Computer" /* Check if the user is using Windows 8 or a newer version, and if so, make the "My Computer"/"This PC" node say "This PC"; otherwise, make it say "My Computer". This is done so that the wording of the node matches the user's Windows version. */, 0, 0);
            myComputerNode.Tag = "MyComputer";
            treeView1.Nodes.Add(myComputerNode);
            
            // Get a list of drives and add them to the file tree
            string[] drives = Environment.GetLogicalDrives();

            foreach (string drive in drives)
            {
                DriveInfo di = new DriveInfo(drive);
                TreeNode node = new TreeNode(drive, Environment.GetEnvironmentVariable("systemroot").StartsWith(di.Name) ? 2 : 1, Environment.GetEnvironmentVariable("systemroot").StartsWith(di.Name) ? 2 : 1);
                node.Tag = drive;

                if (di.IsReady == true)
                    node.Nodes.Add("...");

                myComputerNode.Nodes.Add(node);
            }
            myComputerNode.Expand();
        }

        private void radioMenu_Click(object sender, EventArgs e)
        {
            if (sender == menuItem7)
            {
                Properties.Settings.Default.FolderView = View.LargeIcon;
                Properties.Settings.Default.LargeIcon = true;
                Properties.Settings.Default.SmallIcon = false;
                Properties.Settings.Default.Details = false;
                Properties.Settings.Default.List = false;
                Properties.Settings.Default.Tile = false;
            }
            else if (sender == menuItem8)
            {
                Properties.Settings.Default.FolderView = View.SmallIcon;
                Properties.Settings.Default.LargeIcon = false;
                Properties.Settings.Default.SmallIcon = true;
                Properties.Settings.Default.Details = false;
                Properties.Settings.Default.List = false;
                Properties.Settings.Default.Tile = false;
            }
            else if (sender == menuItem9)
            {
                Properties.Settings.Default.FolderView = View.Details;
                Properties.Settings.Default.LargeIcon = false;
                Properties.Settings.Default.SmallIcon = false;
                Properties.Settings.Default.Details = true;
                Properties.Settings.Default.List = false;
                Properties.Settings.Default.Tile = false;
            }
            else if (sender == menuItem14)
            {
                Properties.Settings.Default.FolderView = View.List;
                Properties.Settings.Default.LargeIcon = false;
                Properties.Settings.Default.SmallIcon = false;
                Properties.Settings.Default.Details = false;
                Properties.Settings.Default.List = true;
                Properties.Settings.Default.Tile = false;
            }
            else if (sender == menuItem16)
            {
                Properties.Settings.Default.FolderView = View.Tile;
                Properties.Settings.Default.LargeIcon = false;
                Properties.Settings.Default.SmallIcon = false;
                Properties.Settings.Default.Details = false;
                Properties.Settings.Default.List = false;
                Properties.Settings.Default.Tile = true;
            }

            Properties.Settings.Default.Save();
            lvFiles.View = Properties.Settings.Default.FolderView;
            menuItem7.Checked = Properties.Settings.Default.LargeIcon;
            menuItem8.Checked = Properties.Settings.Default.SmallIcon;
            menuItem9.Checked = Properties.Settings.Default.Details;
            menuItem14.Checked = Properties.Settings.Default.List;
            menuItem16.Checked = Properties.Settings.Default.Tile;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        private const int SW_SHOW = 5;
        private const uint SEE_MASK_INVOKEIDLIST = 12;
        /// <summary>
        /// Shows the standard Windows file/folder properties dialog for the path that is passed as a parameter.
        /// </summary>
        /// <param name="Filename">The path to the file or folder whose properties dialog is to be shown.</param>
        /// <returns>Boolean</returns>
        /// <example>
        /// ShowFileProperties(@"C:\path\to\file.ext");
        /// </example>
        public static bool ShowFileProperties(string Filename)
        {
            SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
            info.cbSize = Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = Filename;
            info.nShow = SW_SHOW;
            info.fMask = SEE_MASK_INVOKEIDLIST;
            return ShellExecuteEx(ref info);
        }
        public static string GetFileTypeDescription(string fileNameOrExtension)
        {
            SHFILEINFO shfi;
            if (IntPtr.Zero != SHGetFileInfo(
                                fileNameOrExtension,
                                FILE_ATTRIBUTE_NORMAL,
                                out shfi,
                                (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
                                SHGFI_USEFILEATTRIBUTES | SHGFI_TYPENAME))
            {
                return shfi.szTypeName;
            }
            return null;
        }

        [DllImport("shell32")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
        private const uint FILE_ATTRIBUTE_HIDDEN = 0x00000002;
        private const uint FILE_ATTRIBUTE_SYSTEM = 0x00000004;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_ARCHIVE = 0x00000020;
        private const uint FILE_ATTRIBUTE_DEVICE = 0x00000040;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const uint FILE_ATTRIBUTE_TEMPORARY = 0x00000100;
        private const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200;
        private const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
        private const uint FILE_ATTRIBUTE_COMPRESSED = 0x00000800;
        private const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
        private const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000;
        private const uint FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
        private const uint FILE_ATTRIBUTE_VIRTUAL = 0x00010000;

        private const uint SHGFI_ICON = 0x000000100;     // get icon
        private const uint SHGFI_DISPLAYNAME = 0x000000200;     // get display name
        private const uint SHGFI_TYPENAME = 0x000000400;     // get type name
        private const uint SHGFI_ATTRIBUTES = 0x000000800;     // get attributes
        private const uint SHGFI_ICONLOCATION = 0x000001000;     // get icon location
        private const uint SHGFI_EXETYPE = 0x000002000;     // return exe type
        private const uint SHGFI_SYSICONINDEX = 0x000004000;     // get system icon index
        private const uint SHGFI_LINKOVERLAY = 0x000008000;     // put a link overlay on icon
        private const uint SHGFI_SELECTED = 0x000010000;     // show icon in selected state
        private const uint SHGFI_ATTR_SPECIFIED = 0x000020000;     // get only specified attributes
        private const uint SHGFI_LARGEICON = 0x000000000;     // get large icon
        private const uint SHGFI_SMALLICON = 0x000000001;     // get small icon
        private const uint SHGFI_OPENICON = 0x000000002;     // get open icon
        private const uint SHGFI_SHELLICONSIZE = 0x000000004;     // get shell size icon
        private const uint SHGFI_PIDL = 0x000000008;     // pszPath is a pidl
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;     // use passed dwFileAttribute
        [DllImport("Shell32")]

        public static extern int ExtractIconEx(string sFile, int iIndex, ref IntPtr piLargeVersion, ref IntPtr piSmallVersion, int amountIcons);
        private int num = 10;
        private IntPtr[] large;
        private IntPtr[] small;
        private void navigateToFolder(string path, bool rememberPosition = false)
        {
            oldPath = txtLocation.Text;
            int topItemIndex = 0;
            if (rememberPosition)
            {
                try
                {
                    topItemIndex = lvFiles.TopItem.Index;
                }
                catch (Exception ex)
                { }
            }
            listFiles.Clear();
            lvFiles.Items.Clear();
            lvFiles.BeginUpdate();
            DirectoryInfo nodeDirInfo = new DirectoryInfo(path);
            ListViewItem.ListViewSubItem[] subItems;
            ListViewItem item = null;


            // Obtain a handle to the system image list.
            NativeMethods.SHFILEINFO shfi = new NativeMethods.SHFILEINFO();
            IntPtr hSysImgList = NativeMethods.SHGetFileInfo("",
                                                             0,
                                                             ref shfi,
                                                             (uint)Marshal.SizeOf(shfi),
                                                             NativeMethods.SHGFI_SYSICONINDEX
                                                              | NativeMethods.SHGFI_SMALLICON);
            Debug.Assert(hSysImgList != IntPtr.Zero);  // cross our fingers and hope to succeed!

            // Set the ListView control to use that image list.
            IntPtr hOldImgList = NativeMethods.SendMessage(lvFiles.Handle,
                                                           NativeMethods.LVM_SETIMAGELIST,
                                                           NativeMethods.LVSIL_SMALL,
                                                           hSysImgList);


            // Set the ListView control to use that image list.
            IntPtr hOldImgList2 = NativeMethods.SendMessage(lvFiles.Handle,
                                                           NativeMethods.LVM_SETIMAGELIST,
                                                           NativeMethods.LVSIL_NORMAL,
                                                           hSysImgList);
            // If the ListView control already had an image list, delete the old one.
            if (hOldImgList != IntPtr.Zero)
            {
                NativeMethods.ImageList_Destroy(hOldImgList);
            }
            foreach (DirectoryInfo dir in nodeDirInfo.GetDirectories()
                .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden)))
            {
                IntPtr himl = NativeMethods.SHGetFileInfo(dir.FullName,
                0,
                                                          ref shfi,
                                                          (uint)Marshal.SizeOf(shfi),
                                                          NativeMethods.SHGFI_DISPLAYNAME
                                                            | NativeMethods.SHGFI_SYSICONINDEX
                                                            | (lvFiles.View == View.Details || lvFiles.View == View.SmallIcon ? NativeMethods.SHGFI_SMALLICON : NativeMethods.SHGFI_LARGEICON));
                item = new ListViewItem(dir.Name, shfi.iIcon);
                var itemCount = (object)null;
                var itemLabel = (object)null;
                try
                {
                    itemCount = dir.GetFiles().Count() + dir.GetDirectories().Count();
                    itemLabel = (int)itemCount == 1 ? "item" : "items";
                }
                catch (UnauthorizedAccessException)
                {
                    itemCount = "?";
                    itemLabel = "items";
                }
                catch (Exception ex)
                {

                }
                finally
                {
                    subItems = new ListViewItem.ListViewSubItem[]
                        {new ListViewItem.ListViewSubItem(item, "Folder"),
             new ListViewItem.ListViewSubItem(item,
                dir.LastAccessTime.ToShortDateString()),
             new ListViewItem.ListViewSubItem(item, string.Format("{0} {1}", itemCount, itemLabel))};
                    item.ToolTipText = dir.Name + "\r\nType: Folder\r\nSize: " + string.Format("{0} {1}", itemCount, itemLabel) + "\r\nLast Modified: " + dir.LastAccessTime.ToShortDateString();
                    item.SubItems.AddRange(subItems);
                    lvFiles.Items.Add(item);
                    listFiles.Add(dir.FullName);
                    Application.DoEvents();
                    lvFiles.Refresh();
                }
            }
            foreach (FileInfo file in nodeDirInfo.GetFiles()
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden)))
            {
                IntPtr himl = NativeMethods.SHGetFileInfo(file.FullName,
                0,
                                                          ref shfi,
                                                          (uint)Marshal.SizeOf(shfi),
                                                          NativeMethods.SHGFI_DISPLAYNAME
                                                            | NativeMethods.SHGFI_SYSICONINDEX
                                                            | (lvFiles.View == View.Details || lvFiles.View == View.SmallIcon ? NativeMethods.SHGFI_SMALLICON : NativeMethods.SHGFI_LARGEICON));
                bool showExtension = true;
                string fileName;
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = file.Length;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
                // show a single decimal place, and no space.
                string result = string.Format("{0:0.##} {1}", len, sizes[order]);
                if (showExtension)
                {
                    fileName = file.Name;
                }
                else
                {
                    fileName = Path.GetFileNameWithoutExtension(file.Name);
                }
                item = new ListViewItem(fileName, shfi.iIcon);
                subItems = new ListViewItem.ListViewSubItem[]
                    { new ListViewItem.ListViewSubItem(item, GetFileTypeDescription(file.Extension)),
             new ListViewItem.ListViewSubItem(item,
                file.LastAccessTime.ToShortDateString()),
             new ListViewItem.ListViewSubItem(item, result)};
                item.ToolTipText = fileName + "\r\nType: " + GetFileTypeDescription(file.Extension) + "\r\nSize: " + result + "\r\nLast Modified: " + file.LastAccessTime.ToShortDateString();
                item.SubItems.AddRange(subItems);
                lvFiles.Items.Add(item);
                listFiles.Add(file.FullName);
                Application.DoEvents();
                lvFiles.Refresh();
            }

            lvFiles.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.None);
            lvFiles.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.None);
            lvFiles.Columns[2].AutoResize(ColumnHeaderAutoResizeStyle.None);
            lvFiles.Columns[3].AutoResize(ColumnHeaderAutoResizeStyle.None);
            lvFiles.EndUpdate();
            lvFiles.Refresh();
            try
            {
                lvFiles.TopItem = lvFiles.Items[topItemIndex];
            }
            catch (Exception ex)
            { }
            currentPath = path;
            txtLocation.Text = currentPath;
            label1.Focus();
            this.Text = "Index of " + currentPath + " - Files";
            if (Properties.Settings.Default.RecentFolders.Count > 5)
            {
                Properties.Settings.Default.RecentFolders.RemoveAt(0);
            }
            if (Properties.Settings.Default.RecentFolders.Contains(path))
            {
                Properties.Settings.Default.RecentFolders.Remove(path);
            }
            Properties.Settings.Default.RecentFolders.Insert(0, currentPath);
            Properties.Settings.Default.Save();
            UpdateRecentFolderList();
            if (!rememberPosition)
            {
                navigationHistory.Add(path);
            }
        }

        [SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value")]
        public static bool CanRead(string path)
        {
            try
            {
                var readAllow = false;
                var readDeny = false;
                var accessControlList = Directory.GetAccessControl(path);
                if (accessControlList == null)
                    return false;

                //get the access rules that pertain to a valid SID/NTAccount.
                var accessRules = accessControlList.GetAccessRules(true, true, typeof(SecurityIdentifier));
                if (accessRules == null)
                    return false;

                //we want to go over these rules to ensure a valid SID has access
                foreach (FileSystemAccessRule rule in accessRules)
                {
                    if ((FileSystemRights.Read & rule.FileSystemRights) != FileSystemRights.Read) continue;

                    if (rule.AccessControlType == AccessControlType.Allow)
                        readAllow = true;
                    else if (rule.AccessControlType == AccessControlType.Deny)
                        readDeny = true;
                }

                return readAllow && !readDeny;
            }
            catch (UnauthorizedAccessException ex)
            {
                return false;
            }
        }

        private void listView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (lvFiles.FocusedItem != null && e.Button == MouseButtons.Left)
            {
                if (lvFiles.FocusedItem.SubItems[1].Text == "Folder")
                {
                    try
                    {
                        newPath = listFiles[lvFiles.FocusedItem.Index];
                        if (CanRead(newPath))
                        {
                            navigateToFolder(newPath);
                        }
                        else
                        {
                            MessageBox.Show(string.Format("Access to the path \"{0}\" is denied.", newPath), "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            navigateToFolder(txtLocation.Text, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        navigateToFolder(txtLocation.Text);
                    }
                }
                else
                {
                    try
                    {
                        newPath = listFiles[lvFiles.FocusedItem.Index];
                        Process.Start(newPath);
                    }
                    catch (Win32Exception ex)
                    {
                        ShowOpenWithDialog(listFiles[lvFiles.FocusedItem.Index]);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Cannot open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void goToFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog() { Description = "Select a folder to navigate to.", ShowNewFolderButton = false })
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        navigateToFolder(fbd.SelectedPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txtLocation.Text = oldPath;
                        navigateToFolder(txtLocation.Text);
                    }
                }
            }
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openToolStripMenuItem.PerformClick();
        }

        private void newFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                NewFolder(currentPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error while creating folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            navigateToFolder(currentPath, true);
        }

        private void NewFolder(string path)
        {
            Directory.CreateDirectory(GetUniqueFolderName(Path.Combine(path, "New folder")));
        }

        private void aboutFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutFilesForm aboutFilesForm = new AboutFilesForm();
            aboutFilesForm.ShowDialog();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            copyToolStripMenuItem.PerformClick();
        }

        private void newFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                File.Create(GetUniqueFilePath(Path.Combine(currentPath, "New text file.txt"))).Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error while creating file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            navigateToFolder(currentPath, true);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void maximizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
        }

        private void minimizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void copyPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtLocation.Copy();
        }

        private void menuItem11_Click(object sender, EventArgs e)
        {
            navigateToFolder(oldPath);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lvFiles.FocusedItem != null)
            {
                if (lvFiles.FocusedItem.SubItems[1].Text == "Folder")
                {
                    try
                    {
                        newPath = listFiles[lvFiles.FocusedItem.Index];
                        if (CanRead(newPath))
                        {
                            navigateToFolder(newPath);
                        }
                        else
                        {
                            MessageBox.Show(string.Format("Access to the path \"{0}\" is denied.", newPath), "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            navigateToFolder(txtLocation.Text, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        navigateToFolder(txtLocation.Text);
                    }
                }
                else
                {
                    try
                    {
                        newPath = listFiles[lvFiles.FocusedItem.Index];
                        Process.Start(newPath);
                    }
                    catch (Win32Exception ex)
                    {
                        ShowOpenWithDialog(listFiles[lvFiles.FocusedItem.Index]);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Cannot open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }



        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (lvFiles.FocusedItem != null)
            {
                if (lvFiles.FocusedItem.SubItems[1].Text == "Folder")
                {
                    openInNewWindowToolStripMenuItem.Visible = true;
                    openWithToolStripMenuItem.Visible = false;
                }
                else
                {
                    openInNewWindowToolStripMenuItem.Visible = false;
                    copyToolStripMenuItem.Visible = true;
                    openWithToolStripMenuItem.Visible = true;
                }
                openToolStripMenuItem.Visible = true;
                toolStripSeparator1.Visible = true;
                newToolStripMenuItem.Visible = true;
                toolStripSeparator2.Visible = true;
                deleteToolStripMenuItem.Visible = true;
                renameToolStripMenuItem.Visible = true;
                cutToolStripMenuItem.Visible = true;
                copyToolStripMenuItem.Visible = true;
                toolStripSeparator3.Visible = true;
                propertiesToolStripMenuItem.Visible = true;
            }
            else
            {
                openInNewWindowToolStripMenuItem.Visible = false;
                openWithToolStripMenuItem.Visible = false;
                openToolStripMenuItem.Visible = false;
                toolStripSeparator1.Visible = false;
                newToolStripMenuItem.Visible = true;
                copyToolStripMenuItem.Visible = false;
                toolStripSeparator2.Visible = false;
                deleteToolStripMenuItem.Visible = false;
                renameToolStripMenuItem.Visible = false;
                cutToolStripMenuItem.Visible = false;
                toolStripSeparator3.Visible = false;
                propertiesToolStripMenuItem.Visible = false;
            }
            e.Cancel = false;
        }

        private void openInNewWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lvFiles.FocusedItem != null)
            {
                Process.Start(Application.ExecutablePath, listFiles[lvFiles.FocusedItem.Index]);
            }
        }

        private void textFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            newFileToolStripMenuItem.PerformClick();
        }

        private void folderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            newFolderToolStripMenuItem.PerformClick();
        }

        private void copyToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (lvFiles.SelectedItems != null)
            {
                try
                {
                    StringCollection selectedFilesList = new StringCollection();
                    foreach (ListViewItem filePathItem in lvFiles.SelectedItems)
                    {
                        selectedFilesList.Add(listFiles[lvFiles.Items.IndexOf(filePathItem)]);
                    }
                    Clipboard.SetFileDropList(selectedFilesList);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Cannot perform file operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lvFiles.SelectedItems != null)
            {
                foreach (ListViewItem item in lvFiles.SelectedItems)
                {
                    try
                    {
                        if (item.SubItems[1].Text == "Folder")
                        {
                            FileSystem.DeleteDirectory(listFiles[lvFiles.Items.IndexOf(item)], UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently, UICancelOption.DoNothing);
                        }
                        else
                        {
                            FileSystem.DeleteFile(listFiles[lvFiles.Items.IndexOf(item)], UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently, UICancelOption.DoNothing);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Cannot perform file operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                navigateToFolder(currentPath, true);
                UpdateFileTree();
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lvFiles.SelectedItems != null)
            {
                try
                {
                    StringCollection selectedFilesList = new StringCollection();
                    foreach (ListViewItem filePathItem in lvFiles.SelectedItems)
                    {
                        selectedFilesList.Add(listFiles[lvFiles.Items.IndexOf(filePathItem)]);
                    }
                    DataObject data = new DataObject();
                    data.SetData("FileDrop", selectedFilesList);
                    data.SetData("Preferred DropEffect", DragDropEffects.Move);
                    Clipboard.SetDataObject(data, true);
                }
                catch (Exception ex)
                {
                     MessageBox.Show(ex.Message, "Cannot perform file operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lvFiles.FocusedItem != null)
            {
                lvFiles.LabelEdit = true;
                lvFiles.SelectedItems[0].BeginEdit();
            }
        }



        private void menuItem15_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.ShowHiddenFiles = !Properties.Settings.Default.ShowHiddenFiles;
            Properties.Settings.Default.Save();
            navigateToFolder(currentPath);
        }

        private void createFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                NewFolder(currentPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error while creating folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            navigateToFolder(currentPath, true);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            navigateToFolder(Path.GetFullPath(Path.Combine(currentPath, @"..")));
        }

        private void txtLocation_Enter(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtLocation.Text))
            {
                txtLocation.SelectAll();
                txtLocationJustEntered = true;
                oldPath = txtLocation.Text;
            }
            else
            {
                txtLocation.DeselectAll();
                txtLocationJustEntered = false;
            }
        }

        private void txtLocation_Click(object sender, EventArgs e)
        {
            if (txtLocationJustEntered)
            {
                txtLocation.SelectAll();
            }

            txtLocationJustEntered = false;
        }

        /// <summary>
        /// Generates a file name that, if already used by another file within the same folder, has a number directly before the file extension.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>String</returns>
        private static string GetUniqueFilePath(string filePath)
        {
            // Code (mostly) from https://stackoverflow.com/a/22373595/16121348 (modified a little bit here by me, mainly to change the format of the resulting file name)
            if (File.Exists(filePath))
            {
                string folderPath = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string fileExtension = Path.GetExtension(filePath);
                int number = 1;

                Match regex = Regex.Match(fileName, @"^(.+)(\d+)$");

                if (regex.Success)
                {
                    fileName = regex.Groups[1].Value;
                    number = int.Parse(regex.Groups[2].Value);
                }

                do
                {
                    number++;
                    string newFileName = $"{fileName}{number}{fileExtension}";
                    filePath = Path.Combine(folderPath, newFileName);
                }
                while (File.Exists(filePath));
            }

            return filePath;
        }

        private static string GetUniqueFolderName(string path)
        {
            if (Directory.Exists(path))
            {
                string folderName = Path.GetFileName(path);
                int number = 1;

                Match regex = Regex.Match(folderName, @"^(.+)(\d+)$");

                if (regex.Success)
                {
                    folderName = regex.Groups[1].Value;
                    number = int.Parse(regex.Groups[2].Value);
                }

                do
                {
                    number++;
                    string newFolderName = $"{folderName}{number}";
                    path = Path.Combine(Path.GetDirectoryName(path), newFolderName);
                }
                while (Directory.Exists(path));
            }

            return path;
        }

        private void menuItem17_Click(object sender, EventArgs e)
        {
            navigateToFolder(Path.GetFullPath(Path.Combine(currentPath, @"..")));
        }

        private void recentFolder_Click(object sender, EventArgs e)
        {
            try
            {
                navigateToFolder(((MenuItem)sender).Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cannot navigate to folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                navigateToFolder(currentPath, true);
            }
        }

        private void menuClear_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.RecentFolders.Clear();
            Properties.Settings.Default.Save();
            UpdateRecentFolderList();
        }

        private void openWithToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lvFiles.FocusedItem != null)
            {
                try
                {
                    ShowOpenWithDialog(listFiles[lvFiles.FocusedItem.Index]);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error while opening file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static void ShowOpenWithDialog(string path)
        {
            var args = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
            args += ",OpenAs_RunDLL " + path;
            Process.Start("rundll32.exe", args);
        }

        private void listView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            List<string> selection = new List<string>();

            foreach (ListViewItem item in lvFiles.SelectedItems)
            {
                int imgIndex = item.Index;
                selection.Add(listFiles[imgIndex]);
            }

            DataObject data = new DataObject(DataFormats.FileDrop, selection.ToArray());
            this.DoDragDrop(data, DragDropEffects.Move);
        }

        private void menuItem21_Click(object sender, EventArgs e)
        {
            navigateToFolder(currentPath);
            UpdateFileTree();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PasteFilesFromClipboard(currentPath);
            navigateToFolder(currentPath, true);
            UpdateFileTree();
        }

        public static void PasteFilesFromClipboard(string aTargetFolder)
        {
            var aFileDropList = Clipboard.GetFileDropList();
            if (aFileDropList == null || aFileDropList.Count == 0) return;

            bool aMove = false;

            var aDataDropEffect = Clipboard.GetData("Preferred DropEffect");
            if (aDataDropEffect != null)
            {
                MemoryStream aDropEffect = (MemoryStream)aDataDropEffect;
                byte[] aMoveEffect = new byte[4];
                aDropEffect.Read(aMoveEffect, 0, aMoveEffect.Length);
                var aDragDropEffects = (DragDropEffects)BitConverter.ToInt32(aMoveEffect, 0);
                aMove = aDragDropEffects.HasFlag(DragDropEffects.Move);
            }

            foreach (string aFileName in aFileDropList)
            {
                if (aMove)
                {
                    try
                    {
                        if (File.GetAttributes(aFileName).HasFlag(FileAttributes.Directory))
                        {
                            Directory.Move(aFileName, aTargetFolder);
                        }
                        else
                        {
                            File.Move(aFileName, Path.Combine(aTargetFolder, Path.GetFileName(aFileName)));
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Cannot perform file operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    try
                    {
                        if (File.GetAttributes(aFileName).HasFlag(FileAttributes.Directory))
                        {
                            CopyFolder(aFileName, GetUniqueFolderName(Path.Combine(aTargetFolder, Path.GetFileName(aFileName) + " - Copy")));
                        }
                        else
                        {
                            File.Copy(aFileName, GetUniqueFilePath(Path.Combine(aTargetFolder, Path.GetFileName(aFileName).Replace(Path.GetExtension(aFileName), " - Copy") + Path.GetExtension(aFileName))));
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Cannot perform file operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        static void CopyFolder(string sourceFolder, string destinationFolder, bool recursive = true)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceFolder);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source folder not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationFolder);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationFolder, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationFolder, subDir.Name);
                    CopyFolder(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        [Flags]
        public enum ClipboardDragDropEffects
        {
            Scroll = int.MinValue,
            All = -2147483645,
            None = 0,
            Copy = 1,
            Move = 2,
            Link = 4
        }

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lvFiles.FocusedItem != null)
            {
                try
                {
                    ShowFileProperties(listFiles[lvFiles.FocusedItem.Index]);
                }
                catch (Exception ex)
                {

                }
            }
        }

        /// <summary>
        /// Handles the expansion of the items within the in-app file system tree in order to dynamically add subfolders within each of the nodes in the tree when a node is expanded.
        /// </summary>
        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count > 0)
            {
                if (e.Node.Nodes[0].Text == "..." && e.Node.Nodes[0].Tag == null)
                {
                    e.Node.Nodes.Clear();

                    // Get the list of subfolders within the folder represented by the expanded node in the file tree
                    string[] dirs = Directory.GetDirectories(e.Node.Tag.ToString());
                    foreach (string dir in dirs)
                    {
                        DirectoryInfo di = new DirectoryInfo(dir);
                        Icon folderIcon = IconHelper.GetIconOfPath(dir, true, true);
                        if (!imageListTree.Images.ContainsKey(dir))
                        {
                            imageListTree.Images.Add(dir, folderIcon);
                        }
                        TreeNode node = new TreeNode(di.Name, imageListTree.Images.IndexOfKey(dir), imageListTree.Images.IndexOfKey(dir));

                        try
                        {
                            // Keep the folder's full path in the tag for later use
                            node.Tag = dir;

                            // If the folder has subfolders, add the placeholder
                            if (di.GetDirectories().Count() > 0)
                                node.Nodes.Add(null, "...", 1, 1);

                        }
                        catch (UnauthorizedAccessException)
                        {

                        }
                        catch (Exception ex)
                        {

                        }
                        finally
                        {
                            // Add the folder to the file tree, but only if it's not a hidden folder
                            if (!di.Attributes.HasFlag(FileAttributes.Hidden))
                            {
                                e.Node.Nodes.Add(node);
                            }
                        }
                    }
                }
            }
        }

        private void txtLocation_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                try
                {
                    Regex r = new Regex(@"^(([a-zA-Z]\:)|(\\))(\\{1}|((\\{1})[^\\]([^/:*?<>""|]*))+)$");
                    if (r.IsMatch(Environment.ExpandEnvironmentVariables(txtLocation.Text)))
                    {
                        if (Directory.Exists(Environment.ExpandEnvironmentVariables(txtLocation.Text)) && !string.IsNullOrEmpty(Environment.ExpandEnvironmentVariables(txtLocation.Text)))
                        {
                            if (CanRead(Environment.ExpandEnvironmentVariables(txtLocation.Text)))
                            {
                                navigateToFolder(Environment.ExpandEnvironmentVariables(txtLocation.Text));
                            }
                            else
                            {
                                MessageBox.Show(string.Format("Access to the path \"{0}\" is denied.", Environment.ExpandEnvironmentVariables(txtLocation.Text)), "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                txtLocation.Text = oldPath;
                                navigateToFolder(txtLocation.Text, true);
                            }
                        }
                        else
                        {
                            MessageBox.Show(string.Format("The folder {0} could not be found.", Environment.ExpandEnvironmentVariables(txtLocation.Text)), "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            txtLocation.Text = oldPath;
                            navigateToFolder(txtLocation.Text, true);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(txtLocation.Text) || string.IsNullOrWhiteSpace(txtLocation.Text))
                        {
                            MessageBox.Show("Empty/whitespace-only path is not valid.", "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            txtLocation.Text = oldPath;
                            navigateToFolder(txtLocation.Text, true);
                        }
                        else
                        {
                            MessageBox.Show("Path format is invalid.", "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            txtLocation.Text = oldPath;
                            navigateToFolder(txtLocation.Text, true);
                        }
                    }
                }
                catch (Exception)
                {
                }
                label1.Focus();
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeViewHitTestInfo hit = treeView1.HitTest(e.Location);
            if (hit.Location != TreeViewHitTestLocations.PlusMinus && e.Node.Tag.ToString() != "MyComputer")
            {
                oldPath = txtLocation.Text;
                try
                {
                    if (e.Node.Tag.ToString().IndexOfAny(Path.GetInvalidPathChars()) == -1 && !string.IsNullOrEmpty(e.Node.Tag.ToString()) && !string.IsNullOrWhiteSpace(e.Node.Tag.ToString()))
                    {
                        if (Directory.Exists(e.Node.Tag.ToString()))
                        {
                            if (CanRead(e.Node.Tag.ToString()))
                            {
                                navigateToFolder(e.Node.Tag.ToString());
                            }
                            else
                            {
                                MessageBox.Show(string.Format("Access to the path \"{0}\" is denied.", e.Node.Tag.ToString()), "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                txtLocation.Text = oldPath;
                                navigateToFolder(txtLocation.Text, true);
                            }
                        }
                        else
                        {
                            MessageBox.Show(string.Format("The folder {0} could not be found.", e.Node.Tag.ToString()), "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            txtLocation.Text = oldPath;
                            navigateToFolder(txtLocation.Text, true);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(e.Node.Tag.ToString()) || string.IsNullOrWhiteSpace(e.Node.Tag.ToString()))
                        {
                            MessageBox.Show("Empty/whitespace-only path is not valid.", "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            txtLocation.Text = oldPath;
                            navigateToFolder(txtLocation.Text, true);
                        }
                        else
                        {
                            MessageBox.Show("Path format is invalid.", "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            txtLocation.Text = oldPath;
                            navigateToFolder(txtLocation.Text, true);
                        }
                    }
                }
                catch (Exception)
                {
                    txtLocation.Text = oldPath;
                    navigateToFolder(txtLocation.Text, true);
                }
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (navigationHistory.Count > 1 && navigationHistory.IndexOf(currentPath) > 0)
            {
                navigateToFolder(navigationHistory[navigationHistory.IndexOf(currentPath) - 1]);
            }
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            if (navigationHistory.Count > 1 && navigationHistory.IndexOf(currentPath) < navigationHistory.Count - 1)
            {
                navigateToFolder(navigationHistory[navigationHistory.IndexOf(currentPath) + 1]);
            }
        }

        private void menuItem15_Click_1(object sender, EventArgs e)
        {
            btnBack.PerformClick();
        }

        private void menuItem20_Click(object sender, EventArgs e)
        {
            btnForward.PerformClick();
        }

        /// <summary>
        /// Automatically enables/disables the "Back"/"Forward" buttons and menu items depending on whether the user is able to navigate back and/or forward, respectively.
        /// </summary>
        private void enableBackForwardTimer_Tick(object sender, EventArgs e)
        {
            btnBack.Enabled = navigationHistory.Count > 0 && navigationHistory.IndexOf(currentPath) > 0;
            btnForward.Enabled = navigationHistory.Count > 0 && navigationHistory.IndexOf(currentPath) < navigationHistory.Count - 1;
            menuItem15.Enabled = btnBack.Enabled;
            menuItem20.Enabled = btnForward.Enabled;
        }

        private void btnBack_Enter(object sender, EventArgs e)
        {
            label1.Focus();
        }

        private void lvFiles_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            lvFiles.LabelEdit = false;
            if (e.Label != null)
            {
                try
                {
                    if (lvFiles.FocusedItem.SubItems[1].Text == "Folder")
                    {
                        Directory.Move(listFiles[lvFiles.FocusedItem.Index], Path.Combine(currentPath, e.Label));
                    }
                    else
                    {
                        File.Move(listFiles[lvFiles.FocusedItem.Index], Path.Combine(currentPath, e.Label));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Cannot perform file operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            navigateToFolder(currentPath, true);
            UpdateFileTree();
        }
        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("System.Windows.Forms.ListView+SelectedListViewItemCollection", false))
            {
                Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
                TreeNode dn = ((TreeView)sender).GetNodeAt(pt);
                ListView.SelectedListViewItemCollection lvi = (ListView.SelectedListViewItemCollection)e.Data.GetData("System.Windows.Forms.ListView+SelectedListViewItemCollection");

                foreach (ListViewItem item in lvi)
                {
                    try
                    {
                        if (File.GetAttributes(listFiles[item.Index]).HasFlag(FileAttributes.Directory))
                        {
                            Directory.Move(listFiles[item.Index], (string)dn.Tag);
                        }
                        else
                        {
                            File.Move(listFiles[item.Index], Path.Combine((string)dn.Tag, Path.GetFileName(listFiles[item.Index])));
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Cannot perform file operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void lvFiles_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
    internal static class NativeMethods
    {
        public const uint LVM_FIRST = 0x1000;
        public const uint LVM_GETIMAGELIST = (LVM_FIRST + 2);
        public const uint LVM_SETIMAGELIST = (LVM_FIRST + 3);

        public const uint LVSIL_NORMAL = 0;
        public const uint LVSIL_SMALL = 1;
        public const uint LVSIL_STATE = 2;
        public const uint LVSIL_GROUPHEADER = 3;

        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd,
                                                uint msg,
                                                uint wParam,
                                                IntPtr lParam);

        [DllImport("comctl32")]
        public static extern bool ImageList_Destroy(IntPtr hImageList);

        public const uint SHGFI_DISPLAYNAME = 0x200;
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0;
        public const uint SHGFI_SMALLICON = 0x1;
        public const uint SHGFI_SYSICONINDEX = 0x4000;

        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260 /* MAX_PATH */)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [DllImport("shell32")]
        public static extern IntPtr SHGetFileInfo(string pszPath,
                                                  uint dwFileAttributes,
                                                  ref SHFILEINFO psfi,
                                                  uint cbSizeFileInfo,
                                                  uint uFlags);

        [DllImport("uxtheme", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd,
                                                string pszSubAppName,
                                                string pszSubIdList);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool MoveFileWithProgress(string lpExistingFileName, string lpNewFileName, CopyProgressRoutine lpProgressRoutine, IntPtr lpData, MoveFileFlags dwCopyFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CopyFileEx(string lpExistingFileName, string lpNewFileName, CopyProgressRoutine lpProgressRoutine, IntPtr lpData, ref int pbCancel, CopyFileFlags dwCopyFlags);

        internal delegate CopyProgressResult CopyProgressRoutine(long TotalFileSize, long TotalBytesTransferred, long StreamSize, long StreamBytesTransferred, uint dwStreamNumber, CopyProgressCallbackReason dwCallbackReason, IntPtr hSourceFile, IntPtr hDestinationFile, IntPtr lpData);

        internal enum CopyProgressResult : uint
        {
            PROGRESS_CONTINUE,
            PROGRESS_CANCEL,
            PROGRESS_STOP,
            PROGRESS_QUIET,
        }

        internal enum CopyProgressCallbackReason : uint
        {
            CALLBACK_CHUNK_FINISHED,
            CALLBACK_STREAM_SWITCH,
        }

        [Flags]
        internal enum MoveFileFlags : uint
        {
            MOVE_FILE_REPLACE_EXISTSING = 1,
            MOVE_FILE_COPY_ALLOWED = 2,
            MOVE_FILE_DELAY_UNTIL_REBOOT = 4,
            MOVE_FILE_WRITE_THROUGH = 8,
            MOVE_FILE_CREATE_HARDLINK = 16, // 0x00000010
            MOVE_FILE_FAIL_IF_NOT_TRACKABLE = 32, // 0x00000020
        }

        [Flags]
        internal enum CopyFileFlags : uint
        {
            COPY_FILE_FAIL_IF_EXISTS = 1,
            COPY_FILE_RESTARTABLE = 2,
            COPY_FILE_OPEN_SOURCE_FOR_WRITE = 4,
            COPY_FILE_ALLOW_DECRYPTED_DESTINATION = 8,
            COPY_FILE_COPY_SYMLINK = 2048, // 0x00000800
        }
    }
    public static class IconHelper
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObjetc);

        [DllImport("shell32")]
        private static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint flags);

        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        public static Icon GetIconOfPath(string path, bool isSmallIcon, bool isDirectoryOrDrive)
        {
            uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;
            if (isSmallIcon)
                flags |= SHGFI_SMALLICON;

            uint attributes = FILE_ATTRIBUTE_NORMAL;
            if (isDirectoryOrDrive)
                attributes |= FILE_ATTRIBUTE_DIRECTORY;

            int success = SHGetFileInfo(path, attributes, out SHFILEINFO shfi, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags);

            if (success == 0)
                return null;

            return Icon.FromHandle(shfi.hIcon);
        }
    }
}
