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

    public AllocatorState TryAllocate(nint size, out Span<byte> span)
    {
        if (manager.DelegatedCombinedBumpPointerAllocator.TryAllocate(size, out var s) is AllocatorState.AllocationSuccess)
        {
            span = s;
            return AllocatorState.AllocationSuccess;
        }

        // If we must expand.

        if (manager.BumpPointerAllocators.Count >= manager.Token.MaxMemoryMappedFileCount)
        {
            span = [];
            return AllocatorState.OutOfMemory;
        }

        var fileName = Guid.NewGuid();
        var mmf = MemoryMappedFile.CreateFromFile(Path.Combine(manager.Token.CacheDirectory, fileName.ToString()), FileMode.OpenOrCreate, null, manager.Token.MemoryMappedFileInitialSize, MemoryMappedFileAccess.ReadWrite);

        var handle = mmf.CreateViewAccessor().SafeMemoryMappedViewHandle;
        byte* ptr = null;
        handle.AcquirePointer(ref ptr);

        if (ptr == null)
        {
            span = [];
            return AllocatorState.MMapFailedWithNullPointer;
        }

        manager.Handles.Add(new MemoryMappedFileCacheHandle(fileName, new IntPtr(ptr), handle));
        manager.BumpPointerAllocators[fileName] = HeapAllocator.Create(new BumpPointerNativeAllocator(ref Unsafe.AsRef<byte>(ptr), manager.Token.MemoryMappedFileInitialSize));

        var result = manager.DelegatedCombinedBumpPointerAllocator.TryAllocate(size, out var s2);
        switch (result)
        {
            case AllocatorState.AllocationSuccess:
                span = s2;
                return AllocatorState.AllocationSuccess;
            case var _:
                span = [];
                return result;
        }
    }

    // Avoid using this ! The Unsafe.InitBlockUnaligned causes the entire file 
    // to be copied into RAM.
    public AllocatorState TryAllocateZeroed(nint size, out Span<byte> span)
    {
        var result = TryAllocate(size, out var s);
        s.Clear();
        span = s;
        return AllocatorState.AllocationSuccess;
    }

    // this allows us to free one of the memory mapped files
    public IResult<Void, AllocatorState> Free(nint ptr)
    {
        var memoryMappedFileCacheHandle = manager.Handles.FirstOrDefault(cacheHandle => cacheHandle.Pointer == ptr);

        if (memoryMappedFileCacheHandle?.ViewHandle == default)
        {
            return IResult<Void, AllocatorState>.Err0(AllocatorState.ReadFailed);
        }

        memoryMappedFileCacheHandle.ViewHandle.ReleasePointer();
        memoryMappedFileCacheHandle.ViewHandle.Dispose();
        File.Delete(memoryMappedFileCacheHandle.Filename.ToString());

        manager.Handles.Remove(memoryMappedFileCacheHandle);
        manager.BumpPointerAllocators.Remove(memoryMappedFileCacheHandle.Filename);

        return IResult<Void, AllocatorState>.Ok0(Void.Value);
    }
}