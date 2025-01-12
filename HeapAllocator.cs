#region Copyright (c) Pixeval.Caching/Pixeval.Caching
// GPL v3 License
// 
// Pixeval.Caching/Pixeval.Caching
// Copyright (c) 2024 Pixeval.Caching/HeapAllocator.cs
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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pixeval.Caching;

public unsafe record HeapBlock(Memory<byte> Span, nint UnallocatedStart)
{
    public int Size => Span.Length;

    public ref byte StartPtr => ref Unsafe.AsRef<byte>((byte*) Span.Pin().Pointer);

    public ref byte UnallocatedStartPtr => ref Unsafe.AsRef<byte>((byte*) UnallocatedStart);

    public bool RangeContains(ref byte ptr)
    {
        var ptrRawPointer = (byte*) Unsafe.AsPointer(ref ptr);
        var startRawPointer = (byte*) Unsafe.AsPointer(ref StartPtr);
        var end = startRawPointer + Span.Length;
        return ptrRawPointer >= startRawPointer && ptrRawPointer < end;
    }

    public long RelativeOffset<T>(ref byte ptr) where T : unmanaged
    {
        return Unsafe.ByteOffset(ref ptr, ref StartPtr);
    }

    public ref T AbsoluteOffset<T>(nint offset) where T : unmanaged
    {
        return ref Unsafe.AsRef<T>((byte*) Unsafe.AsPointer(ref StartPtr) + offset);
    }

    public ref byte BlockEnd => ref Unsafe.AsRef<byte>((byte*) Unsafe.AsPointer(ref StartPtr) + Span.Length);

    public long AllocatedSize => Unsafe.ByteOffset(ref UnallocatedStartPtr, ref StartPtr);
    public nint UnallocatedStart { get; set; } = UnallocatedStart;
}

public class HeapAllocator : IDisposable
{
    // 1mb, this will be doubled the first time the allocator allocates.
    private nint _lastGrowthSize = 1 * 1024 * 1024;
    private const double ExpandFactor = 2;

    private readonly LinkedList<HeapBlock> _commitedRegions;
    private readonly Action<HeapBlock>? _callbackOnExpansion;
    private bool _available;
    // This is supposed to be a BumpPointerAllocator, the HeapAllocator runs as if it's allocating system memory.
    private readonly INativeAllocator _allocator;

    public nint Size { get; private set; }

    private HeapAllocator(
        nint size,
        Action<HeapBlock>? callbackOnExpansion,
        bool available,
        INativeAllocator allocator)
    {
        Size = size;
        _callbackOnExpansion = callbackOnExpansion;
        _available = available;
        _allocator = allocator;
        _commitedRegions = [];
    }

    public static HeapAllocator Create(INativeAllocator allocator, Action<HeapBlock>? callback = null)
    {
        return new HeapAllocator(0, callback, true, allocator);
    }

    public unsafe IResult<Void, AllocatorState> Expand(nint desiredSize, nint align)
    {
        if (!_available)
        {
            return IResult<Void, AllocatorState>.Err0(AllocatorState.AllocatorClosed);
        }

        _lastGrowthSize = MemoryHelper.RoundToNearestMultipleOf((nint) (_lastGrowthSize * ExpandFactor), align);

        if (_lastGrowthSize <= desiredSize)
        {
            _lastGrowthSize = MemoryHelper.RoundToNearestMultipleOf((nint) (desiredSize * ExpandFactor), align);
        }

        var result = _allocator.TryAllocate(_lastGrowthSize, out var span);

        switch (result)
        {
            case AllocatorState.AllocationSuccess:
                fixed (byte* elem = &MemoryMarshal.GetReference(span))
                {
                    var region = new HeapBlock(new UnmanagedMemoryManager<byte>(span).Memory, new IntPtr(elem));
                    _commitedRegions.AddLast(region);
                    Size += _lastGrowthSize;
                    _callbackOnExpansion?.Invoke(region);
                }
                
                break;
            case var _:
                return IResult<Void, AllocatorState>.Err0(result);
        }

        return IResult<Void, AllocatorState>.Ok0(Void.Value);
    }

    public IResult<(nint ptr, nint actualSize), AllocatorState> Allocate(nint size, nint align)
    {
        if (!_available)
        {
            return IResult<(nint ptr, nint actualSize), AllocatorState>.Err0(AllocatorState.AllocatorClosed);
        }

        var sizeAligned = MemoryHelper.RoundToNearestMultipleOf(size, align);
        var tracker = _commitedRegions.FirstOrDefault(entry => entry.AllocatedSize + sizeAligned <= entry.Size);
        if (tracker != default)
        {
            var padding = MemoryHelper.RoundToNearestMultipleOf(tracker.UnallocatedStart, align);
            tracker.UnallocatedStart = padding;
            var ptr = tracker.UnallocatedStart;
            tracker.UnallocatedStart += sizeAligned;
            return IResult<(nint, nint), AllocatorState>.Ok0((ptr, sizeAligned));
        }

        if (Expand(sizeAligned, align) is IResult<Void, AllocatorState>.Err err)
        {
            return err.Cast<Void, (nint ptr, nint actualSize), AllocatorState>();
        }

        return Allocate(size, align);
    }

    public int BlockIndex(HeapBlock block)
    {
        var result = _commitedRegions.Index().FirstOrDefault(b => Unsafe.AreSame(ref block.StartPtr, ref b.Item.StartPtr));
        return result == default ? -1 : result.Index;
    }

    public unsafe HeapBlock? GetBlock(byte* ptr)
    {
        var result = _commitedRegions.FirstOrDefault(block => block.RangeContains(ref Unsafe.AsRef<byte>(ptr)));
        return result == default ? null : result;
    }

    public long Allocated()
    {
        return !_available ? 0 : _commitedRegions.Select(block => block.AllocatedSize).Sum();
    }

    public void Dispose()
    {
        _available = true;
    }
}