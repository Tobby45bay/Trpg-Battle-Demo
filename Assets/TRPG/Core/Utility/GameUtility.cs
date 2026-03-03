
using System;
using System.Collections.Generic;
using System.Linq;

namespace TRPG.Core.Utility
{
    /// <summary>
    /// Core random provider interface.
    /// Deterministic, seeded, and instance-based for testing and replays.
    /// </summary>
    public interface IRandomProvider
    {
        // === Basic Ranges ===

        /// <summary>
        /// Returns an integer in range [minInclusive, maxExclusive).
        /// Standard behavior: Next(0, 100) returns 0–99.
        /// </summary>
        int Next(int minInclusive, int maxExclusive);

        /// <summary>
        /// Returns a float in range [0.0, 1.0).
        /// </summary>
        float NextFloat();

        /// <summary>
        /// Returns a float in range [minInclusive, maxExclusive).
        /// </summary>
        float NextFloat(float minInclusive, float maxExclusive);

        // === Probability ===

        /// <summary>
        /// Percentage roll: 0–100 (inclusive).
        /// Percent(50) has 50% chance to return true.
        /// Percent(0) always false, Percent(100) always true.
        /// </summary>
        bool Percent(int chance);

        /// <summary>
        /// Probability roll: 0.0–1.0 (clamped).
        /// Percent(0.5f) has 50% chance to return true.
        /// </summary>
        bool Percent(float chance);

        // === Distributions ===

        /// <summary>
        /// Normal (Gaussian) distribution: mean ± stddev.
        /// Useful for stat variance, damage spread, balanced randomness.
        /// </summary>
        float NextNormal(float mean = 0f, float stddev = 1f);

        // === Selection ===

        /// <summary>
        /// Weighted random selection from a dictionary.
        /// Weights should sum to > 0.
        /// Example: NextWeighted(new() { {1, 10f}, {2, 5f}, {3, 1f} })
        /// Returns 1 with ~62% chance, 2 with ~31%, 3 with ~6%.
        /// </summary>
        int NextWeighted(Dictionary<int, float> weights);

        /// <summary>
        /// Randomly selects one element from a list.
        /// Returns default(T) if list is empty (or throws — your choice).
        /// </summary>
        T Choose<T>(IList<T> list);

        // === Modifying Collections ===

        /// <summary>
        /// Fisher-Yates shuffle (in-place).
        /// Mutates the list. Returns the shuffled list for chaining.
        /// </summary>
        IList<T> Shuffle<T>(IList<T> list);

        // === Angles (for games) ===

        /// <summary>
        /// Random angle in degrees [0, 360).
        /// Useful for spread attacks, scatter, knockback direction.
        /// </summary>
        float NextAngle();

        /// <summary>
        /// Random angle in radians [0, 2π).
        /// </summary>
        float NextAngleRadians();
    }

    public interface ISeededRandomProvider : IRandomProvider
    {
        int Seed { get; }
    }

    /// <summary>
    /// Standard system random provider with full interface support.
    /// </summary>
    public sealed class SystemRandomProvider : ISeededRandomProvider
    {
        private readonly System.Random _random;
        private bool _hasCachedNormal = false;
        private float _cachedNormal;

        public int Seed { get; }

        public SystemRandomProvider(int seed)
        {
            Seed = seed;
            _random = new System.Random(seed);
        }

        // === Basic Ranges ===

        public int Next(int minInclusive, int maxExclusive)
            => _random.Next(minInclusive, maxExclusive);

        public float NextFloat()
            => (float)_random.NextDouble();

        public float NextFloat(float minInclusive, float maxExclusive)
        {
            if (minInclusive >= maxExclusive)
                throw new ArgumentException("Min must be less than max");
            return minInclusive + NextFloat() * (maxExclusive - minInclusive);
        }

        // === Probability ===

        public bool Percent(int chance)
        {
            // Clamp to valid range
            chance = Math.Clamp(chance, 0, 100);
            return Next(0, 100) < chance;
        }

        public bool Percent(float chance)
        {
            // Clamp to valid range
            chance = Math.Clamp(chance, 0f, 1f);
            if (chance <= 0f) return false;
            if (chance >= 1f) return true;
            return NextFloat() < chance;
        }

        // === Distributions ===

        /// <summary>
        /// Box-Muller transform with caching.
        /// Two calls to normal() produce two values; we cache one.
        /// </summary>
        public float NextNormal(float mean = 0f, float stddev = 1f)
        {
            if (_hasCachedNormal)
            {
                _hasCachedNormal = false;
                return mean + stddev * _cachedNormal;
            }

            var u1 = NextFloat();
            var u2 = NextFloat();

            // Guard against log(0)
            if (u1 < 1e-6f) u1 = 1e-6f;

            var mag = MathF.Sqrt(-2.0f * MathF.Log(u1));
            var z0 = mag * MathF.Cos(2.0f * MathF.PI * u2);
            var z1 = mag * MathF.Sin(2.0f * MathF.PI * u2);

            _cachedNormal = z1;
            _hasCachedNormal = true;

            return mean + stddev * z0;
        }

        // === Selection ===

        public int NextWeighted(Dictionary<int, float> weights)
        {
            if (weights == null || weights.Count == 0)
                throw new ArgumentException("Weights dictionary cannot be empty");

            float total = 0f;
            foreach (var w in weights.Values)
            {
                if (w < 0f)
                    throw new ArgumentException("Weights must be non-negative");
                total += w;
            }

            if (total <= 0f)
                throw new ArgumentException("Total weight must be positive");

            float roll = NextFloat() * total;
            float cumulative = 0f;

            foreach (var (key, weight) in weights)
            {
                cumulative += weight;
                if (roll < cumulative)
                    return key;
            }

            // Fallback (should not reach here)
            return weights.Keys.Last();
        }

        public T Choose<T>(IList<T> list)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("List cannot be null or empty");

            return list[Next(0, list.Count)];
        }

        // === Modifying Collections ===

        public IList<T> Shuffle<T>(IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            // Fisher-Yates: iterate backwards, swap with random earlier element
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }

        // === Angles ===

        public float NextAngle()
            => NextFloat() * 360f;

        public float NextAngleRadians()
            => NextFloat() * 2f * MathF.PI;
    }

    public interface IQuery<TResult> { }

    public interface IQueryContext { }

    public interface IQueryHandler
    {
        Type QueryType { get; }
        object Handle(object query, IQueryContext context);
    }

    public abstract class QueryHandler<TQuery, TResult> : IQueryHandler
    where TQuery : IQuery<TResult>
    {
        public Type QueryType => typeof(TQuery);

        public object Handle(object query, IQueryContext context)
        {
            return HandleTyped((TQuery)query, context);
        }

        protected abstract TResult HandleTyped(TQuery query, IQueryContext context);
    }

    public sealed class QueryDispatcher
    {
        private readonly Dictionary<Type, IQueryHandler> _handlers = new();

        public void Register(IQueryHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_handlers.ContainsKey(handler.QueryType))
                throw new InvalidOperationException(
                    $"Handler already registered for {handler.QueryType}");

            _handlers.Add(handler.QueryType, handler);
        }

        public TResult Dispatch<TResult>(
            IQuery<TResult> query,
            IQueryContext context)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var type = query.GetType();

            if (!_handlers.TryGetValue(type, out var handler))
                throw new InvalidOperationException(
                    $"No handler registered for {type}");

            return (TResult)handler.Handle(query, context);
        }
    }

    public interface IFormula<TInput, TResult>
    {
        TResult Calculate(TInput input);
    }

    public interface IUtilityEvaluator<T>
    {
        float Evaluate(T option);
    }
}

