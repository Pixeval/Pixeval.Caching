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

public class MemoryMappedFileMemoryManager : IDisposable
{
    public string Directory => _directory;

    public Dictionary<nint, SafeMemoryMappedViewHandle> Handles = [];

    public List<string> Filenames = [];
    private readonly string _directory;

    public MemoryMappedFileMemoryManager(string directory)
    {
        _directory = directory;
        DelegatedCombinedBumpPointerAllocator = new DelegatedMultipleAllocator(BumpPointerAllocators);
    }

    public INativeAllocator Allocator => new MemoryMappedFileAllocator(this);

    public INativeAllocator DelegatedCombinedBumpPointerAllocator { get; }

    public List<INativeAllocator> BumpPointerAllocators { get; } = [];

    public void Dispose()
    {
        foreach (var se in Filenames.Select(f => Path.Combine(_directory, f)))
        {
            File.Delete(se);
        }

        foreach (var (ptr, handle) in Handles)
        {
            if (ptr != 0)
            {
                handle.ReleasePointer();
                handle.Dispose();
            }
        }
        GC.SuppressFinalize(this);
    }
}