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

public unsafe class MemoryMappedFileAllocator(MemoryMappedFileMemoryManager manager) : INativeAllocator
{
    private bool _cacheFileInitialized;

    public IResult<nint, AllocatorError> Allocate(nint size)
    {
        if (_cacheFileInitialized)
        {
            return manager.DelegatedCombinedBumpPointerAllocator.Allocate(size);
        }

        if (size % sizeof(long) != 0)
        {
            return new IResult<nint, AllocatorError>.Err(AllocatorError.UnalignedAllocation);
        }

        var fileName = Guid.NewGuid().ToString();
        var mmf = MemoryMappedFile.CreateFromFile(Path.Combine(manager.Directory, fileName), FileMode.OpenOrCreate, null, size, MemoryMappedFileAccess.ReadWrite);

        var handle = mmf.CreateViewAccessor().SafeMemoryMappedViewHandle;
        byte* ptr = null;
        handle.AcquirePointer(ref ptr);

        if (ptr == null)
        {
            return IResult<IntPtr, AllocatorError>.Err0(AllocatorError.MMapFailedWithNullPointer);
        }

        manager.Handles[new IntPtr(ptr)] = handle;
        manager.Filenames.Add(fileName);
        manager.BumpPointerAllocators.Add(new BumpPointerNativeAllocator(ref Unsafe.AsRef<byte>(ptr), size));
        _cacheFileInitialized = true;
        return manager.DelegatedCombinedBumpPointerAllocator.Allocate(size);
    }

    // Avoid using this ! The Unsafe.InitBlockUnaligned causes the entire file 
    // to be copied into RAM.
    public IResult<nint, AllocatorError> AllocateZeroed(nint size)
    {
        return Allocate(size).IfOk(intPtr => Unsafe.InitBlockUnaligned((void*) intPtr, 0, (uint) size));
    }

    public IResult<Void, AllocatorError> Free(nint ptr, nint _)
    {
        var keyValuePair = manager.Handles.FirstOrDefault(pair => pair.Key == ptr);

        if (keyValuePair.Value == default)
        {
            return IResult<Void, AllocatorError>.Err0(AllocatorError.ReadFailed);
        }

        keyValuePair.Value.ReleasePointer();
        keyValuePair.Value.Dispose();
        manager.Handles.Remove(ptr);
        return IResult<Void, AllocatorError>.Ok0(Void.Value);
    }
}