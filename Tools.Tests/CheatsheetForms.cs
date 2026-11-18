using System.Numerics;

namespace Tools.Tests.Forms;

// Every public form of C# a module's library uses or may, each in a type the cheatsheet's tests
// read with the documentation the compiler writes, its line on the page set beside it in the tests.

/// <summary>A length in meters.</summary>
public readonly struct Meters(double value)
{
    /// <summary>The length's value.</summary>
    public double Value { get; } = value;

    /// <summary>The sum of two lengths.</summary>
    public static Meters operator +(Meters a, Meters b) => new(a.Value + b.Value);

    /// <summary>The length the other way.</summary>
    public static Meters operator -(Meters a) => new(-a.Value);

    /// <summary>Whether two lengths are the same.</summary>
    public static bool operator ==(Meters a, Meters b) => a.Value == b.Value;

    /// <summary>Whether two lengths differ.</summary>
    public static bool operator !=(Meters a, Meters b) => a.Value != b.Value;

    /// <summary>The length as a number of meters.</summary>
    public static implicit operator double(Meters m) => m.Value;

    /// <summary>A number taken as meters.</summary>
    public static explicit operator Meters(double value) => new(value);

    /// <summary>Whether the other is the same length.</summary>
    public override bool Equals(object? other) => other is Meters m && m == this;

    /// <summary>The length's hash.</summary>
    public override int GetHashCode() => Value.GetHashCode();
}

/// <summary>A plan of things drawn, each of a mesh and a tag.</summary>
public sealed class Plan<TMesh, TTag>
{
    /// <summary>One thing drawn.</summary>
    public readonly record struct Item(TMesh Mesh, TTag Tag);

    /// <summary>The things drawn, each with whether it moved.</summary>
    public IReadOnlyList<(Item Item, bool Moved)> Drawn { get; init; } = [];

    /// <summary>Whether a thing stands still.</summary>
    public bool IsStill(in Item item) => true;

    /// <summary>The first thing drawn of a kind, if any.</summary>
    public bool TryFirst<TKind>(out TKind? found) where TKind : class
    {
        found = null;
        return false;
    }
}

/// <summary>Ways a step may be taken, more than one at once.</summary>
[Flags]
public enum Ways
{
    /// <summary>No way.</summary>
    None = 0,

    /// <summary>Forward.</summary>
    Forward = 1,

    /// <summary>Back.</summary>
    Back = 2,
}

/// <summary>A field of heights.</summary>
public sealed class Field
{
    /// <summary>The name a field is kept by where none is given.</summary>
    public const string DefaultName = "field";

    /// <summary>The field every new one starts from.</summary>
    public static readonly Field Empty = new();

    /// <summary>Raised when a height changes.</summary>
    public event EventHandler<int>? Changed;

    /// <summary>The height at a column and a row.</summary>
    public float this[int column, int row] { get => 0; set => Changed?.Invoke(this, column); }

    /// <summary>The heights of the cells given, each by its index.</summary>
    public void Read(ReadOnlySpan<int> cells, Span<float> into, ref int count, params int[] skipped) { }

    /// <summary>The field's tallest place, or none.</summary>
    public Vector3? Tallest(Vector3 from = default, Ways ways = Ways.Forward | Ways.Back) => null;
}

/// <summary>Ways to read a field.</summary>
public static class FieldReading
{
    /// <summary>The field's height at its middle.</summary>
    public static float Middle(this Field field) => 0;
}
