using BitMiracle.LibTiff.Classic;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class HeightMap
{
    private int height, width;
    private int mapSeed;
    private float[,] heights;
    private float[,] TRI, TPI, roughness;
    public float aveTRI,
                  aveTPI,
                  maxTRI,
                  minTRI,
                  stdTRI,
                  stdTPI,
                  maxTPI,
                  minTPI,
                  aveRoughness,
                  stdRough,
                  maxRough,
                  minRough,
                  max,
                  min,
                  ave,
                  time;
    public float[,] Heights { get { return heights; } }
    public HeightMap(int height, int width)
    {
        this.height = height;
        this.width = width;
        heights = new float[height, width];
        TRI = new float[height - 2, width - 2];
        TPI = new float[height - 2, width - 2];
        roughness = new float[height - 2, width - 2];
        aveTPI = 0f;
        stdTPI = 0f;
        maxTPI = float.MinValue;
        minTPI = float.MaxValue;
        aveTRI = 0f;
        stdTRI = 0f;
        maxTRI = float.MinValue;
        minTRI = float.MaxValue;
        aveRoughness = 0f;
        stdRough = 0f;
        maxRough = float.MinValue;
        minRough = float.MaxValue;
        ave = 0f;
        max = float.MinValue;
        min = float.MaxValue;
    }

    public HeightMap(float[,] newData)
    {
        this.height = newData.GetLength(0);
        this.width = newData.GetLength(1);
        SetHeights(newData);
        TRI = new float[height - 2, width - 2];
        TPI = new float[height - 2, width - 2];
        roughness = new float[height - 2, width - 2];
        aveTPI = 0f;
        aveTRI = 0f;
        maxTRI = float.MinValue;
        minTRI = float.MaxValue;
        stdTPI = 0f;
        stdTPI = 0f;
        maxTPI = float.MinValue;
        minTPI = float.MaxValue;
        aveRoughness = 0f;
        stdRough = 0f;
        maxRough = float.MinValue;
        minRough = float.MaxValue;
        ave = 0f;
        max = float.MinValue;
        min = float.MaxValue;
    }

    public HeightMap(string path)
    {
        this.OpenTiff(path);
    }

    public void SetHeights(float[,] newData)
    {
        height = newData.GetLength(0);
        width = newData.GetLength(1);
        this.heights = new float[height, width];
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                this.heights[i, j] = newData[i, j];
            }
        }
    }

    public float this[int y, int x]
    {
        get => heights[y, x];
        set => heights[y, x] = value;
    }
    public static HeightMap operator *(HeightMap a, HeightMap b)
    {
        HeightMap ret = new(a.height, a.width);
        for (int i = 0; i < ret.height; i++)
        {
            for (int j = 0; j < ret.width; j++)
            {
                ret[i, j] = a[i, j] * b[i, j];
            }
        }
        return ret;
    }
    public static HeightMap operator +(HeightMap a, HeightMap b)
    {
        HeightMap ret = new(a.height, a.width);
        for (int i = 0; i < ret.height; i++)
        {
            for (int j = 0; j < ret.width; j++)
            {
                ret[i, j] = a[i, j] + b[i, j];
            }
        }
        return ret;
    }
    public static HeightMap operator *(HeightMap a, float b)
    {
        HeightMap ret = new(a.height, a.width);
        for (int i = 0; i < ret.height; i++)
        {
            for (int j = 0; j < ret.width; j++)
            {
                ret[i, j] = a[i, j] * b;
            }
        }
        return ret;
    }
    public void PassSeed(int value)
    {
        mapSeed = value;
    }
    public void RecalculateFactors()
    {
        aveTPI = 0f;
        aveTRI = 0f;
        maxTRI = float.MinValue;
        minTRI = float.MaxValue;
        stdTPI = 0f;
        stdTPI = 0f;
        maxTPI = float.MinValue;
        minTPI = float.MaxValue;
        aveRoughness = 0f;
        stdRough = 0f;
        maxRough = float.MinValue;
        minRough = float.MaxValue;
        ave = 0f;
        max = float.MinValue;
        min = float.MaxValue;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                ave += heights[i, j];
                if (heights[i, j] > max) max = heights[i, j];
                if (heights[i, j] < min) min = heights[i, j];
            }
        }
        ave = ave / ((float)(width * height));
        for (int i = 1; i < height - 1; i++)
        {
            for (int j = 1; j < width - 1; j++)
            {
                float aveDiff = 0f, aveNeighbor = 0f;
                float maxH = float.MinValue;
                for (int k = 0; k < 3; k++)
                {
                    for (int l = 0; l < 3; l++)
                    {
                        if (l != 1 || k != 1)
                        {
                            aveDiff += MathF.Pow(heights[i, j] - heights[i + k - 1, j + l - 1], 2);
                            aveNeighbor += heights[i + k - 1, j + l - 1];
                            float rough = MathF.Abs(heights[i, j] - heights[i + k - 1, j + l - 1]);
                            if (rough > maxH) maxH = rough;
                        }
                    }
                }
                aveDiff = MathF.Sqrt(aveDiff);
                aveNeighbor /= 8f;
                aveNeighbor = MathF.Abs(aveNeighbor - heights[i, j]);
                if (aveDiff > maxTRI) maxTRI = aveDiff;
                if (aveDiff < minTRI) minTRI = aveDiff;
                if (aveNeighbor > maxTPI) maxTPI = aveNeighbor;
                if (aveNeighbor < minTPI) minTPI = aveNeighbor;
                if (maxH > maxRough) maxRough = maxH;
                if (maxH < minRough) minRough = maxH;
                TRI[i - 1, j - 1] = aveDiff;
                TPI[i - 1, j - 1] = aveNeighbor;
                roughness[i - 1, j - 1] = maxH;
                aveTPI += aveNeighbor;
                aveTRI += aveDiff;
                aveRoughness += maxH;
            }
        }
        aveTPI /= (height - 2) * (width - 2);
        aveTRI /= (height - 2) * (width - 2);
        aveRoughness /= (height - 2) * (width - 2);
        for (int i = 0; i < height - 2; i++)
        {
            for (int j = 0; j < width - 2; j++)
            {
                stdTRI += MathF.Pow(TRI[i, j] - aveTRI, 2);
                stdTPI += MathF.Pow(TPI[i, j] - aveTPI, 2);
                stdRough += MathF.Pow(roughness[i, j] - aveRoughness, 2);
            }
        }
        stdTRI = MathF.Sqrt(stdTRI / (float)((height - 2) * (width - 2)));
        stdTPI = MathF.Sqrt(stdTPI / (float)((height - 2) * (width - 2)));
        stdRough = MathF.Sqrt(stdRough / (float)((height - 2) * (width - 2)));
    }

    public void SaveGreyScale(string path)
    {
        Bitmap image = new(width, height);
        using (Graphics graph = Graphics.FromImage(image))
        {
            Rectangle ImageSize = new Rectangle(0, 0, width, height);
            graph.FillRectangle(Brushes.White, ImageSize);
        }
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                image.SetPixel(j, i, Color.FromArgb((int)(255 * heights[i, j]), (int)(255 * heights[i, j]), (int)(255 * heights[i, j])));
            }
        }
        string name = "";
        image.Save(path + name + ".png");
    }

    public void OpenTiff(string path)
    {
        using (Tiff tif = Tiff.Open(path, "r"))
        {
            FieldValue[] value = tif.GetField(TiffTag.IMAGEWIDTH);
            width = value[0].ToInt();

            value = tif.GetField(TiffTag.IMAGELENGTH);
            height = value[0].ToInt();

            heights = new float[height, width];

            for (int i = 0; i < height; i++)
            {
                byte[] scanline = new byte[tif.ScanlineSize()];
                tif.ReadScanline(scanline, i);
                for (int j = 0; j < width; j++)
                {
                    byte[] val = new[] { scanline[4 * j + 0], scanline[4 * j + 1], scanline[4 * j + 2], scanline[4 * j + 3] };
                    heights[i, j] = BitConverter.ToSingle(val);
                }
            }
        }
        TRI = new float[height - 2, width - 2];
        TPI = new float[height - 2, width - 2];
        roughness = new float[height - 2, width - 2];
        aveTPI = 0f;
        aveTRI = 0f;
        aveRoughness = 0f;
        this.RecalculateFactors();
    }

    public void SaveData(string path)
    {
        string name = "TRI - ";
        using (StreamWriter writer = new StreamWriter(path + name + ".txt"))
        {
            for (int i = 0; i < height - 2; i++)
            {
                for (int j = 0; j < width - 2; j++)
                {
                    writer.WriteLine(TRI[i, j]);
                }
            }
        }
        name = "TPI - ";
        using (StreamWriter writer = new StreamWriter(path + name + ".txt"))
        {
            for (int i = 0; i < height - 2; i++)
            {
                for (int j = 0; j < width - 2; j++)
                {
                    writer.WriteLine(TPI[i, j]);
                }
            }
        }
        name = "Roughness - ";
        using (StreamWriter writer = new StreamWriter(path + name + ".txt"))
        {
            for (int i = 0; i < height - 2; i++)
            {
                for (int j = 0; j < width - 2; j++)
                {
                    writer.WriteLine(roughness[i, j]);
                }
            }
        }
    }
    public void AppendData(string path)
    {
        //if (!File.Exists(path + "AllTRI.txt"))
        //{
        //    File.Create(path + "AllTRI.txt");
        //}
        using (var writer = File.AppendText(path + "AllTRI.txt"))
        {
            for (int i = 0; i < height - 2; i++)
            {
                for (int j = 0; j < width - 2; j++)
                {
                    writer.WriteLine(TRI[i, j] + "");
                }
            }
        }
        //if (!File.Exists(path + "AllTPI.txt"))
        //{
        //    File.Create(path + "AllTPI.txt");
        //}
        using (var writer = File.AppendText(path + "AllTPI.txt"))
        {
            for (int i = 0; i < height - 2; i++)
            {
                for (int j = 0; j < width - 2; j++)
                {
                    writer.WriteLine(TPI[i, j] + "");
                }
            }
        }
        //if (!File.Exists(path + "AllRough.txt"))
        //{
        //    File.Create(path + "AllRough.txt");
        //}
        using (var writer = File.AppendText(path + "AllRough.txt"))
        {
            for (int i = 0; i < height - 2; i++)
            {
                for (int j = 0; j < width - 2; j++)
                {
                    writer.WriteLine(roughness[i, j] + "");
                }
            }
        }
    }

    public void SaveAve(string path)
    {
        using (StreamWriter writer = new StreamWriter(path + " Ave.txt"))
        {
            writer.WriteLine("ave TRI = " + aveTRI);
            writer.WriteLine("TRI std = " + stdTRI);
            writer.WriteLine("TRI max = " + maxTRI);
            writer.WriteLine("TRI min = " + minTRI);
            writer.WriteLine("ave TPI = " + aveTPI);
            writer.WriteLine("TPI std = " + stdTPI);
            writer.WriteLine("TPI max = " + maxTPI);
            writer.WriteLine("TPI min = " + minTPI);
            writer.WriteLine("ave roughness = " + aveTRI);
            writer.WriteLine("roughness std = " + stdRough);
            writer.WriteLine("roughness max = " + maxRough);
            writer.WriteLine("roughness min = " + minRough);
            writer.WriteLine("ave elevation = " + ave);
            writer.WriteLine("max elevation = " + max);
            writer.WriteLine("min elevation = " + min);
        }
    }
    public void SaveCutoffTRI(string path, float value)
    {
        Bitmap image = new(width, height);
        using (Graphics graph = Graphics.FromImage(image))
        {
            Rectangle ImageSize = new Rectangle(0, 0, width, height);
            graph.FillRectangle(Brushes.White, ImageSize);
        }
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int red = (int)(255 * heights[i, j]);
                int green = (int)(255 * heights[i, j]);
                int blue = (int)(255 * heights[i, j]);
                if (i > 1 && j > 1 && i < height - 1 && j < width - 1)
                {
                    if (TRI[i - 1, j - 1] > value)
                    {
                        red = 255;
                        green = 0;
                        blue = 0;
                    }
                }
                image.SetPixel(j, i, Color.FromArgb(red, green, blue));
            }
        }
        string name = "TRI cutoff at " + value + " - " + DateTime.Now.ToString("MM/dd/yyyy/") + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second;
        image.Save(path + name + ".png");
    }
    public void SaveCutoffTPI(string path, float value)
    {
        Bitmap image = new(width, height);
        using (Graphics graph = Graphics.FromImage(image))
        {
            Rectangle ImageSize = new Rectangle(0, 0, width, height);
            graph.FillRectangle(Brushes.White, ImageSize);
        }
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int red = (int)(255 * heights[i, j]);
                int green = (int)(255 * heights[i, j]);
                int blue = (int)(255 * heights[i, j]);
                if (i > 1 && j > 1 && i < height - 1 && j < width - 1)
                {
                    if (TPI[i - 1, j - 1] > value)
                    {
                        red = 255;
                        green = 0;
                        blue = 0;
                    }
                }
                image.SetPixel(j, i, Color.FromArgb(red, green, blue));
            }
        }
        string name = "TPI cutoff at " + value + " - " + DateTime.Now.ToString("MM/dd/yyyy/") + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second;
        image.Save(path + name + ".png");
    }
    public void SaveCutoffRoughness(string path, float value)
    {
        Bitmap image = new(width, height);
        using (Graphics graph = Graphics.FromImage(image))
        {
            Rectangle ImageSize = new Rectangle(0, 0, width, height);
            graph.FillRectangle(Brushes.White, ImageSize);
        }
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int red = (int)(255 * heights[i, j]);
                int green = (int)(255 * heights[i, j]);
                int blue = (int)(255 * heights[i, j]);
                if (i > 1 && j > 1 && i < height - 1 && j < width - 1)
                {
                    if (roughness[i - 1, j - 1] > value)
                    {
                        red = 255;
                        green = 0;
                        blue = 0;
                    }
                }
                image.SetPixel(j, i, Color.FromArgb(red, green, blue));
            }
        }
        string name = "roughness cutoff at " + value + " - " + DateTime.Now.ToString("MM/dd/yyyy/") + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second;
        image.Save(path + name + ".png");
    }
    public void SaveCutoffPic(string path, float value, int which)
    {
        switch (which)
        {
            case 0: SaveCutoffTRI(path, value); break;
            case 1: SaveCutoffTPI(path, value); break;
            case 2: SaveCutoffRoughness(path, value); break;
        }
    }
    public void SaveAverageCutoff(string path, int which)
    {
        float value = which == 0 ? aveTRI : which == 1 ? aveTPI : aveRoughness;
        SaveCutoffPic(path, value, which);
    }
    public void Normalize()
    {
        float max = float.MinValue, min = float.MaxValue;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (heights[i, j] > max) max = heights[i, j];
                if (heights[i, j] < min) min = heights[i, j];
            }
        }
        float range = max - min;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                heights[i, j] = (heights[i, j] - min) / range;
            }
        }
    }
    public static float Compare(HeightMap a, HeightMap b)
    {
        float t = MathF.Abs(a.aveTRI - b.aveTRI) + MathF.Abs(a.aveTRI - b.aveTRI) + MathF.Abs(a.aveTRI - b.aveTRI);
        return t;
    }
}
