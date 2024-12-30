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

// Manages all memory mapped files
public unsafe class MemoryMappedFileAllocator(MemoryMappedFileMemoryManager manager) : INativeAllocator
{

    public IResult<nint, AllocatorError> Allocate(nint size)
    {
        // TODO detect allocatablity

        if (manager.DelegatedCombinedBumpPointerAllocator.Allocate(size) is IResult<nint, AllocatorError>.Ok ok)
        {
            return ok;
        }

        // If we must expand.

        var fileName = Guid.NewGuid();
        var mmf = MemoryMappedFile.CreateFromFile(Path.Combine(manager.Directory, fileName.ToString()), FileMode.OpenOrCreate, null, manager.DefaultMemoryMappedFileSize, MemoryMappedFileAccess.ReadWrite);

        var handle = mmf.CreateViewAccessor().SafeMemoryMappedViewHandle;
        byte* ptr = null;
        handle.AcquirePointer(ref ptr);

        if (ptr == null)
        {
            return IResult<IntPtr, AllocatorError>.Err0(AllocatorError.MMapFailedWithNullPointer);
        }

        manager.Handles.Add(new MemoryMappedFileCacheHandle(fileName, new IntPtr(ptr), handle));
        manager.BumpPointerAllocators[fileName] = HeapAllocator.Create(new BumpPointerNativeAllocator(ref Unsafe.AsRef<byte>(ptr), manager.DefaultMemoryMappedFileSize));
        return manager.DelegatedCombinedBumpPointerAllocator.Allocate(size);
    }

    // Avoid using this ! The Unsafe.InitBlockUnaligned causes the entire file 
    // to be copied into RAM.
    public IResult<nint, AllocatorError> AllocateZeroed(nint size)
    {
        return Allocate(size).IfOk(intPtr => Unsafe.InitBlockUnaligned((void*) intPtr, 0, (uint) size));
    }

    // this allows us to free one of the memory mapped files
    public IResult<Void, AllocatorError> Free(nint ptr)
    {
        var memoryMappedFileCacheHandle = manager.Handles.FirstOrDefault(cacheHandle => cacheHandle.Pointer == ptr);

        if (memoryMappedFileCacheHandle?.ViewHandle == default)
        {
            return IResult<Void, AllocatorError>.Err0(AllocatorError.ReadFailed);
        }

        memoryMappedFileCacheHandle.ViewHandle.ReleasePointer();
        memoryMappedFileCacheHandle.ViewHandle.Dispose();
        File.Delete(memoryMappedFileCacheHandle.Filename.ToString());

        manager.Handles.Remove(memoryMappedFileCacheHandle);
        manager.BumpPointerAllocators.Remove(memoryMappedFileCacheHandle.Filename);

        return IResult<Void, AllocatorError>.Ok0(Void.Value);
    }
}