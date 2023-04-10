using System;
using OpenTap;

namespace TapExtensions.Steps.Publish
{
    public abstract class TestStepBaseWithCustomPublish : TestStep
    {
        internal ICustomPublish ICustomPublish;

        [Display("Suppress result publishing", Group: "Common", Description: "If set to true, step does not publish any results. Verdict is still always set.")]
        public bool SuppressResult { get; set; } = false;

        [Display("Make unique", Group: "Common", Description: "If set to true, step will add a random result parameter to prevend duplicate result names")]
        public bool MakeUnique { get; set; } = false;

        private static Random rd = new Random();
        private static readonly byte[] buffer = new byte[8];
        private static string RandomBase64String
        {
            get
            {
                if (rd == null) rd = new Random();
                rd.NextBytes(buffer);
                return Convert.ToBase64String(buffer);
            }
        }

        public override void Run()
        {
            throw new NotImplementedException();
        }

        protected internal virtual Verdict Publish<T>(string resultName, T result, T lowerLimit, T upperLimit, string unit, string comment) where T : IComparable
        {
            UpdateUniqueParameter();
            InitIResults();
            resultName = resultName.Replace(Name, this.GetFormattedName());
            var verdict = ICustomPublish.Publish(resultName, result, lowerLimit, upperLimit, unit, comment, SuppressResult);
            UpgradeVerdict(verdict);
            return verdict;
        }

        protected internal virtual Verdict Publish<T>(string resultName, T result, T lowerLimit, T upperLimit, EBase baseNumber, string unit, string comment) where T : IComparable
        {
            UpdateUniqueParameter();
            InitIResults();
            resultName = resultName.Replace(Name, this.GetFormattedName());
            var verdict = ICustomPublish.Publish(resultName, result, lowerLimit, upperLimit, baseNumber, unit, comment, SuppressResult);
            UpgradeVerdict(verdict);
            return verdict;
        }

        protected internal virtual Verdict Publish<T>(string resultName, T result, T lowerLimit, T upperLimit, string unit) where T : IComparable
        {
            return Publish(resultName, result, lowerLimit, upperLimit, unit, "");
        }

        protected internal virtual Verdict Publish<T>(string resultName, T result, T lowerLimit, T upperLimit, EBase baseNumber, string unit) where T : IComparable
        {
            return Publish(resultName, result, lowerLimit, upperLimit, baseNumber, unit, "");
        }

        protected internal virtual Verdict Publish<T>(string resultName, T result, T lowerLimit, T upperLimit) where T : IComparable
        {
            return Publish(resultName, result, lowerLimit, upperLimit, "NA", "");
        }

        protected internal virtual Verdict Publish<T>(string resultName, T result, T lowerLimit, T upperLimit, EBase baseNumber) where T : IComparable
        {
            return Publish(resultName, result, lowerLimit, upperLimit, baseNumber, "NA", "");
        }

        protected internal virtual void Publish<T>(string resultName, T result, T lowerLimit, T upperLimit, string unit, Verdict verdict, string comment) where T : IComparable
        {
            UpdateUniqueParameter();
            InitIResults();
            resultName = resultName.Replace(Name, this.GetFormattedName());
            ICustomPublish.Publish<T>(resultName, result, lowerLimit, upperLimit, unit, verdict, comment, SuppressResult);
            UpgradeVerdict(verdict);
        }

        protected internal virtual void Publish<T>(string resultName, T result, T lowerLimit, T upperLimit, EBase baseNumber, string unit, Verdict verdict, string comment) where T : IComparable
        {
            UpdateUniqueParameter();
            InitIResults();
            resultName = resultName.Replace(Name, this.GetFormattedName());
            ICustomPublish.Publish(resultName, result, lowerLimit, upperLimit, baseNumber, unit, verdict, comment, SuppressResult);
            UpgradeVerdict(verdict);
        }

        protected internal virtual void Publish<T>(string resultName, T result, T lowerLimit, T upperLimit, string unit, Verdict verdict) where T : IComparable
        {
            Publish(resultName, result, lowerLimit, upperLimit, unit, verdict, "");
        }

        protected internal virtual void Publish<T>(string resultName, T result, T lowerLimit, T upperLimit, Verdict verdict) where T : IComparable
        {
            Publish(resultName, result, lowerLimit, upperLimit, "NA", verdict, "");
        }

        protected internal virtual void Publish<T>(string resultName, T result, T lowerLimit, T upperLimit,
            EBase baseNumber, string unit, Verdict verdict) where T : IComparable
        {
            Publish(resultName, result, lowerLimit, upperLimit, baseNumber, unit, verdict, "");
        }

        protected internal virtual void Publish<T>(string resultName, T result, T lowerLimit, T upperLimit, EBase baseNumber, Verdict verdict) where T : IComparable
        {
            Publish(resultName, result, lowerLimit, upperLimit, baseNumber, "NA", verdict, "");
        }

        protected internal virtual void Publish<T>(string resultName, T result, T lowerLimit, T upperLimit, Verdict verdict, string comment) where T : IComparable
        {
            Publish(resultName, result, lowerLimit, upperLimit, "NA", verdict, comment);
        }

        protected internal void UpdateUniqueParameter()
        {
            if (!MakeUnique) return;
            StepRun.Parameters.Add(new ResultParameter("TestStepBaseMakeUniqueParameter", RandomBase64String));
        }

        /// <summary> Abort from PlanRun.MainThread is required to abort sequence in OpenTap </summary>
        protected internal void Abort()
        {
            PlanRun.MainThread.Abort();
        }

        /// <summary> Replaces UpgradeVerdict from OpenTap.TestStep </summary>
        protected internal new virtual void UpgradeVerdict(Verdict verdict)
        {
            if (verdict == Verdict.Aborted)
            {
                Abort();
            }

            base.UpgradeVerdict(verdict);
        }

        private void InitIResults()
        {
            if (ICustomPublish != null)
            {
                ICustomPublish.ResultSource = Results;
                return;
            }

            if (Results == null)
            {
                throw new ApplicationException("TestStep::Results is null! Make sure that TestStep::Run is called before calling Publish.");
            }
            ICustomPublish = new CustomPublish(Results);
        }

        /// <summary>
        /// Adds the option to report CPK tags on the comment field of the Publish method, to automate CPK analysis.
        /// <code>Publish(resultName, result, lowLimit, highLimit, unit, <b>EResultTag.Cpk</b>);</code>
        /// </summary>
        [Flags]
        public enum EResultTag
        {
            /// <summary> Cpk process capability - for test results with two-sided test limits </summary>
            Cpk = 0b1,
            /// <summary> Cpk Upper Specification Limit - for test results with one-sided upper test limit only </summary>
            Cpu = 0b10,
            /// <summary> Cpk Lower Specification Limit - for test results with one-sided lower test limit only </summary>
            Cpl = 0b100
        }

        protected internal virtual Verdict Publish<T>(string resultName, T result, T lowerLimit, T upperLimit, string unit, EResultTag tags) where T : IComparable
        {
            return Publish(resultName, result, lowerLimit, upperLimit, unit, tags, "");
        }

        protected internal virtual Verdict Publish<T>(string resultName, T result, T lowerLimit, T upperLimit, string unit, EResultTag tags, string comment) where T : IComparable
        {
            var concatenatedComment = "";

            if (tags != 0)
                concatenatedComment = $"{tags}";

            if (!string.IsNullOrWhiteSpace(comment))
                concatenatedComment += $", {comment}";

            return Publish(resultName, result, lowerLimit, upperLimit, unit, concatenatedComment);
        }
    }
}