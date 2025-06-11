using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using OpenTap;

namespace TapExtensions.Steps.Process
{
    [Display("RunProcess",
        Groups: new[] { "TapExtensions", "Steps", "Process" })]
    public class RunProcess : TestStep
    {
        [Display("Application", Order: 1,
            Description:
            "The path to the program. It should contain either a relative path to OpenTAP installation folder " +
            "or an absolute path to the program.")]
        [FilePath(FilePathAttribute.BehaviorChoice.Open, "exe")]
        public string Application { get; set; }

        [Display("Command Line Arguments", Order: 2, Description: "The arguments passed to the program.")]
        public string Arguments { get; set; }

        [Display("Working Directory", Order: 3, Description: "The directory where the program will be started in.")]
        [DirectoryPath]
        public string WorkingDirectory { get; set; }

        [Display("Expected Response", Order: 4)]
        public string ExpectedResponse { get; set; }

        [Display("Timeout", Order: 5)]
        [Unit("s")]
        public int Timeout { get; set; }

        [Display("Check Exit Code", Order: 6,
            Description:
            "Check the exit code of the application and set verdict to fail if it is non-zero, else pass. " +
            "'Wait For End' must be set for this to work.")]
        public bool CheckExitCode { get; set; }

        private ManualResetEvent _outputWaitHandle, _errorWaitHandle;
        private StringBuilder _output;

        public RunProcess()
        {
            // Default values
            Application = "powershell.exe";
            Arguments = "dir";
            WorkingDirectory = @"C:\";
            ExpectedResponse = "Program Files";
            Timeout = 10;
            CheckExitCode = true;

            // Validation rules
            Rules.Add(() => Timeout > 0,
                "Timeout must be greater than zero", nameof(Timeout));
        }

        public override void Run()
        {
            ThrowOnValidationError(true);



            var process = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = Application,
                    Arguments = Arguments,
                    WorkingDirectory = string.IsNullOrEmpty(WorkingDirectory)
                        ? Directory.GetCurrentDirectory()
                        : Path.GetFullPath(WorkingDirectory),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                }
            };

            var abortRegistration = TapThread.Current.AbortToken.Register(() =>
            {
                Log.Debug($"Ending process '{Application}'.");
                try
                {
                    // process.Kill may throw if it has already exited.
                    try
                    {
                        // signal to the sub process that no more input will arrive.
                        // For many process this has the same effect as CTRL+C as stdin is closed.
                        process.StandardInput.Close();
                    }
                    catch
                    {
                        // this might be ok. It probably means that the input has already been closed.
                    }

                    if (!process.WaitForExit(500)) // give some time for the process to close by itself.
                        process.Kill();
                }
                catch (Exception ex)
                {
                    Log.Warning($"Caught exception when killing process. {ex.Message}");
                }
            });

            _output = new StringBuilder();

            using (_outputWaitHandle = new ManualResetEvent(false))
            using (_errorWaitHandle = new ManualResetEvent(false))
            using (process)
            using (abortRegistration)
            {
                process.OutputDataReceived += OutputDataReceived;
                process.ErrorDataReceived += ErrorDataReceived;

                Log.Debug($"Starting process '{Application}' with arguments '{Arguments}'");
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var timeoutMs = Timeout * 1000;

                if (process.WaitForExit(timeoutMs) &&
                    _outputWaitHandle.WaitOne(timeoutMs) &&
                    _errorWaitHandle.WaitOne(timeoutMs))
                {
                    var resultData = _output.ToString();

                    //ProcessOutput(resultData);
                    if (CheckExitCode)
                    {
                        if (process.ExitCode != 0)
                            UpgradeVerdict(Verdict.Fail);
                        else
                            UpgradeVerdict(Verdict.Pass);
                    }
                }
                else
                {
                    process.OutputDataReceived -= OutputDataReceived;
                    process.ErrorDataReceived -= ErrorDataReceived;

                    var resultData = _output.ToString();

                    //ProcessOutput(resultData);

                    Log.Error("Timed out while waiting for application. Trying to kill process...");

                    process.Kill();
                    UpgradeVerdict(Verdict.Fail);
                }
            }
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (e.Data == null)
                {
                    _outputWaitHandle.Set();
                }
                else
                {
                    Log.Debug(e.Data);

                    lock (_output)
                    {
                        _output.AppendLine(e.Data);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Suppress - Test plan has been aborted and process is disconnected
            }
        }

        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (e.Data == null)
                {
                    _errorWaitHandle.Set();
                }
                else
                {
                    Log.Error(e.Data);

                    lock (_output)
                    {
                        _output.AppendLine(e.Data);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Suppress - Test plan has been aborted and process is disconnected
            }
        }
    }
}