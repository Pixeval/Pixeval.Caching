#region Copyright (c) Pixeval.Caching/Pixeval.Caching
// GPL v3 License
// 
// Pixeval.Caching/Pixeval.Caching
// Copyright (c) 2024 Pixeval.Caching/MemoryMappedFileAllocatorManager.cs
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

using Microsoft.Win32.SafeHandles;

namespace Pixeval.Caching;

public record MemoryMappedFileCacheHandle(Guid Filename, nint Pointer, SafeMemoryMappedViewHandle ViewHandle);

public class MemoryMappedFileMemoryManager : IDisposable
{
    public string Directory { get; }

    public nint Align { get; }

    // Default memory mapped file size is 100MB
    public nint DefaultMemoryMappedFileSize { get; set; } = 100 * 1024 * 1024;

    public List<MemoryMappedFileCacheHandle> Handles = [];

    public MemoryMappedFileMemoryManager(string directory, nint align)
    {
        Directory = directory;
        Align = align;
        DelegatedCombinedBumpPointerAllocator = new DelegatedMultipleAllocator(this, BumpPointerAllocators.Values);
    }

    public INativeAllocator DominantAllocator => new MemoryMappedFileAllocator(this);

    public INativeAllocator DelegatedCombinedBumpPointerAllocator { get; }

    public Dictionary<Guid, HeapAllocator> BumpPointerAllocators { get; } = [];

    public void Dispose()
    {
        foreach (var se in Handles.Select(f => Path.Combine(Directory, f.Filename.ToString())))
        {
            File.Delete(se);
        }

        foreach (var (filename, ptr, handle) in Handles)
        {
            File.Delete(filename.ToString());
            if (ptr != 0)
            {
                handle.ReleasePointer();
                handle.Dispose();
            }
        }
        GC.SuppressFinalize(this);
    }
}