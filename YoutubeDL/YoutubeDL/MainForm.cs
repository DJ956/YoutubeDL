using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoLibrary;
using System.IO;
using System.Linq;

namespace YoutubeDL
{
    public partial class MainForm : Form
    {
        private static readonly string DEFAULT = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "Youtube");

        private static readonly char[] INVALID_CHARS = Path.GetInvalidFileNameChars();

        private YouTube youTube;
        private int maxCount;
        private string videoTitle;

        public MainForm()
        {
            InitializeComponent();

            youTube = YouTube.Default;

            listViewVideos.Scrollable = true;
            listViewVideos.View = View.Details;

            var header = new ColumnHeader();
            header.Text = "URL";
            header.Name = "url";
            header.Width = listViewVideos.Width;
            listViewVideos.Columns.Add(header);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void ButtonFolder_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if(dialog.ShowDialog() == DialogResult.OK)
            {
                textBoxFolder.Text = dialog.SelectedPath;
            }
        }

        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            
            var url = textBoxURL.Text;
            if(url == "" && !Clipboard.ContainsText()) { return; }
            else if(url == "" && Clipboard.ContainsText()) { url = Clipboard.GetText(); }

            listViewVideos.Items.Add(url);
            textBoxURL.Text = "";
        }

        private void ButtonClear_Click(object sender, EventArgs e)
        {
            listViewVideos.Items.Clear();
        }

        private async void ButtonSave_Click(object sender, EventArgs e)
        {
            var root = "";
            if (textBoxFolder.Text == "")
            {
                root = DEFAULT;
                if (!Directory.Exists(DEFAULT)) { Directory.CreateDirectory(DEFAULT); }
            }
            else { root = textBoxFolder.Text; }

            labelProgress.Text = "ダウンロード開始...";
            var progress = new Progress<int>(UpdateProgress);
            var list = new List<string>();
            foreach(ListViewItem item in listViewVideos.Items) { list.Add(item.Text); }

            await Task.Run(() => SaveVideos(root, list, progress));

            MessageBox.Show($"保存しました:{root}", "ダウンロード完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            buttonClear.PerformClick();
        }

        private void SaveVideos(string root, List<string> items, IProgress<int> progress)
        {
            var buffer = new byte[4096];
            foreach (var item in items)
            {
                YouTubeVideo video = null;
                try
                {
                    video = youTube.GetVideo(item);
                }catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, $"Not found {item}", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    continue;
                }

                videoTitle = video.Title;
                videoTitle = string.Concat(videoTitle.Select(c => INVALID_CHARS.Contains(c) ? '_' : c));
                
                var fileName = $"{videoTitle}.{video.FileExtension}";
                var path = Path.Combine(root, fileName);
                                
                using (var memory = new MemoryStream(video.GetBytes()))
                using (var writer = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                {
                    maxCount = (int)memory.Length;
                    while (memory.Position < memory.Length)
                    {
                        memory.Read(buffer, 0, buffer.Length);
                        writer.Write(buffer, 0, buffer.Length);
                        progress.Report((int)memory.Position);
                    }
                    writer.Flush();
                }
                
            }
        }

        private void UpdateProgress(int i)
        {
            double value = (double)i / maxCount * 100;            
            labelProgress.Text = $"処理中:{(int)value}%";
            progressBar.Value = (int)value;
            Text = videoTitle;
        }

        private void 終了EToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }
    }
}
