using OpenTap;
using System.ComponentModel;

namespace TapExtensions.Steps.Examples
{
    [Display("PresetDefaultValuesUsingButtons",
        Groups: new[] { "TapExtensions", "Steps", "Examples" })]
    public class PresetDefaultValuesUsingButtons : TestStep
    {
        #region Settings

        public double SomeValue { get; set; }

        #endregion

        [Display("Default values for A", Order: 1, Group: "Presets")]
        [Browsable(true)]
        public void ButtonPresetA()
        {
            SomeValue = 10;
        }

        [Display("Default values for B", Order: 2, Group: "Presets")]
        [Browsable(true)]
        public void ButtonPresetB()
        {
            SomeValue = 20;
        }

        [Display("Default values for C", Order: 3, Group: "Presets")]
        [Browsable(true)]
        public void ButtonPresetC()
        {
            SomeValue = 30;
        }

        public PresetDefaultValuesUsingButtons()
        {
            // Default values
            SomeValue = 0;
        }

        public override void Run()
        {
            Log.Info($"{nameof(SomeValue)} = {SomeValue}");
        }
    }
}