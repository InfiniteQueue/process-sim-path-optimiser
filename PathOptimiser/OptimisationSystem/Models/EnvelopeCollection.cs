using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tecnomatix.Engineering;

namespace PathOptimiser.OptimisationSystem
{
    public class EnvelopeCollection : IList<CollisionEnvelope>
    {
        public EnvelopeCollection(ITxCompoundOperation operation)
        {
            this.operation = operation;
        }

        private ITxCompoundOperation operation;
        public ITxCompoundOperation Operation => operation;

        public List<CollisionEnvelope> Envelopes = new List<CollisionEnvelope>();

        public double totalPenalty
        {
            get => Envelopes.Count == 0 ? 0 : Envelopes.Sum(env => env.CollisionScore);
        }

        public double FinalTime;

        public double GetRelativeScore(EnvelopeCollection newEnvelopeCollection)
        {
            //if (newEnvelopeCollection.FinalTime - this.FinalTime <= 0.01 && newEnvelopeCollection.totalPenalty - totalPenalty <= 0.01) return double.PositiveInfinity; //If decreasing speed/cnt somehow decreased or did not affect the total path length, and didn't create more penalties, always use this adjustment
            //else 
            //Disabled. An equal relative score is accepted as a valid step, so this isn't necessary

            if (newEnvelopeCollection.totalPenalty > totalPenalty && newEnvelopeCollection.FinalTime > FinalTime) return double.NegativeInfinity; //Everything is worse, reject

            var scoreIncrease = totalPenalty - newEnvelopeCollection.totalPenalty;
            var timePenalty = newEnvelopeCollection.FinalTime * 1 - FinalTime;

            return (scoreIncrease) / (timePenalty); //Decrease in penalty over increase in time, higher is better
        }

        #region Interface
        public CollisionEnvelope this[int index] { get => ((IList<CollisionEnvelope>)Envelopes)[index]; set => ((IList<CollisionEnvelope>)Envelopes)[index] = value; }

        public int Count => ((ICollection<CollisionEnvelope>)Envelopes).Count;

        public bool IsReadOnly => ((ICollection<CollisionEnvelope>)Envelopes).IsReadOnly;

        public void Add(CollisionEnvelope item)
        {
            ((ICollection<CollisionEnvelope>)Envelopes).Add(item);
        }

        public void Clear()
        {
            ((ICollection<CollisionEnvelope>)Envelopes).Clear();
        }

        public bool Contains(CollisionEnvelope item)
        {
            return ((ICollection<CollisionEnvelope>)Envelopes).Contains(item);
        }

        public void CopyTo(CollisionEnvelope[] array, int arrayIndex)
        {
            ((ICollection<CollisionEnvelope>)Envelopes).CopyTo(array, arrayIndex);
        }

        public IEnumerator<CollisionEnvelope> GetEnumerator()
        {
            return ((IEnumerable<CollisionEnvelope>)Envelopes).GetEnumerator();
        }

        public int IndexOf(CollisionEnvelope item)
        {
            return ((IList<CollisionEnvelope>)Envelopes).IndexOf(item);
        }

        public void Insert(int index, CollisionEnvelope item)
        {
            ((IList<CollisionEnvelope>)Envelopes).Insert(index, item);
        }

        public bool Remove(CollisionEnvelope item)
        {
            return ((ICollection<CollisionEnvelope>)Envelopes).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<CollisionEnvelope>)Envelopes).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Envelopes).GetEnumerator();
        }
        #endregion
    }
}
