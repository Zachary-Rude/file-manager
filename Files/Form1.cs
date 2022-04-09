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

namespace Files
{
    public partial class Form1 : Form
    {
        string storedPath;
        string currentPath;
        string newPath;
        string oldPath;
        bool txtLocationJustEntered = false;
        List<string> listFiles = new List<string>();

        public Form1()
        {
            InitializeComponent();
            txtLocation.Text = @"C:\";
            storedPath = txtLocation.Text;
            navigateToFolder(txtLocation.Text);
            listView.View = Properties.Settings.Default.FolderView;
            menuItem7.Checked = Properties.Settings.Default.LargeIcon;
            menuItem8.Checked = Properties.Settings.Default.SmallIcon;
            menuItem9.Checked = Properties.Settings.Default.Details;
            menuItem14.Checked = Properties.Settings.Default.List;
            menuItem16.Checked = Properties.Settings.Default.Tile;
            menuItem15.Checked = Properties.Settings.Default.ShowHiddenFiles;
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
            listView.View = Properties.Settings.Default.FolderView;
            menuItem7.Checked = Properties.Settings.Default.LargeIcon;
            menuItem8.Checked = Properties.Settings.Default.SmallIcon;
            menuItem9.Checked = Properties.Settings.Default.Details;
            menuItem14.Checked = Properties.Settings.Default.List;
            menuItem16.Checked = Properties.Settings.Default.Tile;
        }

