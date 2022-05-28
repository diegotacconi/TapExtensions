using System;
using System.IO;
using System.IO.Compression;
using OpenTap;

namespace TapExtensions.Results
{
    [Display("Zip File",
        Groups: new[] { "TapExtensions", "Results" },
        Description: "Save results and logs into a zip file.")]
    public class ZipFileResultListener : ResultListener
    {
        [Display("Report Path", Order: 1,
            Description: "Path where report files are to be generated")]
        [DirectoryPath]
        public string ReportPath
        {
            get => _fullPath;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    _fullPath = Path.GetFullPath(value);
            }
        }

        private string _fullPath;

        public ZipFileResultListener()
        {
            // Default values
            Name = "Zip";
            ReportPath = @"C:\Temp\Zip";
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

                Log.Info("Saved results to {0}", zipFullPath);
            }
        }
    }
}