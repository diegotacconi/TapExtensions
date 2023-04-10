using System;
using System.Collections.Generic;
using System.Text;
using OpenTap;

namespace TapExtensions.Steps.Publish
{
    internal class CustomResult<T> where T : IComparable
    {
        public string ResultName { get; set; }
        public T ResultValue { get; set; }
        public T LowerLimit { get; set; }
        public T HigherLimit { get; set; }
        public string UnitOfResult { get; set; }
        public string Comment { get; set; }
        public Verdict Verdict { get; set; }

        private readonly List<Type> _supportedTypes = new List<Type>
        {
            typeof(bool),
            typeof(string),
            typeof(char),
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        public CustomResult(string resultName, T resultValue, T lowerLimit, T higherLimit, string unitOfResult,
            Verdict verdict, string comment = null)
        {
            if (comment != null)
                Comment = comment;
            FillInStandardInfo(resultName, resultValue, lowerLimit, higherLimit, unitOfResult, verdict);
        }

        private void FillInStandardInfo(string resultName, T resultValue, T lowerLimit, T higherLimit,
            string unitOfResult, Verdict verdict)
        {
            // Replace unsupported characters on resultName
            resultName = resultName.Replace(@"\", "_");
            resultName = resultName.Replace(@"/", "_");

            // Check if type is supported
            var type = typeof(T);
            if (!_supportedTypes.Contains(type))
            {
                var supportedTypes = new StringBuilder();
                foreach (var supportedType in _supportedTypes)
                    supportedTypes.Append(supportedType).Append(", ");

                throw new ApplicationException(
                    $"CustomResult does not support a result of type '{type}' in result name of '{resultName}'. " +
                    $"Supported types are: {supportedTypes}");
            }

            ResultValue = resultValue;
            LowerLimit = lowerLimit;
            HigherLimit = higherLimit;
            UnitOfResult = unitOfResult;
            ResultName = resultName;
            Verdict = verdict;
        }
    }
}