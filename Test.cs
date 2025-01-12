#region Copyright (c) Pixeval.Caching/Pixeval.Caching
// GPL v3 License
// 
// Pixeval.Caching/Pixeval.Caching
// Copyright (c) 2025 Pixeval.Caching/Test.cs
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

namespace Pixeval.Caching;

public class Test
{
    // Cache 20 objects, 9 of them will be dropped
    public static void CacheTest()
    {
        // a random byte array within 5 Kb
        var byteArr1 = new byte[5120];
        var byteArr2 = new byte[5120];
        var byteArr3 = new byte[5120];
        var byteArr4 = new byte[5120];
        var byteArr5 = new byte[5120];
        var byteArr6 = new byte[5120];
        var byteArr7 = new byte[5120];
        var byteArr8 = new byte[5120];
        var byteArr9 = new byte[5120];
        var byteArr10 = new byte[5120];
        var byteArr11 = new byte[5120];
        var byteArr12 = new byte[5120];
        var byteArr13 = new byte[5120];
        var byteArr14 = new byte[5120];
        var byteArr15 = new byte[5120];
        var byteArr16 = new byte[5120];
        var byteArr17 = new byte[5120];
        var byteArr18 = new byte[5120];
        var byteArr19 = new byte[5120];

        byteArr1.AsSpan().Fill(1);
        byteArr2.AsSpan().Fill(2);
        byteArr3.AsSpan().Fill(3);
        byteArr4.AsSpan().Fill(4);
        byteArr5.AsSpan().Fill(5);
        byteArr6.AsSpan().Fill(6);
        byteArr7.AsSpan().Fill(7);
        byteArr8.AsSpan().Fill(8);
        byteArr9.AsSpan().Fill(9);
        byteArr10.AsSpan().Fill(10);
        byteArr11.AsSpan().Fill(11);
        byteArr12.AsSpan().Fill(12);
        byteArr13.AsSpan().Fill(13);
        byteArr14.AsSpan().Fill(14);
        byteArr15.AsSpan().Fill(15);
        byteArr16.AsSpan().Fill(16);
        byteArr17.AsSpan().Fill(17);
        byteArr18.AsSpan().Fill(18);
        byteArr19.AsSpan().Fill(19);

        var cacheTable = new CacheTable<CacheKey, CacheHeader, CacheProtocol>(new CacheProtocol(), new CacheToken());
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr1",
        }, byteArr1);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr2",
        }, byteArr2);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr3",
        }, byteArr3);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr4",
        }, byteArr4);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr5",
        }, byteArr5);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr6",
        }, byteArr6);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr7",
        }, byteArr7);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr8",
        }, byteArr8);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr9",
        }, byteArr9);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr10",
        }, byteArr10);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr11",
        }, byteArr11);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr12",
        }, byteArr12);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr13",
        }, byteArr13);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr14",
        }, byteArr14);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr15",
        }, byteArr15);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr16",
        }, byteArr16);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr17",
        }, byteArr17);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr18",
        }, byteArr18);
        cacheTable.TryCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr19",
        }, byteArr19);
        for (var i = 0; i < 5; i++)
        {
            cacheTable.TryReadCache(new CacheKey()
            {
                DataLength = 5120,
                Key = "arr1",
            }, out _);
        }
        for (var i = 0; i < 5; i++)
        {
            cacheTable.TryReadCache(new CacheKey()
            {
                DataLength = 5120,
                Key = "arr3",
            }, out _);
        }
        for (var i = 0; i < 5; i++)
        {
            cacheTable.TryReadCache(new CacheKey()
            {
                DataLength = 5120,
                Key = "arr5",
            }, out _);
        }
        for (var i = 0; i < 5; i++)
        {
            cacheTable.TryReadCache(new CacheKey()
            {
                DataLength = 5120,
                Key = "arr7",
            }, out _);
        }
        for (var i = 0; i < 5; i++)
        {
            cacheTable.TryReadCache(new CacheKey()
            {
                DataLength = 5120,
                Key = "arr9",
            }, out _);
        }
        for (var i = 0; i < 5; i++)
        {
            cacheTable.TryReadCache(new CacheKey()
            {
                DataLength = 5120,
                Key = "arr11",
            }, out _);
        }
        for (var i = 0; i < 5; i++)
        {
            cacheTable.TryReadCache(new CacheKey()
            {
                DataLength = 5120,
                Key = "arr12",
            }, out _);
        }
        for (var i = 0; i < 5; i++)
        {
            cacheTable.TryReadCache(new CacheKey()
            {
                DataLength = 5120,
                Key = "arr14",
            }, out _);
        }
        for (var i = 0; i < 5; i++)
        {
            cacheTable.TryReadCache(new CacheKey()
            {
                DataLength = 5120,
                Key = "arr18",
            }, out _);
        }
        // cacheTable.PurgeCompact();
        var result1 = cacheTable.TryReadCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr1",
        }, out var span1);
        var result3 = cacheTable.TryReadCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr3",
        }, out var span3);
        var result5 = cacheTable.TryReadCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr5",
        }, out var span5);
        var result7 = cacheTable.TryReadCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr7",
        }, out var span7);
        var result9 = cacheTable.TryReadCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr9",
        }, out var span9);
        var result11 = cacheTable.TryReadCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr11",
        }, out var span11);
        var result12 = cacheTable.TryReadCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr12",
        }, out var span12);
        var result14 = cacheTable.TryReadCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr14",
        }, out var span14);
        var result18 = cacheTable.TryReadCache(new CacheKey()
        {
            DataLength = 5120,
            Key = "arr18",
        }, out var span18);
        Console.WriteLine("asdasdasd");
    }
}