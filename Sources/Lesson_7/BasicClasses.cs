using System.Collections.Generic;
using System;
using System.Drawing;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;
using System.Data;
using System.Data.Odbc;
using System.Data.OleDb;
namespace MyGIS
{
    public enum GISMapActions
    {
        zoomin, zoomout,
        moveup, movedown, moveleft, moveright
    };

    public enum SHAPETYPE
    {
        point = 1,
        line = 3,
        polygon = 5
    };

    public class GISField
    {
        public Type datatype;
        public string name;
        public GISField(Type _dt, string _name)
        {
            datatype = _dt;
            name = _name;
        }
    }

    public class GISTools
    {
        public static Point[] GetScreenPoints(List<GISVertex> _vertexes, GISView view)
        {
            Point[] points = new Point[_vertexes.Count];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = view.ToScreenPoint(_vertexes[i]);
            }
            return points;
        }

        public static GISVertex CalculateCentroid(List<GISVertex> _vertexes)
        {
            if (_vertexes.Count == 0) return null;
            double x = 0;
            double y = 0;
            for (int i = 0; i < _vertexes.Count; i++)
            {
                x += _vertexes[i].x;
                y += _vertexes[i].y;
            }
            return new GISVertex(x / _vertexes.Count, y / _vertexes.Count);
        }

        public static GISExtent CalculateExtent(List<GISVertex> _vertexes)
        {
            if (_vertexes.Count == 0) return null;
            double minx = Double.MaxValue;
            double miny = Double.MaxValue;
            double maxx = Double.MinValue;
            double maxy = Double.MinValue;
            for (int i = 0; i < _vertexes.Count; i++)
            {
                if (_vertexes[i].x < minx) minx = _vertexes[i].x;
                if (_vertexes[i].x > maxx) maxx = _vertexes[i].x;
                if (_vertexes[i].y < miny) miny = _vertexes[i].y;
                if (_vertexes[i].y > maxy) maxy = _vertexes[i].y;
            }
            return new GISExtent(minx, maxx, miny, maxy);
        }

        public static double CalculateLength(List<GISVertex> _vertexes)
        {
            double length = 0;
            for (int i = 0; i < _vertexes.Count - 1; i++)
            {
                length += _vertexes[i].Distance(_vertexes[i + 1]);
            }
            return length;
        }

        public static double CalculateArea(List<GISVertex> _vertexes)
        {
            double area = 0;
            for (int i = 0; i < _vertexes.Count - 1; i++)
            {
                area +=VectorProduct(_vertexes[i],_vertexes[i + 1]);                
            }
            area += VectorProduct(_vertexes[_vertexes.Count - 1], _vertexes[0]);                
            return area / 2;
        }

