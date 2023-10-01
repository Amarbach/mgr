using System.Drawing;
using System.Diagnostics;
using System.Data.Common;
using static System.Net.Mime.MediaTypeNames;
using BitMiracle.LibTiff.Classic;
using System.IO;
using System.Reflection;
using System;

class Program
{
    public static float TTest(float[,] a, float[,] b)
    {
        float t = 0f;
        float aMean = 0f, bMean = 0f;
        float aNum = a.GetLength(0) * a.GetLength(1), bNum = b.GetLength(0) * b.GetLength(1);
        float sa = 0f, sb = 0f;
        for(int i =0; i < a.GetLength(0); i++)
        {
            for(int j = 0; j < a.GetLength(1); j++)
            {
                aMean += a[i, j];
            }
        }
        aMean /= aNum;

        for (int i = 0; i < b.GetLength(0); i++)
        {
            for (int j = 0; j < b.GetLength(1); j++)
            {
                bMean += b[i, j];
            }
        }
        bMean /= bNum;

        for (int i = 0; i < a.GetLength(0); i++)
        {
            for (int j = 0; j < a.GetLength(1); j++)
            {
                sa += MathF.Pow(aMean - a[i, j], 2);
            }
        }
        sa /= (aNum - 1);
        for (int i = 0; i < b.GetLength(0); i++)
        {
            for (int j = 0; j < b.GetLength(1); j++)
            {
                sb += MathF.Pow(bMean - b[i, j], 2);
            }
        }
        sb /= (bNum - 1);

        t = (aMean - bMean) / MathF.Sqrt((sa / aNum) + (sb / bNum));

        return t;
    }
    public static Bitmap whiteRec(int width, int height)
    {
        Bitmap image = new(width, height);
        using (Graphics graph = Graphics.FromImage(image))
        {
            Rectangle ImageSize = new Rectangle(0, 0, width, height);
            graph.FillRectangle(Brushes.White, ImageSize);
        }
        return image;
    }

    public static void ExperimentBase()
    {
        Console.WriteLine("- Start Base -");
        string path = "results\\base\\";
        string[] paths = new string[] { "TDM1_DEM__30_N44E105_DEM.tif", "TDM1_DEM__30_N30E006_DEM.tif", "TDM1_DEM__30_N27E071_DEM.tif" };

        for (int i = 0; i < paths.Length; i++)
        {
            HeightMap hMap = new(paths[i]);
            hMap.RecalculateFactors();
            hMap.SaveAve(path + i + 2 + " - ");
            hMap.SaveData(path + i + 3 + " - ");
            hMap.Normalize();
            hMap.RecalculateFactors();
            hMap.SaveAve(path + i + 1);
            hMap.SaveData(path + i + 4 + " - ");
            hMap.SaveGreyScale(path + "Pic " + i + 1 + " - ");
        }
        Console.WriteLine("- End Base -");
    }

