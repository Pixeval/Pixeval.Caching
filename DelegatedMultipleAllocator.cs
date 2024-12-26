#region Copyright (c) Pixeval.Caching/Pixeval.Caching
// GPL v3 License
// 
// Pixeval.Caching/Pixeval.Caching
// Copyright (c) 2024 Pixeval.Caching/DelegatedMultipleAllocator.cs
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

using System.Drawing;
using System.Runtime.CompilerServices;

namespace Pixeval.Caching;

public unsafe class DelegatedMultipleAllocator(List<INativeAllocator> allocators) : INativeAllocator
{
    public IResult<IntPtr, AllocatorError> Allocate(nint size)
    {
        foreach (var nativeAllocator in allocators)
        {
            if (nativeAllocator.Allocate(size) is IResult<nint, AllocatorError>.Ok ok)
            {
                return ok;
            }
        }
        return IResult<IntPtr, AllocatorError>.Err0(AllocatorError.AggregateError);
    }

    public IResult<nint, AllocatorError> AllocateZeroed(nint size)
    {
        return Allocate(size).IfOk(intPtr => Unsafe.InitBlockUnaligned((void*) intPtr, 0, (uint)size));
    }


    public IResult<Void, AllocatorError> Free(nint ptr, nint size)
    {
        // Since it's implementers' job to insure the free is done correctly, the free should not be performed if the pointer is not within the
        // managing range of current allocator, we just delegate the free to the first allocator that can free the pointer
        foreach (var nativeAllocator in allocators)
        {
            if (nativeAllocator.Free(ptr, size) is IResult<Void, AllocatorError>.Ok ok)
            {
                return ok;
            }
        }
        return IResult<Void, AllocatorError>.Err0(AllocatorError.AggregateError);
    }
}