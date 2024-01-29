using System;
using OpenTap;

namespace TapExtensions.Steps.InputOutput
{
    [Display("TestStepExample",
        Groups: new[] { "TapExtensions", "Steps", "InputOutput" })]
    public class TestStepExample : TestStep
    {
        public Instrument MyInstrument { get; set; }

        public override void Run()
        {
            if (MyInstrument == null)
                throw new InvalidOperationException();

            Log.Debug($"MyInstrument = {MyInstrument}");
        }
    }
}