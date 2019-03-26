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
        public static string BasePath => Path.Combine(Path.GetTempPath(), "LINQPad-Download");

        public static event EventHandler<object> Log;

        /// <summary>
        /// Download from the url to Temp filename, check with sha256.
        /// </summary>
        /// <param name="filename">Local file name in Temp folder.</param>
        /// <param name="url">Url to download.</param>
        /// <param name="sha256">SHA256 hash, in upper case.</param>
        /// <returns>The local temp file full path.</returns>
        public static async Task<string> Download(string filename, string url, string sha256)
        {
            Directory.CreateDirectory(BasePath);

            const string hashAlgorithm = "SHA256";
            string localFilePath = Path.Combine(BasePath, filename);

            if (File.Exists(localFilePath) && Hash(localFilePath, hashAlgorithm) == sha256)
            {
                Log?.Invoke(nameof(Download), $"{hashAlgorithm} match, Return from \"{localFilePath}\"");
                return localFilePath;
            }

            using (var wc = new WebClient())
            {
                wc.DownloadProgressChanged += (o, e) => Log?.Invoke(o, e);
                wc.DownloadFileCompleted += (o, e) => Log?.Invoke(o, e);
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
                Log?.Invoke(nameof(Unzip), $"\"{selectedFile}\" exists, return from cache.");
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
