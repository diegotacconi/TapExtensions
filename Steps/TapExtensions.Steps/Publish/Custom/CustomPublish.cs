using OpenTap;
using System;
using System.Collections.Generic;

namespace TapExtensions.Steps.Publish.Custom
{
    public class CustomPublish : ICustomPublish
    {
        public ResultSource ResultSource { get; set; }

        public CustomPublish(ResultSource results)
        {
            ResultSource = results ?? throw new ArgumentNullException(nameof(results));
        }

        #region privates

        private static void CheckListCounts(int namesCount, int resultCount, int lowLimitCount, int highLimitCount)
        {
            if (resultCount == 0) throw new ArgumentException(@"Value cannot be an empty collection.", nameof(resultCount));
            if (lowLimitCount == 0) throw new ArgumentException(@"Value cannot be an empty collection.", nameof(lowLimitCount));
            if (highLimitCount == 0) throw new ArgumentException(@"Value cannot be an empty collection.", nameof(highLimitCount));

            if (namesCount == resultCount && namesCount == lowLimitCount && namesCount == highLimitCount) return;

            throw new ArgumentException(
                $"Result names list ({namesCount} nodes), results ({resultCount} nodes), low limits ({lowLimitCount} nodes), high limits ({highLimitCount} nodes) must have equal amount of nodes!");
        }

