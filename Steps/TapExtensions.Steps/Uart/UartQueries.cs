using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using OpenTap;
using TapExtensions.Interfaces.Uart;

namespace TapExtensions.Steps.Uart
{
    [Display("UartQueries",
        Groups: new[] { "TapExtensions", "Steps", "Uart" })]
    public class UartQueries : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)] public IUart Uart { get; set; }

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

        private readonly Random _rnd = new Random();

        public UartQueries()
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
            var endOfMessage = _rnd.Next(10000, 99999).ToString();
            // const string expectedEndOfMessage = "$";
            var cmdWithEom = $"{command}; echo {endOfMessage}";
            var response = Uart.Query(cmdWithEom, endOfMessage, timeout);

            if (!response.Contains(expectedResponse))
                throw new InvalidOperationException(
                    $"Cannot find '{expectedResponse}' in the response to the ssh command of '{command}'");
        }
    }
}