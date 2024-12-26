using Pixeval.Caching;

var manager = new MemoryMappedFileMemoryManager("D:\\mmaptest");
var heapAllocator = HeapAllocator.Create(manager.DominantAllocator);
var ptr = heapAllocator.Allocate(2000000000, 4);