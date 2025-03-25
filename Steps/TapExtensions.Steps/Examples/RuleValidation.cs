using OpenTap;

namespace TapExtensions.Steps.Examples
{
    [Display("RuleValidation",
        Groups: new[] { "TapExtensions", "Steps", "Examples" },
        Description: "Example of how validation rules works")]
    public class RuleValidation : TestStep
    {
        public int MyInt1 { get; set; } = 2;
        public int MyInt2 { get; set; } = 3;
        public int MyInt3 { get; set; } = -1;
        public bool ShouldBeTrue { get; set; } = false;

        public RuleValidation()
        {
            // Validation rules are usually set up by the constructor.
            // They are soft errors and do not block test plan execution by default.
            // When using the GUI, validation rules are checked when editing.
            // Otherwise, they have to be manually checked (see the Run implementation below).

            // Rule validation with multiple setting values
            Rules.Add(() => MyInt1 + MyInt2 == 6, "MyInt1 + MyInt2 must == 6", nameof(MyInt1), nameof(MyInt2));

            // The error message can also be dynamically generated using a function.
            Rules.Add(() => MyInt3 > 0, () => $"MyInt3 must be greater than 0, but it is {MyInt3}", nameof(MyInt3));

            // Calls a function that returns a boolean. Use nameof to make sure the property name is correctly specified.
            Rules.Add(CheckShouldBeTrueFunc, "Must be true to run", nameof(ShouldBeTrue));
        }

        private bool CheckShouldBeTrueFunc()
        {
            return ShouldBeTrue;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();

            // Check validation rules, and do not run if there are any errors with the input values
            ThrowOnValidationError(true);
        }

        public override void Run()
        {
            // Nothing to do
        }
    }
}