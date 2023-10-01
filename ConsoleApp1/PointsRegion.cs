using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PointsRegion : Region
{
    protected List<Vector> points;
    public List<Vector> Points { get { return points; } }
    public PointsRegion(List<Vector> points)
    {
        this.points = points;
        float num = 0;
        this.middle = new Vector(0, 0);
        foreach (Vector v in points)
        {
            num += 1f;
            this.middle += v;
        }
        this.middle.x /= num;
        this.middle.y /= num;
    }
    public PointsRegion(Vector[] points)
    {
        this.points = new List<Vector>(points);
        float num = 0;
        this.middle = new Vector(0, 0);
        foreach (Vector v in points)
        {
            num += 1f;
            this.middle += v;
        }
        this.middle.x /= num;
        this.middle.y /= num;
    }

    public override bool IsPointInside(Vector point)
    {
        foreach(var p in this.points)
        {
            if (p.x == point.x && p.y == point.y) return true;
        }
        return false;
    }
}
