using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RbxAssetScraper
{
    internal enum OutputType
    {
        FilesOnly,
        IndexOnly,
        FilesAndIndex
    }

    internal enum CompressionType
    {
        None,
        GZip,
        BZip2
    }

    internal class Config
    {
        public static string OutputPath { get; set; } = "output";
        public static string OutputExtension { get; set; } = "";
        public static int MaxHttpRequests { get; set; } = 1;
        public static int MaxRetries { get; set; } = 0;
        public static OutputType OutputType { get; set; } = OutputType.FilesOnly;
        public static CompressionType CompressionType { get; set; } = CompressionType.None;
    }
}
