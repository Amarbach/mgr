using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Vector
{
    private float _x, _y;
    public float x { get { return _x; } set { _x = value; } }
    public float y { get { return _y; } set { _y = value; } }
    
    public Vector(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    public float Length { get { return MathF.Sqrt(x * x + y * y); } }
    public static Vector operator -(Vector a)
    {
        return new(-a.x, -a.y);
    }

    public static Vector operator +(Vector a, Vector b)
    {
        return new(a.x + b.x, a.y + b.y);
    }

    public static Vector operator -(Vector a, Vector b) => a + (-b);

    public static Vector operator *(Vector a, float b)
    {
        return new(a.x * b, a.y * b);
    }

    public static Vector operator /(Vector a, float b)
    {
        return new(a.x * (1f/b), a.y * (1f/b));
    }
    public bool Equals(Vector b)
    {
        return this.x == b.x && this.y == b.y;
    }

    public static float Cross(Vector a, Vector b)
    {
        return (a.x * b.y - a.y * b.x);
    }

    public static float Dot(Vector a, Vector b)
    {
        return (a.x * b.x + a.y * b.y);
    }

    public static float Angle(Vector a, Vector b)
    {
        return MathF.Atan2(Cross(a, b), Dot(a, b));
    }

    public override string ToString()
    {
        return "(" + x + ";" + y + ")";
    }
}
