using System.Collections.Generic;
using OpenTap;
using TapExtensions.Interfaces.Dio;
using TapExtensions.Interfaces.Switch;

namespace TapExtensions.Instruments.Switch
{
    [Display("DioToSwitch",
        Groups: new[] { "TapExtensions", "Instruments", "Switch" })]
    public class DioToSwitch : Instrument, ISwitch
    {
        [Display("Dio")] public IDio Dio { get; set; }

        public DioToSwitch()
        {
            Name = nameof(DioToSwitch);
        }

        public void SetRoute(string routeName)
        {
            Log.Debug($"SetRoute('{routeName}')");

            const short dioNumber = 2;
            const EOutputState dioState = EOutputState.Off;
            Dio.SetOutputState(new List<short> { dioNumber }, new List<EOutputState> { dioState });
        }
    }
}