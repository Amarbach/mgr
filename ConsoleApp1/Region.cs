using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class Region
{
    protected Vector middle;
    public Vector Center { get { return middle; } }
    public abstract bool IsPointInside(Vector point);
}
