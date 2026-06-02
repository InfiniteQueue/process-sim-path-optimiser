using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecnomatix.Engineering;

namespace PathOptimiser.Models
{
    internal class OperationToNearMissRegistry
    {
        private static Dictionary<ITxCompoundOperation, double> keyValuePairs = new Dictionary<ITxCompoundOperation, double>();

        public static double GetValue(ITxCompoundOperation key)  => keyValuePairs[key];
        public static double SetValue(ITxCompoundOperation key, double value) => keyValuePairs[key] = value;

        public static ICollection<ITxCompoundOperation> Keys => ((IDictionary<ITxCompoundOperation, double>)keyValuePairs).Keys;

        public static ICollection<double> Values => ((IDictionary<ITxCompoundOperation, double>)keyValuePairs).Values;

        public static int Count => ((ICollection<KeyValuePair<ITxCompoundOperation, double>>)keyValuePairs).Count;

        public static bool IsReadOnly => ((ICollection<KeyValuePair<ITxCompoundOperation, double>>)keyValuePairs).IsReadOnly;

        public static void Add(ITxCompoundOperation key, double value)
        {
            ((IDictionary<ITxCompoundOperation, double>)keyValuePairs).Add(key, value);
        }

        public static void Add(KeyValuePair<ITxCompoundOperation, double> item)
        {
            ((ICollection<KeyValuePair<ITxCompoundOperation, double>>)keyValuePairs).Add(item);
        }

        public static void Clear()
        {
            ((ICollection<KeyValuePair<ITxCompoundOperation, double>>)keyValuePairs).Clear();
        }

        public static bool Contains(KeyValuePair<ITxCompoundOperation, double> item)
        {
            return ((ICollection<KeyValuePair<ITxCompoundOperation, double>>)keyValuePairs).Contains(item);
        }

        public static bool ContainsKey(ITxCompoundOperation key)
        {
            return ((IDictionary<ITxCompoundOperation, double>)keyValuePairs).ContainsKey(key);
        }

        public static void CopyTo(KeyValuePair<ITxCompoundOperation, double>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<ITxCompoundOperation, double>>)keyValuePairs).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<ITxCompoundOperation, double>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<ITxCompoundOperation, double>>)keyValuePairs).GetEnumerator();
        }

        public bool Remove(ITxCompoundOperation key)
        {
            return ((IDictionary<ITxCompoundOperation, double>)keyValuePairs).Remove(key);
        }

        public bool Remove(KeyValuePair<ITxCompoundOperation, double> item)
        {
            return ((ICollection<KeyValuePair<ITxCompoundOperation, double>>)keyValuePairs).Remove(item);
        }

        public bool TryGetValue(ITxCompoundOperation key, out double value)
        {
            return ((IDictionary<ITxCompoundOperation, double>)keyValuePairs).TryGetValue(key, out value);
        }

    }
}
