using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh
{
    [Display("SshQueries",
        Groups: new[] { "TapExtensions", "Steps", "Ssh" })]
    public class SshQueries : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)] public ISsh Dut { get; set; }

        public class Query : ValidatingObject
        {
            [Display("Command", Order: 1)] public string Command { get; set; }

            [Display("Expected Response", Order: 2)]
            public string ExpectedResponse { get; set; }

            [Browsable(false)] [XmlIgnore] public string Response { get; set; }

            [Display("Timeout", Order: 4)]
            [Unit("s")]
            public int Timeout { get; set; }

            public Query()
            {
                // Default values
                Timeout = 5;

                // Validation rules
                Rules.Add(() => Timeout >= 0,
                    "Must be greater than or equal to zero", nameof(Timeout));
            }
        }

        [Display("Queries", Order: 5)] public List<Query> Queries { get; set; }

        #endregion

        public SshQueries()
        {
            // Default values
            Queries = new List<Query>
            {
                new Query { Command = "pwd", ExpectedResponse = "/", Timeout = 5 },
                new Query { Command = "ls", ExpectedResponse = "", Timeout = 5 },
                new Query { Command = "my_var=Hello_World", ExpectedResponse = "", Timeout = 5 },
                new Query { Command = "echo $my_var", ExpectedResponse = "Hello_World", Timeout = 5 }
            };
        }

        public override void Run()
        {
            try
            {
                foreach (var q in Queries)
                {
                    if (!Dut.Query(q.Command, q.Timeout, out var response))
                        throw new InvalidOperationException(
                            "Exit status of ssh command was not 0");

                    if (!response.Contains(q.ExpectedResponse))
                        throw new InvalidOperationException(
                            $"Cannot find '{q.ExpectedResponse}' in the response to the ssh command of '{q.Command}'");
                }

                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }
    }
}