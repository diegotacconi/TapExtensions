using OpenTap;

namespace TapExtensions.Steps.Examples
{
    [Display("RunOtherTests", Groups: new[] { "TapExtensions", "Steps", "Examples" })]
    public class RunOtherTests : TestStep
    {
        public override void Run()
        {
            var stepOne = new LogStep
            {
                LogMessage = "Hello World",
                Severity = LogStep.ESeverity.Warning
            };

            stepOne.Run();

            var stepTwo = new LogStep
            {
                LogMessage = "Hello World Again",
                Severity = LogStep.ESeverity.Warning
            };

            stepTwo.Run();
        }
    }
}