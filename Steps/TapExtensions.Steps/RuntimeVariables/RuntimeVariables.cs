using System;
using System.Collections.Generic;

namespace TapExtensions.Steps.RuntimeVariables
{
    public static class RuntimeVariables
    {
        internal static readonly Dictionary<string, object> SequenceObjects = new Dictionary<string, object>();
        private static readonly object LockObject = new object();

        public static void ClearVariables()
        {
            lock (LockObject)
            {
                SequenceObjects?.Clear();
            }
        }

        public static void Add<T>(string name, T value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    @"String cannot be null or whitespace.", nameof(name));

            lock (LockObject)
            {
                if (!SequenceObjects.ContainsKey(name))
                    SequenceObjects.Add(name, value);
            }
        }

        public static void Set<T>(string name, T value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    @"String cannot be null or whitespace.", nameof(name));

            lock (LockObject)
            {
                if (!SequenceObjects.ContainsKey(name))
                    throw new ArgumentException(
                        $@"The runtime variable of {name} does not exist.", nameof(name));

                SequenceObjects[name] = value;
            }
        }

        public static bool Get<T>(string name, out T value, bool throwIfNotFound = true)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    @"String cannot be null or whitespace.", nameof(name));

            lock (LockObject)
            {
                var found = SequenceObjects.TryGetValue(name, out var storedValue);
                if (found)
                {
                    value = (T)storedValue;
                }
                else
                {
                    if (throwIfNotFound)
                        throw new ArgumentException(
                            $@"The runtime variable of {name} does not exist.", nameof(name));

                    value = default;
                }

                return found;
            }
        }

        public static void Delete(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    @"String cannot be null or whitespace.", nameof(name));

            lock (LockObject)
            {
                if (!SequenceObjects.ContainsKey(name))
                    throw new ArgumentException(
                        $@"The runtime variable of {name} does not exist.", nameof(name));

                SequenceObjects.Remove(name);
            }
        }
    }
}