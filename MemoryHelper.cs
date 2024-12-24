﻿#region Copyright (c) Pixeval.Caching/Pixeval.Caching
// GPL v3 License
// 
// Pixeval.Caching/Pixeval.Caching
// Copyright (c) 2024 Pixeval.Caching/MemoryHelper.cs
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

using System.Numerics;

namespace Pixeval.Caching;

public static class MemoryHelper
{
    public static T RoundToNearestMultipleOf<T>(T number, T align)
        where T :
        IBinaryInteger<T>,
        IBitwiseOperators<T, T, T>,
        IAdditionOperators<T, T, T>,
        ISubtractionOperators<T, T, T>
    {
        return number + (~number + T.One & align - T.One);
    }
}