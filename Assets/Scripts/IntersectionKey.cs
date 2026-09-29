using System;
using UnityEngine;

/// <summary>
/// struct representing a key in dictionary of slice plane intersections
/// </summary>
public struct IntersectionKey
{
    public Vector3Int first;
    public Vector3Int second;

    // key is represented by a segment - 2 points of a cut
    public IntersectionKey(Vector3 first, Vector3 second)
    {
        Vector3Int firstKey = Quantize(first);
        Vector3Int secondKey = Quantize(second);

        // setting fixed order first->second - quantized first point is 'smaller' than second
        if (Compare(firstKey, secondKey) <= 0)
        {
            this.first = firstKey;
            this.second = secondKey;
        }
        else
        {
            this.first = secondKey;
            this.second = firstKey;
        }
    }

    /// <summary>
    /// Transforms 3D point into int for storage
    /// </summary>
    private static Vector3Int Quantize(Vector3 point)
    {
        const float precision = 100000f;

        return new Vector3Int(
            Mathf.RoundToInt(point.x * precision),
            Mathf.RoundToInt(point.y * precision),
            Mathf.RoundToInt(point.z * precision)
        );
    }

    /// <summary>
    /// comparing 2 ints instead of inaccurate float, uses CompareTo from int (values positive, 0, negative)
    /// </summary>
    private static int Compare(Vector3Int a, Vector3Int b)
    {
        if (a.x != b.x)
            return a.x.CompareTo(b.x);

        if (a.y != b.y)
            return a.y.CompareTo(b.y);

        return a.z.CompareTo(b.z);
    }

    public override bool Equals(object obj)
    {
        if (!(obj is IntersectionKey))
            return false;

        IntersectionKey other = (IntersectionKey)obj;

        if (first == other.first && second == other.second)
            return true;
        else
            return false;
    }

    // needed for a custom type key
    public override int GetHashCode()
    {
        unchecked // allows int overflow - we check binary representation
        {
            return HashCode.Combine(first, second);
        }
    }
}