    public static void ExperimentTectonicSpread()
    {
        Console.WriteLine("- Start Tectonic Spread -");
        string path = "results\\plates\\spread2\\";
        int width = 1201;
        int height = 1201;
        int[] noPoints = new int[] { 8 };
        float[] peakChance = new float[] { 0.1f };
        float[] densityRange = new float[] { 0.2f };
        float[] minDensity = new float[] { 0.05f };
        PartitionMethode[] methode = new PartitionMethode[] { PartitionMethode.VORONOI };
        float aveave = 0f,
              avemax = 0f,
              avemin = 0f,
              aveaveTRI = 0f,
              avemaxTRI = 0f,
              aveminTRI = 0f,
              avestdTRI = 0f,
              aveaveTPI = 0f,
              avestdTPI = 0f,
              avemaxTPI = 0f,
              aveminTPI = 0f,
              aveminRough = 0f,
              aveaveRough = 0f,
              avestdRough = 0f,
              avemaxRough = 0f,
              aveTime = 0f;
        var rng = new Random();
        for (int i = 0; i < 100; i++)
        {
            HeightMapGenerator map = new HeightMapGenerator();
            map.SetDimensions(width, height)
               .SetPartitionType(methode[0])
               .SetRegionNumber(noPoints[0])
               .SetBuildStrategy(BuildStrategy.SPREAD)
               .SetPeakChance(peakChance[0])
               .SetDensityRange(densityRange[0])
               .SetMinDensity(minDensity[0])
               .SetSeed(rng.Next());

            HeightMap hMap = map.Generate();
            hMap.Normalize();
            hMap.RecalculateFactors();
            if (i == 69) hMap.SaveGreyScale(path + "Pic");
            hMap.SaveData(path + i);

            aveave += hMap.ave;
            avemax += hMap.max;
            avemin += hMap.min;
            aveaveTRI += hMap.aveTRI;
            avemaxTRI += hMap.maxTRI;
            aveminTRI += hMap.minTRI;
            avestdTRI += hMap.stdTRI;
            aveaveTPI += hMap.aveTPI;
            avestdTPI += hMap.stdTPI;
            avemaxTPI += hMap.maxTPI;
            aveminTPI += hMap.minTPI;
            aveminRough += hMap.minRough;
            aveaveRough += hMap.aveRoughness;
            avestdRough += hMap.stdRough;
            avemaxRough += hMap.maxRough;
            aveTime += hMap.time;
            Console.WriteLine((i + 1) + "/" + 100);
        }
        aveave /= 100f;
        avemax /= 100f;
        avemin /= 100f;
        aveaveTRI /= 100f;
        avemaxTRI /= 100f;
        aveminTRI /= 100f;
        avestdTRI /= 100f;
        aveaveTPI /= 100f;
        avestdTPI /= 100f;
        avemaxTPI /= 100f;
        aveminTPI /= 100f;
        aveminRough /= 100f;
        aveaveRough /= 100f;
        avestdRough /= 100f;
        avemaxRough /= 100f;
        aveTime /= 100f;


        File.WriteAllText(path + "aveave.txt", "" + aveave + "\n"
                                               + "max " + avemax + "\n"
                                               + "min " + avemin + "\n"
                                               + "aveTRI " + aveaveTRI + "\n"
                                               + "maxTRI " + avemaxTRI + "\n"
                                               + "minTRI " + aveminTRI + "\n"
                                               + "stdTRI " + avestdTRI + "\n"
                                               + "aveTPI " + aveaveTPI + "\n"
                                               + "stdTPI " + avestdTPI + "\n"
                                               + "maxTPI " + avemaxTPI + "\n"
                                               + "minTPI " + aveminTPI + "\n"
                                               + "minRough " + aveminRough + "\n"
                                               + "aveRough " + aveaveRough + "\n"
                                               + "stdRough " + avestdRough + "\n"
                                               + "maxRough " + avemaxRough + "\n"
                                               + "time " + aveTime + "\n");
        Console.WriteLine("- End Tectonic Spread -");
    }

    public static void ExperimentPerlin()
    {
        Console.WriteLine("- Start Perlin -");
        string path = "results\\perlin\\";
        int resNum = 100;
        int width = 1201;
        int height = 1201;
        int[] octaves = new int[] { 9 };
        float[] persistance = new float[] { 0.5f };
        float[] lacunarity = new float[] { 2f };
        float[] scale = new float[] { 256f };

        float aveave = 0f,
              avemax = 0f,
              avemin = 0f,
              aveaveTRI = 0f,
              avemaxTRI = 0f,
              aveminTRI = 0f,
              avestdTRI = 0f,
              aveaveTPI = 0f,
              avestdTPI = 0f,
              avemaxTPI = 0f,
              aveminTPI = 0f,
              aveminRough = 0f,
              aveaveRough = 0f,
              avestdRough = 0f,
              avemaxRough = 0f,
              aveTime = 0f;

        for (int i = 0; i < resNum; i++)
        {
            HeightMapGenerator map = new HeightMapGenerator();
            map.SetDimensions(width, height)
               .SetOctaves(octaves[0])
               .SetPersistance(persistance[0])
               .SetLacunarity(lacunarity[0])
               .SetScale(scale[0])
               .SetSeed(new Random().Next());

            HeightMap hMap = map.GenerateFromPerlin();
            hMap.Normalize();
            hMap.RecalculateFactors();
            if (i == 69) hMap.SaveGreyScale(path + "Pic");
            hMap.SaveData(path + i);

            aveave += hMap.ave;
            avemax += hMap.max;
            avemin += hMap.min;
            aveaveTRI += hMap.aveTRI;
            avemaxTRI += hMap.maxTRI;
            aveminTRI += hMap.minTRI;
            avestdTRI += hMap.stdTRI;
            aveaveTPI += hMap.aveTPI;
            avestdTPI += hMap.stdTPI;
            avemaxTPI += hMap.maxTPI;
            aveminTPI += hMap.minTPI;
            aveminRough += hMap.minRough;
            aveaveRough += hMap.aveRoughness;
            avestdRough += hMap.stdRough;
            avemaxRough += hMap.maxRough;
            aveTime += hMap.time;
            Console.WriteLine((i + 1) + "/" + 100);
        }
        aveave /= 100f;
        avemax /= 100f;
        avemin /= 100f;
        aveaveTRI /= 100f;
        avemaxTRI /= 100f;
        aveminTRI /= 100f;
        avestdTRI /= 100f;
        aveaveTPI /= 100f;
        avestdTPI /= 100f;
        avemaxTPI /= 100f;
        aveminTPI /= 100f;
        aveminRough /= 100f;
        aveaveRough /= 100f;
        avestdRough /= 100f;
        avemaxRough /= 100f;
        aveTime /= 100f;


        File.WriteAllText(path + "aveave.txt", "" + aveave + "\n"
                                               + "max " + avemax + "\n"
                                               + "min " + avemin + "\n"
                                               + "aveTRI " + aveaveTRI + "\n"
                                               + "maxTRI " + avemaxTRI + "\n"
                                               + "minTRI " + aveminTRI + "\n"
                                               + "stdTRI " + avestdTRI + "\n"
                                               + "aveTPI " + aveaveTPI + "\n"
                                               + "stdTPI " + avestdTPI + "\n"
                                               + "maxTPI " + avemaxTPI + "\n"
                                               + "minTPI " + aveminTPI + "\n"
                                               + "minRough " + aveminRough + "\n"
                                               + "aveRough " + aveaveRough + "\n"
                                               + "stdRough " + avestdRough + "\n"
                                               + "maxRough " + avemaxRough + "\n"
                                               + "time " + aveTime + "\n");
        Console.WriteLine("- End Perlin -");
    }

