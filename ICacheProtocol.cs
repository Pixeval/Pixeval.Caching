﻿#region Copyright (c) Pixeval.Caching/Pixeval.Caching
// GPL v3 License
// 
// Pixeval.Caching/Pixeval.Caching
// Copyright (c) 2025 Pixeval.Caching/ICacheProtocol.cs
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

public interface ICacheProtocol<in TKey, THeader> where THeader : unmanaged
{
    THeader GetHeader(TKey key);

    Span<byte> SerializeHeader(THeader header);

    THeader DeserializeHeader(Span<byte> span);

    static abstract int GetHeaderLength();

    int GetDataLength(THeader header);
}