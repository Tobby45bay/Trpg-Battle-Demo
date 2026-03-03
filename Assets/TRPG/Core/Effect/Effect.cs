
using System;
using System.Collections.Generic;
using System.Linq;
using TRPG.Core.Event;
#nullable enable
namespace TRPG.Core.Effects
{
    public interface IEffectData
    {
        int ID {  get; }
        string Name { get; }
        int Duration { get; }
    }

    public interface IEffectDefinition
    {
        int Id { get; }
        string Name { get; }
        int Duration { get; }
        string LifetimePolicyType { get; }

        string EffectType { get; } // "Poison", "StatBuff", "Reflect", etc.

        Dictionary<string, object> Parameters { get; }
        Dictionary<string, object> LifetimeParameters { get; }
    }

    public static class EffectDefinitionExtensions
    {
        public static T GetParam<T>(this IEffectDefinition def, string key)
            => (T)def.Parameters[key];

        public static bool TryGetParam<T>(this IEffectDefinition def, string key, out T value)
        {
            if (def.Parameters.TryGetValue(key, out var obj) && obj is T t)
            {
                value = t;
                return true;
            }
            value = default!;
            return false;
        }
    }

    public interface IEffectFactory
    {
        ReactiveEffectInstance Create(
            IEffectDefinition definition,
            IEffectContext context);
    }

    public sealed class EffectFactory : IEffectFactory
    {
        private readonly Dictionary<string, Func<IEffectDefinition, IEffectContext, ReactiveEffectInstance>> _builders
            = new();

        public void Register(
            string effectType,
            Func<IEffectDefinition, IEffectContext, ReactiveEffectInstance> builder)
        {
            _builders[effectType] = builder;
        }

        public ReactiveEffectInstance Create(
            IEffectDefinition definition,
            IEffectContext context)
        {
            if (!_builders.TryGetValue(definition.EffectType, out var builder))
                throw new InvalidOperationException($"Effect type '{definition.EffectType}' not registered.");



            return builder(definition, context);
        }
    }

    public interface IEffectContext
    {
        int? SourceUnitId { get; }
        int? TargetUnitId { get; }
    }

    public interface ILifetimePolicy
    {
        bool ShouldExpire(IEffectContext context);
    }

    public sealed class DurationPolicy : ILifetimePolicy
    {
        private int remaining;
        public DurationPolicy(int duration) => remaining = duration;
        public bool ShouldExpire(IEffectContext context) => --remaining <= 0;
    }

    public class ConditionalPolicy : ILifetimePolicy
    {
        private readonly Func<IEffectContext, bool> _condition;
        public ConditionalPolicy(Func<IEffectContext, bool> condition) => _condition = condition;
        public bool ShouldExpire(IEffectContext context) => _condition(context);
    }

    public interface IStackableEffect
    {
        int Stack { get; }
        int MaxStack { get; }

        void AddStack();
        void RemoveStack();
        bool IsAtMaxStack { get; }
    }

    public interface IReactiveEffect
    {
        void Register(EventBus bus);
        void Unregister(EventBus bus);
    }

    public abstract class ReactiveEffectInstance : IReactiveEffect
    {
        protected readonly IEffectData Data;
        protected readonly IEffectContext Context;
        private readonly ILifetimePolicy? _lifetimePolicy;

        protected readonly object RuntimeId = new();

        private EffectManager? _manager;
        private EventBus? _bus;

        protected int RemainingDuration;
        private bool _isExpired;

        protected ReactiveEffectInstance(IEffectData data,IEffectContext context, ILifetimePolicy? lifetimePolicy)
        {
            Data = data;
            Context = context;
            RemainingDuration = data.Duration;
            _lifetimePolicy = lifetimePolicy;
        }

        internal void Bind(EffectManager manager, EventBus bus)
        {
            _manager = manager;
            _bus = bus;
        }

        protected void Expire()
        {
            if (_isExpired)
                return;

            _isExpired = true;

            OnExpired();

            _manager?.RemoveEffect(this);
        }

        protected virtual void OnExpired()
        {
            // Override in child effects to remove stat modifiers, etc.
        }

        protected void DecreaseDuration()
        {
            if (Data.Duration <= 0)
                return;

            RemainingDuration--;

            if (RemainingDuration <= 0)
                Expire();
        }

        protected void CheckLifetimePolicy()
        {
            if (_lifetimePolicy != null &&
                _lifetimePolicy.ShouldExpire(Context))
            {
                Expire();
            }
        }

        public abstract void Register(EventBus bus);
        public abstract void Unregister(EventBus bus);
    }

    public abstract class StackableEffect : ReactiveEffectInstance, IStackableEffect
    {
        public int Stack { get; private set; } = 1;
        public abstract int MaxStack { get; }

        public bool IsAtMaxStack => Stack >= MaxStack;

        public void AddStack()
        {
            if (!IsAtMaxStack) Stack++;
        }

        public void RemoveStack()
        {
            if (Stack > 0) Stack--;
            if (Stack <= 0) Expire();
        }

        protected StackableEffect(IEffectData data, IEffectContext ctx, ILifetimePolicy? lifetimePolicy)
            : base(data, ctx, lifetimePolicy) { }
    }

    public sealed class EffectManager
    {
        private readonly EventBus _bus;
        private readonly HashSet<ReactiveEffectInstance> _active = new();

        public EffectManager(EventBus bus)
        {
            _bus = bus;
        }

        public void ApplyEffect(ReactiveEffectInstance instance)
        {
            if (_active.Contains(instance))
                return;

            instance.Bind(this, _bus);

            _active.Add(instance);
            instance.Register(_bus);
        }

        public void RemoveEffect(ReactiveEffectInstance instance)
        {
            if (!_active.Remove(instance))
                return;

            instance.Unregister(_bus);
        }

        public void ClearAll()
        {
            foreach (var effect in _active.ToArray())
            {
                RemoveEffect(effect);
            }
        }
    }
}