        private void navigateToFolder(string path)
        {
            oldPath = txtLocation.Text;
            listFiles.Clear();
            listView.Items.Clear();
            DirectoryInfo nodeDirInfo = new DirectoryInfo(path);
            ListViewItem.ListViewSubItem[] subItems;
            ListViewItem item = null;

            foreach (DirectoryInfo dir in nodeDirInfo.GetDirectories()
                .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden) || Properties.Settings.Default.ShowHiddenFiles))
            {
                item = new ListViewItem(dir.Name, 0);
                subItems = new ListViewItem.ListViewSubItem[]
                    {new ListViewItem.ListViewSubItem(item, "Folder"),
             new ListViewItem.ListViewSubItem(item,
                dir.LastAccessTime.ToShortDateString())};
                item.SubItems.AddRange(subItems);
                listView.Items.Add(item);
                listFiles.Add(dir.FullName);
            }
            foreach (FileInfo file in nodeDirInfo.GetFiles()
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) || Properties.Settings.Default.ShowHiddenFiles))
            {
                string fileType;
                bool showExtension = true;
                string fileName;
                int fileIndex = 1;
                if (file.Extension == ".py")
                {
                    fileIndex = 2;
                    fileType = "Python File";
                    showExtension = true;
                }
                else if (file.Extension == ".zip")
                {
                    fileIndex = 3;
                    fileType = "Zipfile";
                    showExtension = true;
                }
                else if (file.Extension == ".rar")
                {
                    fileType = "RAR Archive";
                    showExtension = true;
                }
                else if (file.Extension == ".png")
                {
                    fileType = "PNG Image";
                    showExtension = true;
                }
                else if (file.Extension == ".jpg" || file.Extension == ".jpeg" || file.Extension == ".jfif")
                {
                    fileType = "JPEG Image";
                    showExtension = true;
                }
                else if (file.Extension == ".gif")
                {
                    fileType = "GIF Image";
                    showExtension = true;
                }
                else if (file.Extension == ".bmp")
                {
                    fileType = "Bitmap Image";
                    showExtension = true;
                }
                else if (file.Extension == ".webp")
                {
                    fileType = "WebP Image";
                    showExtension = true;
                }
                else if (file.Extension == ".svg")
                {
                    fileType = "Scalable Vector Graphics";
                    showExtension = true;
                }
                else if (file.Extension == ".cur")
                {
                    fileType = "Static Cursor";
                    showExtension = true;
                }
                else if (file.Extension == ".ani")
                {
                    fileType = "Animated Cursor";
                    showExtension = true;
                }
                else if (file.Extension == ".txt")
                {
                    fileType = "Text File";
                    showExtension = true;
                }
                else if (file.Extension == ".log")
                {
                    fileType = "Log File";
                    showExtension = true;
                }
                else if (file.Extension == ".ico")
                {
                    fileType = "Windows Icon";
                    showExtension = true;
                }
                else if (file.Extension == ".lnk")
                {
                    fileType = "Windows Shortcut";
                    showExtension = false;
                }
                else if (file.Extension == ".appref-ms")
                {
                    fileType = "ClickOnce Application Reference";
                    showExtension = false;
                }
                else if (file.Extension == ".sb3")
                {
                    fileType = "Scratch 3.0 Project";
                    showExtension = true;
                }
                else if (file.Extension == ".sb2")
                {
                    fileType = "Scratch 2.0 Project";
                    showExtension = true;
                }
                else if (file.Extension == ".sb")
                {
                    fileType = "Scratch Project";
                    showExtension = true;
                }
                else if (file.Extension == ".exe")
                {
                    fileType = "Windows Executable";
                    showExtension = true;
                }
                else if (file.Extension == ".msi")
                {
                    fileType = "Installer";
                    showExtension = true;
                }
                else if (file.Extension == ".com")
                {
                    fileType = "DOS App";
                    showExtension = true;
                }
                else if (file.Extension == ".url")
                {
                    fileType = "Internet Shortcut";
                    showExtension = false;
                }
                else if (file.Extension == ".mp3")
                {
                    fileType = "MP3 Audio";
                    showExtension = true;
                }
                else if (file.Extension == ".mp4")
                {
                    fileType = "MP4 Video";
                    showExtension = true;
                }
                else if (file.Extension == ".mpeg")
                {
                    fileType = "MPEG Media";
                    showExtension = true;
                }
                else if (file.Extension == ".wav")
                {
                    fileType = "Wave Audio";
                    showExtension = true;
                }
                else if (file.Extension == ".mid" || file.Extension == ".midi")
                {
                    fileType = "MIDI Audio";
                    showExtension = true;
                }
                else if (file.Extension == ".sln")
                {
                    fileType = "Visual Studio Solution File";
                    showExtension = true;
                }
                else if (file.Extension == ".csproj")
                {
                    fileType = "Visual Studio C# Project";
                    showExtension = true;
                }
                else if (file.Extension == ".vbproj")
                {
                    fileType = "Visual Studio VB.NET Project";
                    showExtension = true;
                }
                else if (file.Extension == ".dll")
                {
                    fileType = "Dynamic Link Library";
                    showExtension = true;
                }
                else if (file.Extension == ".cs")
                {
                    fileIndex = 4;
                    fileType = "C# Source File";
                    showExtension = true;
                }
                else if (file.Extension == ".bat" || file.Extension == ".cmd")
                {
                    fileType = "Batch File";
                    showExtension = true;
                }
                else if (file.Extension == ".sh")
                {
                    fileType = "Shell Script";
                    showExtension = true;
                }
                else if (file.Extension == ".iss")
                {
                    fileType = "Inno Setup Script";
                    showExtension = true;
                }
                else if (file.Extension == ".ini")
                {
                    fileType = "Windows Configuration File";
                    showExtension = true;
                }
                else if (file.Extension == ".reg")
                {
                    fileType = "Windows Registry Entry";
                    showExtension = true;
                }
                else if (file.Extension == ".html" || file.Extension == ".htm")
                {
                    fileIndex = 6;
                    fileType = "HTML Document";
                    showExtension = true;
                }
                else if (file.Extension == ".js")
                {
                    fileIndex = 7;
                    fileType = "JavaScript File";
                    showExtension = true;
                }
                else if (file.Extension == ".pdf")
                {
                    fileIndex = 8;
                    fileType = "PDF Document";
                    showExtension = true;
                }
                else if (file.Extension == ".css")
                {
                    fileIndex = 5;
                    fileType = "CSS File";
                    showExtension = true;
                }
                else
                {
                    fileIndex = 1;
                    if (file.Extension != "")
                    {
                        fileType = file.Extension.Replace(".", "").ToUpper() + " File";
                        showExtension = true;
                    }
                    else
                    {
                        fileType = "File";
                        showExtension = true;
                    }
                }
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
                item = new ListViewItem(fileName, fileIndex);
                subItems = new ListViewItem.ListViewSubItem[]
                    { new ListViewItem.ListViewSubItem(item, fileType),
             new ListViewItem.ListViewSubItem(item,
                file.LastAccessTime.ToShortDateString()),
             new ListViewItem.ListViewSubItem(item, result)};
                item.ToolTipText = fileName + "\r\nType: " + fileType + "\r\nSize: " + result + "\r\nDate modified: " + file.LastAccessTime.ToShortDateString();
                item.SubItems.AddRange(subItems);
                listView.Items.Add(item);
                listFiles.Add(file.FullName);
            }

            listView.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.None);
            listView.Columns[0].Width = 165;
            listView.Columns[1].Width = 105;
            listView.Columns[3].Width = 70;
            currentPath = path;
            txtLocation.Text = currentPath;
            this.Text = "Index of " + currentPath + " - Files";
            if (Properties.Settings.Default.RecentFolders.Count > 5)
            {
                Properties.Settings.Default.RecentFolders.RemoveAt(0);
            }
            Properties.Settings.Default.RecentFolders.Add(currentPath);
            Properties.Settings.Default.Save();
            UpdateRecentFolderList();
        }

        private void listView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView.FocusedItem != null)
            {
                if (listView.FocusedItem.SubItems[1].Text == "Folder")
                {
                    try
                    {
                        newPath = listFiles[listView.FocusedItem.Index];
                        navigateToFolder(newPath);
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
                        newPath = listFiles[listView.FocusedItem.Index];
                        Process.Start(newPath);
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
                        txtLocation.Text = fbd.SelectedPath;
                        navigateToFolder(txtLocation.Text);
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

        private void txtLocation_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    if (Directory.Exists(Path.GetFullPath(txtLocation.Text)))
                    {
                        navigateToFolder(Path.GetFullPath(txtLocation.Text));
                    }
                    else
                    {
                        MessageBox.Show(string.Format("The folder {0} could not be found.", txtLocation.Text), "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txtLocation.Text = oldPath;
                        navigateToFolder(txtLocation.Text);
                    }
                        
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Location is not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtLocation.Text = oldPath;
                    navigateToFolder(txtLocation.Text);
                }
                e.SuppressKeyPress = true;
            }
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Process.Start(ofd.FileName);
                }
            }
        }

        private void newFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DirectoryInfo di = Directory.CreateDirectory(txtLocation.Text + @"\New folder");
            expTree1.Refresh();
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
            File.Create(Path.Combine(currentPath, "Untitled.txt"));
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
            if (listView.FocusedItem != null)
            {
                if (listView.FocusedItem.SubItems[1].Text == "Folder")
                {
                    try
                    {
                        newPath = listFiles[listView.FocusedItem.Index];
                        navigateToFolder(newPath);
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
                        newPath = listFiles[listView.FocusedItem.Index];
                        Process.Start(newPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Cannot open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void openWithToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView.FocusedItem != null)
            {
                newPath = listFiles[listView.FocusedItem.Index];
                ProcessStartInfo p = new ProcessStartInfo();
                p.FileName = newPath;
                p.ErrorDialog = true;
                Process.Start(p);
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listView.FocusedItem != null)
            {
                if (listView.FocusedItem.SubItems[1].Text == "Folder")
                {
                    openInNewWindowToolStripMenuItem.Visible = true;
                    copyToolStripMenuItem.Visible = false;
                }
                else
                {
                    openInNewWindowToolStripMenuItem.Visible = false;
                    copyToolStripMenuItem.Visible = true;
                }
                openToolStripMenuItem.Visible = true;
                toolStripSeparator1.Visible = true;
                newToolStripMenuItem.Visible = true;
                toolStripSeparator2.Visible = true;
                deleteToolStripMenuItem.Visible = true;
                renameToolStripMenuItem.Visible = true;
                cutToolStripMenuItem.Visible = true;
            }
            else
            {
                openInNewWindowToolStripMenuItem.Visible = false;
                openToolStripMenuItem.Visible = false;
                toolStripSeparator1.Visible = false;
                newToolStripMenuItem.Visible = true;
                copyToolStripMenuItem.Visible = false;
                toolStripSeparator2.Visible = false;
                deleteToolStripMenuItem.Visible = false;
                renameToolStripMenuItem.Visible = false;
                cutToolStripMenuItem.Visible = false;
            }
            e.Cancel = false;
        }

        private void openInNewWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView.FocusedItem != null)
            {
                Process.Start(Application.ExecutablePath, listFiles[listView.FocusedItem.Index]);
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
            if (listView.FocusedItem != null)
            {
                try
                {
                    File.Copy(listFiles[listView.FocusedItem.Index], GetUniqueFilePath(Path.Combine(currentPath, Path.GetFileName(listFiles[listView.FocusedItem.Index]))));
                    navigateToFolder(currentPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Cannot perform file operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView.FocusedItem != null)
            {
                try
                {
                    if (listView.FocusedItem.SubItems[1].Text == "Folder")
                    {
                        Directory.Delete(listFiles[listView.FocusedItem.Index], true);
                    }
                    else
                    {
                        File.Delete(listFiles[listView.FocusedItem.Index]);
                    }
                    navigateToFolder(currentPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Cannot perform file operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView.FocusedItem != null)
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog() { Description = "Select the folder where you want the file to be cut to.", SelectedPath = currentPath, ShowNewFolderButton = false })
                {
                   if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            if (listView.FocusedItem.SubItems[1].Text == "Folder")
                            {
                                Directory.Move(listFiles[listView.FocusedItem.Index], Path.Combine(fbd.SelectedPath, Path.GetFileName(listFiles[listView.FocusedItem.Index])));
                                navigateToFolder(currentPath);
                            }
                            else
                            {
                                File.Move(listFiles[listView.FocusedItem.Index], Path.Combine(fbd.SelectedPath, Path.GetFileName(listFiles[listView.FocusedItem.Index])));
                                navigateToFolder(currentPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Cannot perform file operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView.FocusedItem != null)
            {
                using (RenameForm renameForm = new RenameForm())
                {
                    renameForm.txtFileName.Text = Path.GetFileName(listFiles[listView.FocusedItem.Index]);
                    if (renameForm.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            if (listView.FocusedItem.SubItems[1].Text == "Folder")
                            {
                                Directory.Move(listFiles[listView.FocusedItem.Index], Path.Combine(currentPath, renameForm.txtFileName.Text));
                                navigateToFolder(currentPath);
                            }
                            else
                            {
                                File.Move(listFiles[listView.FocusedItem.Index], Path.Combine(currentPath, renameForm.txtFileName.Text));
                                navigateToFolder(currentPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Cannot perform file operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void menuItem15_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.ShowHiddenFiles = !Properties.Settings.Default.ShowHiddenFiles;
            Properties.Settings.Default.Save();
            menuItem15.Checked = Properties.Settings.Default.ShowHiddenFiles;
            expTree1.ShowHiddenFolders = Properties.Settings.Default.ShowHiddenFiles;
            navigateToFolder(currentPath);
        }

        private void createFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Directory.CreateDirectory(Path.Combine(currentPath, "New folder"));
            navigateToFolder(currentPath);
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

        private static string GetUniqueFilePath(string filePath)
        {
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

        private void menuItem17_Click(object sender, EventArgs e)
        {
            navigateToFolder(Path.GetFullPath(Path.Combine(currentPath, @"..")));
        }

        private void recentFolder_Click(object sender, EventArgs e)
        {
            navigateToFolder(((MenuItem)sender).Text);
        }

        private void menuClear_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.RecentFolders.Clear();
            Properties.Settings.Default.Save();
            UpdateRecentFolderList();
        }
    }
}
