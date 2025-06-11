using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using OpenTap;

namespace TapExtensions.Steps.Process
{
    public enum SCPIRegexBehavior
    {
        [Display("Groups As Columns", "Each regex group is treated as a separate column.")]
        GroupsAsDimensions,
        [Display("Groups As Rows", "Each regex group is treated as a separate row, with one column.")]
        GroupsAsResults
    }

    public abstract class RegexOutputStep : TestStep
    {
        [Browsable(false)]
        [XmlIgnore]
        public abstract bool GeneratesOutput { get; }

        private static bool IsValidRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return false;
            try
            {
                Regex.Match("", pattern);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        [EnabledIf("GeneratesOutput", true)]
        [Display("Regular Expression", Group: "Set Verdict", Order: 1.1, Collapsed: true, Description: "The regular expression to apply to the output.")]
        [HelpLink("EditorHelp.chm::/CreatingATestPlan/Working with Test Steps/Using Regex in Output Parameters.html")]
        public Enabled<string> RegularExpressionPattern { get; set; }

        [EnabledIf("GeneratesOutput", true)]
        [EnabledIf("RegularExpressionPattern", true)]
        [Display("Step Verdict on Match", Group: "Set Verdict", Order: 1.2, Collapsed: true, Description: "The verdict of the step when the regex did match the result.")]
        public Verdict VerdictOnMatch { get; set; }

        [EnabledIf("GeneratesOutput", true)]
        [EnabledIf("RegularExpressionPattern", true)]
        [Display("Step Verdict on No Match", Group: "Set Verdict", Order: 1.3, Collapsed: true, Description: "The verdict of the step when the regex did not match the result.")]
        public Verdict VerdictOnNoMatch { get; set; }

        [EnabledIf("GeneratesOutput", true)]
        [Display("Regular Expression", Group: "Results", Order: 1.5, Collapsed: true, Description: "The regular expression to apply to the output.")]
        [HelpLink("EditorHelp.chm::/CreatingATestPlan/Working with Test Steps/Using Regex in Output Parameters.html")]
        public Enabled<string> ResultRegularExpressionPattern { get; set; }

        [EnabledIf("GeneratesOutput", true)]
        [EnabledIf("ResultRegularExpressionPattern", true)]
        [Display("Result Name", Group: "Results", Order: 1.51, Collapsed: true, Description: "The name of the result.")]
        public string ResultName { get; set; }

        [EnabledIf("GeneratesOutput", true)]
        [EnabledIf("ResultRegularExpressionPattern", true)]
        [Display("Regex Behavior", Group: "Results", Order: 1.51, Collapsed: true, Description: "How the step should publish the matched values of the regular expression as a result table.")]
        public SCPIRegexBehavior Behavior { get; set; }

        [EnabledIf("GeneratesOutput", true)]
        [EnabledIf("ResultRegularExpressionPattern", true)]
        [Display("Column Names", Group: "Results", Order: 1.51, Collapsed: true, Description: "The name of the columns of the resulting groups. The titles must be separated by commas.")]
        public string DimensionTitles { get; set; }

        public RegexOutputStep()
        {
            RegularExpressionPattern = new Enabled<string>() { IsEnabled = false, Value = "(.*)" };
            VerdictOnMatch = Verdict.Pass;
            VerdictOnNoMatch = Verdict.Fail;

            ResultRegularExpressionPattern = new Enabled<string>() { IsEnabled = false, Value = "(.*)" };
            Behavior = SCPIRegexBehavior.GroupsAsDimensions;
            ResultName = "Regex Result";
            DimensionTitles = "";

            Rules.Add(new ValidationRule(() => ResultRegularExpressionPattern.IsEnabled == false || IsValidRegex(ResultRegularExpressionPattern.Value), "Invalid regular expression.", "ResultRegularExpressionPattern"));
            Rules.Add(new ValidationRule(() => RegularExpressionPattern.IsEnabled == false || IsValidRegex(RegularExpressionPattern.Value), "Invalid regular expression.", "RegularExpressionPattern"));
        }

        protected void ProcessOutput(string Output)
        {
            if (RegularExpressionPattern.IsEnabled)
            {
                var Matches = Regex.Matches(Output, RegularExpressionPattern.Value);

                if (Matches.Count > 0)
                    UpgradeVerdict(VerdictOnMatch);
                else
                    UpgradeVerdict(VerdictOnNoMatch);
            }

            if (ResultRegularExpressionPattern.IsEnabled)
            {
                var Matches = Regex.Matches(Output, ResultRegularExpressionPattern.Value);

                foreach (Match Match in Matches)
                {
                    if ((Match.Length <= 0) || (!Match.Success))
                        continue;

                    switch (Behavior)
                    {
                        case SCPIRegexBehavior.GroupsAsDimensions:
                            {
                                var Name = ResultName;
                                var titles = DimensionTitles.Split(',').ToList();
                                var results = Match.Groups.OfType<Capture>().Skip(1).Select(x => x.Value).ToList();

                                if (titles.Count != results.Count)
                                {
                                    Log.Error("Number of Column Names ({0}) does not match number of results ({1}).", titles.Count, results.Count);
                                    UpgradeVerdict(Verdict.Error);
                                    return;
                                }

                                Results.Publish(Name, titles, results.ToArray());
                                break;
                            }
                        case SCPIRegexBehavior.GroupsAsResults:
                            {
                                var Name = ResultName;
                                var titles = DimensionTitles.Split(',').ToList();

                                if (titles.Count != 1)
                                {
                                    Log.Error("Number of Column Names ({0}) does not match number of results (1).", titles.Count);
                                    UpgradeVerdict(Verdict.Error);
                                    return;
                                }

                                for (int i = 1; i < Match.Groups.Count; i++)
                                {
                                    Results.Publish(Name, titles, Match.Groups[i].Value);
                                }
                                break;
                            }
                    }
                }
            }
        }
    }
}
