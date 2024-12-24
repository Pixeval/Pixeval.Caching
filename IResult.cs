#region Copyright (c) Pixeval.Caching/Pixeval.Caching
// GPL v3 License
// 
// Pixeval.Caching/Pixeval.Caching
// Copyright (c) 2024 Pixeval.Caching/IResult.cs
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

using System.Diagnostics.CodeAnalysis;

namespace Pixeval.Caching;

public interface IResult<T, E>
{
    public record Ok(T Value) : IResult<T, E>;

    public record Err(E Error) : IResult<T, E>;

    public static IResult<T, E> Ok0(T value) => new Ok(value);

    public static IResult<T, E> Err0(E error) => new Err(error);
}

public static class ResultExtension
{
    public static IResult<R, E> Select<T, E, R>(this IResult<T, E> result, Func<T, R> selector)
    {
        return result switch
        {
            IResult<T, E>.Ok(var value) => new IResult<R, E>.Ok(selector(value)),
            IResult<T, E>.Err(var error) => new IResult<R, E>.Err(error),
            _ => throw new NotImplementedException()
        };
    }

    public static IResult<R, E> SelectMany<T, E, R>(this IResult<T, E> result, Func<T, IResult<R, E>> selector)
    {
        return result switch
        {
            IResult<T, E>.Ok(var value) => selector(value),
            IResult<T, E>.Err(var error) => new IResult<R, E>.Err(error),
            _ => throw new NotImplementedException()
        };
    }

    public static bool TryGetValue<T, E>(this IResult<T, E> result, [MaybeNullWhen(false)] out T value)
    {
        value = result switch
        {
            IResult<T, E>.Ok(var v) => v,
            _ => default
        };
        return value is not null;
    }

    public static IResult<T, E> IfOk<T, E>(this IResult<T, E> result, Action<T> action)
    {
        return result.Select(value =>
        {
            action(value);
            return value;
        });
    }

    public static IResult<R, E> Cast<T, R, E>(this IResult<T, E>.Err err)
    {
        return IResult<R, E>.Err0(err.Error);
    }
}