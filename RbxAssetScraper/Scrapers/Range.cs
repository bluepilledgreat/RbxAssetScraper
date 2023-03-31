using RbxAssetScraper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RbxAssetScraper.Scrapers
{
    internal class Range : IScraper
    {
        private long Completed;
        private long TotalAssets;
        private List<string> Errors;
        //private SortedDictionary<long, string> Assets;
        private long StartRange;
        private long EndRange;

        public Range()
        {
            Completed = 0;
            TotalAssets = 0;
            Errors = new List<string>();
            //Assets = new SortedDictionary<long, string>();
        }

        private void UpdateProgress()
        {
            string progress = $"RbxAssetScraper | Range {this.StartRange} to {this.EndRange} | {this.Completed}/{this.TotalAssets} | {this.Errors.Count} ERRORS";
            Console.Title = progress;
        }

        public string BuildDefaultOutputPath(string input)
            => $"{input}_assets";

        public async Task Start(string input)
        {
            if (Config.OutputType == OutputType.IndexOnly)
            {
                Console.WriteLine("Range scraper does not support indexes.");
                return;
            }

            // only a warning for files and index
            if (Config.OutputType == OutputType.FilesAndIndex)
                Console.WriteLine("Range scraper does not support indexes, and will continue in files only mode.");

            string[] segments = input.Split('-');
            this.StartRange = long.Parse(segments[0]);
            this.EndRange = long.Parse(segments[1]);
            this.TotalAssets = (this.EndRange - this.StartRange) + 1; // maths genius right here

            Downloader downloader = new Downloader();
            // set up events
            downloader.OnSuccess += DownloaderOnSuccess;
            downloader.OnFailure += DownloaderOnFailure;

            UpdateProgress();
            for (long i = this.StartRange; i <= this.EndRange; i++)
            {
                await downloader.QueueDownloadAsync(i);
            }

            while (this.Completed != this.TotalAssets)
                await Task.Delay(50);

            int totalErrors = this.Errors.Count;
            Console.WriteLine("COMPLETED!");
            Console.WriteLine($"Total Assets: {this.TotalAssets}");
            Console.WriteLine($"Downloaded:   {this.TotalAssets - totalErrors}");
            Console.WriteLine($"Errors:       {totalErrors}");

            if (totalErrors > 0)
            {
                Console.WriteLine("Errors list can be found in the output folder under the name \"errors.txt\"");
                await File.WriteAllLinesAsync($"{Config.OutputPath}/errors.txt", this.Errors);
            }

            Console.WriteLine("Press [ENTER] to exit.");
            Console.ReadKey();
        }

        private void DownloaderOnSuccess(object sender, DownloaderSuccessEventArgs e)
        {
            FileWriter.Save(FileWriter.ConstructPath($"{Config.OutputPath}/{e.Input}"), e.ContentStream, DateTime.Parse(e.LastModified));

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
