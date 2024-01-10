using System;
using System.Collections.Generic;
using System.Text;
using OpenTap;

namespace TapExtensions.Steps.Benchmark
{
    [Display("LogLorem",
        Groups: new[] { "TapExtensions", "Steps", "Benchmark" })]
    public class LogLorem : TestStep
    {
        [Display("MinWords", Order: 1, Description: "Minimum number of words per sentence")]
        public int MinWords { get; set; }

        [Display("MaxWords", Order: 2, Description: "Maximum number of words per sentence")]
        public int MaxWords { get; set; }

        [Display("MinSentences", Order: 3, Description: "Minimum number of sentences per paragraph")]
        public int MinSentences { get; set; }

        [Display("MaxSentences", Order: 4, Description: "Maximum number of sentences per paragraph")]
        public int MaxSentences { get; set; }

        [Display("MinParagraphs", Order: 5, Description: "Minimum number of paragraphs")]
        public int MinParagraphs { get; set; }

        [Display("MaxParagraphs", Order: 6, Description: "Maximum number of paragraphs")]
        public int MaxParagraphs { get; set; }

        [Display("LogMessage", Order: 7, Description: "Optional log message")]
        public string LogMessage { get; set; }

        [Display("TimeDelay", Order: 8, Description: "Optional time delay")]
        [Unit("s")]
        public double TimeDelay { get; set; }

        private static readonly Random Rnd = new Random(DateTime.Now.Millisecond);

        public LogLorem()
        {
            // Default values
            MinWords = 5;
            MaxWords = 10;
            MinSentences = 3;
            MaxSentences = 3;
            MinParagraphs = 10;
            MaxParagraphs = 10;
            TimeDelay = 0;

            // Validation rules
            Rules.Add(() => MinWords >= 0,
                "Must be greater than or equal to zero", nameof(MinWords));
            Rules.Add(() => MaxWords >= 0,
                "Must be greater than or equal to zero", nameof(MaxWords));
            Rules.Add(() => MinSentences >= 0,
                "Must be greater than or equal to zero", nameof(MinSentences));
            Rules.Add(() => MaxSentences >= 0,
                "Must be greater than or equal to zero", nameof(MaxSentences));
            Rules.Add(() => MinParagraphs >= 0,
                "Must be greater than or equal to zero", nameof(MinParagraphs));
            Rules.Add(() => MaxParagraphs >= 0,
                "Must be greater than or equal to zero", nameof(MaxParagraphs));
            Rules.Add(() => MinWords <= MaxWords,
                "Lower number cannot be greater than upper number", nameof(MinWords));
            Rules.Add(() => MinWords <= MaxWords,
                "Lower number cannot be greater than upper number", nameof(MaxWords));
            Rules.Add(() => MinSentences <= MaxSentences,
                "Lower number cannot be greater than upper number", nameof(MinSentences));
            Rules.Add(() => MinSentences <= MaxSentences,
                "Lower number cannot be greater than upper number", nameof(MaxSentences));
            Rules.Add(() => MinParagraphs <= MaxParagraphs,
                "Lower number cannot be greater than upper number", nameof(MinParagraphs));
            Rules.Add(() => MinParagraphs <= MaxParagraphs,
                "Lower number cannot be greater than upper number", nameof(MaxParagraphs));
            Rules.Add(() => TimeDelay >= 0,
                "Must be greater than or equal to zero", nameof(TimeDelay));
        }

        public override void Run()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(LogMessage))
                    Log.Debug(LogMessage);

                var messages = LoremIpsum(MinWords, MaxWords, MinSentences, MaxSentences, MinParagraphs, MaxParagraphs);
                foreach (var message in messages)
                    Log.Debug(message);

                Sleep(TimeDelay);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        private static List<string> LoremIpsum(int minWords, int maxWords, int minSentences, int maxSentences,
            int minParagraphs, int maxParagraphs)
        {
            var words = new[]
            {
                "ad", "adipisicing", "aliqua", "aliquip", "amet", "anim", "aute", "cillum", "commodo", "consectetur",
                "consequat", "culpa", "cupidatat", "deserunt", "do", "dolor", "dolore", "duis", "ea", "eiusmod", "elit",
                "enim", "esse", "est", "et", "eu", "ex", "excepteur", "exercitation", "fugiat", "id", "in",
                "incididunt", "ipsum", "irure", "labore", "laboris", "laborum", "lorem", "magna", "minim", "mollit",
                "nisi", "non", "nostrud", "nulla", "occaecat", "officia", "pariatur", "proident", "qui", "quis",
                "reprehenderit", "sint", "sit", "sunt", "tempor", "ullamco", "ut", "velit", "veniam", "voluptate"
            };

            // var random = new Random();
            var paragraphs = new List<string>();
            var numParagraphs = Rnd.Next(minParagraphs, maxParagraphs + 1);
            for (var p = 0; p < numParagraphs; p++)
            {
                var sentence = new StringBuilder();
                var numSentences = Rnd.Next(minSentences, maxSentences + 1);
                for (var s = 0; s < numSentences; s++)
                {
                    var numWords = Rnd.Next(minWords, maxWords + 1);
                    for (var w = 0; w < numWords; w++)
                    {
                        if (w > 0)
                            sentence.Append(" ");

                        var word = words[Rnd.Next(words.Length)];
                        sentence.Append(w == 0 ? word.FirstCharToUpper() : word);
                    }

                    sentence.Append(". ");
                }

                paragraphs.Add(sentence.ToString());
            }

            return paragraphs;
        }

        private static void Sleep(double timeDelay)
        {
            if (timeDelay > 0)
                TapThread.Sleep(TimeSpan.FromSeconds(timeDelay));
        }
    }

    public static class StringExtensions
    {
        public static string FirstCharToUpper(this string input)
        {
            switch (input)
            {
                case null:
                    throw new ArgumentNullException(nameof(input));
                case "":
                    throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default:
                    return input[0].ToString().ToUpper() + input.Substring(1);
            }
        }
    }
}