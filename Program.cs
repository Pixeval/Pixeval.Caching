using Pixeval.Caching;

var manager = new MemoryMappedFileMemoryManager("D:\\mmaptest", 4);
var ptr = manager.DominantAllocator.Allocate(2047);
var ptr2 = manager.DominantAllocator.Allocate(2048);
Console.WriteLine(ptr);
Console.WriteLine(ptr2);