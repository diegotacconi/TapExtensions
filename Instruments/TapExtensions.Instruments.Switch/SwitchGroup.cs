using OpenTap;
using System;
using System.Collections.Generic;
using TapExtensions.Interfaces.Switch;

namespace TapExtensions.Instruments.Switch
{
    [Display("SwitchGroup",
        Groups: new[] { "TapExtensions", "Instruments", "Switch" })]
    public class SwitchGroup : Instrument, ISwitch
    {
        public void SetRoute(string routeName)
        {
            throw new NotImplementedException();
        }

        private static Dictionary<string, string> SsuRoutes = new Dictionary<string, string>
        {
            // @formatter:off

            // DownLink B7
            { "Rf1",  "A1_ANT1toSA" },
            { "Rf2",  "A1_ANT2toSA" },
            { "Rf3",  "A1_ANT3toSA" },
            { "Rf4",  "A1_ANT4toSA" },
            { "Rf5",  "A1_ANT5toSA" },
            { "Rf6",  "A1_ANT6toSA" },
            { "Rf7",  "A1_ANT7toSA" },
            { "Rf8",  "A1_ANT8toSA" },
            { "Rf9",  "" },
            { "Rf10", "" },
            { "Rf11", "" },
            { "Rf12", "" },
            { "Rf13", "" },
            { "Rf14", "" },
            { "Rf15", "" },
            { "Rf16", "" },

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
            { "Rf65", "" },
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
            { "Rf90", "" },
            { "Rf91", "" },
            { "Rf92", "" },
            { "Rf93", "" },
            { "Rf94", "" },
            { "Rf95", "" },
            { "Rf96", "" }

            // @formatter:on
        };

        internal static string GetSsuRoute(string rf)
        {
            if (!SsuRoutes.TryGetValue(rf, out var ssuRoute))
                throw new ArgumentException(
                    $"{nameof(SsuRoutes)} does not have an entry " +
                    $"for {nameof(rf)}={rf}.");

            return ssuRoute;
        }
    }
}