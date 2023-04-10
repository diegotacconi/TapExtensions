using System;
using System.Collections.Generic;
using OpenTap;

namespace TapExtensions.Steps.Publish
{
    public enum EBase
    {
        Base2 = 2,
        Base8 = 8,
        Base16 = 16
    }

    public interface ICustomPublish
    {
        Verdict Publish<T>(string name, T result, T lowLimit, T highLimit, string unit, string comment = null, bool suppressResult = false) where T : IComparable;

        Verdict Publish<T>(string name, T result, T lowLimit, T highLimit, EBase baseNumber, string unit, string comment = null, bool suppressResult = false) where T : IComparable;

        Verdict Publish<T>(List<string> names, List<T> results, List<T> lowLimits, List<T> highLimits, List<string> units, string comment = null, bool suppressResult = false) where T : IComparable;

        void Publish<T>(string name, T result, T lowLimit, T highLimit, string unit, Verdict verdict, string comment = null, bool suppressResult = false) where T : IComparable;

        void Publish<T>(string name, T result, T lowLimit, T highLimit, EBase baseNumber, string unit, Verdict verdict, string comment = null, bool suppressResult = false);

        ResultSource ResultSource { get; set; }
    }
}