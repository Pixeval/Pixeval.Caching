using Pixeval.Caching;

var manager = new MemoryMappedFileMemoryManager("D:\\mmaptest");
var heapAllocator = HeapAllocator.Create(manager.Allocator);
var ptr = heapAllocator.Allocate(2000000000, 4);