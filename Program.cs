using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pixeval.Caching;

namespace Pixeval.Caching;

public class CacheKey : IEquatable<CacheKey>
{
    public string Key { get; set; }

    public int DataLength { get; set; }

    public bool Equals(CacheKey? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Key == other.Key;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((CacheKey) obj);
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
}

[StructLayout(LayoutKind.Sequential)]
public record struct CacheHeader(int DataLength);

public class CacheProtocol : ICacheProtocol<CacheKey, CacheHeader>
{
    public CacheHeader GetHeader(CacheKey key)
    {
        return new CacheHeader(key.DataLength);
    }

    public Span<byte> SerializeHeader(CacheHeader header)
    {
        return ConvertToBytes(header);
    }

    public unsafe CacheHeader DeserializeHeader(Span<byte> span)
    {
        var ptr = (int*) Unsafe.AsPointer(ref span.GetPinnableReference());
        return new CacheHeader(*ptr);
    }

    public static unsafe int GetHeaderLength()
    {
        return sizeof(CacheHeader);
    }

    public int GetDataLength(CacheHeader header)
    {
        return header.DataLength;
    }

    public static unsafe byte[] ConvertToBytes<T>(T value) where T : unmanaged
    {
        var pointer = (byte*) &value;

        var bytes = new byte[sizeof(T)];
        for (int i = 0; i < sizeof(T); i++)
        {
            bytes[i] = pointer[i];
        }

        return bytes;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        /*var memoryManager = new MemoryMappedFileMemoryManager("D://mmaptest", 8);
        var cacheTable = new CacheTable<CacheKey, CacheHeader, CacheProtocol>(memoryManager, new CacheProtocol());
        Span<byte> span = stackalloc byte[512];
        span.Fill(15);
        var cacheKey = new CacheKey()
        {
            DataLength = span.Length,
            Key = "test",
        };
        cacheTable.TryCache(cacheKey, span);

        if (cacheTable.TryReadCache(cacheKey, out var readSpan))
        {
            Console.WriteLine("Cache read success");
        }*/
        Test.CacheTest();
    }
}