// Controlling Radial R573413610 with Keysight M9187A PXI Digital IO
//
// PXI Vext,      W228 Block2 Module3 Pin1,  E11, W640, Red,    +C   (28V)
// PXI Output 2,  W229 Block2 Module5 Pin12, D11, W641, Green,  -1
// PXI Output 6,  W230 Block2 Module5 Pin13, C11, W642, Blue,   -2
// PXI Output 10, W231 Block2 Module5 Pin14, B11, W643, Yellow, -3
// PXI Output 14, W232 Block2 Module5 Pin15, A11, W644, Violet, -4
// PXI Output 18, W233 Block2 Module5 Pin16, A10, W645, Gray,   -5
// PXI Output 22, W234 Block2 Module5 Pin17, B10, W646, Orange, -6

using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private readonly List<short> _channels = new List<short> { 2, 6, 10, 14, 18, 22 };

        public void SetRoute(string routeName)
        {
            Log.Debug($"SetRoute('{routeName}')");

            DioClearOutputStates(EOutputState.Off);

            short dioNumber = 0;
            const EOutputState dioState = EOutputState.Sink;
            switch (routeName)
            {
                case "Cto1":
                    dioNumber = _channels[0];
                    break;
                case "Cto2":
                    dioNumber = _channels[1];
                    break;
                case "Cto3":
                    dioNumber = _channels[2];
                    break;
                case "Cto4":
                    dioNumber = _channels[3];
                    break;
                case "Cto5":
                    dioNumber = _channels[4];
                    break;
                case "Cto6":
                    dioNumber = _channels[5];
                    break;
                default:
                    throw new InvalidOperationException(
                        $@"Case not found for {nameof(routeName)} of '{routeName}'.");
            }

            TapThread.Sleep(100);
            Dio.SetOutputState(new List<short> { dioNumber }, new List<EOutputState> { dioState });
        }

        private void DioClearOutputStates(EOutputState state)
        {
            if (!Enum.IsDefined(typeof(EOutputState), state))
                throw new InvalidEnumArgumentException(nameof(state), (int)state, typeof(EOutputState));

            var states = new List<EOutputState>();

            foreach (var channel in _channels)
                states.Add(state);

            Dio.SetOutputState(_channels, states);
        }
    }
}