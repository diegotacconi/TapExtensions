using System;
using System.ComponentModel;
using System.IO;
using OpenTap;

namespace TapExtensions.Settings.Integrity
{
    [Display("Integrity", "Integrity Settings")]
    [Browsable(true)]
    public class IntegritySettings : ComponentSettings<IntegritySettings>, ITestPlanRunMonitor
    {
        [Display("Verify TestPlan XML", Order: 1,
            Description:
            "Verifies the XML of the test plan in memory with the XML found in the corresponding test plan on disk.\n" +
            "This prevents the test plan from running if there is any discrepancy.")]
        public bool VerifyTestPlanXml { get; set; }

        private static readonly TraceSource Log = OpenTap.Log.CreateSource("Integrity");

        public IntegritySettings()
        {
            VerifyTestPlanXml = false;
        }

        public void EnterTestPlanRun(TestPlanRun plan)
        {
            try
            {
                if (VerifyTestPlanXml)
                {
                    var pathParam = plan.Parameters["TestPlanPath"];

                    if (!(pathParam is string path)) return;
                    if (path == "NULL" || string.IsNullOrEmpty(path)) return;
                    if (!File.Exists(path)) return;

                    var xmlFromFile = File.ReadAllText(path);
                    var xmlFromMemory = plan.TestPlanXml;

                    if (!xmlFromMemory.Equals(xmlFromFile))
                    {
                        Log.Error($"TestPlan in memory is NOT equal to TestPlan found on disk at {path}");
                        plan.MainThread.Abort();
                    }

                    Log.Debug($"TestPlan in memory is equal to TestPlan found on disk at {path}");
                }
            }
            catch (Exception ex)
            {
                // It is crucial that this method does not prevent test plans from executing since it cannot be disabled.
                // Just log the error and return
                Log.Warning(ex.Message);
            }
        }

        public void ExitTestPlanRun(TestPlanRun plan)
        {
        }
    }
}