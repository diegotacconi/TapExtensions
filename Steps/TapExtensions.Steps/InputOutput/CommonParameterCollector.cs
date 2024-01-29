using OpenTap;

namespace TapExtensions.Steps.InputOutput
{
    [Display("CommonParameterCollector",
        Groups: new[] { "TapExtensions", "Steps", "InputOutput" })]
    public class CommonParameterCollector : TestStep
    {
        [Output]
        public Instrument Instrument1 { get; set; }

        [Output]
        public Instrument Instrument2 { get; set; }

        [Output]
        public Instrument Instrument3 { get; set; }

        public override void Run()
        {
            Log.Debug($"Instrument1 = {Instrument1}");
            Log.Debug($"Instrument2 = {Instrument2}");
            Log.Debug($"Instrument3 = {Instrument3}");
        }
    }
}