    public static void ExperimentSqDiamond()
    {
        Console.WriteLine("- Start SqDia -");
        string path = "results\\midpoint\\";
        int width = 1201;
        int height = 1201;
        float[] fluctuation = new float[] { 0.1f };

        float aveave = 0f,
              avemax = 0f,
              avemin = 0f,
              aveaveTRI = 0f,
              avemaxTRI = 0f,
              aveminTRI = 0f,
              avestdTRI = 0f,
              aveaveTPI = 0f,
              avestdTPI = 0f,
              avemaxTPI = 0f,
              aveminTPI = 0f,
              aveminRough = 0f,
              aveaveRough = 0f,
              avestdRough = 0f,
              avemaxRough = 0f,
              aveTime = 0f;

        var rng = new Random();
        for (int i = 0; i < 100; i++)
        {
            HeightMapGenerator map = new HeightMapGenerator();

            map.SetDimensions(width, height)
               .SetFluctuation(fluctuation[0])
               .SetSeed(rng.Next());

            HeightMap hMap = map.GenerateFromMidpointdisp();
            hMap.Normalize();
            hMap.RecalculateFactors();
            if (i == 69) hMap.SaveGreyScale(path + "Pic");
            hMap.SaveData(path + i);

            aveave += hMap.ave;
            avemax += hMap.max;
            avemin += hMap.min;
            aveaveTRI += hMap.aveTRI;
            avemaxTRI += hMap.maxTRI;
            aveminTRI += hMap.minTRI;
            avestdTRI += hMap.stdTRI;
            aveaveTPI += hMap.aveTPI;
            avestdTPI += hMap.stdTPI;
            avemaxTPI += hMap.maxTPI;
            aveminTPI += hMap.minTPI;
            aveminRough += hMap.minRough;
            aveaveRough += hMap.aveRoughness;
            avestdRough += hMap.stdRough;
            avemaxRough += hMap.maxRough;
            aveTime += hMap.time;
            Console.WriteLine((i + 1) + "/" + 100);
        }
        aveave /= 100f;
        avemax /= 100f;
        avemin /= 100f;
        aveaveTRI /= 100f;
        avemaxTRI /= 100f;
        aveminTRI /= 100f;
        avestdTRI /= 100f;
        aveaveTPI /= 100f;
        avestdTPI /= 100f;
        avemaxTPI /= 100f;
        aveminTPI /= 100f;
        aveminRough /= 100f;
        aveaveRough /= 100f;
        avestdRough /= 100f;
        avemaxRough /= 100f;
        aveTime /= 100f;

        //File.Create(path + "aveave.txt");
        File.WriteAllText(path + "aveave.txt", "" + aveave + "\n"
                                               + "max " + avemax + "\n"
                                               + "min " + avemin + "\n"
                                               + "aveTRI " + aveaveTRI + "\n"
                                               + "maxTRI " + avemaxTRI + "\n"
                                               + "minTRI " + aveminTRI + "\n"
                                               + "stdTRI " + avestdTRI + "\n"
                                               + "aveTPI " + aveaveTPI + "\n"
                                               + "stdTPI " + avestdTPI + "\n"
                                               + "maxTPI " + avemaxTPI + "\n"
                                               + "minTPI " + aveminTPI + "\n"
                                               + "minRough " + aveminRough + "\n"
                                               + "aveRough " + aveaveRough + "\n"
                                               + "stdRough " + avestdRough + "\n"
                                               + "maxRough " + avemaxRough + "\n"
                                               + "time " + aveTime + "\n");
        Console.WriteLine("- End SqDia -");
    }

