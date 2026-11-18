using System.Numerics;

namespace Tools.Tests.Samples;

// Types the cheatsheet's tests read with the documentation the compiler writes for them, each a
// form a module's library takes.

/// <summary>A shape that has an area.</summary>
public interface IShape
{
    /// <summary>The shape's area in square meters.</summary>
    double Area { get; }

    /// <summary>The shape made larger by a factor.</summary>
    IShape Scaled(double by);
}

/// <summary>A square of one side.</summary>
public sealed class Square : IShape
{
    /// <inheritdoc/>
    public double Area => 1;

    /// <inheritdoc/>
    public IShape Scaled(double by) => this;

    /// <inheritdoc cref="IShape.Area"/>
    public double Size => 1;
}

/// <summary>A named thing.</summary>
public abstract class Named
{
    /// <summary>The thing's name as a reader reads it.</summary>
    public abstract string Name();
}

/// <summary>A thing named by a word.</summary>
public sealed class Word : Named
{
    /// <inheritdoc/>
    public override string Name() => "word";
}

/// <summary>A place on the ground and its seed.</summary>
/// <param name="Place">Where it stands, in meters.</param>
/// <param name="Seed">The seed it came with.</param>
public readonly record struct Planted(Vector3 Place, uint Seed);

/// <summary>A ray cast as far as asked.</summary>
public static class Ray
{
    /// <summary>The first thing the ray meets within its reach.</summary>
    public static float Reach(float far = float.PositiveInfinity, double near = double.NegativeInfinity, float none = float.NaN) => far;
}

/// <summary>A pair of a key and a value.</summary>
public sealed class Pair<TKey, TValue>
{
    /// <summary>The pair's key.</summary>
    public TKey? Key { get; init; }
}
