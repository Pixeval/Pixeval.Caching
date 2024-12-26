#region Copyright (c) Pixeval.Caching/Pixeval.Caching
// GPL v3 License
// 
// Pixeval.Caching/Pixeval.Caching
// Copyright (c) 2024 Pixeval.Caching/ManagedNativeAllocator.cs
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

/// <summary>
/// The name of this class may sound weird, but it actually refers to a "managed heap of native memory", yes :)
/// </summary>
public unsafe class BumpPointerNativeAllocator(ref byte ptrStart, nint heapSize) : INativeAllocator
{
    public nint BumpingPointer { get; private set; } = new(Unsafe.AsPointer(ref ptrStart));

    public IResult<nint, AllocatorError> Allocate(nint size)
    {
        ref var ptr = ref Unsafe.AsRef<byte>((void*) BumpingPointer);
        var newAddress = Unsafe.AddByteOffset(ref ptr, heapSize);
        if (newAddress > size)
        {
            return IResult<IntPtr, AllocatorError>.Err0(AllocatorError.OutOfMemory);
        }
        BumpingPointer = newAddress;
        return IResult<IntPtr, AllocatorError>.Ok0(ptr);
    }

    public IResult<nint, AllocatorError> AllocateZeroed(nint size)
    {
        return Allocate(size).IfOk(ni => Unsafe.InitBlockUnaligned((void*) ni, 0, (uint) size));
    }

    public IResult<Void, AllocatorError> Free(nint ptr)
    {
        // Bumping pointer allocator does not free pointers, it's done be the allocator
        throw new NotImplementedException();
    }
}