        public static double VectorProduct(GISVertex v1, GISVertex v2)
        {
            return v1.x * v2.y - v1.y * v2.x;
        }
    }
    public class GISLayer
    {
        public string Name;
        public GISExtent Extent;
        public bool DrawAttributeOrNot;
        public int LabelIndex;
        public SHAPETYPE ShapeType;

        List<GISFeature> Features = new List<GISFeature>();

        public List<GISField> Fields;
        public GISLayer(string _name, SHAPETYPE _shapetype, GISExtent _extent, List<GISField> _fields)
        {
            Name = _name;
            ShapeType = _shapetype;
            Extent = _extent;
            Fields = _fields;
        }
        public GISLayer(string _name, SHAPETYPE _shapetype, GISExtent _extent)
        {
            Name = _name;
            ShapeType = _shapetype;
            Extent = _extent;
            Fields = new List<GISField>();
        }
        public void draw(Graphics graphics, GISView view)
        {
            for (int i = 0; i < Features.Count; i++)
            {
                Features[i].draw(graphics, view, DrawAttributeOrNot, LabelIndex);
            }
        }
        public void AddFeature(GISFeature feature)
        {
            Features.Add(feature);
        }
        public int FeatureCount()
        {
            return Features.Count;
        }
        public GISFeature GetFeature(int i)
        {
            return Features[i];
        }
    }

    public class GISShapefile
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct RecordHeader
        {
            public int RecordNumber;
            public int RecordLength;
            public int ShapeType;
        };
        static RecordHeader ReadRecordHeader(BinaryReader br)
        {
            byte[] buff = br.ReadBytes(Marshal.SizeOf(typeof(RecordHeader)));
            GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);
            RecordHeader header = (RecordHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(RecordHeader));
            handle.Free();
            return header;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct ShapeFileHeader
        {
            public int Unused1, Unused2, Unused3, Unused4;
            public int Unused5, Unused6, Unused7, Unused8;
            public int ShapeType;
            public double Xmin;
            public double Ymin;
            public double Xmax;
            public double Ymax;
            public double Unused9, Unused10, Unused11, Unused12;
        };

        static ShapeFileHeader ReadFileHeader(BinaryReader br)
        {
            byte[] buff = br.ReadBytes(Marshal.SizeOf(typeof(ShapeFileHeader)));
            GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);
            ShapeFileHeader header = (ShapeFileHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(ShapeFileHeader));
            handle.Free();
            return header;
        }
        static int FromBigToLittle(int bigvalue)
        {
            byte[] bigbytes = new byte[4];
            GCHandle handle = GCHandle.Alloc(bigbytes, GCHandleType.Pinned);
            Marshal.StructureToPtr(bigvalue, handle.AddrOfPinnedObject(), false);
            handle.Free();
            byte b2 = bigbytes[2];
            byte b3 = bigbytes[3];
            bigbytes[3] = bigbytes[0];
            bigbytes[2] = bigbytes[1];
            bigbytes[1] = b2;
            bigbytes[0] = b3;
            return BitConverter.ToInt32(bigbytes, 0);
        }
        static DataTable ReadDBF(string dbffilename)
        {
            System.IO.FileInfo f = new FileInfo(dbffilename);
            DataSet ds = null;
            string constr = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + f.DirectoryName + ";Extended Properties=DBASE III";
            using (OleDbConnection con = new OleDbConnection(constr))
            {
                var sql = "select * from " + f.Name;
                OleDbCommand cmd = new OleDbCommand(sql, con);
                con.Open();
                ds = new DataSet(); ;
                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                da.Fill(ds);
            }
            return ds.Tables[0];
        }

        static List<GISField> ReadFields(DataTable table)
        {
            List<GISField> fields = new List<GISField>();
            foreach (DataColumn column in table.Columns)
            {
                fields.Add(new GISField(column.DataType, column.ColumnName));
            }
            return fields;
        }
        static GISAttribute ReadAttribute(DataTable table, int RowIndex)
        {
            GISAttribute attribute = new GISAttribute();
            DataRow row = table.Rows[RowIndex];
            for (int i = 0; i < table.Columns.Count; i++)
            {
                attribute.AddValue(row[i]);
            }
            return attribute;
        }

        public static GISLayer ReadShapeFile(string shpfilename)
        {
            FileStream fsr = new FileStream(shpfilename, FileMode.Open);
            BinaryReader br = new BinaryReader(fsr);
            ShapeFileHeader sfh = ReadFileHeader(br);
            SHAPETYPE ShapeType = (SHAPETYPE) Enum.Parse(typeof(SHAPETYPE), sfh.ShapeType.ToString());
            GISExtent extent = new GISExtent(sfh.Xmax, sfh.Xmin, sfh.Ymax, sfh.Ymin);
            string dbffilename = shpfilename.Replace(".shp", ".dbf");
            DataTable table = ReadDBF(dbffilename);
            GISLayer layer = new GISLayer(shpfilename, ShapeType, extent, ReadFields(table));
            int rowindex = 0;

            while (br.PeekChar() != -1)
            {
                RecordHeader rh = ReadRecordHeader(br);
                int RecordLength = FromBigToLittle(rh.RecordLength) * 2 - 4;
                byte[] RecordContent = br.ReadBytes(RecordLength);
                if (ShapeType == SHAPETYPE.point)
                {
                    GISPoint onepoint = ReadPoint(RecordContent);
                    GISFeature onefeature = new GISFeature(onepoint, ReadAttribute(table, rowindex));
                    layer.AddFeature(onefeature);
                }
                if (ShapeType == SHAPETYPE.line)
                {
                    List<GISLine> lines = ReadLines(RecordContent);
                    for (int i = 0; i < lines.Count; i++)
                    {
                        GISFeature onefeature = new GISFeature(lines[i], ReadAttribute(table, rowindex));
                        layer.AddFeature(onefeature);
                    }
                }
                if (ShapeType == SHAPETYPE.polygon)
                {
                    List<GISPolygon> polygons = ReadPolygons(RecordContent);
                    for (int i = 0; i < polygons.Count; i++)
                    {
                        GISFeature onefeature = new GISFeature(polygons[i], ReadAttribute(table, rowindex));
                        layer.AddFeature(onefeature);
                    }
                }
                rowindex++;
            }
            br.Close();
            fsr.Close();
            return layer;
        }
        static GISPoint ReadPoint(byte[] RecordContent)
        {
            double x = BitConverter.ToDouble(RecordContent, 0);
            double y = BitConverter.ToDouble(RecordContent, 8);
            return new GISPoint(new GISVertex(x, y));
        }
        static List<GISLine> ReadLines(byte[] RecordContent)
        {
            int N = BitConverter.ToInt32(RecordContent, 32);
            int M = BitConverter.ToInt32(RecordContent, 36);
            int[] parts = new int[N + 1];

            for (int i = 0; i < N; i++)
            {
                parts[i] = BitConverter.ToInt32(RecordContent, 40 + i * 4);
            }
            parts[N] = M;
            List<GISLine> lines = new List<GISLine>();
            for (int i = 0; i < N; i++)
            {
                List<GISVertex> vertexs = new List<GISVertex>();
                for (int j = parts[i]; j < parts[i + 1]; j++)
                {
                    double x = BitConverter.ToDouble(RecordContent, 40 + N * 4 + j * 16);
                    double y = BitConverter.ToDouble(RecordContent, 40 + N * 4 + j * 16 + 8);
                    vertexs.Add(new GISVertex(x, y));
                }
                lines.Add(new GISLine(vertexs));
            }
            return lines;
        }

        static List<GISPolygon> ReadPolygons(byte[] RecordContent)
        {
            int N = BitConverter.ToInt32(RecordContent, 32);
            int M = BitConverter.ToInt32(RecordContent, 36);
            int[] parts = new int[N + 1];
            for (int i = 0; i < N; i++)
            {
                parts[i] = BitConverter.ToInt32(RecordContent, 40 + i * 4);
            }
            parts[N] = M;
            List<GISPolygon> polygons = new List<GISPolygon>();
            for (int i = 0; i < N; i++)
            {
                List<GISVertex> vertexs = new List<GISVertex>();
                for (int j = parts[i]; j < parts[i + 1]; j++)
                {
                    double x = BitConverter.ToDouble(RecordContent, 40 + N * 4 + j * 16);
                    double y = BitConverter.ToDouble(RecordContent, 40 + N * 4 + j * 16 + 8);
                    vertexs.Add(new GISVertex(x, y));
                }
                polygons.Add(new GISPolygon(vertexs));
            }
            return polygons;
        }

    }

    public class GISView
    {
        GISExtent CurrentMapExtent;
        Rectangle MapWindowSize;
        double MapMinX, MapMinY;
        int WinW, WinH;
        double MapW, MapH;
        double ScaleX, ScaleY;

        public GISView(GISExtent _extent, Rectangle _rectangle)
        {
            Update(_extent, _rectangle);
        }

        public void Update(GISExtent _extent, Rectangle _rectangle)
        {
            CurrentMapExtent = _extent;
            MapWindowSize = _rectangle;
            MapMinX = CurrentMapExtent.getMinX();
            MapMinY = CurrentMapExtent.getMinY();
            WinW = MapWindowSize.Width;
            WinH = MapWindowSize.Height;
            MapW = CurrentMapExtent.getWidth();
            MapH = CurrentMapExtent.getHeight();
            ScaleX = MapW / WinW;
            ScaleY = MapH / WinH;
        }

        public void ChangeView(GISMapActions action)
        {
            CurrentMapExtent.ChangeExtent(action);
            Update(CurrentMapExtent, MapWindowSize);
        }

        public void UpdateExtent(GISExtent extent)
        {
            CurrentMapExtent.CopyFrom(extent);
            Update(CurrentMapExtent, MapWindowSize);
        }

        public Point ToScreenPoint(GISVertex onevertex)
        {
            double ScreenX = (onevertex.x - MapMinX) / ScaleX;
            double ScreenY = WinH - (onevertex.y - MapMinY) / ScaleY;
            return new Point((int)ScreenX, (int)ScreenY);
        }

        public GISVertex ToMapVertex(Point point)
        {
            double MapX = ScaleX * point.X + MapMinX;
            double MapY = ScaleY * (WinH - point.Y) + MapMinY;
            return new GISVertex(MapX, MapY);
        }
    }


    public class GISFeature
    {
        public GISSpatial spatialpart;
        public GISAttribute attributepart;

        public GISFeature(GISSpatial spatial, GISAttribute attribute)
        {
            spatialpart = spatial;
            attributepart = attribute;
        }

        public void draw(Graphics graphics, GISView view, bool DrawAttributeOrNot, int index)
        {
            spatialpart.draw(graphics, view);
            if (DrawAttributeOrNot)
                attributepart.draw(graphics, view, spatialpart.centroid, index);
        }

        public object getAttribute(int index)
        {
            return attributepart.GetValue(index);
        }

    }

    public abstract class GISSpatial
    {
        public GISVertex centroid;
        public GISExtent extent;

        public abstract void draw(Graphics graphics, GISView view);
    }

    public class GISExtent
    {
        public GISVertex upright;
        public GISVertex bottomleft;

        public GISExtent(GISVertex _bottomleft, GISVertex _upright)
        {
            upright = _upright;
            bottomleft = _bottomleft;
        }

        public GISExtent(double x1, double x2, double y1, double y2)
        {
            upright = new GISVertex(Math.Max(x1, x2), Math.Max(y1, y2));
            bottomleft = new GISVertex(Math.Min(x1, x2), Math.Min(y1, y2));
        }

        public void CopyFrom(GISExtent extent)
        {
            upright.CopyFrom(extent.upright);
            bottomleft.CopyFrom(extent.bottomleft);
        }

        double ZoomingFactor = 2;
        double MovingFactor = 0.25;

        public void ChangeExtent(GISMapActions action)
        {
            double newminx = bottomleft.x, newminy = bottomleft.y,
                newmaxx = upright.x, newmaxy = upright.y;
            switch (action)
            {
                case GISMapActions.zoomin:
                    newminx = ((getMinX() + getMaxX()) - getWidth() / ZoomingFactor) / 2;
                    newminy = ((getMinY() + getMaxY()) - getHeight() / ZoomingFactor) / 2;
                    newmaxx = ((getMinX() + getMaxX()) + getWidth() / ZoomingFactor) / 2;
                    newmaxy = ((getMinY() + getMaxY()) + getHeight() / ZoomingFactor) / 2;
                    break;
                case GISMapActions.zoomout:
                    newminx = ((getMinX() + getMaxX()) - getWidth() * ZoomingFactor) / 2;
                    newminy = ((getMinY() + getMaxY()) - getHeight() * ZoomingFactor) / 2;
                    newmaxx = ((getMinX() + getMaxX()) + getWidth() * ZoomingFactor) / 2;
                    newmaxy = ((getMinY() + getMaxY()) + getHeight() * ZoomingFactor) / 2;
                    break;
                case GISMapActions.moveup:
                    newminy = getMinY() - getHeight() * MovingFactor;
                    newmaxy = getMaxY() - getHeight() * MovingFactor;
                    break;
                case GISMapActions.movedown:
                    newminy = getMinY() + getHeight() * MovingFactor;
                    newmaxy = getMaxY() + getHeight() * MovingFactor;
                    break;
                case GISMapActions.moveleft:
                    newminx = getMinX() + getWidth() * MovingFactor;
                    newmaxx = getMaxX() + getWidth() * MovingFactor;
                    break;
                case GISMapActions.moveright:
                    newminx = getMinX() - getWidth() * MovingFactor;
                    newmaxx = getMaxX() - getWidth() * MovingFactor;
                    break;
            }
            upright.x = newmaxx;
            upright.y = newmaxy;
            bottomleft.x = newminx;
            bottomleft.y = newminy;
        }

        public double getMinX()
        {
            return bottomleft.x;
        }

        public double getMaxX()
        {
            return upright.x;
        }

        public double getMinY()
        {
            return bottomleft.y;
        }

        public double getMaxY()
        {
            return upright.y;
        }

        public double getWidth()
        {
            return upright.x - bottomleft.x;
        }

        public double getHeight()
        {
            return upright.y - bottomleft.y;
        }
    }


    public class GISVertex
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

        public void CopyFrom(GISVertex v)
        {
            x = v.x;
            y = v.y;
        }
    }



    public class GISPoint : GISSpatial
    {
        public GISPoint(GISVertex onevertex)
        {
            centroid = onevertex;
            extent = new GISExtent(onevertex, onevertex);
        }

        public override void draw(Graphics graphics, GISView view)
        {
            Point screenpoint = view.ToScreenPoint(centroid);
            graphics.FillEllipse(new SolidBrush(Color.Red),
                new Rectangle(screenpoint.X - 3, screenpoint.Y - 3, 6, 6));
        }

        public double Distance(GISVertex anothervertex)
        {
            return centroid.Distance(anothervertex);
        }
    }

    public class GISLine : GISSpatial
    {
        List<GISVertex> Vertexes;
        public double Length;
        public GISLine(List<GISVertex> _vertexes)
        {
            Vertexes = _vertexes;
            centroid = GISTools.CalculateCentroid(_vertexes);
            extent = GISTools.CalculateExtent(_vertexes);
            Length = GISTools.CalculateLength(_vertexes);
        }
        public override void draw(Graphics graphics, GISView view)
        {
            Point[] points = GISTools.GetScreenPoints(Vertexes, view);
            graphics.DrawLines(new Pen(Color.Red, 2), points);
        }
        public GISVertex FromNode()
        {
            return Vertexes[0];
        }
        public GISVertex ToNode()
        {
            return Vertexes[Vertexes.Count - 1];
        }

    }
    public class GISPolygon : GISSpatial
    {
        List<GISVertex> Vertexes;
        public double Area;
        public GISPolygon(List<GISVertex> _vertexes)
        {
            Vertexes = _vertexes;
            centroid = GISTools.CalculateCentroid(_vertexes);
            extent = GISTools.CalculateExtent(_vertexes);
            Area = GISTools.CalculateArea(_vertexes);
        }
        public override void draw(Graphics graphics, GISView view)
        {
            Point[] points = GISTools.GetScreenPoints(Vertexes, view);
            graphics.FillPolygon(new SolidBrush(Color.Yellow), points);
            graphics.DrawPolygon(new Pen(Color.White, 2), points);
        }
    }

    public class GISAttribute
    {
        ArrayList values = new ArrayList();

        public void AddValue(object o)
        {
            values.Add(o);
        }

        public object GetValue(int index)
        {
            return values[index];
        }


        public void draw(Graphics graphics, GISView view, GISVertex location, int index)
        {
            Point screenpoint = view.ToScreenPoint(location);
            graphics.DrawString(values[index].ToString(),
                new Font("宋体", 20),
                new SolidBrush(Color.Green),
                new PointF(screenpoint.X, screenpoint.Y));
        }
    }

}