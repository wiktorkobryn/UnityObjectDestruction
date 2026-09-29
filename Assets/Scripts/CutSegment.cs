using UnityEngine;

public struct CutSegment
{
    public VertexData first;
    public VertexData second;

    public CutSegment(VertexData first, VertexData second)
    {
        this.first = first;
        this.second = second;
    }
}
