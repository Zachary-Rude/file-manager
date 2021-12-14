using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace CSharpFileExplorer
{
    [ToolboxItem(true)]
    public sealed class FileExplorer : TreeView
    {
        private readonly Timer _toolTimer;
        private readonly ToolTip _toolTip;
        private readonly List<FileSystemWatcher> _watchers;
        private bool _disposed;

        [ToolboxItem("Show ToolTips")] public bool ShowToolTips { get; set; } = true;

        [ToolboxItem("Filtering Extensions")] public bool FilteringExtensions { get; set; } = false;

        [ToolboxItem("Bookmark Color")] public Color BookMarkColor { get; set; } = Color.FromArgb(0xFFFFCB);

        [ToolboxItem("Include File Extensions")]
        public bool IncludeExtensions { get; set; } = true;

        public List<string> Extensions { get; set; }

        public List<string> Bookmarks { get; set; }

        public Action<object, TreeNodeMouseClickEventArgs> SelectionAction { get; set; }

        public ContextMenuStrip NodeContextMenuStrip { get; set; }

        public ContextMenuStrip TreeContextMenuStrip { get; set; }

        public FileExplorer()
        {
            _toolTimer = new Timer();
            _toolTip = new ToolTip();
            _watchers = new List<FileSystemWatcher>();
            Bookmarks = new List<string>();
            Extensions = new List<string>();
            MouseDown += ScriptTreeView_MouseDown;
            MouseHover += ScriptTreeView_MouseHover;

            if (ShowToolTips)
            {
                NodeMouseHover += ScriptTreeView_OnNodeMouseHover;
                _toolTimer.Elapsed += Timer_Elapsed;
            }

            DragDrop += ScriptTreeView_DragDrop;
            BeforeExpand += ScriptTreeView_BeforeExpand;
            NodeMouseClick += ScriptTreeView_NodeMouseClick;

            AfterCollapse += (q, e) =>
            {
                e.Node.ImageKey = "folder_closed";
                e.Node.SelectedImageKey = "folder_closed";
                e.Node.Nodes.Clear();
                AddDecoyNode(e.Node);
            };

            DragEnter += (q, e) => e.Effect = DragDropEffects.Move;
            ItemDrag += (q, e) => DoDragDrop(e.Item, DragDropEffects.Move);
            BuildContextMenuSettings();
            LoadIcons();
            NativeMethods.SetWindowTheme(Handle, "explorer", null);

            AllowDrop = true;
        }

        private void ScriptTreeView_MouseHover(object sender, EventArgs e)
        {
            _toolTip.Hide(this);
            _toolTimer.Stop();
            _toolTimer.Start();
            _toolTimer.Interval = 2000;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _toolTimer.Stop();

            Invoke(new MethodInvoker(delegate
            {
                var mousePos = PointToClient(MousePosition);
                var selectedNode = GetNodeAt(mousePos);
                mousePos.Y += 10;
                mousePos.X += 10;

                if (selectedNode != null)
                {
                    if (IsDirectory(selectedNode.Name))
                    {
                        var dirInfo = new DirectoryInfo(selectedNode.Name);
                        double dirSize = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Sum(file => file.Length);

                        string dirSizeString;

                        if (dirSize >= Math.Pow(10, 9))
                            dirSizeString = (dirSize / Math.Pow(10, 9)).ToString("N1") + " GB";
                        else if (dirSize >= Math.Pow(10, 6))
                            dirSizeString = (dirSize / Math.Pow(10, 6)).ToString("N1") + " MB";
                        else if (dirSize >= Math.Pow(10, 3))
                            dirSizeString = (dirSize / Math.Pow(10, 3)).ToString("N1") + " KB";
                        else
                            dirSizeString = $"{dirSize} B";

                        _toolTip.Show($"Date Created: {File.GetCreationTime(selectedNode.Name).ToLocalTime():g}\n" +
                                      $"Size: {dirSizeString}\n", this, mousePos);
                    }
                    else
                    {
                        var toolDisplayTip = string.Empty;

                        double fileSize = new FileInfo(selectedNode.Name).Length;
                        var fileInfo = FileVersionInfo.GetVersionInfo(selectedNode.Name);

                        var fileSizeString = string.Empty;

                        if (fileSize >= Math.Pow(10, 9))
                            fileSizeString = (fileSize / Math.Pow(10, 9)).ToString("N1") + " GB";
                        else if (fileSize >= Math.Pow(10, 6))
                            fileSizeString = (fileSize / Math.Pow(10, 6)).ToString("N1") + " MB";
                        else if (fileSize >= Math.Pow(10, 3))
                            fileSizeString = (fileSize / Math.Pow(10, 3)).ToString("N1") + " KB";
                        else
                            fileSizeString = $"{fileSize} B";

                        if (fileInfo.FileDescription != null)
                            toolDisplayTip += $"File Description: {fileInfo.FileDescription}\n";
                        if (fileInfo.FileVersion != null)
                            toolDisplayTip += $"File Version: {fileInfo.FileVersion}\n";

                        toolDisplayTip += $"Date Created: {File.GetCreationTime(selectedNode.Name).ToLocalTime():g}\n" +
                                          $"Size: {fileSizeString}\n";

                        _toolTip.Show(toolDisplayTip, this, mousePos);
                    }

                    _toolTimer.Stop();
                    _toolTimer.Start();
                    _toolTimer.Interval = 5000;
                }
                else
                {
                    _toolTip.Hide(this);
                }
            }));
        }

        private void ScriptTreeView_OnNodeMouseHover(object sender, TreeNodeMouseHoverEventArgs e)
        {
            _toolTip.Hide(this);
            _toolTimer.Stop();
            _toolTimer.Start();
            _toolTimer.Interval = 2000;
        }

        public string ReadSelectedNode()
        {
            if (File.Exists(SelectedNode.Name)) return File.ReadAllText(SelectedNode.Name);

            return string.Empty;
        }

        public void PopulateView(string inputFolder)
        {
            if (inputFolder.Equals(string.Empty))
                throw new Exception("Failed to provide a folder path! Please set a folder path value!");

            if (!Directory.Exists(inputFolder)) throw new Exception("Invalid folder path! Does not exist!");

            var node = Nodes.Add(Path.GetFileName(inputFolder));
            node.Name = inputFolder;
            SetNodeIcon(inputFolder, node);
            AddDecoyNode(node);
            BuildFileWatcher(inputFolder);
        }

        private void ScriptTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            _toolTip.Hide(this);
            _toolTimer.Stop();
            _toolTimer.Start();
            _toolTimer.Interval = 3500;

            RemoveDecoyNode(e.Node);
            var fileArray = Directory.GetFiles(e.Node.Name);
            var directoryArray = Directory.GetDirectories(e.Node.Name);

            if (fileArray.Length == 0 && directoryArray.Length == 0)
            {
                AddDecoyNode(e.Node);
                e.Cancel = true;
                return;
            }

            foreach (var subdirectory in directoryArray)
                if (!e.Node.Nodes.ContainsKey(subdirectory))
                {
                    var fileNode = CreateFileNode(e.Node, subdirectory, Path.GetFileName(subdirectory));
                    AddDecoyNode(fileNode);
                }

            foreach (var fileName in fileArray)
                if (!e.Node.Nodes.ContainsKey(fileName))
                {
                    var displayName = IncludeExtensions
                        ? Path.GetFileName(fileName)
                        : Path.GetFileNameWithoutExtension(fileName);

                    if (FilteringExtensions)
                    {
                        if (Extensions.Contains(Path.GetExtension(fileName)))
                            CreateFileNode(e.Node, fileName, displayName);
                    }
                    else
                    {
                        CreateFileNode(e.Node, fileName, displayName);
                    }
                }

            e.Node.ImageKey = "folder_open";
            e.Node.SelectedImageKey = "folder_open";
        }

        private void DisableAllFileWatchers()
        {
            _watchers.ForEach(prop => prop.EnableRaisingEvents = false);
        }

        private void EnableAllFileWatchers()
        {
            _watchers.ForEach(prop => prop.EnableRaisingEvents = true);
        }

        private void BuildFileWatcher(string path)
        {
            var watcher = new FileSystemWatcher(path);
            _watchers.Add(watcher);
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            watcher.Renamed += (q, e) =>
            {
                Invoke(new MethodInvoker(delegate
                {
                    var modifiedNode = Nodes.Find(e.OldFullPath, true);

                    if (modifiedNode.Length == 1)
                    {
                        modifiedNode[0].Name = e.FullPath;
                        modifiedNode[0].Text = Path.GetFileName(e.FullPath);
                    }
                }));
            };

            watcher.Deleted += (q, e) =>
            {
                Invoke(new MethodInvoker(delegate
                {
                    var modifiedNode = Nodes.Find(e.FullPath, true);

                    if (modifiedNode.Length == 1) modifiedNode[0].Remove();
                }));
            };

            watcher.Created += (q, e) =>
            {
                Invoke(new MethodInvoker(delegate
                {
                    var modifiedNode = Nodes.Find(Path.GetDirectoryName(e.FullPath), true);

                    if (modifiedNode.Length == 1)
                        CreateFileNode(modifiedNode[0], e.FullPath, Path.GetFileName(e.FullPath));
                }));
            };
        }

        private TreeNode CreateFileNode(TreeNode baseNode, string path, string text)
        {
            var node = baseNode.Nodes.Add(path, text);
            node.Name = path;
            SetNodeIcon(node.Name, node);

            if (Bookmarks.Contains(node.Name))
                node.BackColor = BookMarkColor;

            return node;
        }

        private void ScriptTreeView_DragDrop(object sender, DragEventArgs e)
        {
            var targetPoint = PointToClient(new Point(e.X, e.Y));
            var targetNode = GetNodeAt(targetPoint);
            var draggedNode = (TreeNode) e.Data.GetData(typeof(TreeNode));

            if (draggedNode == null) return;

            if (targetNode == null)
            {
                draggedNode.Remove();
                Nodes.Add(draggedNode);
                draggedNode.Expand();
            }
            else
            {
                if (IsDirectory(targetNode.Name))
                {
                    var parentNode = targetNode;

                    if (!draggedNode.Equals(targetNode))
                    {
                        var canDrop = true;

                        while (canDrop && parentNode != null)
                        {
                            canDrop = !ReferenceEquals(draggedNode, parentNode);
                            parentNode = parentNode.Parent;
                        }

                        if (canDrop)
                        {
                            DisableAllFileWatchers();
                            var oldFilepath = draggedNode.Name;
                            var newFilePath = targetNode.Name + "\\" + draggedNode.Text;


                            if (IsDirectory(draggedNode.Name))
                                Directory.Move(oldFilepath, newFilePath);
                            else
                                File.Move(oldFilepath, newFilePath);

                            draggedNode.Name = newFilePath;
                            draggedNode.Remove();
                            targetNode.Nodes.Add(draggedNode);
                            targetNode.Expand();
                            DisableAllFileWatchers();
                        }
                    }
                }
            }

            SelectedNode = draggedNode;
        }

        private void AddDecoyNode(TreeNode baseNode)
        {
            baseNode.Nodes.Add("stub");
        }

        private void RemoveDecoyNode(TreeNode baseNode)
        {
            baseNode.Nodes[0].Remove();
        }

        private bool IsDirectory(string filePath)
        {
            var attr = File.GetAttributes(filePath);
            return attr.HasFlag(FileAttributes.Directory);
        }

        private void BuildContextMenuSettings()
        {
            TreeContextMenuStrip = new ContextMenuStrip();
            NodeContextMenuStrip = new ContextMenuStrip();

            TreeContextMenuStrip.Items.Add("Add Directory").Click += (q, e) =>
            {
                DisableAllFileWatchers();
                using (var dialog = new FolderBrowserDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK) PopulateView(dialog.SelectedPath);
                }

                EnableAllFileWatchers();
            };

            NodeContextMenuStrip.Items.Add("Bookmark/Remove Bookmark").Click += (q, e) =>
            {
                if (SelectedNode != null)
                {
                    if (!Bookmarks.Contains(SelectedNode.Name))
                    {
                        SelectedNode.BackColor = BookMarkColor;
                        Bookmarks.Add(SelectedNode.Name);
                    }
                    else
                    {
                        SelectedNode.BackColor = BackColor;
                        Bookmarks.Remove(SelectedNode.Name);
                    }
                }
            };


            NodeContextMenuStrip.Items.Add("Create File").Click += (q, e) =>
            {
                if (IsDirectory(SelectedNode.Name))
                {
                    TreeNode node = null;

                    if (SelectedNode.Nodes.Count == 1)
                        node = SelectedNode.Nodes[0];
                    else
                        node = SelectedNode.Nodes.Add("stub");

                    BeforeExpand -= ScriptTreeView_BeforeExpand;
                    SelectedNode.Expand();
                    BeforeExpand += ScriptTreeView_BeforeExpand;
                    var box = new TextBox();
                    box.Size = new Size(100, 5);
                    box.Location = new Point(node.Bounds.X + 2, node.Bounds.Y - 2);
                    Controls.Add(box);
                    box.Select();

                    box.KeyDown += (n, t) =>
                    {
                        if (t.KeyCode == Keys.Enter)
                        {
                            node.Text = box.Text;
                            node.Name = node.Parent.Name + $"\\{node.Text}";
                            DisableAllFileWatchers();
                            File.Create(node.Name).Close();
                            EnableAllFileWatchers();
                            Controls.Remove(box);
                            box.Dispose();
                            SetNodeIcon(node.Name, node);
                        }
                    };
                }
            };

            NodeContextMenuStrip.Items.Add("Create Directory").Click += (q, e) =>
            {
                if (IsDirectory(SelectedNode.Name))
                {
                    BeforeExpand -= ScriptTreeView_BeforeExpand;
                    TreeNode node = null;

                    if (SelectedNode.Nodes.Count == 1)
                        node = SelectedNode.Nodes[0];
                    else
                        node = SelectedNode.Nodes.Add("stub");

                    SelectedNode.Expand();
                    BeforeExpand += ScriptTreeView_BeforeExpand;
                    var box = new TextBox();
                    box.Size = new Size(100, 5);
                    box.Location = new Point(node.Bounds.X + 2, node.Bounds.Y - 2);
                    Controls.Add(box);
                    box.Select();

                    box.KeyDown += (n, t) =>
                    {
                        if (t.KeyCode == Keys.Enter)
                        {
                            node.Text = box.Text;
                            node.Name = node.Parent.Name + $"\\{node.Text}";
                            DisableAllFileWatchers();
                            Directory.CreateDirectory(node.Name);
                            EnableAllFileWatchers();
                            Controls.Remove(box);
                            box.Dispose();
                            SetNodeIcon(node.Name, node);
                        }
                    };
                }
            };

            NodeContextMenuStrip.Items.Add("Delete").Click += (q, e) =>
            {
                if (!IsDirectory(SelectedNode.Name))
                {
                    var dialogResult = MessageBox.Show("Are you sure you wish to delete this file?", "Warning!",
                        MessageBoxButtons.YesNo);

                    if (dialogResult == DialogResult.Yes)
                    {
                        DisableAllFileWatchers();
                        File.Delete(SelectedNode.Name);
                        EnableAllFileWatchers();
                        SelectedNode.Remove();

                        if (SelectedNode.Nodes.Count == 0)
                        {
                            AddDecoyNode(SelectedNode);
                            SelectedNode.Collapse();
                        }
                    }
                }
                else
                {
                    var dialogResult = MessageBox.Show("Are you sure you wish to delete this directory?", "Warning!",
                        MessageBoxButtons.YesNo);

                    if (dialogResult == DialogResult.Yes)
                    {
                        var directoryInfo = new DirectoryInfo(SelectedNode.Name);
                        DisableAllFileWatchers();
                        directoryInfo.Delete(true);
                        EnableAllFileWatchers();
                        SelectedNode.Remove();

                        if (SelectedNode.Nodes.Count == 0)
                        {
                            AddDecoyNode(SelectedNode);
                            SelectedNode.Collapse();
                        }
                    }
                }
            };

            NodeContextMenuStrip.Items.Add("Remove").Click += (q, e) => { SelectedNode.Remove(); };
        }

        private void ScriptTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Right) != 0)
                if (SelectedNode == null || GetNodeAt(e.X, e.Y) == null)
                    TreeContextMenuStrip.Show(((Control) sender).PointToScreen(new Point(e.X, e.Y)));
        }

        private void ScriptTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            SelectedNode = e.Node;

            if ((e.Button & MouseButtons.Right) != 0)
                NodeContextMenuStrip.Show(((Control) sender).PointToScreen(new Point(e.X, e.Y)));
            else if ((e.Button & MouseButtons.Left) != 0) SelectionAction(sender, e);
        }

        private void LoadIcons()
        {
            ImageList = new ImageList();

            ImageList.Images.Add(new Bitmap(1, 1)); //DEFAULT = NULL PIC
            ImageList.Images.Add("folder_closed", IconLoader.GetStockIcon(NativeMethods.SHSTOCKICONID.SIID_FOLDERBACK));
            ImageList.Images.Add("folder_open", IconLoader.GetStockIcon(NativeMethods.SHSTOCKICONID.SIID_FOLDER));
            ImageList.Images.Add(".txt", IconLoader.GetFileTypeIcon(".txt"));
            ImageList.Images.Add(".lua", IconLoader.GetFileTypeIcon(".lua"));
            ImageList.Images.Add(".json", IconLoader.GetFileTypeIcon(".json"));
            ImageList.Images.Add(".xml", IconLoader.GetFileTypeIcon(".xml"));
            ImageList.Images.Add(".js", IconLoader.GetFileTypeIcon(".js"));
            ImageList.Images.Add(".cpp", IconLoader.GetFileTypeIcon(".cpp"));
            ImageList.Images.Add(".jpg", IconLoader.GetFileTypeIcon(".jpg"));
            ImageList.Images.Add(".png", IconLoader.GetFileTypeIcon(".png"));
            ImageList.Images.Add(".gif", IconLoader.GetFileTypeIcon(".gif"));
            ImageList.Images.Add(".dll", IconLoader.GetFileTypeIcon(".dll"));
            ImageList.Images.Add(".config", IconLoader.GetFileTypeIcon(".config"));
            ImageList.Images.Add(".cs", IconLoader.GetFileTypeIcon(".cs"));
        }


        private void SetNodeIcon(string fileName, TreeNode fileNode)
        {
            if (IsDirectory(fileName))
            {
                fileNode.ImageKey = "folder_closed";
                fileNode.SelectedImageKey = "folder_closed";
                return;
            }

            var extension = Path.GetExtension(fileName);

            if (extension.Equals(".exe"))
            {
                var icon = Icon.ExtractAssociatedIcon(fileName);
                if (icon != null)
                {
                    var iconBitmap = icon.ToBitmap();
                    iconBitmap.MakeTransparent();
                    ImageList.Images.Add(fileName, iconBitmap);
                    fileNode.ImageKey = fileName;
                    fileNode.SelectedImageKey = fileName;
                }
            }
            else if (!ImageList.Images.ContainsKey(extension))
            {
                var icon = IconLoader.GetFileTypeIcon(extension);
                ImageList.Images.Add(extension, icon);
                fileNode.ImageKey = extension;
                fileNode.SelectedImageKey = extension;
            }
            else
            {
                fileNode.ImageKey = extension;
                fileNode.SelectedImageKey = extension;
            }
        }

        public new void Dispose()
        {
            if (!_disposed)
            {
                TreeContextMenuStrip.Dispose();
                NodeContextMenuStrip.Dispose();

                foreach (var watcher in _watchers)
                    watcher.Dispose();

                _disposed = true;
            }

            base.Dispose();
        }
    }
}