﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RbxAssetScraper.Scrapers
{
    internal class Game : IScraper
    {
        private int Completed;
        private int TotalVersions;
        private List<string> Errors;
        private SortedDictionary<long, string> Versions;
        private long Id;

        public Game()
        {
            Completed = 0;
            TotalVersions = 0;
            Errors = new List<string>();
            Versions = new SortedDictionary<long, string>();
            Id = 0;
        }

        private void UpdateProgress()
        {
            string progress = $"RbxAssetScraper | Place {this.Id} | {this.Completed}/{this.TotalVersions} | {this.Errors.Count} ERRORS";
            Console.Title = progress;
        }

        public async Task Start(string input)
        {
            if (!long.TryParse(input, out this.Id))
            {
                Console.WriteLine("Input must be a number!");
                return;
            }

            // get latest version
            try
            {
                HttpResponseMessage response = await Downloader.AssetDeliveryRequestAsync(this.Id);
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    Console.WriteLine("Asset is copylocked");
                    return;
                }

                // 403 means that the specific version is gone, but is still uncopylocked
                Downloader.EnsureSuccessStatusCode(response.StatusCode, allow403: true);

                this.TotalVersions = int.Parse(response.Headers.GetValues("roblox-assetversionnumber").First());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get total asset versions: {ex}");
                return;
            }

            if (Config.OutputType == OutputType.IndexOnly || Config.OutputType == OutputType.FilesAndIndex)
                Versions[0] = $"Versions for game id {this.Id}";

            Downloader downloader = new Downloader();
            // set up events
            downloader.OnSuccess += DownloaderOnSuccess;
            downloader.OnFailure += DownloaderOnFailure;

            UpdateProgress();
            for (int i = 1; i <= this.TotalVersions; i++)
                await downloader.QueueDownloadAsync(this.Id, i);

            while (this.Completed != this.TotalVersions)
                await Task.Delay(50);

            int totalErrors = this.Errors.Count;
            Console.WriteLine($"COMPLETED {this.Id}!");
            Console.WriteLine($"Total Versions: {this.TotalVersions}");
            Console.WriteLine($"Downloaded:     {this.TotalVersions - totalErrors}");
            Console.WriteLine($"Errors:         {totalErrors}");

            if (totalErrors > 0)
            {
                Console.WriteLine("Errors list can be found in the output folder under the name \"errors.txt\"");
                await File.WriteAllLinesAsync($"{Config.OutputPath}\\errors.txt", this.Errors);
            }

            if (this.Versions.Count > 0)
            {
                Console.WriteLine("Index list can be found in the output folder under the name \"index.txt\"");
                await File.WriteAllLinesAsync($"{Config.OutputPath}\\index.txt", this.Versions.Select(x => x.Value));
            }

            Console.WriteLine("Press [ENTER] to exit.");
            Console.ReadKey();
        }

        private void DownloaderOnSuccess(object sender, long id, int version, string cdnUrl, string lastModified, double fileSizeInMb, Stream contentStream)
        {
            FileWriter.Save(FileWriter.ConstructPath($"{Config.OutputPath}\\{id}-v{version}"), contentStream, DateTime.Parse(lastModified));

            if (Config.OutputType == OutputType.IndexOnly || Config.OutputType == OutputType.FilesAndIndex)
                Versions[version] = $"{version} | {cdnUrl} [{lastModified} | {fileSizeInMb} MB]";

            this.Completed++;
            UpdateProgress();
        }

        private void DownloaderOnFailure(object sender, long id, int version, string message)
        {
            this.Completed++;
            Errors.Add(version.ToString());
            UpdateProgress();
            Console.WriteLine($"{version} failed to download: {message}");
        }
    }
}