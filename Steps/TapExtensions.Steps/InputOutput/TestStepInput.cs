using System;
using OpenTap;

namespace TapExtensions.Steps.InputOutput
{
    [Display("TestStepInput",
        Groups: new[] { "TapExtensions", "Steps", "InputOutput" })]
    public class TestStepInput : TestStep
    {
        public Input<Instrument> MyInstrument { get; set; }

        public TestStepInput()
        {
            MyInstrument = new Input<Instrument>();
        }

        public override void Run()
        {
            if (MyInstrument == null)
                throw new InvalidOperationException();

            Log.Debug($"MyInstrument = {MyInstrument.Value}");
        }
    }
}