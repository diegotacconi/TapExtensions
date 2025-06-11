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
        [Display("Application", Order: 1, Description: "The name of the application process to run.")]
        [FilePath(FilePathAttribute.BehaviorChoice.Open, "exe")]
        public string Application { get; set; }

        [Display("Command Line Arguments", Order: 2, Description: "The arguments passed to the application process.")]
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
            Description: "Checks the exit code of the application and fails if it is non-zero.")]
        public bool CheckExitCode { get; set; }

        private ManualResetEvent _outputWaitHandle, _errorWaitHandle;
        private readonly StringBuilder _output = new StringBuilder();

        public RunProcess()
        {
            // Default values
            Application = "powershell.exe";
            Arguments = "dir";
            WorkingDirectory = "";
            ExpectedResponse = "";
            Timeout = 10;
            CheckExitCode = true;

            // Validation rules
            Rules.Add(() => Timeout > 0,
                "Timeout must be greater than zero", nameof(Timeout));
        }

        public override void Run()
        {
            ThrowOnValidationError(true);

            try
            {
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

                lock (_output)
                {
                    _output.Clear();
                }

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
                        if (!string.IsNullOrEmpty(ExpectedResponse))
                            lock (_output)
                            {
                                if (!_output.ToString().Contains(ExpectedResponse))
                                    throw new InvalidOperationException(
                                        $"Cannot find expected response of '{ExpectedResponse}'");
                            }

                        if (CheckExitCode && process.ExitCode != 0)
                            throw new InvalidOperationException(
                                "Exit code was not zero");
                    }
                    else
                    {
                        process.OutputDataReceived -= OutputDataReceived;
                        process.ErrorDataReceived -= ErrorDataReceived;
                        process.Kill();
                        throw new InvalidOperationException(
                            "Timed out while waiting for application to end");
                    }
                }

                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
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