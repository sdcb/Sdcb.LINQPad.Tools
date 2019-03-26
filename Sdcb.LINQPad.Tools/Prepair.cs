using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Sdcb.LINQPad.Tools
{
    public static class Prepair
    {
        /// <summary>
        /// Download from the url to Temp filename, check with sha256.
        /// </summary>
        /// <param name="filename">Local file name in Temp folder.</param>
        /// <param name="url">Url to download.</param>
        /// <param name="sha256">SHA256 hash, in upper case.</param>
        /// <returns>The local temp file full path.</returns>
        public static async Task<string> Download(string filename, string url, string sha256)
        {
            const string hashAlgorithm = "SHA256";
            string localFilePath = Path.Combine(Path.GetTempPath(), filename);

            if (File.Exists(localFilePath) && Hash(localFilePath, hashAlgorithm) == sha256)
            {
                Console.WriteLine($"{hashAlgorithm} match, Return from \"{localFilePath}\"");
                return localFilePath;
            }

            using (var wc = new WebClient())
            {
                Console.WriteLine($"Downloading {url}...");
                wc.DownloadProgressChanged += (o, e) => Console.WriteLine($"{e.BytesReceived} of {e.TotalBytesToReceive}...");
                wc.DownloadFileCompleted += (o, e) => Console.WriteLine($"Download complete.");
                await wc.DownloadFileTaskAsync(url, localFilePath);
            }

            var localHash = Hash(localFilePath, hashAlgorithm);
            if (localHash != sha256)
            {
                throw new InvalidDataException($"{hashAlgorithm} not match. Actual: {localHash}, expected: {sha256}.");
            }

            return localFilePath;
        }

        /// <summary>
        /// Unzip the zipFilePath, if selectPath not exists.
        /// </summary>
        /// <param name="zipFilePath">zip file to unzip.</param>
        /// <param name="selectPath">path to be checked.</param>
        /// <returns>Combine the zip file directory and selectPath.</returns>
        public static string Unzip(string zipFilePath, string selectPath)
        {
            string directoryName = Path.GetDirectoryName(zipFilePath);
            string selectedFile = Path.Combine(directoryName, selectPath);
            if (File.Exists(selectedFile) || Directory.Exists(selectedFile))
            {
                Console.WriteLine($"\"{selectedFile}\" exists, return from cache.");
                return selectedFile;
            }

            ZipFile.ExtractToDirectory(zipFilePath, directoryName);
            return selectedFile;
        }

        internal static string Hash(string filePath, string hashAlgorithm)
        {
            using (var algorithm = HashAlgorithm.Create(hashAlgorithm))
            using (var stream = File.OpenRead(filePath))
            using (var bufferedStream = new BufferedStream(stream, 65536))
            {
                var hash = algorithm.ComputeHash(bufferedStream);
                return String.Join("", hash.Select(x => x.ToString("X2")));
            }
        }
    }
}
