using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModPackagerUI
{
    public class MainForm : Form
    {
        private readonly TextBox _nameBox;
        private readonly TextBox _folderBox;
        private readonly TextBox _outBox;
        private readonly CheckBox _encryptBox;
        private readonly CheckBox _indexBox;
        private readonly TextBox _log;
        private readonly Button _goButton;
        private readonly Button _openButton;

        public MainForm()
        {
            Text = "Mod 打包工具 (ModPackager UI)";
            Font = new Font("Microsoft YaHei UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(560, 430);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 180,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(10, 10, 10, 0),
                ColumnStyles =
                {
                    new ColumnStyle(SizeType.Absolute, 110),
                    new ColumnStyle(SizeType.Percent, 100)
                },
                RowStyles =
                {
                    new RowStyle(SizeType.Absolute, 32),
                    new RowStyle(SizeType.Absolute, 32),
                    new RowStyle(SizeType.Absolute, 32),
                    new RowStyle(SizeType.Absolute, 28),
                    new RowStyle(SizeType.Absolute, 40)
                }
            };

            table.Controls.Add(new Label { Text = "Mod 名称:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 0);
            table.Controls.Add(_nameBox = new TextBox { Dock = DockStyle.Fill }, 1, 0);

            table.Controls.Add(new Label { Text = "Mod 文件夹:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 1);
            table.Controls.Add(MakeWithBrowse(_folderBox = new TextBox { Dock = DockStyle.Fill }, PickFolder, (box, path) =>
            {
                box.Text = path;
                // default the output directory to the parent of the selected folder
                if (string.IsNullOrWhiteSpace(_outBox.Text) && !string.IsNullOrEmpty(path))
                {
                    var parent = Path.GetDirectoryName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (!string.IsNullOrEmpty(parent))
                        _outBox.Text = parent;
                }
                // default the mod name to the folder name
                if (string.IsNullOrWhiteSpace(_nameBox.Text) && !string.IsNullOrEmpty(path))
                    _nameBox.Text = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }), 1, 1);

            table.Controls.Add(new Label { Text = "输出目录:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 2);
            table.Controls.Add(MakeWithBrowse(_outBox = new TextBox { Dock = DockStyle.Fill }, PickFolder, (box, path) => box.Text = path), 1, 2);

            var options = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            _encryptBox = new CheckBox { Text = "加密包 (AES-256)", AutoSize = true };
            _indexBox = new CheckBox { Text = "生成 index.json", AutoSize = true };
            options.Controls.Add(_encryptBox);
            options.Controls.Add(_indexBox);
            table.Controls.Add(new Label(), 0, 3);
            table.Controls.Add(options, 1, 3);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            _goButton = new Button { Text = "开始打包", AutoSize = true, MinimumSize = new Size(110, 0) };
            _goButton.Click += OnGoClicked;
            _openButton = new Button { Text = "打开输出文件夹", AutoSize = true, Enabled = false };
            _openButton.Click += (s, e) =>
            {
                var dir = _outBox.Text.Trim();
                if (Directory.Exists(dir))
                    Process.Start("explorer.exe", dir);
            };
            buttons.Controls.Add(_goButton);
            buttons.Controls.Add(_openButton);
            table.Controls.Add(new Label(), 0, 4);
            table.Controls.Add(buttons, 1, 4);

            _log = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            Controls.Add(_log);
            Controls.Add(table);
            table.SendToBack();

            void PickFolder(TextBox box, Action<TextBox, string> onPicked)
            {
                using var dlg = new FolderBrowserDialog { Description = "选择文件夹", ShowNewFolderButton = true };
                if (Directory.Exists(box.Text))
                    dlg.SelectedPath = box.Text;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    onPicked(box, dlg.SelectedPath);
            }
        }

        private static Panel MakeWithBrowse(TextBox box, Action<TextBox, Action<TextBox, string>> pickFolder, Action<TextBox, string> onPicked)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Height = box.Height };
            var browse = new Button { Text = "浏览...", Dock = DockStyle.Right, Width = 64 };
            browse.Click += (s, e) => pickFolder(box, onPicked);
            panel.Controls.Add(box);
            panel.Controls.Add(browse);
            return panel;
        }

        private void Log(string text)
        {
            _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        }

        private void OnGoClicked(object sender, EventArgs e)
        {
            var modName = _nameBox.Text.Trim();
            var sourceFolder = _folderBox.Text.Trim();
            var outDir = _outBox.Text.Trim();

            if (string.IsNullOrEmpty(modName))
            {
                MessageBox.Show(this, "请填写 Mod 名称。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // strip invalid file name characters
            modName = string.Join("", modName.Split(Path.GetInvalidFileNameChars())).Trim();

            if (string.IsNullOrEmpty(modName))
            {
                MessageBox.Show(this, "Mod 名称包含无效字符,请修改。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(sourceFolder) || !Directory.Exists(sourceFolder))
            {
                MessageBox.Show(this, "请选择需要打包的文件夹。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.EnumerateFileSystemEntries(sourceFolder).Any())
            {
                MessageBox.Show(this, "所选文件夹是空的,没有可打包的内容。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(outDir))
            {
                MessageBox.Show(this, "请填写输出目录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var encrypt = _encryptBox.Checked;
            var genIndex = _indexBox.Checked;
            var packageName = modName + ".mods";

            _goButton.Enabled = false;
            _log.Clear();

            Task.Run(() =>
            {
                try
                {
                    Directory.CreateDirectory(outDir);
                    var outFilePath = Path.Combine(outDir, packageName);
                    Packaging.BuildPackage(sourceFolder, outFilePath, encrypt, msg => Invoke(() => Log(msg)));

                    if (genIndex)
                    {
                        Packaging.WriteIndex(outDir, packageName);
                        Invoke(() => Log("已生成 index.json"));
                    }

                    Invoke(() =>
                    {
                        Log("打包完成!");
                        MessageBox.Show(this, $"打包完成!\n\n输出文件: {outFilePath}", "完成",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        _openButton.Enabled = true;
                    });
                }
                catch (Exception ex)
                {
                    Invoke(() =>
                    {
                        Log($"出错: {ex.Message}");
                        MessageBox.Show(this, $"打包失败:\n\n{ex.Message}", "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
                finally
                {
                    Invoke(() => _goButton.Enabled = true);
                }
            });
        }
    }
}
