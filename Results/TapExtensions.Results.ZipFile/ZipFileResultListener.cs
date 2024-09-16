using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using OpenTap;
using TapExtensions.Interfaces.ResultListener;

namespace TapExtensions.Results.ZipFile
{
    [Display("Zip File",
        Groups: new[] { "TapExtensions", "Results" },
        Description: "Save results and logs into a zip file")]
    public class ZipFileResultListener : ResultListener, IZipFile
    {
        [Display("Report Path", Order: 1,
            Description: "Path where report files are to be generated")]
        [DirectoryPath]
        public string ReportPath
        {
            get => _fullPath;
            set => _fullPath = !string.IsNullOrWhiteSpace(value) ? Path.GetFullPath(value) : "";
        }

        private string _fullPath;
        private readonly List<AdditionalFile> _additionalFiles = new List<AdditionalFile>();

        private class AdditionalFile
        {
            public string Name { get; set; }
            public MemoryStream Contents { get; set; }
        }

        public ZipFileResultListener()
        {
            Name = "Zip";
            ReportPath = @"C:\Temp\Zip";
        }

        public override void OnTestPlanRunStart(TestPlanRun planRun)
        {
            _additionalFiles.Clear();
        }

        public void AddExistingFile(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException($"File not found at {fileName}");

            var memoryStream = new MemoryStream();
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                fileStream.CopyTo(memoryStream);
            }

            AddNewFile(Path.GetFileName(fileName), memoryStream);
        }

        public void AddNewFile(string fileName, MemoryStream fileContents)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new InvalidOperationException($"{nameof(fileName)} cannot be empty");
            if (!(fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0))
                throw new InvalidOperationException($"{nameof(fileName)} is not a valid");
            if (fileContents.Length == 0)
                throw new InvalidOperationException($"{nameof(fileContents)} cannot be empty");

            var item = new AdditionalFile { Name = fileName, Contents = fileContents };
            var duplicate = _additionalFiles.FindIndex(x => x.Name == fileName);
            if (duplicate != -1)
            {
                _additionalFiles[duplicate] = item;
                Log.Warning($"Replace {fileName}");
            }
            else
            {
                _additionalFiles.Add(item);
                Log.Debug($"Add {fileName}");
            }
        }

        public void AddNewFile(string fileName, string fileContents)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new InvalidOperationException($"{nameof(fileName)} cannot be empty");
            if (!(fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0))
                throw new InvalidOperationException($"{nameof(fileName)} is not a valid");
            if (string.IsNullOrWhiteSpace(fileContents))
                throw new InvalidOperationException($"{nameof(fileContents)} cannot be empty");

            AddNewFile(fileName, GenerateStreamFromString(fileContents));
        }

        private static MemoryStream GenerateStreamFromString(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException("string cannot be empty");

            return new MemoryStream(Encoding.UTF8.GetBytes(value));
        }

        public override void OnTestPlanRunCompleted(TestPlanRun planRun, Stream logStream)
        {
            var fileName = string.Format("{0}{1:d2}{2:d2}-{3:d2}{4:d2}{5:d2}_{6}",
                DateTime.Now.Year,
                DateTime.Now.Month,
                DateTime.Now.Day,
                DateTime.Now.Hour,
                DateTime.Now.Minute,
                DateTime.Now.Second,
                planRun.Verdict
            );

            // Create zip file containing html result file, txt log file, etc.
            using (var memoryStream = new MemoryStream())
            {
                // Create zip in memory
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // Create log file in memory
                    var logFileName = fileName + ".txt";
                    var logFile = zipArchive.CreateEntry(logFileName, CompressionLevel.Optimal);
                    using (var logContentsEntryStream = logFile.Open())
                    {
                        logStream.Seek(0, SeekOrigin.Begin);
                        logStream.CopyTo(logContentsEntryStream);
                    }

                    // Create html results file
                    // ...

                    // Add additional files from test steps
                    foreach (var additionalFile in _additionalFiles)
                    {
                        var entry = zipArchive.CreateEntry(additionalFile.Name, CompressionLevel.Optimal);
                        using (var entryContents = entry.Open())
                        {
                            additionalFile.Contents.Seek(0, SeekOrigin.Begin);
                            additionalFile.Contents.CopyTo(entryContents);
                        }
                    }
                }

                // Create directory if it doesn't exist
                Directory.CreateDirectory(ReportPath);

                // Write zip file to disk
                var zipFullPath = Path.Combine(ReportPath, fileName + ".zip");
                using (var zipFileStream = new FileStream(zipFullPath, FileMode.Create))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(zipFileStream);
                }

                Log.Info($"Saved results to {zipFullPath}");
            }
        }
    }
}