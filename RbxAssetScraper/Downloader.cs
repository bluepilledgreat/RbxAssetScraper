using RbxAssetScraper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RbxAssetScraper
{
    internal class Downloader
    {
        private static HttpClient HttpClient;

        public delegate void Finished(object sender, DownloaderFinishedEventArgs e);
        public delegate void Success(object sender, DownloaderSuccessEventArgs e);
        public delegate void Failure(object sender, DownloaderFailureEventArgs e);

        public event Finished? OnFinished;
        public event Success? OnSuccess;
        public event Failure? OnFailure;

        private int RunningHttp = 0;

        static Downloader()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All,
                AllowAutoRedirect = false
            };

            HttpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(3)
            };
        }

        public static bool IsSuccessStatusCode(System.Net.HttpStatusCode code, bool allow403 = false)
        {
            switch (code)
            {
                case System.Net.HttpStatusCode.OK:
                case System.Net.HttpStatusCode.Redirect:
                case System.Net.HttpStatusCode.RedirectKeepVerb:
                    return true;
                case System.Net.HttpStatusCode.Forbidden:
                    return allow403;
                default:
                    return false;
            }
        }

        public static void EnsureSuccessStatusCode(System.Net.HttpStatusCode code, bool allow403 = false)
        {
            if (!IsSuccessStatusCode(code, allow403))
                throw new HttpRequestException($"Status code not successful (got {code}, {(int)code})");
        }

        private void InvokeOnFinished(string input, int version)
        {
            OnFinished?.Invoke(this, new()
            {
                Input = input,
                Version = version
            });
        }

        private void InvokeOnSuccess(string input, int version, string cdnUrl, string lastModified, double fizeSizeInMB, Stream contentStream)
        {
            InvokeOnFinished(input, version);
            OnSuccess?.Invoke(this, new()
            {
                Input = input,
                Version = version,
                CdnUrl = cdnUrl,
                LastModified = lastModified,
                FileSizeMB = fizeSizeInMB,
                ContentStream = contentStream
            });
        }

        private void InvokeOnFailure(string input, int version, string reason)
        {
            InvokeOnFinished(input, version);
            OnFailure?.Invoke(this, new()
            {
                Input = input,
                Version = version,
                Reason = reason
            });
        }

        public static Task<HttpResponseMessage> AssetDeliveryRequestAsync(long id, int version = 0)
        {
            return HttpClient.GetAsync($"https://assetdelivery.roblox.com/v1/asset/?id={id}&version={version}");
        }

        public static Task<HttpResponseMessage> ContentStoreRequestAsync(string hash)
        {
            return HttpClient.GetAsync($"https://contentstore.roblox.com/v1/content?hash={hash}");
        }

        // TODO: split this into multiple functions
        public async Task QueueDownloadAsync(long id, int version = 0, int retry = 0)
        {
            while (this.RunningHttp >= Config.MaxHttpRequests)
                await Task.Delay(50);

            Task task = AssetDeliveryRequestAsync(id, version).ContinueWith(async x =>
            {
                try
                {
                    HttpResponseMessage response = x.Result;
                    System.Net.HttpStatusCode statusCode = response.StatusCode;
                    if (statusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        InvokeOnFinished(id.ToString(), version);
                        InvokeOnFailure(id.ToString(), version, "Asset is moderated");
                        RunningHttp--;
                        return;
                    }
                    else if (statusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        InvokeOnFinished(id.ToString(), version);
                        InvokeOnFailure(id.ToString(), version, "Asset is copylocked");
                        RunningHttp--;
                        return;
                    }
                    EnsureSuccessStatusCode(response.StatusCode);

                    string cdnUrl = response.Headers.GetValues("Location").First();
                    HttpResponseMessage cdnResponse = await HttpClient.GetAsync(cdnUrl);
                    string lastModified = cdnResponse.Content.Headers.GetValues("Last-Modified").First();

                    Task task = cdnResponse.Content.ReadAsStreamAsync().ContinueWith(async task =>
                    {
                        try
                        {
                            using (Stream stream = await task)
                                InvokeOnSuccess(id.ToString(), version, cdnUrl, lastModified, Math.Round(stream.Length / 1024f / 1024f, 6), stream);
                        }
                        catch (Exception ex)
                        {
                            InvokeOnFailure(id.ToString(), version, ex.ToString());
                        }
                    });
                }
                catch (Exception ex)
                {
                    if (retry <= Config.MaxRetries)
                    {
                        RunningHttp--;
                        await QueueDownloadAsync(id, version, retry+1);
                        return;
                    }
                    InvokeOnFailure(id.ToString(), version, ex.ToString());
                }
                RunningHttp--;
            });

            RunningHttp++;
        }

        public async Task QueueCdnDownloadAsync(string hash, int retry = 0)
        {
            while (this.RunningHttp >= Config.MaxHttpRequests)
                await Task.Delay(50);

            Task task = ContentStoreRequestAsync(hash).ContinueWith(async x =>
            {
                try
                {
                    HttpResponseMessage response = x.Result;
                    System.Net.HttpStatusCode statusCode = response.StatusCode;
                    EnsureSuccessStatusCode(response.StatusCode);

                    string cdnUrl = response.Headers.GetValues("Location").First();
                    HttpResponseMessage cdnResponse = await HttpClient.GetAsync(cdnUrl);
                    string lastModified = cdnResponse.Content.Headers.GetValues("Last-Modified").First();

                    Task task = cdnResponse.Content.ReadAsStreamAsync().ContinueWith(async task =>
                    {
                        try
                        {
                            using (Stream stream = await task)
                                InvokeOnSuccess(hash, 0, cdnUrl, lastModified, Math.Round(stream.Length / 1024f / 1024f, 6), stream);
                        }
                        catch (Exception ex)
                        {
                            InvokeOnFailure(hash, 0, ex.ToString());
                        }
                    });
                }
                catch (Exception ex)
                {
                    if (retry <= Config.MaxRetries)
                    {
                        RunningHttp--;
                        await QueueCdnDownloadAsync(hash, retry + 1);
                        return;
                    }
                    InvokeOnFailure(hash, 0, ex.ToString());
                }
                RunningHttp--;
            });

            RunningHttp++;
        }
    }
}
