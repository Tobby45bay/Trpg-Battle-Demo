using System;
using System.Collections.Generic;
using TRPG.Core.Logging;

namespace TRPG.Core.Event
{
    public readonly struct EventKey : IEquatable<EventKey>
    {
        private readonly string key;

        /// <summary>
        /// Raw string value of the trigger key.
        /// </summary>
        public readonly string Key => key;

        public EventKey(string key)
        {
            this.key = key;
        }

        public readonly bool Equals(EventKey other)
            => key == other.key;

        public override readonly bool Equals(object obj)
            => obj is EventKey other && Equals(other);

        public override readonly int GetHashCode()
            => key != null ? key.GetHashCode() : 0;

        public override readonly string ToString()
            => key;
    }

    /// <summary>
    /// Marker interface for events.
    /// Events must be pure data.
    /// </summary>
    public interface IEventContext { }

    /// <summary>
    /// Base interface for event listeners.
    /// </summary>
    public interface IEventListener
    {
        Type EventType { get; }
        int Priority { get; }
        void Handle(IEventContext ev);
    }

    /// <summary>
    /// Strongly-typed event listener base class.
    /// </summary>
    public abstract class EventListener<T> : IEventListener where T : IEventContext
    {

        public Type EventType => typeof(T);
        /// <summary>
        /// Lower number = executed earlier.
        /// </summary>
        public abstract int Priority { get; }

        public void Handle(IEventContext ev)
        {
            HandleTyped((T)ev);
        }

        protected abstract void HandleTyped(T ev);
    }

    /// <summary>
    /// Deterministic, instance-based event bus with logging support.
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, List<IEventListener>> _listeners = new();
        private readonly Queue<IEventContext> _eventQueue = new();
        private readonly ICoreLogger? _logger;

        /// <summary>
        /// Optional tracing hook (replay, analytics, debugging).
        /// </summary>
        public Action<IEventContext>? OnEventDispatched;

        public EventBus(ICoreLogger? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Registers a listener.
        /// </summary>
        public void Register(IEventListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            var type = listener.EventType;

            if (!_listeners.TryGetValue(type, out var list))
            {
                list = new List<IEventListener>();
                _listeners[type] = list;
            }

            list.Add(listener);
            // Ensure deterministic ordering
            list.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            _logger?.Log(
                LogLevel.Trace,
                "EventBus",
                $"Registered listener {listener.GetType().Name} for {type.Name} (Priority {listener.Priority})"
            );
        }

        /// <summary>
        /// Removes a previously registered listener.
        /// </summary>
        public void Unregister(IEventListener listener)
        {
            var type = listener.EventType;

            if (_listeners.TryGetValue(type, out var list))
            {
                list.Remove(listener);

                _logger?.Log(
                    LogLevel.Trace,
                    "EventBus",
                    $"Unregistered listener {listener.GetType().Name} from {type.Name}"
                );
            }
        }


        /// <summary>
        /// Enqueue an event for later processing.
        /// </summary>
        public void Enqueue(IEventContext ev)
        {
            if (ev == null)
                throw new ArgumentNullException(nameof(ev));

            _eventQueue.Enqueue(ev);

            _logger?.Log(
                LogLevel.Trace,
                "EventBus",
                $"Enqueued event {ev.GetType().Name} (Queue size: {_eventQueue.Count})"
            );
        }

        /// <summary>
        /// Processes a single event from the queue.
        /// </summary>
        public void ProcessNext()
        {
            if (_eventQueue.Count == 0)
                return;

            var ev = _eventQueue.Dequeue();

            _logger?.Log(
                LogLevel.Trace,
                "EventBus",
                $"Processing event {ev.GetType().Name}"
            );

            Dispatch(ev);
        }

        /// <summary>
        /// Processes all queued events in FIFO order.
        /// </summary>
        public void ProcessAll()
        {
            while (_eventQueue.Count > 0)
            {
                ProcessNext();
            }
        }

        /// <summary>
        /// Clears all queued events.
        /// </summary>
        public void ClearQueue()
        {
            _eventQueue.Clear();

            _logger?.Log(
                LogLevel.Info,
                "EventBus",
                "Event queue cleared"
            );
        }

        private void Dispatch(IEventContext ev)
        {
            OnEventDispatched?.Invoke(ev);

            var type = ev.GetType();

            if (!_listeners.TryGetValue(type, out var listeners))
            {
                _logger?.Log(
                    LogLevel.Trace,
                    "EventBus",
                    $"No listeners for {type.Name}"
                );
                return;
            }
            // Snapshot prevents modification issues during iteration
            var snapshot = listeners.ToArray();

            foreach (var listener in snapshot)
            {
                try
                {
                    _logger?.Log(
                        LogLevel.Trace,
                        "EventBus",
                        $"Executing {listener.GetType().Name} for {type.Name}"
                    );

                    listener.Handle(ev);
                }
                catch (Exception ex)
                {
                    _logger?.Log(
                        LogLevel.Error,
                        "EventBus",
                        $"Listener {listener.GetType().Name} threw exception: {ex.Message}"
                    );

                    throw; // preserve deterministic failure
                }
            }
        }
    }
}