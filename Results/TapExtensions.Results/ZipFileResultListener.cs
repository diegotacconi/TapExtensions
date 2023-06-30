using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using OpenTap;
using TapExtensions.Interfaces.Results;

namespace TapExtensions.Results
{
    [Display("Zip File",
        Groups: new[] { "TapExtensions", "Results" },
        Description: "Save results and logs into a zip file.")]
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
            // ToDo: check for same fileName in the list, and replace contents
            // ToDo: check and remove duplicate files

            _additionalFiles.Add(new AdditionalFile { Name = fileName, Contents = fileContents });
            Log.Debug($"Add {fileName}");
        }

        public void AddNewFile(string fileName, string fileContents)
        {
            AddNewFile(fileName, GenerateStreamFromString(fileContents));
        }

        private static MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
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

                    // Create screenshot file
                    // ...

                    // Add additional files from TestSteps
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