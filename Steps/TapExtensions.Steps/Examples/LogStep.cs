using OpenTap;

namespace TapExtensions.Steps.Examples
{

    [Display("LogStep", Groups: new[] { "TapExtensions", "Steps", "Examples" })]
    public class LogStep : TestStep
    {
        [Display("Log Message", "The log message to be output.", Order: -1)]
        public string LogMessage { get; set; }

        public enum ESeverity
        {
            Debug,
            [Display("Information")] Info,
            Warning,
            Error
        }

        [Display("Log Severity", "What log level the message will be written at.")]
        public ESeverity Severity { get; set; }

        public LogStep()
        {
            Severity = ESeverity.Info;
            LogMessage = "";
        }

        public override void Run()
        {
            switch (Severity)
            {
                case ESeverity.Debug:
                    Log.Debug(LogMessage);
                    break;
                case ESeverity.Info:
                    Log.Info(LogMessage);
                    break;
                case ESeverity.Warning:
                    Log.Warning(LogMessage);
                    break;
                case ESeverity.Error:
                    Log.Error(LogMessage);
                    break;
            }
        }
    }
}