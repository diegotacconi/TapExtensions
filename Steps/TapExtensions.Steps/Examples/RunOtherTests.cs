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
                Message = "Hello 1",
                Severity = LogStep.ESeverity.Warning
            };

            stepOne.Run();


            var stepTwo = new LogStep
            {
                Message = "Hello 2",
                Severity = LogStep.ESeverity.Warning
            };

            stepTwo.Run();


            new LogStep { Message = "Hello 3" }.Run();
            new LogStep { Message = "Hello 4" }.Run();
            new LogStep { Message = "Hello 5" }.Run();
        }
    }
}