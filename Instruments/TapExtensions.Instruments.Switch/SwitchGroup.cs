using System;
using System.Collections.Generic;
using System.Linq;
using OpenTap;
using TapExtensions.Interfaces.Switch;

namespace TapExtensions.Instruments.Switch
{
    [Display("SwitchGroup",
        Groups: new[] { "TapExtensions", "Instruments", "Switch" })]
    public class SwitchGroup : Instrument, ISwitch
    {
        #region Settings

        [Display("List of Switch Instruments")]
        public List<ISwitch> SwitchInstruments
        {
            get
            {
                _switchInstruments.RemoveAll(item => item == null || item.Name == nameof(SwitchGroup));
                return _switchInstruments;
            }
            set => _switchInstruments = value;
        }

        private List<ISwitch> _switchInstruments = new List<ISwitch>();

        #endregion

        public SwitchGroup()
        {
            Name = "SwitchGroup";
        }

        public void SetRoute(string routeName)
        {
            if (string.IsNullOrWhiteSpace(routeName))
                return;

            var routeGroup = GetRouteGroup(routeName);
            Log.Debug($"routeGroup = '{routeGroup}'");

            // Split SsuRoutes string into multiple route strings
            var separators = new List<char> { ',', '\t', '\n', '\r' };
            var parts = routeGroup.Split(separators.ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

            // Remove all white-spaces from the beginning and end of the route strings
            var routes = new List<string>();
            foreach (var part in parts)
                routes.Add(part.Trim());

            foreach (var route in routes)
            {
                var routeString = route.Split('_').Last().Trim();
                var instrumentName = GetInstrumentName(route.Split('_').First());
                var switchInstrument = SwitchInstruments.First(x => x.Name.Equals(instrumentName));
                if (switchInstrument == null)
                    throw new InvalidOperationException($"Cannot find Switch Instrument named '{instrumentName}'");

                // Log.Debug($"Setting route '{routeString}' on {switchInstrument}");
                switchInstrument.SetRoute(routeString);
            }
        }

        private static Dictionary<string, string> _instruments = new Dictionary<string, string>
        {
            { "A1", "AppSsu1" },
            { "A2", "AppSsu2" },
            { "A3", "AppSsu3" },
            { "A4", "AppSsu4" },
            { "P1", "PxiSwitch" }
        };

        internal static string GetInstrumentName(string instrumentCode)
        {
            if (!_instruments.TryGetValue(instrumentCode, out var instrumentName))
                throw new ArgumentException(
                    $"{nameof(_instruments)} does not have an entry for '{instrumentCode}'.");

            return instrumentName;
        }

        private static Dictionary<string, string> _routeGroups = new Dictionary<string, string>
        {
            // @formatter:off

            // DownLink B7
            { "Rf1",  "AppSsu1.ANT1toSA,  PxiDio.Cto2" },
            { "Rf2",  "AppSsu1.ANT2toSA,  PxiDio.Cto2" },
            { "Rf3",  "AppSsu1.ANT3toSA,  PxiDio.Cto2" },
            { "Rf4",  "AppSsu1.ANT4toSA,  PxiDio.Cto2" },
            { "Rf5",  "AppSsu1.ANT5toSA,  PxiDio.Cto2" },
            { "Rf6",  "AppSsu1.ANT6toSA,  PxiDio.Cto2" },
            { "Rf7",  "AppSsu1.ANT7toSA,  PxiDio.Cto2" },
            { "Rf8",  "AppSsu1.ANT8toSA,  PxiDio.Cto2" },

            { "Rf9",  "AppSsu4.ANT1toSA,  PxiDio.Cto3" },
            { "Rf10", "AppSsu4.ANT2toSA,  PxiDio.Cto3" },
            { "Rf11", "AppSsu4.ANT3toSA,  PxiDio.Cto3" },
            { "Rf12", "AppSsu4.ANT4toSA,  PxiDio.Cto3" },
            { "Rf13", "AppSsu4.ANT5toSA,  PxiDio.Cto3" },
            { "Rf14", "AppSsu4.ANT6toSA,  PxiDio.Cto3" },
            { "Rf15", "AppSsu4.ANT7toSA,  PxiDio.Cto3" },
            { "Rf16", "AppSsu4.ANT8toSA,  PxiDio.Cto3" },


            // DownLink B1B3
            { "Rf17", "AppSsu1.ANT9toSA,  PxiDio.Cto2" },
            { "Rf18", "AppSsu1.ANT10toSA, PxiDio.Cto2" },
            { "Rf19", "AppSsu1.ANT11toSA, PxiDio.Cto2" },
            { "Rf20", "AppSsu1.ANT12toSA, PxiDio.Cto2" },
            { "Rf21", "AppSsu1.ANT13toSA, PxiDio.Cto2" },
            { "Rf22", "AppSsu1.ANT14toSA, PxiDio.Cto2" },
            { "Rf23", "AppSsu1.ANT15toSA, PxiDio.Cto2" },
            { "Rf24", "AppSsu1.ANT16toSA, PxiDio.Cto2" },

            { "Rf25", "AppSsu4.ANT9toSA,  PxiDio.Cto3" },
            { "Rf26", "AppSsu4.ANT10toSA, PxiDio.Cto3" },
            { "Rf27", "AppSsu4.ANT11toSA, PxiDio.Cto3" },
            { "Rf28", "AppSsu4.ANT12toSA, PxiDio.Cto3" },
            { "Rf29", "AppSsu4.ANT13toSA, PxiDio.Cto3" },
            { "Rf30", "AppSsu4.ANT14toSA, PxiDio.Cto3" },
            { "Rf31", "AppSsu4.ANT15toSA, PxiDio.Cto3" },
            { "Rf32", "AppSsu4.ANT16toSA, PxiDio.Cto3" },


            // DownLink B1B3
            { "Rf65", "AppSsu2.ANT16toSA, PxiDio.Cto1" },
            { "Rf66", "AppSsu2.ANT15toSA, PxiDio.Cto1" },
            { "Rf67", "AppSsu2.ANT14toSA, PxiDio.Cto1" },
            { "Rf68", "AppSsu2.ANT13toSA, PxiDio.Cto1" },
            { "Rf69", "AppSsu2.ANT12toSA, PxiDio.Cto1" },
            { "Rf70", "AppSsu2.ANT11toSA, PxiDio.Cto1" },
            { "Rf71", "AppSsu2.ANT10toSA, PxiDio.Cto1" },
            { "Rf72", "AppSsu2.ANT9toSA,  PxiDio.Cto1" },

            { "Rf73", "AppSsu3.ANT16toSA, PxiDio.Cto4" },
            { "Rf74", "AppSsu3.ANT15toSA, PxiDio.Cto4" },
            { "Rf75", "AppSsu3.ANT14toSA, PxiDio.Cto4" },
            { "Rf76", "AppSsu3.ANT13toSA, PxiDio.Cto4" },
            { "Rf77", "AppSsu3.ANT12toSA, PxiDio.Cto4" },
            { "Rf78", "AppSsu3.ANT11toSA, PxiDio.Cto4" },
            { "Rf79", "AppSsu3.ANT10toSA, PxiDio.Cto4" },
            { "Rf80", "AppSsu3.ANT9toSA,  PxiDio.Cto4" },


            // DownLink B7
            { "Rf81", "AppSsu2.ANT8toSA,  PxiDio.Cto1" },
            { "Rf82", "AppSsu2.ANT7toSA,  PxiDio.Cto1" },
            { "Rf83", "AppSsu2.ANT6toSA,  PxiDio.Cto1" },
            { "Rf84", "AppSsu2.ANT5toSA,  PxiDio.Cto1" },
            { "Rf85", "AppSsu2.ANT4toSA,  PxiDio.Cto1" },
            { "Rf86", "AppSsu2.ANT3toSA,  PxiDio.Cto1" },
            { "Rf87", "AppSsu2.ANT2toSA,  PxiDio.Cto1" },
            { "Rf88", "AppSsu2.ANT1toSA,  PxiDio.Cto1" },

            { "Rf89", "AppSsu3.ANT8toSA,  PxiDio.Cto4" },
            { "Rf90", "AppSsu3.ANT7toSA,  PxiDio.Cto4" },
            { "Rf91", "AppSsu3.ANT6toSA,  PxiDio.Cto4" },
            { "Rf92", "AppSsu3.ANT5toSA,  PxiDio.Cto4" },
            { "Rf93", "AppSsu3.ANT4toSA,  PxiDio.Cto4" },
            { "Rf94", "AppSsu3.ANT3toSA,  PxiDio.Cto4" },
            { "Rf95", "AppSsu3.ANT2toSA,  PxiDio.Cto4" },
            { "Rf96", "AppSsu3.ANT1toSA,  PxiDio.Cto4" }

            // @formatter:on
        };

        internal static string GetRouteGroup(string routeName)
        {
            if (!_routeGroups.TryGetValue(routeName, out var routeGroup))
                throw new ArgumentException(
                    $"{nameof(_routeGroups)} does not have an entry for '{routeName}'.");

            return routeGroup;
        }
    }
}