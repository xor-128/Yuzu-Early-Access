using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace YuzuUpdate
{
    public partial class MainWindow : MetroWindow
    {
        private readonly Progress<float> progressPercentage;
        private readonly Progress<string> progressText;
        public MainWindow()
        {
            InitializeComponent();
            Task.Factory.StartNew(Load);
            progressPercentage = new Progress<float>();
            progressText = new Progress<string>();
            progressPercentage.ProgressChanged += ProgressPercentage_ProgressChanged;
            progressText.ProgressChanged += ProgressText_ProgressChanged;
        }

        private void ProgressText_ProgressChanged(object? sender, string e)
        {
            updateText.Content = e;
        }

        private void ProgressPercentage_ProgressChanged(object? sender, float e)
        {
            progressBar.Value = 10f + (e * 85f);
        }

        private static void Kill(string[] processName)
        {
            foreach (var process in processName)
            {
                Process.GetProcessesByName(process).ToList().ForEach(e => e.Kill());
            }
        }

        public async Task Load()
        {
            try
            {
                var percentage = ((IProgress<float>)progressPercentage);
                var text = ((IProgress<string>)progressText);

                Kill(new[] { "yuzu", "yuzu-cmd", "yuzu-room" });

                await Task.Delay(100);

                if (Directory.Exists(Path.Combine(App.CurrentFolder, "yuzu-early-access")))
                    Directory.Delete(Path.Combine(App.CurrentFolder, "yuzu-early-access"), true);

                using (var zipMemoryStream = new MemoryStream())
                {
                    text.Report("Downloading...");

                    await App.HttpClient.DownloadAsync(
                        App.LatestUpdate!["assets"]!.AsArray()[0]!["browser_download_url"]!.GetValue<string>(),
                        zipMemoryStream, progressPercentage);

                    using (ZipArchive srcArchive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Read))
                    {

                        percentage.Report(97f);
                        text.Report("Unzipping...");

                        srcArchive.ExtractToDirectory(App.CurrentFolder);
                    }

                    percentage.Report(99f);
                    text.Report("Finishing up...");

                    File.WriteAllText(Path.Combine(App.CurrentFolder, "version.txt"),
                        App.LatestUpdate["name"]!.GetValue<string>());

                    Process.Start(Path.Combine(App.CurrentFolder, "yuzu-early-access", "yuzu.exe"));
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                throw;
            }
            finally
            {
                Environment.Exit(0);
            }
        }
    }
}
