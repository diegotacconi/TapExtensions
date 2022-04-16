using System;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.DcPwr;

namespace TapExtensions.Steps.DcPwr
{
    [Display("DcPowerOff",
        Groups: new[] {"TapExtensions", "Steps", "DcPwr"})]
    public class DcPowerOff : TestStep
    {
        [Display("DcPwr", Group: "Instruments",
            Description: "DC Power Supply instrument interface")]
        public IDcPwr DcPwr { get; set; }

        public override void Run()
        {
            try
            {
                if (!DcPwr.IsConnected)
                    throw new InvalidOperationException("Power Supply not connected or initialized!");

                DcPwr.SetOutputState(EState.Off);

                // Publish(Name, true, true, true, "bool");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                // Publish(Name, false, true, true, "bool");
            }
        }
    }
}