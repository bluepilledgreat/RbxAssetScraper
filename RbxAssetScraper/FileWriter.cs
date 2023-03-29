using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RbxAssetScraper
{
    internal class FileWriter
    {
        public static string ConstructPath(string start)
            => $"{start}{(string.IsNullOrEmpty(Config.OutputExtension) ? "" : $".{Config.OutputExtension}")}";

        public static void Save(string filePath, Stream stream, DateTime? lastModified = null)
        {
            if (Config.OutputType == OutputType.FilesOnly || Config.OutputType == OutputType.FilesAndIndex)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    switch (Config.CompressionType)
                    {
                        case CompressionType.GZip:
                            using (Ionic.Zlib.GZipStream compressor = new Ionic.Zlib.GZipStream(ms, Ionic.Zlib.CompressionMode.Compress, true))
                                stream.CopyTo(compressor);
                            filePath += ".gz";
                            break;
                        case CompressionType.BZip2:
                            using (Ionic.BZip2.BZip2OutputStream compressor = new Ionic.BZip2.BZip2OutputStream(ms, true))
                                stream.CopyTo(compressor);
                            filePath += ".bz2";
                            break;
                        default:
                            stream.CopyTo(ms);
                            break;
                    }

                    ms.Position = 0; // or else it wont write anything
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        ms.CopyTo(fileStream);

                    if (lastModified.HasValue)
                        File.SetLastWriteTime(filePath, (DateTime)lastModified);
                }
            }
            stream.Dispose();
        }
    }
}
