using OpenTap;

namespace TapExtensions.Steps.Examples
{
    [Display("LogStep", Groups: new[] { "TapExtensions", "Steps", "Examples" })]
    public class LogStep : TestStep
    {
        [Display("Message", Order: 1)] public string Message { get; set; } = "";

        public enum ESeverity
        {
            Debug,
            Info,
            Warning,
            Error
        }

        [Display("Severity", Order: 2)] public ESeverity Severity { get; set; } = ESeverity.Info;

        public override void Run()
        {
            switch (Severity)
            {
                case ESeverity.Debug:
                    Log.Debug(Message);
                    break;
                case ESeverity.Info:
                    Log.Info(Message);
                    break;
                case ESeverity.Warning:
                    Log.Warning(Message);
                    break;
                case ESeverity.Error:
                    Log.Error(Message);
                    break;
            }
        }
    }
}