        private static void CheckParameters<T>(List<string> names, List<T> results, List<T> lowLimits, List<T> highLimits, List<string> units)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));
            if (results == null) throw new ArgumentNullException(nameof(results));
            if (lowLimits == null) throw new ArgumentNullException(nameof(lowLimits));
            if (highLimits == null) throw new ArgumentNullException(nameof(highLimits));
            if (units == null) throw new ArgumentNullException(nameof(units));
            if (names.Count == 0) throw new ArgumentException(@"Value cannot be an empty collection.", nameof(names));
            if (results.Count == 0) throw new ArgumentException(@"Value cannot be an empty collection.", nameof(results));
            if (lowLimits.Count == 0) throw new ArgumentException(@"Value cannot be an empty collection.", nameof(lowLimits));
            if (highLimits.Count == 0) throw new ArgumentException(@"Value cannot be an empty collection.", nameof(highLimits));
            if (units.Count == 0) throw new ArgumentException(@"Value cannot be an empty collection.", nameof(units));
        }

        private static Verdict CompareResultAgainstLimit<T>(T result, T lowLimit, T highLimit) where T : IComparable
        {
            if (result is bool boolResult && lowLimit is bool boolLowLimit && highLimit is bool boolHighLimit)
            {
                if (boolResult == boolLowLimit && boolResult == boolHighLimit)
                {
                    return Verdict.Pass;
                }
                return Verdict.Fail;
            }

            if (result is string stringResult && lowLimit is string stringLowLimit && highLimit is string stringHighLimit)
            {
                if (stringResult.Equals(stringLowLimit) && stringResult.Equals(stringHighLimit))
                {
                    return Verdict.Pass;
                }
                return Verdict.Fail;
            }

            if (result.CompareTo(lowLimit) < 0 || result.CompareTo(highLimit) > 0)
            {
                return Verdict.Fail;
            }
            return Verdict.Pass;
        }

        private static double ConvertNotNumbers(double value, out Verdict verdict)
        {
            if (IsInfinityOrNaN(value))
            {
                verdict = Verdict.Fail;
                return double.MinValue;
            }

            verdict = Verdict.Pass;
            return value;
        }

        private static float ConvertNotNumbers(float value, out Verdict verdict)
        {
            if (IsInfinityOrNaN(value))
            {
                verdict = Verdict.Fail;
                return float.MinValue;
            }
            verdict = Verdict.Pass;
            return value;
        }

        private static string ConvertBase<T>(T value, EBase baseNumber)
        {
            var result = string.Empty;
            string preFix;

            switch (baseNumber)
            {
                case EBase.Base2:
                    preFix = "0b";
                    break;
                case EBase.Base8:
                    preFix = "0o";
                    break;
                case EBase.Base16:
                    preFix = "0x";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(baseNumber), baseNumber, @"Only Binary (EBase.Base2), Octal (EBase.Base8) and Hex (EBase.Base16) and valid values");
            }

            switch (value)
            {
                case short shortRes when value is short:
                    result += Convert.ToString(shortRes, (int)baseNumber);
                    break;
                case ushort ushortRes when value is ushort:
                    result += Convert.ToString(ushortRes, (int)baseNumber);
                    break;
                case int iRes when value is int:
                    result += Convert.ToString(iRes, (int)baseNumber);
                    break;
                case uint uiRes when value is uint:
                    result += Convert.ToString(uiRes, (int)baseNumber);
                    break;
                case long lRes when value is long:
                    result += Convert.ToString(lRes, (int)baseNumber);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, @"Publish with Base only supports integer types.");
            }

            return preFix + result.ToUpper();
        }

        private static bool IsInfinityOrNaN(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value);
        }

        private static bool IsInfinityOrNaN(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value);
        }

        private static void StringNullCheck(string stringToCheck, string nameOfString)
        {
            if (stringToCheck == null) throw new ArgumentNullException(nameOfString, @"Value cannot be null!");
        }

        private static void StringNullOrWhiteSpaceCheck(string stringToCheck, string nameOfString)
        {
            if (string.IsNullOrWhiteSpace(stringToCheck)) throw new ArgumentException(@"Value cannot be null or whitespace!", nameOfString);
        }

        private void UpgradeVerdict(ref Verdict originalVerdict, Verdict verdict)
        {
            if (originalVerdict < verdict)
                originalVerdict = verdict;
        }

        #region double

        private Verdict Publish(string name, double result, double lowLimit, double highLimit, string unit, string comment = null, bool suppressResult = false)
        {
            var verdict = Verdict.NotSet;
            result = ConvertNotNumbers(result, out var convertVerdict);
            UpgradeVerdict(ref verdict, convertVerdict);
            lowLimit = ConvertNotNumbers(lowLimit, out convertVerdict);
            UpgradeVerdict(ref verdict, convertVerdict);
            highLimit = ConvertNotNumbers(highLimit, out convertVerdict);
            UpgradeVerdict(ref verdict, convertVerdict);

            UpgradeVerdict(ref verdict, CompareResultAgainstLimit(result, lowLimit, highLimit));

            if (!suppressResult)
            {
                ResultSource.Publish(new CustomResult<double>(name, result, lowLimit, highLimit, unit, verdict, comment));
            }

            return verdict;
        }

        private Verdict Publish(List<string> names, List<double> results, List<double> lowLimits, List<double> highLimits, List<string> units, string comment = null, bool suppressResult = false)
        {
            var overAllVerdict = Verdict.NotSet;
            for (var i = 0; i < results.Count; i++)
            {
                StringNullCheck(units[i], "units@" + i);
                StringNullOrWhiteSpaceCheck(names[i], "names@" + i);

                var verdict = Verdict.NotSet;
                results[i] = ConvertNotNumbers(results[i], out var convertVerdict);
                UpgradeVerdict(ref verdict, convertVerdict);

                lowLimits[i] = ConvertNotNumbers(lowLimits[i], out convertVerdict);
                UpgradeVerdict(ref verdict, convertVerdict);

                highLimits[i] = ConvertNotNumbers(highLimits[i], out convertVerdict);
                UpgradeVerdict(ref verdict, convertVerdict);

                UpgradeVerdict(ref verdict, CompareResultAgainstLimit(results[i], lowLimits[i], highLimits[i]));
                UpgradeVerdict(ref overAllVerdict, verdict);

                if (!suppressResult)
                {
                    ResultSource.Publish(new CustomResult<double>(names[i], results[i], lowLimits[i], highLimits[i], units[i], verdict, comment));
                }
            }

            return overAllVerdict;
        }

        private void Publish(string name, double result, double lowLimit, double highLimit, string unit, Verdict verdict, string comment = null, bool suppressResult = false)
        {
            if (IsInfinityOrNaN(result))
            {
                result = double.MinValue;
            }

            if (IsInfinityOrNaN(lowLimit))
            {
                lowLimit = double.MinValue;
            }

            if (IsInfinityOrNaN(highLimit))
            {
                highLimit = double.MinValue;
            }

            if (!suppressResult)
            {
                ResultSource.Publish(new CustomResult<double>(name, result, lowLimit, highLimit, unit, verdict, comment));
            }
        }

        #endregion

        #region bool

        private Verdict Publish(string name, bool result, bool lowLimit, bool highLimit, string unit, string comment = null, bool suppressResult = false)
        {
            Verdict verdict = Verdict.NotSet;
            UpgradeVerdict(ref verdict, CompareResultAgainstLimit(result, lowLimit, highLimit));

            if (!suppressResult)
            {
                ResultSource.Publish(new CustomResult<bool>(name, result, lowLimit, highLimit, unit, verdict, comment));
            }

            return verdict;
        }

        private Verdict Publish(List<string> names, List<bool> results, List<bool> lowLimits, List<bool> highLimits, List<string> units, string comment = null, bool suppressResult = false)
        {
            CheckParameters(names, results, lowLimits, highLimits, units);

            CheckListCounts(names.Count, results.Count, lowLimits.Count, highLimits.Count);

            var overAllVerdict = Verdict.NotSet;
            for (var i = 0; i < results.Count; i++)
            {
                StringNullCheck(units[i], "units@" + i);
                StringNullOrWhiteSpaceCheck(names[i], "names@" + i);

                var verdict = Verdict.NotSet;
                UpgradeVerdict(ref verdict, CompareResultAgainstLimit(results[i], lowLimits[i], highLimits[i]));
                UpgradeVerdict(ref overAllVerdict, verdict);

                if (!suppressResult)
                {
                    ResultSource.Publish(new CustomResult<bool>(names[i], results[i], lowLimits[i], highLimits[i], units[i], verdict, comment));
                }
            }

            return overAllVerdict;
        }

        #endregion

        #region string

        private Verdict Publish(string name, string result, string lowLimit, string highLimit, string unit, string comment = null, bool suppressResult = false)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (lowLimit == null) throw new ArgumentNullException(nameof(lowLimit));
            if (highLimit == null) throw new ArgumentNullException(nameof(highLimit));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(@"Value cannot be null or whitespace.", nameof(name));

            var verdict = Verdict.NotSet;
            UpgradeVerdict(ref verdict, CompareResultAgainstLimit(result, lowLimit, highLimit));

            if (!suppressResult)
            {
                ResultSource.Publish(new CustomResult<string>(name, result, lowLimit, highLimit, unit, verdict, comment));
            }

            return verdict;
        }

        private Verdict Publish(List<string> names, List<string> results, List<string> lowLimits, List<string> highLimits, List<string> units, string comment = null, bool suppressResult = false)
        {
            CheckParameters(names, results, lowLimits, highLimits, units);

            CheckListCounts(names.Count, results.Count, lowLimits.Count, highLimits.Count);

            var overAllVerdict = Verdict.NotSet;
            for (var i = 0; i < results.Count; i++)
            {
                StringNullCheck(units[i], "units@" + i);
                StringNullOrWhiteSpaceCheck(names[i], "names@" + i);
                if (results[i] == null) throw new ArgumentNullException("results" + " @ " + i);
                if (lowLimits[i] == null) throw new ArgumentNullException("lowLimits" + " @ " + i);
                if (highLimits[i] == null) throw new ArgumentNullException("highLimits" + " @ " + i);

                var verdict = Verdict.Pass;
                UpgradeVerdict(ref verdict, CompareResultAgainstLimit(results[i], lowLimits[i], highLimits[i]));
                UpgradeVerdict(ref overAllVerdict, verdict);

                if (!suppressResult)
                {
                    ResultSource.Publish(new CustomResult<string>(names[i], results[i], lowLimits[i], highLimits[i], units[i], verdict, comment));
                }
            }

            return overAllVerdict;
        }

        private void Publish(string name, string result, string lowLimit, string highLimit, string unit, Verdict verdict, string comment = null, bool suppressResult = false)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (lowLimit == null) throw new ArgumentNullException(nameof(lowLimit));
            if (highLimit == null) throw new ArgumentNullException(nameof(highLimit));

            if (!suppressResult)
            {
                ResultSource.Publish(new CustomResult<string>(name, result, lowLimit, highLimit, unit, verdict, comment));
            }
        }

        #endregion

        #region float

        private Verdict Publish(string name, float result, float lowLimit, float highLimit, string unit, string comment = null, bool suppressResult = false)
        {
            var verdict = Verdict.NotSet;
            result = ConvertNotNumbers(result, out var convertVerdict);
            UpgradeVerdict(ref verdict, convertVerdict);
            lowLimit = ConvertNotNumbers(lowLimit, out convertVerdict);
            UpgradeVerdict(ref verdict, convertVerdict);
            highLimit = ConvertNotNumbers(highLimit, out convertVerdict);
            UpgradeVerdict(ref verdict, convertVerdict);

            UpgradeVerdict(ref verdict, CompareResultAgainstLimit(result, lowLimit, highLimit));

            if (!suppressResult)
            {
                ResultSource.Publish(new CustomResult<float>(name, result, lowLimit, highLimit, unit, verdict, comment));
            }

            return verdict;
        }

        private Verdict Publish(List<string> names, List<float> results, List<float> lowLimits, List<float> highLimits, List<string> units, string comment = null, bool suppressResult = false)
        {
            CheckParameters(names, results, lowLimits, highLimits, units);

            CheckListCounts(names.Count, results.Count, lowLimits.Count, highLimits.Count);

            var overAllVerdict = Verdict.NotSet;
            for (var i = 0; i < results.Count; i++)
            {
                StringNullCheck(units[i], "units@" + i);
                StringNullOrWhiteSpaceCheck(names[i], "names@" + i);

                var verdict = Verdict.NotSet;
                results[i] = ConvertNotNumbers(results[i], out var convertVerdict);
                UpgradeVerdict(ref verdict, convertVerdict);

                lowLimits[i] = ConvertNotNumbers(lowLimits[i], out convertVerdict);
                UpgradeVerdict(ref verdict, convertVerdict);

                highLimits[i] = ConvertNotNumbers(highLimits[i], out convertVerdict);
                UpgradeVerdict(ref verdict, convertVerdict);

                UpgradeVerdict(ref verdict, CompareResultAgainstLimit(results[i], lowLimits[i], highLimits[i]));
                UpgradeVerdict(ref overAllVerdict, verdict);

                if (!suppressResult)
                {
                    ResultSource.Publish(new CustomResult<float>(names[i], results[i], lowLimits[i], highLimits[i], units[i], verdict, comment));
                }
            }

            return overAllVerdict;
        }

        private void Publish(string name, float result, float lowLimit, float highLimit, string unit, Verdict verdict, string comment = null, bool suppressResult = false)
        {
            if (IsInfinityOrNaN(result))
            {
                result = float.MinValue;
            }

            if (IsInfinityOrNaN(lowLimit))
            {
                lowLimit = float.MinValue;
            }

            if (IsInfinityOrNaN(highLimit))
            {
                highLimit = float.MinValue;
            }

            if (!suppressResult)
            {
                ResultSource.Publish(new CustomResult<float>(name, result, lowLimit, highLimit, unit, verdict, comment));
            }
        }

        #endregion

        #endregion

        #region publics

        public Verdict Publish<T>(string name, T result, T lowLimit, T highLimit, string unit, string comment = null, bool suppressResult = false) where T : IComparable
        {
            StringNullCheck(unit, nameof(unit));
            StringNullOrWhiteSpaceCheck(name, nameof(name));

            var verdict = Verdict.NotSet;

            if (result is float floatResult && lowLimit is float floatLowLimit && highLimit is float floatHighLimit)
            {
                return Publish(name, floatResult, floatLowLimit, floatHighLimit, unit, comment, suppressResult);
            }

            if (result is string stringResult && lowLimit is string stringLowLimit && highLimit is string stringHighLimit)
            {
                return Publish(name, stringResult, stringLowLimit, stringHighLimit, unit, comment, suppressResult);
            }

            if (result is double doubleResult && lowLimit is double doubleLowLimit && highLimit is double doubleHighLimit)
            {
                return Publish(name, doubleResult, doubleLowLimit, doubleHighLimit, unit, comment, suppressResult);
            }

            if (result is bool boolResult && lowLimit is bool boolLowLimit && highLimit is bool boolHighLimit)
            {
                return Publish(name, boolResult, boolLowLimit, boolHighLimit, unit, comment, suppressResult);
            }

            UpgradeVerdict(ref verdict, CompareResultAgainstLimit(result, lowLimit, highLimit));

            if (!suppressResult)
            {
                ResultSource.Publish(new CustomResult<T>(name, result, lowLimit, highLimit, unit, verdict, comment));
            }

            return verdict;
        }

        public Verdict Publish<T>(string name, T result, T lowLimit, T highLimit, EBase baseNumber, string unit, string comment = null, bool suppressResult = false) where T : IComparable
        {
            StringNullCheck(unit, nameof(unit));
            StringNullOrWhiteSpaceCheck(name, nameof(name));

            var verdict = Verdict.NotSet;

            UpgradeVerdict(ref verdict, CompareResultAgainstLimit(result, lowLimit, highLimit));

            if (!suppressResult)
            {
                ResultSource.Publish(new CustomResult<string>(name, ConvertBase(result, baseNumber), ConvertBase(lowLimit, baseNumber), ConvertBase(highLimit, baseNumber), unit, verdict, comment));
            }

            return verdict;
        }

        public Verdict Publish<T>(List<string> names, List<T> results, List<T> lowLimits, List<T> highLimits, List<string> units, string comment = null, bool suppressResult = false) where T : IComparable
        {
            CheckParameters(names, results, lowLimits, highLimits, units);
            CheckListCounts(names.Count, results.Count, lowLimits.Count, highLimits.Count);

            if (results is List<float> floatResults && lowLimits is List<float> floatLowLimits && highLimits is List<float> floatHighLimits)
            {
                return Publish(names, floatResults, floatLowLimits, floatHighLimits, units, comment, suppressResult);
            }

            if (results is List<double> doubleResults && lowLimits is List<double> doubleLowLimits && highLimits is List<double> doubleHighLimits)
            {
                return Publish(names, doubleResults, doubleLowLimits, doubleHighLimits, units, comment, suppressResult);
            }

            if (results is List<string> stringResults && lowLimits is List<string> stringLowLimits && highLimits is List<string> stringHighLimits)
            {
                return Publish(names, stringResults, stringLowLimits, stringHighLimits, units, comment, suppressResult);
            }

            if (results is List<bool> boolResults && lowLimits is List<bool> boolLowLimits && highLimits is List<bool> boolHighLimits)
            {
                return Publish(names, boolResults, boolLowLimits, boolHighLimits, units, comment, suppressResult);
            }

            var overAllVerdict = Verdict.Pass;
            for (var i = 0; i < results.Count; i++)
            {
                StringNullCheck(units[i], "units@" + i);
                StringNullOrWhiteSpaceCheck(names[i], "names@" + i);

                var verdict = Verdict.NotSet;
                UpgradeVerdict(ref verdict, CompareResultAgainstLimit(results[i], lowLimits[i], highLimits[i]));
                UpgradeVerdict(ref overAllVerdict, verdict);

                if (!suppressResult)
                {
                    ResultSource.Publish(new CustomResult<T>(names[i], results[i], lowLimits[i], highLimits[i], units[i], verdict, comment));
                }
            }

            return overAllVerdict;
        }

        public void Publish<T>(string name, T result, T lowLimit, T highLimit, string unit, Verdict verdict, string comment = null, bool suppressResult = false) where T : IComparable
        {
            StringNullCheck(unit, nameof(unit));
            StringNullOrWhiteSpaceCheck(name, nameof(name));

            if (result is float floatResult && lowLimit is float floatLowLimit && highLimit is float floatHighLimit)
            {
                Publish(name, floatResult, floatLowLimit, floatHighLimit, unit, verdict, comment, suppressResult);
                return;
            }

            if (result is string stringResult && lowLimit is string stringLowLimit && highLimit is string stringHighLimit)
            {
                Publish(name, stringResult, stringLowLimit, stringHighLimit, unit, verdict, comment, suppressResult);
                return;
            }

            if (result is double doubleResult && lowLimit is double doubleLowLimit && highLimit is double doubleHighLimit)
            {
                Publish(name, doubleResult, doubleLowLimit, doubleHighLimit, unit, verdict, comment, suppressResult);
                return;
            }

            if (!suppressResult)
            {
                ResultSource.Publish(new CustomResult<T>(name, result, lowLimit, highLimit, unit, verdict, comment));
            }
        }

        public void Publish<T>(string name, T result, T lowLimit, T highLimit, EBase baseNumber, string unit, Verdict verdict, string comment = null, bool suppressResult = false)
        {
            StringNullCheck(unit, nameof(unit));
            StringNullOrWhiteSpaceCheck(name, nameof(name));

            if (!suppressResult)
            {
                ResultSource.Publish(new CustomResult<string>(name, ConvertBase(result, baseNumber),
                    ConvertBase(lowLimit, baseNumber), ConvertBase(highLimit, baseNumber), unit, verdict, comment));
            }
        }

        #endregion
    }
}