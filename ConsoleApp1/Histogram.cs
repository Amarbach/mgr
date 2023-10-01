using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class HistogramF
{
    List<float> data;
    float max, min, binSize;
    int numBins = -1;
    int[] bins;

    public HistogramF(List<float> newData)
    {
        this.data = newData;
    }

    public HistogramF()
    { }

    public void SetNumBins(int nBins)
    {
        numBins = nBins;
        binSize = ((dynamic)max - (dynamic)min) / (float)(numBins-1);
        bins = new int[numBins];
    }

    public void SetData2DArray(float[,] newData)
    {
        List<float> converted = new();
        for (int i = 0; i < newData.GetLength(0); i++)
        {
            for (int j = 0; j < newData.GetLength(1); j++)
            {
                converted.Add(newData[i, j]);
            }
        }
        data = converted;
        max = data.Max(val => val);
        min = data.Min(val => val);
    }
    public void SetData1DArray(float[] newData)
    {
        List<float> converted = new(newData);
        max = data.Max(val => val);
        min = data.Min(val => val);
    }
    public void SetDataList(List<float> newData)
    {
        this.data = newData;
        max = data.Max(val => val);
        min = data.Min(val => val);
    }
    public void BuildHistogram()
    {
        if (numBins == -1) SetNumBins(10);
        bins = new int[numBins];
        for (int i =0; i < numBins; i++)
        {
            bins[i] = 0;
        }
        foreach(float value in data)
        {
            for (int i = 0; i < numBins; i++)
            {
                if (value >= i * binSize + min && value < (i+1) * binSize + min)
                {
                    bins[i]++;
                    break;
                }
            }
        }
    }
    public void SavePicture(string path)
    {
        int width = 400, height = 400;
        int scaleW = 50, scaleH = 50;
        int binW = width / numBins;
        float binH = (float)height / (bins.Max(val => val) + 1);
        Bitmap image = new(width + scaleW, height + scaleH);
        using (Graphics graph = Graphics.FromImage(image))
        {
            Rectangle ImageSize = new Rectangle(0, 0, width+scaleW, height+scaleH);
            graph.FillRectangle(Brushes.White, ImageSize);
            for (int i = 0; i < numBins; i++)
            {
                Rectangle rect = new Rectangle(scaleW + i*binW, height - (int)(binH * bins[i]), binW, (int)(binH * bins[i]));
                graph.FillRectangle(Brushes.Blue, rect);
            }
            Pen p = new(Color.Black, 1);
            graph.DrawLine(p, scaleW, 0, scaleW, height);
            graph.DrawLine(p, scaleW - 10, 0, scaleW, 0);
            graph.DrawLine(p, scaleW - 10, height/2, scaleW, height/2);
            graph.DrawLine(p, scaleW, height, scaleW + width, height);
        }
        //Add scale

        image.Save(path + ".png");
    }
}

