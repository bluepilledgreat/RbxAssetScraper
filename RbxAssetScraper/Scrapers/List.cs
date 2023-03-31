using RbxAssetScraper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RbxAssetScraper.Scrapers
{
    internal class List : IScraper
    {
        private int Completed;
        private int CompletedVersions;
        private int TotalIds;
        private int TotalVersions;
        private List<string> Errors;
        private SortedDictionary<string, string> Assets;
        private bool DownloadVersions;

        public List(bool downloadVersions)
        {
            Completed = 0;
            CompletedVersions = 0;
            TotalIds = 0;
            TotalVersions = 0;
            Errors = new List<string>();
            Assets = new SortedDictionary<string, string>();
            DownloadVersions = downloadVersions;
        }

        private void UpdateProgress()
        {
            string progress;
            if (DownloadVersions)
                progress = $"RbxAssetScraper | {Completed}/{TotalIds} | {CompletedVersions}/{TotalVersions} | {Errors.Count} ERRORS";
            else
                progress = $"RbxAssetScraper | {Completed}/{TotalIds} | {Errors.Count} ERRORS";
            Console.Title = progress;
        }

        public string BuildDefaultOutputPath(string input)
            => $"{Path.GetFileNameWithoutExtension(input)}_assets";

        public async Task Start(string input)
        {
            if (!File.Exists(input))
            {
                Console.WriteLine("Provided list does not exist");
                return;
            }

            if (DownloadVersions)
            {
                Console.WriteLine("List versions is an experimental feature.");
                if (Config.OutputType == OutputType.FilesOnly || Config.OutputType == OutputType.FilesAndIndex)
                    Console.WriteLine("List versions may output weird index files!");
            }

            string[] strIds = await File.ReadAllLinesAsync(input);
            this.TotalIds = strIds.Count();
            this.TotalVersions = this.TotalIds;

            Downloader downloader = new Downloader();
            // set up events
            downloader.OnSuccess += DownloaderOnSuccess;
            downloader.OnFailure += DownloaderOnFailure;

            UpdateProgress();
            foreach (string strId in strIds)
            {
                if (!long.TryParse(strId, out long id))
                {
                    // lets check if its an md5 hash (cdn)
                    // https://stackoverflow.com/a/1715468
                    if (Regex.IsMatch(strId, "^[0-9a-fA-F]{32}$", RegexOptions.Compiled))
                    {
                        await downloader.QueueCdnDownloadAsync(strId);
                        continue;
                    }

                    DownloaderOnFailure(downloader, new() { Input = strId, Version = 0, Reason = $"{strId} is not a valid number!" });
                    continue;
                }

                if (DownloadVersions)
                {
                    int versions = 0;
                    try
                    {
                        HttpResponseMessage response = await Downloader.AssetDeliveryRequestAsync(id);
                        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                        {
                            throw new Exception("Asset is copylocked");
                        }

                        // 403 means that the specific version is gone, but is still uncopylocked
                        Downloader.EnsureSuccessStatusCode(response.StatusCode, allow403: true);

                        versions = int.Parse(response.Headers.GetValues("roblox-assetversionnumber").First());
                    }
                    catch (Exception ex)
                    {
                        this.Completed++;
                        DownloaderOnFailure(downloader, new() { Input = id.ToString(), Version = -1, Reason = $"Failed to get total versions: {ex}" });
                        continue;
                    }

                    this.TotalVersions += versions-1;

                    for (int i = 1; i <= versions; i++)
                        await downloader.QueueDownloadAsync(id, i);

                    this.Completed++;
                    continue;
                }

                await downloader.QueueDownloadAsync(id);
            }

            while (this.CompletedVersions != this.TotalVersions)
                await Task.Delay(50);

            int totalErrors = this.Errors.Count;
            Console.WriteLine("COMPLETED!");
            Console.WriteLine($"Total IDs:  {this.TotalIds}");
            if (DownloadVersions)
            {
                Console.WriteLine($"Total Versions: {this.TotalVersions}");
                Console.WriteLine($"Downloaded: {this.TotalVersions - totalErrors}");
            }
            else
                Console.WriteLine($"Downloaded: {this.TotalIds - totalErrors}");
            Console.WriteLine($"Errors:     {totalErrors}");

            if (totalErrors > 0)
            {
                Console.WriteLine("Errors list can be found in the output folder under the name \"errors.txt\"");
                await File.WriteAllLinesAsync($"{Config.OutputPath}\\errors.txt", this.Errors);
            }

            if (this.Assets.Count > 0)
            {
                Console.WriteLine("Index list can be found in the output folder under the name \"index.txt\"");
                await File.WriteAllLinesAsync($"{Config.OutputPath}\\index.txt", this.Assets.Select(x => x.Value));
            }

            Console.WriteLine("Press [ENTER] to exit.");
            Console.ReadKey();
        }

        private void DownloaderOnSuccess(object sender, DownloaderSuccessEventArgs e)
        {
            string path;
            if (DownloadVersions && e.Version != 0 && (Config.OutputType == OutputType.FilesOnly || Config.OutputType == OutputType.FilesAndIndex))
            {
                Directory.CreateDirectory($"{Config.OutputPath}\\{e.Input}");
                path = FileWriter.ConstructPath($"{Config.OutputPath}\\{e.Input}\\{e.Input}-v{e.Version}");
            }
            else
                path = FileWriter.ConstructPath($"{Config.OutputPath}\\{e.Input}");

            FileWriter.Save(path, e.ContentStream, DateTime.Parse(e.LastModified));

            string indexLine = DownloadVersions && e.Version != 0 ? $"{e.Input} v{e.Version}" : e.Input;
            if (Config.OutputType == OutputType.IndexOnly || Config.OutputType == OutputType.FilesAndIndex)
                Assets[indexLine] = $"{indexLine} | {e.CdnUrl} [{e.LastModified} | {e.FileSizeMB} MB]";

            this.CompletedVersions++;
            if (!DownloadVersions || e.Version == 0)
                this.Completed++;
            UpdateProgress();
        }

        private void DownloaderOnFailure(object sender, DownloaderFailureEventArgs e)
        {
            string display = DownloadVersions && e.Version != 0 ? $"{e.Input} v{e.Version}" : e.Input;
            Errors.Add(display);
            this.CompletedVersions++;
            if (!DownloadVersions || e.Version == 0)
                this.Completed++;
            UpdateProgress();
            Console.WriteLine($"{display} failed to download: {e.Reason}");
        }
    }
}
