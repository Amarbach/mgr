using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

 class Continent
{
    private float density;
    private bool land;
    private float level;
    private Region area;
    private Vector moveDirection;
    public float Density {  get { return density; } }
    public bool IsLand { get { return land; } }
    public float Height { get { return level; } }
    public Region Area { get { return area; } }
    public Vector Direction { get { return moveDirection; } }

    public Continent(bool land, Region area, Vector moveDirection)
    {
        Random rng = new Random();
        this.land = land;
        this.area = area;
        this.moveDirection = moveDirection;
        this.level = land ? rng.NextSingle() / 10f : -.5f + (rng.NextSingle() / 5f);
        this.density = 0.01f + rng.NextSingle() * 0.3f;
    }

    public Continent(float level, Region area, Vector moveDirection, float minDensity, float densityRange)
    {
        Random rng = new Random();
        this.level = level;
        this.area = area;
        this.moveDirection = moveDirection;
        this.density = minDensity + rng.NextSingle() * densityRange;
    }

    public Continent(bool land, Region area, Vector moveDirection, float level)
    {
        Random rng = new Random();
        this.land = land;
        this.area = area;
        this.moveDirection = moveDirection;
        this.level = level;
        this.density = 0.01f + rng.NextSingle() * 0.15f;
    }
}
