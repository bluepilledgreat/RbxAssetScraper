using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RbxAssetScraper.Scrapers
{
    internal class List : IScraper
    {
        private int Completed;
        private int TotalIds;
        private List<string> Errors;
        private SortedDictionary<long, string> Assets;

        public List()
        {
            Completed = 0;
            TotalIds = 0;
            Errors = new List<string>();
            Assets = new SortedDictionary<long, string>();
        }

        private void UpdateProgress()
        {
            string progress = $"RbxAssetScraper | {Completed}/{TotalIds} | {Errors.Count} ERRORS";
            Console.Title = progress;
        }

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
                    Console.WriteLine($"{strId} is not a valid number!");
                    this.Completed++;
                    Errors.Add(strId);
                    UpdateProgress();
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

        private void DownloaderOnSuccess(object sender, long id, int version, string cdnUrl, string lastModified, double fileSizeInMb, Stream contentStream)
        {
            FileWriter.Save(FileWriter.ConstructPath($"{Config.OutputPath}\\{id}"), contentStream, DateTime.Parse(lastModified));

            if (Config.OutputType == OutputType.IndexOnly || Config.OutputType == OutputType.FilesAndIndex)
                Assets[id] = $"{id} | {cdnUrl} [{lastModified} | {fileSizeInMb} MB]";
            
            this.Completed++;
            UpdateProgress();
        }

        private void DownloaderOnFailure(object sender, long id, int version, string message)
        {
            this.Completed++;
            Errors.Add(id.ToString());
            UpdateProgress();
            Console.WriteLine($"{id} failed to download: {message}");
        }
    }
}
