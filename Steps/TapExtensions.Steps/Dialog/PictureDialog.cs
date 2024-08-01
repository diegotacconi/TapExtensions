using System;
using OpenTap;

namespace TapExtensions.Steps.Dialog
{
    [Display("PictureDialog", Groups: new[] { "TapExtensions", "Steps", "Dialog" })]
    public class PictureDialog : TestStepBaseWithDialog
    {
        public override void Run()
        {
            try
            {
                var okayButton = ShowDialog();
                UpgradeVerdict(okayButton ? Verdict.Pass : Verdict.Fail);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }
    }
}