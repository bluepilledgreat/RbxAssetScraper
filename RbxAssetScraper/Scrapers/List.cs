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
        private int TotalIds;
        private List<string> Errors;
        private SortedDictionary<string, string> Assets;

        public List()
        {
            Completed = 0;
            TotalIds = 0;
            Errors = new List<string>();
            Assets = new SortedDictionary<string, string>();
        }

        private void UpdateProgress()
        {
            string progress = $"RbxAssetScraper | {Completed}/{TotalIds} | {Errors.Count} ERRORS";
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

            string[] strIds = await File.ReadAllLinesAsync(input);
            this.TotalIds = strIds.Count();

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

                await downloader.QueueDownloadAsync(id);
            }

            while (this.Completed != this.TotalIds)
                await Task.Delay(50);

            int totalErrors = this.Errors.Count;
            Console.WriteLine("COMPLETED!");
            Console.WriteLine($"Total IDs:  {this.TotalIds}");
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
            FileWriter.Save(FileWriter.ConstructPath($"{Config.OutputPath}\\{e.Input}"), e.ContentStream, DateTime.Parse(e.LastModified));

            if (Config.OutputType == OutputType.IndexOnly || Config.OutputType == OutputType.FilesAndIndex)
                Assets[e.Input] = $"{e.Input} | {e.CdnUrl} [{e.LastModified} | {e.FileSizeMB} MB]";
            
            this.Completed++;
            UpdateProgress();
        }

        private void DownloaderOnFailure(object sender, DownloaderFailureEventArgs e)
        {
            Errors.Add(e.Input);
            this.Completed++;
            UpdateProgress();
            Console.WriteLine($"{e.Input} failed to download: {e.Reason}");
        }
    }
}
