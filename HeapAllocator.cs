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

namespace Pixeval.Caching;

public unsafe record struct HeapBlock(Memory<byte> Span, nint UnallocatedStart)
{
    public int Size => Span.Length;

    public ref byte StartPtr => ref Unsafe.AsRef<byte>((byte*) Span.Pin().Pointer);

    public ref byte UnallocatedStartPtr => ref Unsafe.AsRef<byte>((byte*) UnallocatedStart);

    public bool RangeContains(ref byte ptr)
    {
        ref var end = ref Unsafe.AddByteOffset(ref StartPtr, Span.Length);
        return Unsafe.AreSame(ref end, ref ptr) ||
               (Unsafe.IsAddressGreaterThan(ref ptr, ref StartPtr) &&
                Unsafe.IsAddressLessThan(ref ptr, ref end));
    }

    public long RelativeOffset<T>(ref byte ptr) where T : unmanaged
    {
        return Unsafe.ByteOffset(ref ptr, ref StartPtr);
    }

    public ref T AbsoluteOffset<T>(nint offset) where T : unmanaged
    {
        return ref Unsafe.AsRef<T>(Unsafe.AsPointer(ref Unsafe.AddByteOffset(ref StartPtr, offset)));
    }

    public ref byte BlockEnd => ref Unsafe.AddByteOffset(ref StartPtr, Span.Length);

    public long AllocatedSize => Unsafe.ByteOffset(ref UnallocatedStartPtr, ref StartPtr);
}

public class HeapAllocator
{
    private readonly LinkedList<HeapBlock> _commitedRegions;
    private readonly Action<HeapBlock>? _callbackOnExpansion;
    private bool _available;
    private readonly INativeAllocator _allocator;

    public nint Size { get; private set; }

    private HeapAllocator(
        nint size,
        LinkedList<HeapBlock> commitedRegions,
        Action<HeapBlock>? callbackOnExpansion,
        bool available,
        INativeAllocator allocator)
    {
        Size = size;
        _commitedRegions = commitedRegions;
        _callbackOnExpansion = callbackOnExpansion;
        _available = available;
        _allocator = allocator;
    }

    public static HeapAllocator Create(INativeAllocator allocator, Action<HeapBlock>? callback = null)
    {
        return new HeapAllocator(0, new LinkedList<HeapBlock>(), callback, true, allocator);
    }

    public IResult<Void, AllocatorError> Expand(nint desiredSize, nint align)
    {
        if (!_available)
        {
            return IResult<Void, AllocatorError>.Err0(AllocatorError.AllocatorClosed);
        }

        // dude it's not superb to use desiredSize here, but we're not making a gc...
        // for the same reason we omit the expand factor.
        var newBlockSize = MemoryHelper.RoundToNearestMultipleOf(desiredSize, align);

        switch (_allocator.Allocate(newBlockSize))
        {
            case IResult<nint, AllocatorError>.Ok(var intPtr):
                var region = new HeapBlock(new UnmanagedMemoryManager<byte>(intPtr, (int) newBlockSize).Memory, intPtr);
                _commitedRegions.AddLast(region);
                Size += newBlockSize;
                _callbackOnExpansion?.Invoke(region);
                break;
            case IResult<nint, AllocatorError>.Err(var allocatorError):
                return IResult<Void, AllocatorError>.Err0(allocatorError);
        }

        return IResult<Void, AllocatorError>.Ok0(Void.Value);
    }

    public IResult<nint, AllocatorError> Allocate(nint size, nint align)
    {
        if (!_available)
        {
            return IResult<nint, AllocatorError>.Err0(AllocatorError.AllocatorClosed);
        }

        var tracker = _commitedRegions.FirstOrDefault(entry => entry.AllocatedSize + size <= entry.Size);
        if (tracker != default)
        {
            var padding = MemoryHelper.RoundToNearestMultipleOf(tracker.UnallocatedStart, align);
            tracker.UnallocatedStart += padding;
            var ptr = tracker.UnallocatedStart;
            tracker.UnallocatedStart += size;
            return IResult<nint, AllocatorError>.Ok0(ptr);
        }

        if (Expand(size, align) is IResult<Void, AllocatorError>.Err err)
        {
            return err.Cast<Void, nint, AllocatorError>();
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

    public void Free()
    {
        foreach (var commitedRegion in _commitedRegions)
        {
            _allocator.Free(commitedRegion.StartPtr);
        }

        _available = false;
    }
}