    public static void ExperimentWorley()
    {
        Console.WriteLine("- Start Worley -");
        string path = "results\\worley\\";
        int width = 1201;
        int height = 1201;
        int[] horCells = new int[] { 4 };
        int[] verCells = new int[] { 4 };
        float[] fluctuation = new float[] { 0.15f };
        int[] octaves = new int[] { 5 };
        float[] persistance = new float[] { 0.5f };
        float[] lacunarity = new float[] { 2f };
        float[] scale = new float[] { 256f };
        float aveave = 0f,
              avemax = 0f,
              avemin = 0f,
              aveaveTRI = 0f,
              avemaxTRI = 0f,
              aveminTRI = 0f,
              avestdTRI = 0f,
              aveaveTPI = 0f,
              avestdTPI = 0f,
              avemaxTPI = 0f,
              aveminTPI = 0f,
              aveminRough = 0f,
              aveaveRough = 0f,
              avestdRough = 0f,
              avemaxRough = 0f,
              aveTime = 0f;

        var rng = new Random();

        for (int i = 0; i < 100; i++)
        {
            HeightMapGenerator map = new HeightMapGenerator();

            map.SetDimensions(width, height)
               .SetHorizontalCells(horCells[0])
               .SetVerticalCells(verCells[0])
               .SetFlip(true)
               .SetFluctuation(fluctuation[0])
               .SetOctaves(octaves[0])
               .SetPersistance(persistance[0])
               .SetLacunarity(lacunarity[0])
               .SetScale(scale[0])
               .SetSeed(rng.Next());

            HeightMap hMap1 = map.GenerateFromWorleyNoise();
            hMap1.Normalize();
            HeightMap hMap2 = map.GenerateFromMidpointdisp();
            hMap2.Normalize();
            HeightMap hMap = hMap1 * 0.2f + hMap2 * 0.8f;
            hMap.Normalize();
            hMap.RecalculateFactors();
            if (i == 0)
            {
                hMap1.SaveGreyScale(path + "Pic1");
                hMap2.SaveGreyScale(path + "Pic2");
                hMap.SaveGreyScale(path + "Pic");
            }
            if (i < 30) hMap.SaveData(path + i);

            aveave += hMap.ave;
            avemax += hMap.max;
            avemin += hMap.min;
            aveaveTRI += hMap.aveTRI;
            avemaxTRI += hMap.maxTRI;
            aveminTRI += hMap.minTRI;
            avestdTRI += hMap.stdTRI;
            aveaveTPI += hMap.aveTPI;
            avestdTPI += hMap.stdTPI;
            avemaxTPI += hMap.maxTPI;
            aveminTPI += hMap.minTPI;
            aveminRough += hMap.minRough;
            aveaveRough += hMap.aveRoughness;
            avestdRough += hMap.stdRough;
            avemaxRough += hMap.maxRough;
            aveTime += hMap1.time + hMap2.time;
            Console.WriteLine((i + 1) + "/" + 100);
        }
        aveave /= 100f;
        avemax /= 100f;
        avemin /= 100f;
        aveaveTRI /= 100f;
        avemaxTRI /= 100f;
        aveminTRI /= 100f;
        avestdTRI /= 100f;
        aveaveTPI /= 100f;
        avestdTPI /= 100f;
        avemaxTPI /= 100f;
        aveminTPI /= 100f;
        aveminRough /= 100f;
        aveaveRough /= 100f;
        avestdRough /= 100f;
        avemaxRough /= 100f;
        aveTime /= 100f;

        //File.Create(path + "aveave.txt");
        File.WriteAllText(path + "aveave.txt", "" + aveave + "\n"
                                               + "max " + avemax + "\n"
                                               + "min " + avemin + "\n"
                                               + "aveTRI " + aveaveTRI + "\n"
                                               + "maxTRI " + avemaxTRI + "\n"
                                               + "minTRI " + aveminTRI + "\n"
                                               + "stdTRI " + avestdTRI + "\n"
                                               + "aveTPI " + aveaveTPI + "\n"
                                               + "stdTPI " + avestdTPI + "\n"
                                               + "maxTPI " + avemaxTPI + "\n"
                                               + "minTPI " + aveminTPI + "\n"
                                               + "minRough " + aveminRough + "\n"
                                               + "aveRough " + aveaveRough + "\n"
                                               + "stdRough " + avestdRough + "\n"
                                               + "maxRough " + avemaxRough + "\n"
                                               + "time " + aveTime + "\n");
        Console.WriteLine("- End Worley -");
    }

    static void Main(string[] args)
    {
        //ExperimentBase();
        //ExperimentPerlin();
        //ExperimentSqDiamond();
        //ExperimentTectonicSpread();
        //ExperimentWorley();
    }
}