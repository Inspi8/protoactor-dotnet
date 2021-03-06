using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Proto
{
    public static class EventStreamExtensions
    {
        public static EventProbe<T> GetProbe<T>(this EventStream<T> eventStream) => new EventProbe<T>(eventStream);
    }

    [PublicAPI]
    public class EventProbe<T>
    {
        private readonly ConcurrentQueue<T> _events = new ConcurrentQueue<T>();
        private readonly object _lock = new object();
        private readonly ILogger _logger = Log.CreateLogger<EventProbe<T>>();
        private readonly Subscription<T> _subscription;
        private EventExpectation<T>? _currentExpectation;

        public EventProbe(EventStream<T> eventStream)
        {
            _subscription = eventStream.Subscribe(e =>
                {
                    lock (_lock)
                    {
                        _events.Enqueue(e);
                        NotifyChanges();
                    }
                }
            );
        }

        public Task Expect<TE>() where TE : T
        {
            lock (_lock)
            {
                var expectation = new EventExpectation<T>(@event => @event is TE);
                _currentExpectation = expectation;
                NotifyChanges();
                return expectation.Task;
            }
        }

        public Task<T> Expect<TE>(Func<TE, bool> predicate) where TE : T
        {
            lock (_lock)
            {
                var expectation = new EventExpectation<T>(@event =>
                    {
                        return @event switch
                        {
                            TE e when predicate(e) => true,
                            _                      => false
                        };
                    }
                );
                _logger.LogDebug("Setting expectation");
                _currentExpectation = expectation;
                NotifyChanges();
                return expectation.Task;
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _currentExpectation = null;
                _subscription.Unsubscribe();
            }
        }

        //TODO: make lockfree
        private void NotifyChanges()
        {
            while (_currentExpectation != null && _events.TryDequeue(out var @event))
            {
                if (_currentExpectation.Evaluate(@event))
                {
                    _logger.LogDebug("Got expected event {@event} ", @event);
                    _currentExpectation = null;
                    return;
                }

                _logger.LogDebug("Got unexpected {@event}, ignoring", @event);
            }
        }
    }
}