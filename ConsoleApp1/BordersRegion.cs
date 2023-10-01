using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;


public class BordersRegion : PointsRegion
{
    private List<Line> borders;
    public List<Line> Borders { get { return borders; } }
    public BordersRegion(List<Vector> points) : base(points)
    {
        //this.points = points;
        //float num = 0;
        //this.middle = new Vector(0, 0);
        //foreach (Vector v in points)
        //{
        //    num += 1f;
        //    this.middle += v;
        //}
        //this.middle.x /= num;
        //this.middle.y /= num;
        for(int i =0; i < this.points.Count; i++)
        {
            this.points[i] += new Vector(1, 1);
        }
        Vector leftUp = new Vector(float.MaxValue, float.MaxValue);
        foreach (Vector point in this.points)
        {
            //if (point.y > leftUp.y) continue;
            //else if (point.y < leftUp.y)
            //{
            //    leftUp = point;
            //    continue;
            //}
            //else if (point.x < leftUp.x) leftUp = point;
            if (point.Length < leftUp.Length) leftUp = point;
        }
        this.points.Remove(leftUp);
        
        this.points.Sort((a, b) => Vector.Angle(leftUp, a).CompareTo(Vector.Angle(leftUp, b)));
        Stack<Vector> borderPoints = new Stack<Vector>();
        borderPoints.Push(leftUp);

        foreach (Vector point in this.points)
        {
            if (borderPoints.Count > 1)
            {
                Vector top = borderPoints.Pop();
                while (borderPoints.Count > 1 && Vector.Cross(top - borderPoints.Peek(), point - borderPoints.Peek()) < 0f)
                {
                    top = borderPoints.Pop();
                }
                borderPoints.Push(top);
            }
            borderPoints.Push(point);
        }
        this.points = borderPoints.ToList();
        for (int i = 0; i < this.points.Count; i++)
        {
            this.points[i] -= new Vector(1, 1);
        }
        borders = new List<Line>();
        for (int i = 0; i < this.points.Count - 1; i++)
        {
            borders.Add(new Line(this.points[i], this.points[i + 1]));
        }
        borders.Add(new Line(this.points[this.points.Count - 1], this.points[0]));
    }

    public override bool IsPointInside(Vector point)
    {
        for (int i = 0; i < points.Count; i++)
        {
            var borderVec = points[(i + 1) % points.Count] - points[i];
            var toPointVec = point - points[i];
            if (Vector.Cross(borderVec, toPointVec) > 0f) return false;
        }
        return true;
    }

    public List<BordersRegion> Split()
    {
        List<BordersRegion> regions = new List<BordersRegion>();

        Line longest = borders.Find(x => x.Length == borders.Max(y => y.Length));
        if (longest != null)
        {
            float perpA = 0f, perpB = 0f, cutX = 0f, cutY = 0f;
            Random rng = new Random();
            if (!float.IsNaN(longest.A) && longest.A != 0f) {
                cutX = longest.GetMiddle().x + (rng.NextSingle() - 0.5f) * (longest.AsVector().x) / 6f;
                cutY = longest.A * cutX + longest.B;
                perpA = MathF.Tan(MathF.Atan(1f / (-longest.A)) + (rng.NextSingle() - 0.5f) * MathF.PI / 4f);

                //perpA = 1f / (-longest.A);
                //perpA += (float)((rng.NextDouble() - 0.5) / 3.0) * perpA;
                perpB = cutY - perpA * cutX;
            }
            else if (float.IsNaN(longest.A))
            {
                cutX = longest.GetMiddle().x;
                cutY = longest.GetMiddle().y + (rng.NextSingle() - 0.5f) * (longest.AsVector().y) / 6f;
                perpA = MathF.Tan(MathF.Atan(0f) + (rng.NextSingle() - 0.5f) * MathF.PI / 4f);
                //perpA = (rng.NextSingle() - 0.5f)/2f;
                perpB = cutY;
            }
            else if (longest.A == 0f)
            {
                cutX = longest.GetMiddle().x + (rng.NextSingle() - 0.5f) * (longest.AsVector().x) / 6f;
                cutY = longest.B;
                perpA = MathF.Tan(MathF.Atan(float.MaxValue) + (rng.NextSingle() - 0.5f) * MathF.PI / 4f);
                //float maxA = 1400;
                //perpA = rng.NextSingle() >= 0.5f? (-maxA) + maxA * (rng.NextSingle()+1f)/2f : maxA - maxA * (rng.NextSingle()+1f) / 2f;
                perpB = cutY - perpA * cutX;
            }
            Line perp = new Line(perpA, perpB);

            List<Line> otherBorders = new List<Line>(borders.Where((x) => { return x != longest; }));
            //foreach (Line l in borders)
            //{
            //    if (l != longest) otherBorders.Add(l);
            //}
            //Line cut;
            Vector? cutPoint = null;
            foreach (Line l in otherBorders)
            {
                if (perp.IsCrossing(l, out cutPoint)) break;
            }
            if (cutPoint != null)
            {
                Vector perpCut = new Vector(cutX, cutY);
                List<Vector> left = new List<Vector>();
                List<Vector> right = new List<Vector>();
                left.Add(perpCut);
                left.Add((Vector)cutPoint);
                right.Add(perpCut);
                right.Add((Vector)cutPoint);

                foreach (Vector point in points)
                {
                    if (perp.IsBelow(point)) left.Add(point);
                    else right.Add(point);
                }

                regions.Add(new(left));
                regions.Add(new(right));
            }
            else
            {
                Console.WriteLine("Err!");
            }
        }

        return regions;
    }
}
