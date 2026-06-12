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


        public RelativeScore GetRelativeScore(EnvelopeCollection originalCollection)
        {
            return new RelativeScore(originalCollection, this);
        }

        public struct RelativeScore
        {
            public RelativeScore(EnvelopeCollection original, EnvelopeCollection newEnvelope)
            {
                collisionScoreChange = newEnvelope.totalPenalty - original.totalPenalty;
                timeChange = newEnvelope.FinalTime - original.FinalTime;
                improving = collisionScoreChange < 0;
                    scoreLossPerSecond = -collisionScoreChange / timeChange;
            }

            public bool improving;
            public double scoreLossPerSecond;
            public double collisionScoreChange;
            public double timeChange;
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
