using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RbxAssetScraper.Models
{
    internal class DownloaderFinishedEventArgs
    {
        public string Input { get; init; } = null!;
        public int Version { get; init; }
    }

    internal class DownloaderSuccessEventArgs
    {
        public string Input { get; init; } = null!;
        public int Version { get; init; }
        public string CdnUrl { get; init; } = null!;
        public string LastModified { get; init; } = null!;
        public double FileSizeMB { get; init; }
        public Stream ContentStream { get; init; } = null!;
    }

    internal class DownloaderFailureEventArgs
    {
        public string Input { get; init; } = null!;
        public int Version { get; init; }
        public string Reason { get; init; } = null!;
    }
}
