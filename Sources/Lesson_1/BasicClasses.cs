using System.Collections.Generic;
using System;
using System.Drawing;
namespace MyGIS
{
    class GISVertex
    {
        public double x;
        public double y;

        public GISVertex(double _x, double _y)
        {
            x = _x;
            y = _y;
        }

        public double Distance(GISVertex anothervertex)
        {
            return Math.Sqrt((x - anothervertex.x) * (x - anothervertex.x) + (y - anothervertex.y) * (y - anothervertex.y));
        }
    }

    class GISPoint
    {
        public GISVertex Location;
        public string Attribute;

        public GISPoint(GISVertex onevertex, string onestring)
        {
            Location = onevertex;
            Attribute = onestring;
        }

        public void DrawPoint(Graphics graphics)
        {
            graphics.FillEllipse(new SolidBrush(Color.Red), new Rectangle((int)(Location.x) - 3, (int)(Location.y) - 3, 6, 6));
        }

        public void DrawAttribute(Graphics graphics)
        {
            graphics.DrawString(Attribute, new Font("宋体", 20), new SolidBrush(Color.Green), new PointF((int)(Location.x), (int)(Location.y)));
        }

        public double Distance(GISVertex anothervertex)
        {
            return Location.Distance(anothervertex);
        }
    }

    class GISLine
    {
        List<GISVertex> AllVertexs;
    }

    class GISPolygon
    {
        List<GISVertex> AllVertexs;
    }

}