using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace YuzuUpdate
{
    public partial class App : Application
    {
        public static JsonObject? LatestUpdate = null;
        public static string CurrentFolder = AppDomain.CurrentDomain.BaseDirectory;
        public static HttpClient HttpClient = new HttpClient();
        protected override void OnStartup(StartupEventArgs e)
        {
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "request");

            try
            {
                // Global mutex for single app
                try
                {
                    Semaphore.OpenExisting(@"Global\YuzuUpdate");
                    Environment.Exit(0);
                }
                catch (Exception)
                {
                    new Semaphore(1, 1, @"Global\YuzuUpdate");
                }

                var response = HttpClient.Send(new HttpRequestMessage(HttpMethod.Get, 
                    "https://api.github.com/repos/Kryptuq/Yuzu-Early-Access-files/releases"));
                var responseString = new StreamReader(response.Content.ReadAsStream()).ReadToEnd();

                LatestUpdate = JsonNode.Parse(responseString)!.AsArray()[0]!.AsObject();

                if (!File.Exists(Path.Combine(CurrentFolder, "version.txt")) ||
                    !File.Exists(Path.Combine(CurrentFolder, "yuzu-early-access", "yuzu.exe")))
                {
                    base.OnStartup(e);
                    return;
                }

                if (File.ReadAllText(Path.Combine(CurrentFolder, "version.txt")) == LatestUpdate["name"]!.GetValue<string>())
                {
                    Process.Start(Path.Combine(CurrentFolder, "yuzu-early-access", "yuzu.exe"));
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                throw;
            }

            base.OnStartup(e);
        }
    }
}
