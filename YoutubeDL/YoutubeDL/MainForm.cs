using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoLibrary;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

namespace YoutubeDL
{
    public partial class MainForm : Form
    {
        private static readonly string DEFAULT = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Youtube");
        private static readonly string LOG_PATH = Path.Combine(DEFAULT, "youtubedl.log");

        private static readonly char[] INVALID_CHARS = Path.GetInvalidFileNameChars();

        private YouTube youTube;
        private long? maxCount = 0;        

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
            textBoxFolder.Text = DEFAULT;
            versionLabel.Text = $"バージョン:{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}";

            if(Directory.Exists(DEFAULT) == false)
            {
                Directory.CreateDirectory(DEFAULT);
            }
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

            //ダウンロード処理
            labelProgress.Text = "ダウンロード開始...";
            var progress = new Progress<int>(UpdateProgress);
            var list = new List<string>();
            foreach(ListViewItem item in listViewVideos.Items) { list.Add(item.Text); }

            //結果出力
            var result = await Task.Run(() => SaveVideosAsync(root, list, progress));
            var successCnt = result.Values.Count(x => x == true);
            Utils.Logging(LOG_PATH, result);
            Process.Start(LOG_PATH);
            Process.Start(root);

            MessageBox.Show($"保存しました:{root}", $"ダウンロード完了 {successCnt}/{result.Count}", MessageBoxButtons.OK, MessageBoxIcon.Information);
            buttonClear.PerformClick();
        }


        private async Task<Dictionary<string, bool>> SaveVideosAsync(string root, List<string> items, IProgress<int> progress)
        {
            var results = new Dictionary<string, bool>();

            var buffer = new byte[4096];
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                YouTubeVideo video = null;
                try
                {
                    video = youTube.GetVideo(item);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, $"Not found {item}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    results.Add(item, false);
                    continue;
                }

                var videoTitle = video.Title;
                videoTitle = string.Concat(videoTitle.Select(c => INVALID_CHARS.Contains(c) ? '_' : c));
                if (videoTitle == "YouTube") videoTitle += i; //ファイル名がYoutubeで被らないようにする

                Invoke(new Action(() =>
                {
                    this.Text = $"{i + 1}/{items.Count} - {video.Title}";
                }));

                var fileName = $"{videoTitle}{video.FileExtension}";
                var path = Path.Combine(root, fileName);

                try
                {
                    using (var writer = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                    using (var client = new VideoClient())
                    using (var input = await client.StreamAsync(video))
                    {
                        maxCount = video.ContentLength.Value;
                        int read;
                        int totalRead = 0;
                        while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await writer.WriteAsync(buffer, 0, read);

                            totalRead += read;
                            progress.Report(totalRead);
                        }
                        await writer.FlushAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, $"Download error {item}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    results.Add($"{item} : {videoTitle}", false);
                    continue;
                }

                results.Add($"{item} : {videoTitle}", true);
            }

            return results;
        }

        private void UpdateProgress(int i)
        {
            double value = (double)i / maxCount.Value * 100;            
            labelProgress.Text = $"処理中:{(int)value}%";
            progressBar.Value = (int)value;            
        }

        private void 終了EToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }

        private async void OpenFileMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "URLリスト(*.txt)|*.txt";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var lists = await Utils.LoadUrlsAsync(dialog.FileName);
                lists.ForEach(li => listViewVideos.Items.Add(li));
            }
        }
    }
}
