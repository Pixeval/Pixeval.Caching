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

public class MemoryMappedFileMemoryManager(string directory) : IDisposable
{
    public string Directory => directory;

    public List<SafeMemoryMappedViewHandle> Handles = [];

    public List<string> Filenames = [];

    public INativeAllocator Allocator => new MemoryMappedFileAllocator(this);

    public void Dispose()
    {
        foreach (var se in Filenames.Select(f => Path.Combine(directory, f)))
        {
            File.Delete(se);
        }

        Handles.ForEach(h =>
        {
            h.ReleasePointer();
            h.Dispose();
        });
    }
}