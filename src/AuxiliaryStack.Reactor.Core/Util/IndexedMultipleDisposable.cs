﻿using System;
using System.Threading;

namespace AuxiliaryStack.Reactor.Core.Util
{
    /// <summary>
    /// Tracks IDisposables with an associated index and replaces them
    /// only if an incoming index is bigger than the hosted index.
    /// </summary>
    internal struct IndexedMultipeDisposableStruct
    {
        IndexedEntry entry;

        public bool Replace(IDisposable next, long nextIndex)
        {
            var e = Volatile.Read(ref entry);
            for (;;)
            {
                if (e == IndexedEntry.DISPOSED)
                {
                    next?.Dispose();
                    return false;
                }
                if (e != null)
                {
                    if (e.index > nextIndex)
                    {
                        return true;
                    }
                }
                IndexedEntry u = new IndexedEntry(nextIndex, next);

                IndexedEntry v = Interlocked.CompareExchange(ref entry, u, e);
                if (v == e)
                {
                    return true;
                }
                else
                {
                    e = v;
                }
            }
        }

        public void Dispose()
        {
            var a = Volatile.Read(ref entry);
            if (a != IndexedEntry.DISPOSED)
            {
                a = Interlocked.Exchange(ref entry, IndexedEntry.DISPOSED);
                if (a != IndexedEntry.DISPOSED)
                {
                    a?.d?.Dispose();
                }
            }
        }
    }


    /// <summary>
    /// Tracks IDisposables with an associated index and replaces them
    /// only if an incoming index is bigger than the hosted index.
    /// </summary>
    internal sealed class IndexedMultipleDisposable : IDisposable
    {
        IndexedMultipeDisposableStruct entry;

        public bool Replace(IDisposable next, long nextIndex)
        {
            return entry.Replace(next, nextIndex);
        }

        public void Dispose()
        {
            entry.Dispose();
        }
    }

    sealed class IndexedEntry
    {

        internal static readonly IndexedEntry DISPOSED = new IndexedEntry(long.MaxValue, null);

        internal readonly long index;

        internal readonly IDisposable d;

        internal IndexedEntry(long index, IDisposable d)
        {
            this.index = index;
            this.d = d;
        }
    }
}
