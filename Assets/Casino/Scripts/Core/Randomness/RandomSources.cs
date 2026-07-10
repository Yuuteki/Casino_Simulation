using System;
using System.Collections.Generic;

namespace Casino.Core.Randomness
{
    public interface IRandomSource
    {
        int NextInt(int exclusiveMax);
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random random;

        public SystemRandomSource()
            : this(Environment.TickCount)
        {
        }

        public SystemRandomSource(int seed)
        {
            random = new Random(seed);
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            }

            return random.Next(exclusiveMax);
        }
    }

    public sealed class ScriptedRandomSource : IRandomSource
    {
        private readonly Queue<int> scriptedValues;

        public ScriptedRandomSource(IEnumerable<int> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            scriptedValues = new Queue<int>(values);
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            }

            if (scriptedValues.Count == 0)
            {
                throw new InvalidOperationException("Scripted random source has no remaining values.");
            }

            var value = scriptedValues.Dequeue();
            if (value < 0 || value >= exclusiveMax)
            {
                throw new InvalidOperationException("Scripted random value is outside the requested range.");
            }

            return value;
        }
    }

    public static class FisherYatesShuffle
    {
        public static void Shuffle<T>(IList<T> items, IRandomSource randomSource)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            for (var index = items.Count - 1; index > 0; index--)
            {
                var swapIndex = randomSource.NextInt(index + 1);
                var item = items[index];
                items[index] = items[swapIndex];
                items[swapIndex] = item;
            }
        }
    }
}
