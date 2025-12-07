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
            { "Rf1",  "AppSsu1.ANT1toSA, PxiDio.Cto2" },
            { "Rf2",  "AppSsu1.ANT2toSA" },
            { "Rf3",  "AppSsu1.ANT3toSA" },
            { "Rf4",  "AppSsu1.ANT4toSA" },
            { "Rf5",  "AppSsu1.ANT5toSA" },
            { "Rf6",  "AppSsu1.ANT6toSA" },
            { "Rf7",  "AppSsu1.ANT7toSA" },
            { "Rf8",  "AppSsu1.ANT8toSA" },
            { "Rf9",  "" },
            { "Rf10", "" },
            { "Rf11", "" },
            { "Rf12", "" },
            { "Rf13", "" },
            { "Rf14", "" },
            { "Rf15", "" },
            { "Rf16", "AppSsu4.ANT8toSA, PxiDio.Cto3" },

            // DownLink B1B3
            { "Rf17", "" },
            { "Rf18", "" },
            { "Rf19", "" },
            { "Rf20", "" },
            { "Rf21", "" },
            { "Rf22", "" },
            { "Rf23", "" },
            { "Rf24", "" },
            { "Rf25", "" },
            { "Rf26", "" },
            { "Rf27", "" },
            { "Rf28", "" },
            { "Rf29", "" },
            { "Rf30", "" },
            { "Rf31", "" },
            { "Rf32", "" },

            // DownLink B1B3
            { "Rf65", "AppSsu2.ANT16toSA, PxiDio.Cto1" },
            { "Rf66", "" },
            { "Rf67", "" },
            { "Rf68", "" },
            { "Rf69", "" },
            { "Rf70", "" },
            { "Rf71", "" },
            { "Rf72", "" },
            { "Rf73", "" },
            { "Rf74", "" },
            { "Rf75", "" },
            { "Rf76", "" },
            { "Rf77", "" },
            { "Rf78", "" },
            { "Rf79", "" },
            { "Rf80", "" },
            { "Rf81", "" },

            // DownLink B7
            { "Rf82", "" },
            { "Rf83", "" },
            { "Rf84", "" },
            { "Rf85", "" },
            { "Rf86", "" },
            { "Rf87", "" },
            { "Rf88", "" },
            { "Rf89", "" },
            { "Rf90", "AppSsu3.ANT7toSA, PxiDio.Cto4" },
            { "Rf91", "" },
            { "Rf92", "" },
            { "Rf93", "" },
            { "Rf94", "" },
            { "Rf95", "" },
            { "Rf96", "" }

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