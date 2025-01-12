#region Copyright (c) Pixeval.Caching/Pixeval.Caching
// GPL v3 License
// 
// Pixeval.Caching/Pixeval.Caching
// Copyright (c) 2024 Pixeval.Caching/CacheTable.cs
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pixeval.Caching;

public class CacheTable<TKey, THeader, TProtocol>(MemoryMappedFileMemoryManager memoryManager, TProtocol protocol) 
    where THeader : unmanaged 
    where TKey : IEquatable<TKey>
    where TProtocol : ICacheProtocol<TKey, THeader>
{
    public MemoryMappedFileMemoryManager MemoryManager { get; } = memoryManager;

    private readonly Dictionary<TKey, (nint ptr, int allocatedLength)> _cacheTable = [];

    private PriorityQueue<TKey, int> _lruCacheIndex = new();

    // ReSharper disable once InconsistentNaming
    public int CacheLRUFactor { get; set; } = 2;

    private TProtocol _protocol = protocol;

    /// <summary>
    /// While calling this method, all cache operations should be halted.
    /// </summary>
    private unsafe void Compact()
    {
        var retain = _lruCacheIndex.Count / CacheLRUFactor;
        var newPriorityQueue = new PriorityQueue<TKey, int>();
        for (var i = 0; i < retain; i++)
        {
            _lruCacheIndex.TryDequeue(out var element, out var priority);
            newPriorityQueue.Enqueue(element!, priority);
        }

        _lruCacheIndex = newPriorityQueue;

        var garbage = new Dictionary<nint, int>();

        foreach (var key in _lruCacheIndex.UnorderedItems.Select(x => x.Element))
        {
            if (TryReadCache(key, out var span))
            {
                var pointer = Unsafe.AsPointer(ref span.GetPinnableReference());
                garbage[(nint) pointer] = span.Length;
            }
        }

        var grouped = garbage.GroupBy(
            tuple => MemoryManager.BumpPointerAllocators.First(pair => pair.Value.GetBlock((byte*) tuple.Key) != default).Value,
            tuple => tuple);
        foreach (var group in grouped)
        {
            group.Key.Compact(group.ToDictionary(tuple => tuple.Key, tuple => tuple.Value), garbage.Keys.ToHashSet());
        }
    }

    public unsafe AllocatorState TryCache(TKey key, Span<byte> span)
    {
        if (_cacheTable.ContainsKey(key))
        {
            return AllocatorState.AllocationSuccess;
        }

        var header = _protocol.SerializeHeader(_protocol.GetHeader(key));

        var result = MemoryManager.DominantAllocator.TryAllocate(header.Length + _protocol.GetDataLength(_protocol.GetHeader(key)), out var cacheArea);
        if (result is AllocatorState.AllocationSuccess)
        {
            header.CopyTo(cacheArea);
            span.CopyTo(cacheArea[header.Length..]);
            _cacheTable[key] = ((nint) Unsafe.AsPointer(ref cacheArea.GetPinnableReference()), cacheArea.Length);
            return AllocatorState.AllocationSuccess;
        }

        return result;
    }

    public unsafe bool TryReadCache(TKey key, out Span<byte> span)
    {
        if (_cacheTable.TryGetValue(key, out var tuple))
        {
            var headerLength = TProtocol.GetHeaderLength();
            var header = new Span<byte>((byte*) tuple.ptr, headerLength);
            var headerStruct = _protocol.DeserializeHeader(header);
            var dataLength = _protocol.GetDataLength(headerStruct);
            var totalSpan = new Span<byte>((void*) tuple.ptr, tuple.allocatedLength);
            span = totalSpan[headerLength..(headerLength + dataLength)];

            _lruCacheIndex.Remove(key, out _, out var oldPriority);
            _lruCacheIndex.Enqueue(key, oldPriority + 1);

            return true;
        }

        span = Span<byte>.Empty;
        return false;
    }
}