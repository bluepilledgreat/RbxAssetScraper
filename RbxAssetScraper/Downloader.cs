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

        public delegate void Finished(object sender, long id, int version);
        public delegate void Success(object sender, long id, int version, string cdnUrl, string lastModified, double fileSizeInMb, Stream contentStream);
        public delegate void Failure(object sender, long id, int version, string reason);

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

        public static Task<HttpResponseMessage> AssetDeliveryRequestAsync(long id, int version = 0)
        {
            return HttpClient.GetAsync($"https://assetdelivery.roblox.com/v1/asset/?id={id}&version={version}");
        }

        // TODO: split this into multiple functions
        public async Task QueueDownloadAsync(long id, int version = 0, int retry = 0)
        {
            while (this.RunningHttp >= Config.MaxHttpRequests)
            {
                await Task.Delay(50);
            }

            Task task = AssetDeliveryRequestAsync(id, version).ContinueWith(async x =>
            {
                try
                {
                    HttpResponseMessage response = x.Result;
                    System.Net.HttpStatusCode statusCode = response.StatusCode;
                    if (statusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        OnFinished?.Invoke(this, id, version);
                        OnFailure?.Invoke(this, id, version, "Asset has been moderated");
                        RunningHttp--;
                        return;
                    }
                    else if (statusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        OnFinished?.Invoke(this, id, version);
                        OnFailure?.Invoke(this, id, version, "No access to asset");
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
                            {
                                OnFinished?.Invoke(this, id, version);
                                OnSuccess?.Invoke(this, id, version, cdnUrl, lastModified, Math.Round(stream.Length / 1024f / 1024f, 6), stream);
                            }
                        }
                        catch (Exception ex)
                        {
                            OnFinished?.Invoke(this, id, version);
                            OnFailure?.Invoke(this, id, version, ex.ToString());
                        }
                    });
                    //GeneralTasks.Add(task);
                }
                catch (Exception ex)
                {
                    // 409 = conflict = not allowed
                    if (retry <= Config.MaxRetries && !ex.Message.Contains("409"))
                    {
                        RunningHttp--;
                        await QueueDownloadAsync(id, version, retry+1);
                        return;
                    }
                    OnFinished?.Invoke(this, id, version);
                    OnFailure?.Invoke(this, id, version, ex.ToString());
                }
                RunningHttp--;
            });

            RunningHttp++;
            //HttpTasks.Add(task);
        }
    }
}
