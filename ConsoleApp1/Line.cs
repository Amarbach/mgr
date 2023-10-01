using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

public class Line
{
    private Vector p0, p1;
    private float a, b;
    public float A { get { return a; } set { a = value; } }
    public float B { get { return b; } set { b = value; } }
    public float Length { get { return (p1 - p0).Length; } }
    public Line(Vector p0, Vector p1)
    {
        this.p0 = p0.x < p1.x ? p0 : p1;
        this.p1 = p1.x > p0.x ? p1 : p0;
        if (p0.x == p1.x) 
        {
            this.p0 = p0.y < p1.y ? p0 : p1;
            this.p1 = p1.y > p0.y ? p1 : p0;
            a = float.NaN; b = float.NaN;
        }
        else
        {
            a = (p0.y - p1.y) / (p0.x - p1.x);
            b = p0.y - a * p0.x;
        }
    }

    public Line(float a, float b)
    {
        this.p0 = new(float.NegativeInfinity, a > 0 ? float.NegativeInfinity : float.PositiveInfinity);
        this.p1 = new(float.PositiveInfinity, a > 0 ? float.PositiveInfinity : float.NegativeInfinity);
        this.a = a;
        this.b = b;
    }
    public Vector AsVector()
    {
        return p1 - p0;
    }
    public void DrawOn(Bitmap image, Color color)
    {
        if (p0.x == float.PositiveInfinity || p0.y == float.PositiveInfinity || p0.x == float.NegativeInfinity || p0.y == float.NegativeInfinity) return;
        if (a <= 1.0f && a >= -1.0f)
        {
            for (int i = (int)p0.x; i < (int)p1.x; i++)
            {
                int y = (int)(i * a + b);
                image.SetPixel(i, y, color);
            }
        }
        else if (a > 1.0f || a < -1.0f)
        {
            for (int i = (int)p0.y; i != (int)p1.y; i = p0.y > p1.y ? i - 1 : i + 1)
            {
                int x = (int)((i - b) / a);
                image.SetPixel(x, i, color);
            }
        }
        else if (float.IsNaN(a))
        {
            for (int i = (int)p0.y; i != (int)p1.y; i = p0.y > p1.y ? i - 1 : i + 1)
            {
                int x = (int)p0.x;
                image.SetPixel(x, i, color);
            }
        }
    }
    public void DrawOn(Bitmap image)
    {
        Color color = Color.Black;
        this.DrawOn(image, color);
    }

    public bool IsBelow(Vector point)
    {
        if (float.IsNaN(this.A))
        {
            return point.x < this.p0.x;
        }
        float y = a * point.x + b;
        return point.y < y;
    }

    public bool IsCrossing(Line other, out Vector? point)
    {
        if (float.IsNaN(other.A))
        {
            float x = other.GetMiddle().x;
            float y = x * this.A + this.B;

            if (y > other.p0.y && y < other.p1.y)
            {
                point = new Vector(x, y);
                return true;
            }
        }
        else if (this.A != other.A)
        {
            float x = (this.B - other.B) / (other.A - this.A);
            float y = ((other.A * this.B) - (this.A * other.B)) / (other.A - this.A);

            point = new Vector(x, y);
            return (x <= this.p1.x && x >= this.p0.x && x >= other.p0.x && x <= other.p1.x);
        }
        point = null;
        return false;
    }

    public Vector GetMiddle()
    {
        float x = (p0.x + p1.x) / 2.0f;
        float y = (p0.y + p1.y) / 2.0f;
        return new Vector(x, y);
    }

    public bool Equals(Line b)
    {
        return this.A == b.A && this.B == b.B && this.p0.Equals(b.p0) && this.p1.Equals(b.p1);
    }

    public List<Vector> ListAllPoints()
    {
        List<Vector> ret = new List<Vector>();
        if (this.A <= 1.0f && this.A >= -1.0f)
        {
            for (int i = (int)p0.x; i < (int)p1.x; i++)
            {
                int y = (int)(i * a + b);
                ret.Add(new Vector(i, y));
            }
        }
        else if (this.A > 1.0f || this.A < -1.0f)
        {
            for(int i = (int)p0.y; i != (int)p1.y; i = p0.y > p1.y ? i - 1 : i + 1)
            {
                int x = (int)((i - b) / a);
                ret.Add(new Vector(x, i));
            }
        }
        return ret;
    }
}
