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

        [Display("Dut", Order: 1)] public ISecureShell Dut { get; set; }

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
                Rules.Add(() => Timeout > 0,
                    "Timeout must be greater than zero", nameof(Timeout));
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
                new Query { Command = "ls", Timeout = 5 }
            };
        }

        public override void Run()
        {
            try
            {
                foreach (var q in Queries)
                    RunQuery(q.Command, q.ExpectedResponse, q.Timeout);

                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }

        private void RunQuery(string command, string expectedResponse, int timeout)
        {
            var okResult = Dut.SendSshQuery(command, timeout, out var response);
            if (!okResult)
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{command}'");

            if (!string.IsNullOrEmpty(expectedResponse) && !response.Contains(expectedResponse))
                throw new InvalidOperationException(
                    $"Cannot find '{expectedResponse}' in the response to the command of '{command}'");
        }
    }
}