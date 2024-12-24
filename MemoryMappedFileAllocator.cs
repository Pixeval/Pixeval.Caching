#region Copyright (c) Pixeval.Caching/Pixeval.Caching
// GPL v3 License
// 
// Pixeval.Caching/Pixeval.Caching
// Copyright (c) 2024 Pixeval.Caching/MemoryMappedFileAllocator.cs
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

using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace Pixeval.Caching;

public readonly unsafe struct MemoryMappedFileAllocator(MemoryMappedFileMemoryManager manager) : INativeAllocator
{
    public IResult<nint, AllocatorError> Allocate(nint size)
    {
        if (size % sizeof(long) != 0)
        {
            return new IResult<nint, AllocatorError>.Err(AllocatorError.UnalignedAllocation);
        }

        var fileName = Guid.NewGuid().ToString();
        var mmf = MemoryMappedFile.CreateFromFile(Path.Combine(manager.Directory, fileName), FileMode.OpenOrCreate, null, size, MemoryMappedFileAccess.ReadWrite);

        var handle = mmf.CreateViewAccessor().SafeMemoryMappedViewHandle;
        byte* ptr = null;
        handle.AcquirePointer(ref ptr);
        manager.Handles.Add(handle);
        manager.Filenames.Add(fileName);
        return IResult<nint, AllocatorError>.Ok0((nint) ptr);
    }

    // Avoid using this ! The Unsafe.InitBlockUnaligned causes the entire file 
    // to be copied into RAM.
    public IResult<nint, AllocatorError> AllocateZeroed(nint size)
    {
        return Allocate(size).IfOk(intPtr => Unsafe.InitBlockUnaligned((void*) intPtr, 0, (uint) size));
    }

    public IResult<Void, AllocatorError> Free(nint ptr)
    {
        var handle = manager.Handles.FirstOrDefault(handle =>
        {
            byte* pPtr = null;
            handle.AcquirePointer(ref pPtr);
            if (pPtr == (byte*) ptr)
            {
                handle.ReleasePointer();
                return true;
            }

            handle.ReleasePointer();
            return false;
        });

        if (handle == default)
        {
            return IResult<Void, AllocatorError>.Err0(AllocatorError.ReadFailed);
        }

        handle.ReleasePointer();
        handle.Dispose();
        return IResult<Void, AllocatorError>.Ok0(Void.Value);
    }
}