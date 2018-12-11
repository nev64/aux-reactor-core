using System;
using AuxiliaryStack.Monads;
using static AuxiliaryStack.Monads.Option;

namespace AuxiliaryStack.Reactor.Core
{
    public enum SignalType
    {
        Complete = 0,
        Next = 1,
        Error = 2
    }

    public readonly struct Signal<T>
    {
        private readonly T _value;
        private readonly Exception _error;
        private readonly SignalType _type;

        private Signal(T value, Exception error, SignalType type)
        {
            _value = value;
            _error = error;
            _type = type;
        }

        public SignalType Type => _type;

        public bool IsNext => _type == SignalType.Next;

        public bool IsError => _type == SignalType.Error;

        public bool IsComplete => _type == SignalType.Complete;

        public Option<T> Next => _type == SignalType.Next ? Just(_value) : None<T>();

        public Option<Exception> Error => _type == SignalType.Error ? Just(_error) : None<Exception>();

        public static Signal<T> OfComplete() => new Signal<T>(default, default, SignalType.Complete);
        
        public static Signal<T> OfError(Exception ex) => new Signal<T>(default, ex, SignalType.Error);
        
        public static Signal<T> OfNext(T value) => new Signal<T>(value, default, SignalType.Next);

    }
}
