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
        private static Dictionary<ITxRoboticOrderedCompoundOperation, double> keyValuePairs = new Dictionary<ITxRoboticOrderedCompoundOperation, double>();

        public static double GetValue(ITxRoboticOrderedCompoundOperation key)  => keyValuePairs[key];
        public static double SetValue(ITxRoboticOrderedCompoundOperation key, double value) => keyValuePairs[key] = value;

        public static ICollection<ITxRoboticOrderedCompoundOperation> Keys => ((IDictionary<ITxRoboticOrderedCompoundOperation, double>)keyValuePairs).Keys;

        public static ICollection<double> Values => ((IDictionary<ITxRoboticOrderedCompoundOperation, double>)keyValuePairs).Values;

        public static int Count => ((ICollection<KeyValuePair<ITxRoboticOrderedCompoundOperation, double>>)keyValuePairs).Count;

        public static bool IsReadOnly => ((ICollection<KeyValuePair<ITxRoboticOrderedCompoundOperation, double>>)keyValuePairs).IsReadOnly;

        public static void Add(ITxRoboticOrderedCompoundOperation key, double value)
        {
            ((IDictionary<ITxRoboticOrderedCompoundOperation, double>)keyValuePairs).Add(key, value);
        }

        public static void Add(KeyValuePair<ITxRoboticOrderedCompoundOperation, double> item)
        {
            ((ICollection<KeyValuePair<ITxRoboticOrderedCompoundOperation, double>>)keyValuePairs).Add(item);
        }

        public static void Clear()
        {
            ((ICollection<KeyValuePair<ITxRoboticOrderedCompoundOperation, double>>)keyValuePairs).Clear();
        }

        public static bool Contains(KeyValuePair<ITxRoboticOrderedCompoundOperation, double> item)
        {
            return ((ICollection<KeyValuePair<ITxRoboticOrderedCompoundOperation, double>>)keyValuePairs).Contains(item);
        }

        public static bool ContainsKey(ITxRoboticOrderedCompoundOperation key)
        {
            return ((IDictionary<ITxRoboticOrderedCompoundOperation, double>)keyValuePairs).ContainsKey(key);
        }

        public static void CopyTo(KeyValuePair<ITxRoboticOrderedCompoundOperation, double>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<ITxRoboticOrderedCompoundOperation, double>>)keyValuePairs).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<ITxRoboticOrderedCompoundOperation, double>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<ITxRoboticOrderedCompoundOperation, double>>)keyValuePairs).GetEnumerator();
        }

        public bool Remove(ITxRoboticOrderedCompoundOperation key)
        {
            return ((IDictionary<ITxRoboticOrderedCompoundOperation, double>)keyValuePairs).Remove(key);
        }

        public bool Remove(KeyValuePair<ITxRoboticOrderedCompoundOperation, double> item)
        {
            return ((ICollection<KeyValuePair<ITxRoboticOrderedCompoundOperation, double>>)keyValuePairs).Remove(item);
        }

        public bool TryGetValue(ITxRoboticOrderedCompoundOperation key, out double value)
        {
            return ((IDictionary<ITxRoboticOrderedCompoundOperation, double>)keyValuePairs).TryGetValue(key, out value);
        }

    }
}
