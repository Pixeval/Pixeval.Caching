We explain, in this file, how the allocators work in this library

The allocator is separated into 3 different subclasses, all derived from the base interface `INativeAllocator`, the subclasses are respectively:

1. `BumpPointerNativeAllocator`
2. `MemoryMappedFileAllocator`
3. `DelegatedMultipleAllocator`

these 3 allocators cooperate closely and are managed by a single class called `MemoryMappedFileAllocatorManager`.

In summary, the cache layout is as follows: the cache itself can be separated into several files
each of which is an instance of the memory mapped file, and each memory mapped file is refined into 
different chunks, now, for each memory mapped file, there is an allocator responsible for allocating 
the chunks, similarly, there is also a manager for managing the memory mapped files, thus it forms 
naturally a tree-likes structure of allocators.