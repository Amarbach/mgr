using AVXPerlinNoise;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum PartitionMethode
{
    RANDOMFILL,
    VORONOI,
    SPLIT
}

public enum BuildStrategy
{
    NONE,
    PEAKS,
    SPREAD
}

public class HeightMapGenerator
{
    //global
    private int seed;
    private int width, height;
    private Random rng;

    //plates
    private PartitionMethode partitionType = PartitionMethode.RANDOMFILL;
    private BuildStrategy buildStrat = BuildStrategy.SPREAD;
    private float peakChance = 0.01f, minDensity = 0.05f, densityRange = 0.2f;
    private int areaNum = 6;

    private float[,] mountainDist, shoreDist, trenchDist, uplandDist, oceanDist, heightMap;
    private int[,] correlation;
    private Continent[] continents;
    private List<Region> regions;

    //perlin
    private float persistance = 0.5f, scale = 10f, lacunarity = 2f;
    private int octaves = 1;

    //square-diamond
    private float fluctuation;

    //worley
    float horCells = 5, verCells = 5;
    bool flip = false;

    public HeightMapGenerator()
    {
        rng = new Random();
        List<Region> regions = new List<Region>();
    }

    public HeightMapGenerator SetSeed(int value)
    {
        seed = value;
        rng = new Random(seed);
        return this;
    }
    public HeightMapGenerator SetDimensions(int width, int height)
    {
        this.width = width;
        this.height = height;

        correlation = new int[height, width];
        mountainDist = new float[height, width];
        shoreDist = new float[height, width];
        trenchDist = new float[height, width];
        uplandDist = new float[height, width];
        oceanDist = new float[height, width];
        heightMap = new float[height, width];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                correlation[i, j] = 0;
                mountainDist[i, j] = float.PositiveInfinity;
                shoreDist[i, j] = float.PositiveInfinity;
                trenchDist[i, j] = float.PositiveInfinity;
                uplandDist[i, j] = float.PositiveInfinity;
                oceanDist[i, j] = float.PositiveInfinity;
                heightMap[i, j] = 0.0f;
            }
        }
        return this;
    }
    public HeightMapGenerator SetPartitionType(PartitionMethode methode)
    {
        partitionType = methode;
        return this;
    }
    public HeightMapGenerator SetRegionNumber(int number)
    {
        areaNum = number;
        return this;
    }
    public HeightMapGenerator SetBuildStrategy(BuildStrategy bStrat)
    {
        buildStrat = bStrat;
        return this;
    }
    public HeightMapGenerator SetPeakChance(float chance)
    {
        peakChance = chance;
        return this;
    }
    public HeightMapGenerator SetMinDensity(float value)
    {
        minDensity = value;
        return this;
    }
    public HeightMapGenerator SetDensityRange(float value)
    {
        densityRange = value;
        return this;
    }
    public HeightMapGenerator SetPersistance(float value)
    {
        persistance = value;
        return this;
    }
    public HeightMapGenerator SetScale(float value)
    {
        scale = value;
        return this;
    }
    public HeightMapGenerator SetLacunarity(float value)
    {
        lacunarity = value;
        return this;
    }
    public HeightMapGenerator SetFluctuation(float value)
    {
        fluctuation = value;
        return this;
    }
    public HeightMapGenerator SetHorizontalCells(float value)
    {
        horCells = MathF.Floor(value);
        horCells = horCells < 1 ? 1 : horCells;
        return this;
    }
    public HeightMapGenerator SetVerticalCells(float value)
    {
        verCells = MathF.Floor(value);
        verCells = horCells < 1 ? 1 : verCells;
        return this;
    }
    public HeightMapGenerator SetOctaves(int value)
    {
        octaves = value;
        return this;
    }
    public HeightMapGenerator SetFlip(bool value)
    {
        flip = value;
        return this;
    }
    public HeightMap Generate()
    {
        var sw = new Stopwatch();
        sw.Start();
        GenerateRegions();
        if (partitionType == PartitionMethode.VORONOI || partitionType == PartitionMethode.RANDOMFILL)
        {
            if (buildStrat == BuildStrategy.NONE) GeneratePointGraphs();
            else if (buildStrat == BuildStrategy.PEAKS) GeneratePointGraphsAlt();
            else if (buildStrat == BuildStrategy.SPREAD) GeneratePointGraphsAlt2();
        }
        else
        {
            GenerateBorderGraphs();
        }
        if (buildStrat != BuildStrategy.SPREAD) GenerateHeightmapProper();
        sw.Stop();
        var ret = new HeightMap(heightMap);
        ret.time = sw.ElapsedMilliseconds;
        return ret;
    }

    private void GenerateRegions()
    {
        this.regions = new();
        switch (partitionType)
        {
            case PartitionMethode.RANDOMFILL:
                RandomFillPartition();
                break;
            case PartitionMethode.VORONOI:
                VoronoiPartition();
                break;
            case PartitionMethode.SPLIT:
                SplitPartition();
                break;
        }

    }

    private float Distance(float x0, float y0, float x1, float y1)
    {
        return MathF.Sqrt(MathF.Pow(x0 - x1, 2) + MathF.Pow(y0 - y1, 2));
    }

    private void SplitPartition()
    {
        List<Vector> points = new List<Vector>()
        {
            new Vector(0, 0),
            new Vector(width - 1, height - 1),
            new Vector(width - 1, 0),
            new Vector(0, height - 1),

        };

        BordersRegion region = new BordersRegion(points);
        var nextStep = new List<BordersRegion>() { region };
        for (int i = 0; i < areaNum; i++)
        {
            var curStep = nextStep;
            nextStep = new();
            foreach (BordersRegion reg in curStep)
            {
                nextStep.AddRange(reg.Split());
            }
        }
        var conversion = new List<Region>();
        foreach (BordersRegion reg in nextStep)
        {
            conversion.Add(reg);
        }
        regions = conversion;
    }

    private void VoronoiPartition()
    {
        int[,] corelation = new int[height, width];

        Random rng = new Random();
        List<Vector> points = new List<Vector>();
        List<List<Vector>> regions = new List<List<Vector>>();
        for (int i = 0; i < areaNum; i++)
        {
            points.Add(new Vector(rng.Next(width), rng.Next(height)));
            regions.Add(new List<Vector>());
        }

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int whoMinDist = -1;
                float minDist = float.MaxValue;
                for (int k = 0; k < areaNum; k++)
                {
                    float dist = (Distance(j, i, (int)points[k].x, (int)points[k].y));
                    if (minDist > dist)
                    {
                        minDist = dist;
                        whoMinDist = k;
                    }
                }
                regions[whoMinDist].Add(new Vector(j, i));
                corelation[i, j] = whoMinDist;
            }
        }

        List<PointsRegion> ret = new();

        foreach (var region in regions)
        {
            this.regions.Add(new PointsRegion(region.ToArray()));
        }
    }

    private void RandomFillPartition()
    {
        Stack<Vector> currStep;
        Stack<Vector> nextStep = new();
        short[,] corelation = new short[height, width];
        for (int i = 0; i < height; i++) for (int j = 0; j < width; j++) corelation[i, j] = 0;


        for (int i = 0; i < areaNum; i++)
        {
            Vector randVec = new Vector(rng.Next(width - 1), rng.Next(height - 1));
            corelation[(int)randVec.y, (int)randVec.x] = (short)(i + 1);
            nextStep.Push(randVec);
        }

        while (nextStep.Count > 0)
        {
            currStep = nextStep;
            nextStep = new();
            while (currStep.Count > 0)
            {
                Vector curr = currStep.Pop();
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var target = curr + new Vector(j - 1, i - 1);
                        float chance = 1.0f - ((i != 0 ? 0.3f : 0f) + (j != 0 ? 0.3f : 0f));
                        if (target.x >= 0 && target.x < width && target.y >= 0 && target.y < height)
                        {
                            if (corelation[(int)target.y, (int)target.x] == 0 && rng.NextDouble() > 1 - chance)
                            {
                                corelation[(int)target.y, (int)target.x] = corelation[(int)curr.y, (int)curr.x];
                                nextStep.Push(target);
                            }
                        }
                    }
                }
            }
        }
        List<List<Vector>> points = new();
        for (int i = 0; i < areaNum; i++)
        {
            points.Add(new List<Vector>());
        }

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                short[] counts = new short[areaNum];
                for (int k = 0; k < areaNum; k++) counts[k] = 0;
                short maxind = 0;
                for (int k = -1; k < 2; k++)
                {
                    for (int l = -1; l < 2; l++)
                    {
                        var target = new Vector(j + l, i + k);
                        if (target.x >= 0 && target.x < width && target.y >= 0 && target.y < height && corelation[(int)target.y, (int)target.x] != 0)
                        {
                            counts[corelation[(int)target.y, (int)target.x] - 1]++;
                            if (counts[corelation[(int)target.y, (int)target.x] - 1] > counts[maxind]) maxind = (short)(corelation[(int)target.y, (int)target.x] - 1);
                        }
                    }
                }
                corelation[i, j] = (short)(maxind + 1);
                points[corelation[i, j] - 1].Add(new Vector(j, i));
            }
        }
        foreach (List<Vector> point in points)
        {
            regions.Add(new PointsRegion(point.ToArray()));
        }
    }

    private void GeneratePointGraphs()
    {
        continents = new Continent[regions.Count];
        for (int i = 0; i < regions.Count; i++)
        {
            continents[i] = new((rng.NextSingle() > 0.6f), regions[i], new Vector(rng.NextSingle() * 2 - 1f, rng.NextSingle() * 2 - 1f));
            PointsRegion reg = (PointsRegion)regions[i];
            foreach (Vector point in reg.Points)
            {
                correlation[(int)point.y, (int)point.x] = i;
            }
        }

        List<Vector> mountainSpread = new List<Vector>();
        List<Vector> shoreSpread = new List<Vector>();
        List<Vector> trenchSpread = new List<Vector>();
        List<Vector> uplandSpread = new List<Vector>();
        List<Vector> oceanSpread = new List<Vector>();

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                List<Continent> inVicnity = new() { continents[correlation[i, j]] };
                for (int k = -1; k < 2; k++)
                {
                    for (int l = -1; l < 2; l++)
                    {
                        Vector target = new Vector(j + l, i + k);
                        if (target.x >= 0 && target.x < width && target.y >= 0 && target.y < height && target.x != j && target.y != i)
                        {
                            var targetContinent = continents[correlation[(int)target.y, (int)target.x]];
                            if (!inVicnity.Contains(targetContinent))
                            {
                                inVicnity.Add(targetContinent);
                            }
                        }
                    }
                }
                if (inVicnity.Count > 1)
                {
                    Vector toAdd = new Vector(j, i);
                    float dirTo1 = MathF.Abs(Vector.Angle(inVicnity[1].Area.Center - inVicnity[0].Area.Center, inVicnity[0].Direction));
                    float dirTo0 = MathF.Abs(Vector.Angle(inVicnity[0].Area.Center - inVicnity[1].Area.Center, inVicnity[1].Direction));
                    if ((dirTo1 >= 3f * MathF.PI / 8f && dirTo0 >= 3f * MathF.PI / 8f) && (dirTo1 <= 5f * MathF.PI / 8f && dirTo0 <= 5f * MathF.PI / 8f))
                    {
                        //transf
                        if (inVicnity[0].IsLand ^ inVicnity[1].IsLand)
                        {
                            shoreSpread.Add(toAdd);
                        }
                        else if (inVicnity[0].IsLand)
                        {
                            uplandSpread.Add(toAdd);
                        }
                        else
                        {
                            oceanSpread.Add(toAdd);
                        }
                    }
                    else if ((dirTo1 > 5f * MathF.PI / 8f && dirTo0 > 5f * MathF.PI / 8f))
                    {
                        //div
                        trenchSpread.Add(toAdd);
                    }
                    else
                    {
                        //conv
                        mountainSpread.Add(toAdd);
                    }
                }
            }
        }


        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                var curPoint = new Vector(j, i);
                if (mountainSpread.Count > 0) mountainDist[i, j] = mountainSpread.Min(vec => (vec - curPoint).Length);
                if (trenchSpread.Count > 0) trenchDist[i, j] = trenchSpread.Min(vec => (vec - curPoint).Length);
                if (oceanSpread.Count > 0) oceanDist[i, j] = oceanSpread.Min(vec => (vec - curPoint).Length);
                if (uplandSpread.Count > 0) uplandDist[i, j] = uplandSpread.Min(vec => (vec - curPoint).Length);
                if (shoreSpread.Count > 0) shoreDist[i, j] = shoreSpread.Min(vec => (vec - curPoint).Length);
            }
        }
    }

    private void GeneratePointGraphsAlt()
    {
        float mountChance = 0.03f, trenchChance = 0.06f;
        float distToBoundry = 50f;
        continents = new Continent[regions.Count];
        for (int i = 0; i < regions.Count; i++)
        {
            continents[i] = new((rng.NextSingle() > 0.6f), regions[i], new Vector(rng.NextSingle() * 2 - 1f, rng.NextSingle() * 2 - 1f));
            PointsRegion reg = (PointsRegion)regions[i];
            foreach (Vector point in reg.Points)
            {
                correlation[(int)point.y, (int)point.x] = i;
            }
        }

        List<Vector> mountainSpread = new List<Vector>();
        List<Vector> shoreSpread = new List<Vector>();
        List<Vector> trenchSpread = new List<Vector>();
        List<Vector> uplandSpread = new List<Vector>();
        List<Vector> oceanSpread = new List<Vector>();

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                List<Continent> inVicnity = new() { continents[correlation[i, j]] };
                for (int k = -1; k < 2; k++)
                {
                    for (int l = -1; l < 2; l++)
                    {
                        Vector target = new Vector(j + l, i + k);
                        if (target.x >= 0 && target.x < width && target.y >= 0 && target.y < height && target.x != j && target.y != i)
                        {
                            var targetContinent = continents[correlation[(int)target.y, (int)target.x]];
                            if (!inVicnity.Contains(targetContinent))
                            {
                                inVicnity.Add(targetContinent);
                            }
                        }
                    }
                }
                if (inVicnity.Count > 1)
                {
                    Vector toAdd = new Vector(j, i);
                    float dirTo1 = MathF.Abs(Vector.Angle(inVicnity[1].Area.Center - inVicnity[0].Area.Center, inVicnity[0].Direction));
                    float dirTo0 = MathF.Abs(Vector.Angle(inVicnity[0].Area.Center - inVicnity[1].Area.Center, inVicnity[1].Direction));
                    if ((dirTo1 >= 3f * MathF.PI / 8f && dirTo0 >= 3f * MathF.PI / 8f) && (dirTo1 <= 5f * MathF.PI / 8f && dirTo0 <= 5f * MathF.PI / 8f))
                    {
                        //transf
                        if (inVicnity[0].IsLand ^ inVicnity[1].IsLand)
                        {
                            shoreSpread.Add(toAdd);
                        }
                        else if (inVicnity[0].IsLand)
                        {
                            uplandSpread.Add(toAdd);
                        }
                        else
                        {
                            oceanSpread.Add(toAdd);
                        }
                    }
                    else if ((dirTo1 > 5f * MathF.PI / 8f && dirTo0 > 5f * MathF.PI / 8f))
                    {
                        //div
                        if (rng.NextSingle() < trenchChance) trenchSpread.Add(toAdd + new Vector(rng.NextSingle(), rng.NextSingle()) * distToBoundry);
                    }
                    else
                    {
                        //conv
                        if (rng.NextSingle() < mountChance) mountainSpread.Add(toAdd + new Vector(rng.NextSingle(), rng.NextSingle()) * distToBoundry);
                    }
                }
            }
        }


        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                var curPoint = new Vector(j, i);
                if (mountainSpread.Count > 0) mountainDist[i, j] = mountainSpread.Min(vec => (vec - curPoint).Length);
                if (trenchSpread.Count > 0) trenchDist[i, j] = trenchSpread.Min(vec => (vec - curPoint).Length);
                if (oceanSpread.Count > 0) oceanDist[i, j] = oceanSpread.Min(vec => (vec - curPoint).Length);
                if (uplandSpread.Count > 0) uplandDist[i, j] = uplandSpread.Min(vec => (vec - curPoint).Length);
                if (shoreSpread.Count > 0) shoreDist[i, j] = shoreSpread.Min(vec => (vec - curPoint).Length);
                Console.Write((float)(i * width + j) / (float)(height * width) + "\r");
            }
        }
    }

    private void GeneratePointGraphsAlt2()
    {
        bool[,] visited = new bool[height, width];
        int dataTest = 0;
        float maxElevation = 1.0f;

        continents = new Continent[regions.Count];
        for (int i = 0; i < regions.Count; i++)
        {
            continents[i] = new(0.1f + rng.NextSingle() * 0.3f, regions[i], new Vector(rng.NextSingle() * 2 - 1f, rng.NextSingle() * 2 - 1f), minDensity, densityRange);
            PointsRegion reg = (PointsRegion)regions[i];
            foreach (Vector point in reg.Points)
            {
                correlation[(int)point.y, (int)point.x] = i;
            }
        }

        Stack<Vector> spread = new Stack<Vector>();

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                List<Continent> inVicnity = new() { continents[correlation[i, j]] };
                visited[i, j] = false;
                heightMap[i, j] = inVicnity[0].Height;
                for (int k = -1; k < 2; k++)
                {
                    for (int l = -1; l < 2; l++)
                    {
                        Vector target = new Vector(j + l, i + k);
                        if (target.x >= 0 && target.x < width && target.y >= 0 && target.y < height && target.x != j && target.y != i)
                        {
                            var targetContinent = continents[correlation[(int)target.y, (int)target.x]];
                            if (!inVicnity.Contains(targetContinent))
                            {
                                inVicnity.Add(targetContinent);
                            }
                        }
                    }
                }
                if (inVicnity.Count > 1)
                {
                    Vector toAdd = new Vector(j, i);
                    Vector vecTo1 = inVicnity[1].Area.Center - inVicnity[0].Area.Center;
                    float angTo1 = Vector.Angle(vecTo1, inVicnity[0].Direction);
                    float angTo0 = Vector.Angle(vecTo1, inVicnity[1].Direction);

                    //float magn = (inVicnity[0].Direction - inVicnity[1].Direction).Length;
                    //
                    //float paralel0 = inVicnity[0].Direction.Length * MathF.Cos(angTo1);
                    //float paralel1 = inVicnity[1].Direction.Length * MathF.Cos(angTo0);
                    //float orthogonal0 = inVicnity[0].Direction.Length * MathF.Sin(angTo1);
                    //float orthogonal1 = inVicnity[1].Direction.Length * MathF.Sin(angTo0);

                    float perpTension = inVicnity[0].Direction.Length * MathF.Cos(angTo1);
                    perpTension -= inVicnity[1].Direction.Length * MathF.Cos(angTo0);
                    perpTension = MathF.Abs(perpTension);

                    float minElevation = MathF.Max(inVicnity[0].Height, inVicnity[1].Height);

                    //float chance = 0.3f;
                    if (rng.NextSingle() < peakChance)
                    {
                        heightMap[i, j] = minElevation + (perpTension + rng.NextSingle() * 0.25f) * (minElevation - inVicnity[0].Height);
                        spread.Push(toAdd);
                    }
                    visited[i, j] = true;
                }
            }
        }

        Stack<Vector> nextStep = new Stack<Vector>();
        while (spread.Count > 0)
        {
            nextStep = spread;
            spread = new();
            while (nextStep.Count > 0)
            {
                Vector curr = nextStep.Pop();
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var target = curr + new Vector(j - 1, i - 1);
                        if (target.x >= 0 && target.x < width && target.y >= 0 && target.y < height && !target.Equals(curr))
                        {
                            float rang = continents[correlation[(int)target.y, (int)target.x]].Density;
                            float cutoff = continents[correlation[(int)target.y, (int)target.x]].Height;
                            float perturbation = (rng.NextSingle() * rang + 1f - rang);
                            float newVal = (heightMap[(int)curr.y, (int)curr.x] - cutoff) * perturbation + cutoff;
                            if (newVal > heightMap[(int)target.y, (int)target.x])
                            {
                                if (newVal > cutoff)
                                {
                                    heightMap[(int)target.y, (int)target.x] = newVal;
                                    if (perturbation <= 1f) spread.Push(target);
                                    else dataTest++;
                                }
                                else
                                {
                                    heightMap[(int)target.y, (int)target.x] = cutoff;
                                }
                            }
                        }
                    }
                }
            }
        }

        float max = float.MinValue, min = float.MaxValue;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (heightMap[i, j] > max) max = heightMap[i, j];
                if (heightMap[i, j] < min) min = heightMap[i, j];
            }
        }
        float range = max;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                heightMap[i, j] = heightMap[i, j] / range;
                if (heightMap[i, j] < 0f) heightMap[i, j] = 0f;
            }
        }

    }
    private void GenerateBorderGraphs()
    {
        continents = new Continent[regions.Count];
        for (int i = 0; i < regions.Count; i++)
        {
            continents[i] = new((rng.NextSingle() > 0.6f), regions[i], new Vector(rng.NextSingle() * 2 - 1f, rng.NextSingle() * 2 - 1f));
            PointsRegion reg = (PointsRegion)regions[i];
            foreach (Vector point in reg.Points)
            {
                correlation[(int)point.y, (int)point.x] = i;
            }
        }

        List<Vector> mountainSpread = new List<Vector>();
        List<Vector> shoreSpread = new List<Vector>();
        List<Vector> trenchSpread = new List<Vector>();
        List<Vector> uplandSpread = new List<Vector>();
        List<Vector> oceanSpread = new List<Vector>();
        List<Line> allBorders = new();

        foreach (Region region in regions)
        {
            BordersRegion borderReg = (BordersRegion)region;
            foreach (Line border in borderReg.Borders)
            {
                if (allBorders.Where((x) => { return x.Equals(border); }).Count() <= 0) allBorders.Add(border);
            }
        }

        foreach (Line border in allBorders)
        {
            List<Continent> inVicinity = new();
            foreach (BordersRegion reg in regions)
            {
                if (reg.Borders.Where((x) => { return x.Equals(border); }).Count() > 0)
                {
                    int x = (int)reg.Center.x;
                    int y = (int)reg.Center.y;
                    inVicinity.Add(continents[correlation[y, x]]);
                }
            }
            if (inVicinity.Count() > 1)
            {
                float dirTo1 = MathF.Abs(Vector.Angle(inVicinity[1].Area.Center - inVicinity[0].Area.Center, inVicinity[0].Direction));
                float dirTo0 = MathF.Abs(Vector.Angle(inVicinity[0].Area.Center - inVicinity[1].Area.Center, inVicinity[1].Direction));
                if ((dirTo1 >= 3f * MathF.PI / 8f && dirTo0 >= 3f * MathF.PI / 8f) && (dirTo1 <= 5f * MathF.PI / 8f && dirTo0 <= 5f * MathF.PI / 8f))
                {
                    //transf
                    if (inVicinity[0].IsLand ^ inVicinity[1].IsLand)
                    {
                        foreach (Vector pt in border.ListAllPoints()) shoreSpread.Add(pt);
                    }
                    else if (inVicinity[0].IsLand)
                    {
                        foreach (Vector pt in border.ListAllPoints()) uplandSpread.Add(pt);
                    }
                    else
                    {
                        foreach (Vector pt in border.ListAllPoints()) oceanSpread.Add(pt);
                    }
                }
                else if ((dirTo1 > 5f * MathF.PI / 8f && dirTo0 > 5f * MathF.PI / 8f))
                {
                    //div
                    foreach (Vector pt in border.ListAllPoints()) trenchSpread.Add(pt);
                }
                else
                {
                    //conv
                    foreach (Vector pt in border.ListAllPoints()) mountainSpread.Add(pt);
                }
            }
        }

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                var curPoint = new Vector(j, i);
                if (mountainSpread.Count > 0) mountainDist[i, j] = mountainSpread.Min(vec => (vec - curPoint).Length);
                if (trenchSpread.Count > 0) trenchDist[i, j] = trenchSpread.Min(vec => (vec - curPoint).Length);
                if (oceanSpread.Count > 0) oceanDist[i, j] = oceanSpread.Min(vec => (vec - curPoint).Length);
                if (uplandSpread.Count > 0) uplandDist[i, j] = uplandSpread.Min(vec => (vec - curPoint).Length);
                if (shoreSpread.Count > 0) shoreDist[i, j] = shoreSpread.Min(vec => (vec - curPoint).Length);
            }
        }
    }

    private void GenerateHeightmapProper()
    {
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                float elevation = continents[correlation[i, j]].Height;
                elevation = elevation + Homografic(mountainDist[i, j]) * (1f - elevation);
                elevation = elevation + Homografic(trenchDist[i, j]) * (-1f - elevation);
                elevation = elevation + SmoothStep(uplandDist[i, j]) * (0.2f - elevation);//diff interpolation
                elevation = elevation + SmoothStep(shoreDist[i, j]) * (0f - elevation);
                elevation = elevation + SmoothStep(oceanDist[i, j]) * (-0.6f - elevation);//diff interpolation
                heightMap[i, j] = elevation;
            }
        }
    }

    public Bitmap GenerateMountainGraphPicture()
    {
        Bitmap image = new(width, height);
        using (Graphics graph = Graphics.FromImage(image))
        {
            Rectangle ImageSize = new Rectangle(0, 0, width, height);
            graph.FillRectangle(Brushes.White, ImageSize);
        }
        float max = float.MinValue;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (mountainDist[i, j] > max) max = mountainDist[i, j];
            }
        }

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                float curPoint = mountainDist[i, j] / max;
                int red = (int)(curPoint * 255);
                int green = (int)(curPoint * 255);//(int)(curPoint <= 0f ? (curPoint + 1) * 255 : (-curPoint + 1) * 255);
                int blue = (int)(curPoint * 255);// (int)(curPoint >= 0f ? 0 : (-curPoint) * 255);
                Color pointCol = Color.FromArgb(red, green, blue);
                image.SetPixel(j, i, pointCol);
            }
        }

        return image;
    }

    private float SmoothStep(float x)
    {
        float func = x >= 30f ? 30f : x;
        func = func <= 0 ? 0f : func;
        func = 3f * MathF.Pow(func / 30f - 1f, 2) + 2f * MathF.Pow(func / 30f - 1f, 3);
        return func;
    }

    private float Homografic(float x)
    {
        float func = 1f / (0.01f * x + 1f);
        func = func >= 0f ? func : 1f;
        return func;
    }

    public HeightMap GenerateFromPerlin()
    {
        var gen = new Perlin();
        float x = rng.NextSingle() * scale, y = rng.NextSingle() * scale, z = rng.NextSingle() * scale;
        Stopwatch sw = new Stopwatch();
        sw.Start();
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                heightMap[i, j] = gen.OptimizedOctavePerlin(x + j, y + i, z, octaves, persistance, lacunarity, scale);
            }
        }
        sw.Stop();
        var ret = new HeightMap(heightMap);
        ret.time = sw.ElapsedMilliseconds;
        return ret;
    }

    public HeightMap GenerateFromNoise()
    {

        float[,] baseNoise = new float[height, width];
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                baseNoise[i, j] = rng.NextSingle() * 2f;
            }
        }

        for (int k = 0; k < areaNum; k++)
        {
            float stepHor = (float)width / MathF.Pow(2, k);
            float stepVer = (float)height / MathF.Pow(2, k);

            int stepNum = (int)MathF.Pow(2, k);
            float[,] keyVals = new float[stepNum, stepNum];
            for (int i = 0; i < stepNum; i++)
            {
                for (int j = 0; j < stepNum; j++)
                {
                    float posX = (float)j * stepHor;
                    float posY = (float)i * stepVer;

                    int posX0 = (int)posX;
                    int posY0 = (int)posY;
                    int posX1 = (posX0 + 1) % stepNum;
                    int posY1 = (posY0 + 1) % stepNum;
                    float dispX = posX - (float)posX0;
                    float dispY = posY - (float)posY0;

                    keyVals[i, j] = (1 - dispX) * (1 - dispY) * baseNoise[posY0, posX0] + (1 - dispX) * dispY * baseNoise[posY1, posX0] + dispX * (1 - dispY) * baseNoise[posY0, posX1] + dispX * dispY * baseNoise[posY1, posX1];
                }
            }
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int keyPosX = (int)((float)j / stepHor);
                    int keyPosY = (int)((float)i / stepVer);

                    float dispX = ((float)j / stepHor);
                    dispX = dispX - MathF.Truncate(dispX);
                    float dispY = ((float)i / stepVer);
                    dispY = dispY - MathF.Truncate(dispY);

                    heightMap[i, j] += ((1 - dispX) * (1 - dispY) * keyVals[keyPosY, keyPosX] + (1 - dispX) * dispY * keyVals[(keyPosY + 1) % stepNum, keyPosX] + dispX * (1 - dispY) * keyVals[keyPosY, (keyPosX + 1) % stepNum] + dispX * dispY * keyVals[(keyPosY + 1) % stepNum, (keyPosX + 1) % stepNum]) * (1f / stepNum);
                }
            }
        }
        NormalizeMap();

        return new HeightMap(heightMap);
    }
    public HeightMap GenerateFromWorleyNoise()
    {
        var sw = new Stopwatch();
        sw.Start();
        float cellW = (float)width / horCells;
        float cellH = (float)height / verCells;
        List<Vector> points = new List<Vector>();
        for (int i = 0; i < verCells; i++)
        {
            for (int j = 0; j < horCells; j++)
            {
                points.Add(new Vector(j * cellW + rng.NextSingle() * cellW, i * cellH + rng.NextSingle() * cellH));
            }
        }
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                Vector cur = new Vector(j, i);
                points.Sort((a, b) => { return ((int)(a - cur).Length) - (int)(b - cur).Length; });
                heightMap[i, j] = 1f * (points[0] - cur).Length + (-1f) * (points[1] - cur).Length;
                if (flip) heightMap[i, j] = -heightMap[i, j];
            }
        }
        sw.Stop();
        var ret = new HeightMap(heightMap);
        ret.time = sw.ElapsedMilliseconds;
        return ret;
    }
    public HeightMap GenerateFromMidpointdisp()
    {

        int exp = 1;
        int newHeight = width < height ? width : height;
        for (; exp < 12; exp++)
        {
            if (MathF.Pow(2, exp) > newHeight) break;
        }
        newHeight = (int)(Math.Pow(2, exp) + 1);
        float[,] midpointMap = new float[newHeight, newHeight];
        var sw = new Stopwatch();
        sw.Start();
        for (int i = 0; i < newHeight; i++)
        {
            for (int j = 0; j < newHeight; j++)
            {
                if ((i == 0 && j == 0) || (i == 0 && j == newHeight - 1) || (i == newHeight - 1 && j == 0) || (i == newHeight - 1 && j == newHeight - 1)) midpointMap[i, j] = rng.NextSingle();
                else midpointMap[i, j] = 0.0f;
            }
        }

        int step = newHeight - 1;
        for (int i = 0; i < exp; i++)
        {
            int smallStep = step / 2;
            for (int j = 0; j < (int)Math.Pow(2, i); j++)
            {
                for (int k = 0; k < (int)Math.Pow(2, i); k++)
                {
                    midpointMap[step * j, step * k + smallStep] = (midpointMap[step * j, step * k] + midpointMap[step * j, step * (k + 1)]) / 2f + ((rng.NextSingle() - 0.5f) * fluctuation / (i+1));
                    midpointMap[step * j + smallStep, step * k] = (midpointMap[step * j, step * k] + midpointMap[step * (j + 1), step * (k)]) / 2f + ((rng.NextSingle() - 0.5f) * fluctuation / (i + 1));
                    midpointMap[step * (j + 1), step * k + smallStep] = (midpointMap[step * (j + 1), step * k] + midpointMap[step * (j + 1), step * (k + 1)]) / 2f + ((rng.NextSingle() - 0.5f) * fluctuation / (i + 1));
                    midpointMap[step * j + smallStep, step * (k + 1)] = (midpointMap[step * j, step * (k + 1)] + midpointMap[step * (j + 1), step * (k + 1)]) / 2f + ((rng.NextSingle() - 0.5f) * fluctuation / (i + 1));
                    midpointMap[step * j + smallStep, step * k + smallStep] = (midpointMap[step * j, step * k + smallStep] + midpointMap[step * j + smallStep, step * k] + midpointMap[step * (j + 1), step * k + smallStep] + midpointMap[step * j + smallStep, step * (k + 1)]) / 4f + ((rng.NextSingle() - 0.5f) * fluctuation / (i + 1));
                }
            }
            step = smallStep;
        }

        width = newHeight;
        height = newHeight;
        float max = float.MinValue;
        float min = float.MaxValue;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (max < midpointMap[i, j]) max = midpointMap[i, j];
                if (min > midpointMap[i, j]) min = midpointMap[i, j];
            }
        }
        float range = max - min;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                midpointMap[i, j] = (midpointMap[i, j] - min) / range;
            }
        }
        heightMap = midpointMap;
        sw.Stop();
        var ret = new HeightMap(heightMap);
        ret.time = sw.ElapsedMilliseconds;
        return ret;
    }

    private void NormalizeMap()
    {
        float max = float.MinValue, min = float.MaxValue;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (heightMap[i, j] > max) max = heightMap[i, j];
                if (heightMap[i, j] < min) min = heightMap[i, j];
            }
        }
        float range = max - min;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                heightMap[i, j] = (heightMap[i, j] - min) / range;
            }
        }
    }
}
