using System.Collections.Generic;
using System;
using System.Drawing;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;
using System.Data;
using System.Data.Odbc;
using System.Text;
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

    public enum ALLTYPES
    {
        System_Boolean,
        System_Byte,
        System_Char,
        System_Decimal,
        System_Double,
        System_Single,
        System_Int32,
        System_Int64,
        System_SByte,
        System_Int16,
        System_String,
        System_UInt32,
        System_UInt64,
        System_UInt16
    };

    public enum SelectResult
    {
        //正常选择状态：选择到一个结果
        OK,
        //错误选择状态：备选集是空的
        EmptySet,
        //错误选择状态：点击选择时距离空间对象太远
        TooFar,
        //错误选择状态：未知空间对象
        UnknownType
    };



    public class GISConst
    {
        public static double MinScreenDistance=5;
    }

    public class GISSelect
    {
        public GISFeature SelectedFeature=null;
        public SelectResult Select(GISVertex vertex, List<GISFeature> features, SHAPETYPE shapetype, GISView view)
        {
            if (features.Count == 0) return SelectResult.EmptySet;
            GISExtent MinSelectExtent = BuildExtent(vertex, view);
            switch (shapetype)
            {
                case SHAPETYPE.point:
                    return SelectPoint(vertex, features, view, MinSelectExtent);
                case SHAPETYPE.line:
                    return SelectLine(vertex, features, view, MinSelectExtent);
                case SHAPETYPE.polygon:
                    return SelectPolygon(vertex, features, view, MinSelectExtent);
            }
            return SelectResult.UnknownType;
        }

        public GISExtent BuildExtent(GISVertex vertex, GISView view)
        {
            Point p0 = view.ToScreenPoint(vertex);
            Point p1 = new Point(p0.X + (int)GISConst.MinScreenDistance, p0.Y + (int)GISConst.MinScreenDistance);
            Point p2 = new Point(p0.X - (int)GISConst.MinScreenDistance, p0.Y - (int)GISConst.MinScreenDistance);
            GISVertex gp1 = view.ToMapVertex(p1);
            GISVertex gp2 = view.ToMapVertex(p2);
            return new GISExtent(gp1.x, gp2.x, gp1.y, gp2.y);
        }

        public SelectResult SelectPolygon(GISVertex vertex, List<GISFeature> features, GISView view, GISExtent MinSelectExtent)
        {
            return SelectResult.TooFar;
        }

        public SelectResult SelectPoint(GISVertex vertex, List<GISFeature> features, GISView view, GISExtent MinSelectExtent)
        {
            Double distance = Double.MaxValue;
            int id = -1;
            for (int i = 0; i < features.Count; i++)
            {
                if (MinSelectExtent.InsertectOrNot(features[i].spatialpart.extent) == false) continue;
                GISPoint point = (GISPoint)(features[i].spatialpart);
                double dist = point.Distance(vertex);
                if (dist < distance)
                {
                    distance = dist;
                    id = i;
                }
            }
            if (id == -1)
            {
                SelectedFeature = null;
                return SelectResult.TooFar;
            }
            else
            {
                double screendistance = view.ToScreenDistance(vertex, features[id].spatialpart.centroid);
                if (screendistance <= GISConst.MinScreenDistance)
                {
                    SelectedFeature = features[id];
                    return SelectResult.OK;
                }
                else
                {
                    SelectedFeature = null;
                    return SelectResult.TooFar;
                }
            }
        }

        public SelectResult SelectLine(GISVertex vertex, List<GISFeature> features, GISView view, GISExtent MinSelectExtent)
        {
            Double distance = Double.MaxValue;
            int id = -1;
            for (int i = 0; i < features.Count; i++)
            {
                if (MinSelectExtent.InsertectOrNot(features[i].spatialpart.extent) == false) continue;
                GISLine line = (GISLine)(features[i].spatialpart);
                double dist = line.Distance(vertex);
                if (dist < distance)
                {
                    distance = dist;
                    id = i;
                }
            }
            if (id == -1)
            {
                SelectedFeature = null;
                return SelectResult.TooFar;
            }
            else
            {
                double screendistance = view.ToScreenDistance(distance);
                if (screendistance <= GISConst.MinScreenDistance)
                {
                    SelectedFeature = features[id];
                    return SelectResult.OK;
                }
                else
                {
                    SelectedFeature = null;
                    return SelectResult.TooFar;
                }
            }
        }
    }

    public class GISMyFile
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct MyFileHeader
        {
            public double MinX, MinY, MaxX, MaxY;
            public int FeatureCount, ShapeType, FieldCount;
        };

        static void WriteMultipleVertexes(List<GISVertex> vs, BinaryWriter bw)
        {
            bw.Write(vs.Count);
            for (int vc = 0; vc < vs.Count; vc++)
                vs[vc].WriteVertex(bw);
        }

        static List<GISVertex> ReadMultipleVertexes(BinaryReader br)
        {
            List<GISVertex> vs = new List<GISVertex>();
            int vcount = br.ReadInt32();
            for (int vc = 0; vc < vcount; vc++)
                vs.Add(new GISVertex(br));
            return vs;
        }

        static void WriteFeatures(GISLayer layer, BinaryWriter bw)
        {
            for (int featureindex = 0; featureindex < layer.FeatureCount(); featureindex++)
            {
                GISFeature feature = layer.GetFeature(featureindex);
                if (layer.ShapeType == SHAPETYPE.point)
                {
                    ((GISPoint)feature.spatialpart).centroid.WriteVertex(bw);
                }
                else if (layer.ShapeType == SHAPETYPE.line)
                {
                    GISLine line = (GISLine)(feature.spatialpart);
                    WriteMultipleVertexes(line.Vertexes, bw);
                }
                else if (layer.ShapeType == SHAPETYPE.polygon)
                {
                    GISPolygon polygon = (GISPolygon)(feature.spatialpart);
                    WriteMultipleVertexes(polygon.Vertexes, bw);
                }
                WriteAttributes(feature.attributepart, bw);
            }
        }

        static void ReadFeatures(GISLayer layer, BinaryReader br, int FeatureCount)
        {
            for (int featureindex = 0; featureindex < FeatureCount; featureindex++)
            {
                GISFeature feature = new GISFeature(null, null);
                if (layer.ShapeType == SHAPETYPE.point)
                    feature.spatialpart = new GISPoint(new GISVertex(br));
                else if (layer.ShapeType == SHAPETYPE.line)
                    feature.spatialpart = new GISLine(ReadMultipleVertexes(br));
                else if (layer.ShapeType == SHAPETYPE.polygon)
                    feature.spatialpart = new GISPolygon(ReadMultipleVertexes(br));
                feature.attributepart = ReadAttributes(layer.Fields, br);
                layer.AddFeature(feature);
            }
        }

        static void WriteAttributes(GISAttribute attribute, BinaryWriter bw)
        {
            for (int i = 0; i < attribute.ValueCount(); i++)
            {
                Type type = attribute.GetValue(i).GetType();
                if (type.ToString() == "System.Boolean")
                    bw.Write((bool)attribute.GetValue(i));
                else if (type.ToString() == "System.Byte")
                    bw.Write((byte)attribute.GetValue(i));
                else if (type.ToString() == "System.Char")
                    bw.Write((char)attribute.GetValue(i));
                else if (type.ToString() == "System.Decimal")
                    bw.Write((decimal)attribute.GetValue(i));
                else if (type.ToString() == "System.Double")
                    bw.Write((double)attribute.GetValue(i));
                else if (type.ToString() == "System.Single")
                    bw.Write((float)attribute.GetValue(i));
                else if (type.ToString() == "System.Int32")
                    bw.Write((int)attribute.GetValue(i));
                else if (type.ToString() == "System.Int64")
                    bw.Write((long)attribute.GetValue(i));
                else if (type.ToString() == "System.UInt16")
                    bw.Write((ushort)attribute.GetValue(i));
                else if (type.ToString() == "System.UInt32")
                    bw.Write((uint)attribute.GetValue(i));
                else if (type.ToString() == "System.UInt64")
                    bw.Write((ulong)attribute.GetValue(i));
                else if (type.ToString() == "System.SByte")
                    bw.Write((sbyte)attribute.GetValue(i));
                else if (type.ToString() == "System.Int16")
                    bw.Write((short)attribute.GetValue(i));
                else if (type.ToString() == "System.String")
                    GISTools.WriteString((string)attribute.GetValue(i), bw);
            }
        }

        static GISAttribute ReadAttributes(List<GISField> fs, BinaryReader br)
        {
            GISAttribute atribute = new GISAttribute();
            for (int i = 0; i < fs.Count; i++)
            {
                Type type = fs[i].datatype;
                if (type.ToString() == "System.Boolean")
                    atribute.AddValue(br.ReadBoolean());
                else if (type.ToString() == "System.Byte")
                    atribute.AddValue(br.ReadByte());
                else if (type.ToString() == "System.Char")
                    atribute.AddValue(br.ReadChar());
                else if (type.ToString() == "System.Decimal")
                    atribute.AddValue(br.ReadDecimal());
                else if (type.ToString() == "System.Double")
                    atribute.AddValue(br.ReadDouble());
                else if (type.ToString() == "System.Single")
                    atribute.AddValue(br.ReadSingle());
                else if (type.ToString() == "System.Int32")
                    atribute.AddValue(br.ReadInt32());
                else if (type.ToString() == "System.Int64")
                    atribute.AddValue(br.ReadInt64());
                else if (type.ToString() == "System.UInt16")
                    atribute.AddValue(br.ReadUInt16());
                else if (type.ToString() == "System.UInt32")
                    atribute.AddValue(br.ReadUInt32());
                else if (type.ToString() == "System.UInt64")
                    atribute.AddValue(br.ReadUInt64());
                else if (type.ToString() == "System.SByte")
                    atribute.AddValue(br.ReadSByte());
                else if (type.ToString() == "System.Int16")
                    atribute.AddValue(br.ReadInt16());
                else if (type.ToString() == "System.String")
                    atribute.AddValue(GISTools.ReadString(br));
            }
            return atribute;
        }

        static void WriteFileHeader(GISLayer layer, BinaryWriter bw)
        {
            MyFileHeader mfh = new MyFileHeader();
            mfh.MinX = layer.Extent.getMinX();
            mfh.MinY = layer.Extent.getMinY();
            mfh.MaxX = layer.Extent.getMaxX();
            mfh.MaxY = layer.Extent.getMaxY();
            mfh.FeatureCount = layer.FeatureCount();
            mfh.ShapeType = (int)(layer.ShapeType);
            mfh.FieldCount = layer.Fields.Count;
            bw.Write(GISTools.ToBytes(mfh));
        }

        static void WriteFields(List<GISField> fields, BinaryWriter bw)
        {
            for (int fieldindex = 0; fieldindex < fields.Count; fieldindex++)
            {
                GISField field = fields[fieldindex]; 
                bw.Write(GISTools.TypeToInt(field.datatype));
                GISTools.WriteString(field.name, bw);
            }
        }

        static List<GISField> ReadFields(BinaryReader br, int FieldCount)
        {
            List<GISField> fields = new List<GISField>();
            for (int fieldindex = 0; fieldindex < FieldCount; fieldindex++)
            {
                Type fieldtype = GISTools.IntToType(br.ReadInt32());
                string fieldname = GISTools.ReadString(br);
                fields.Add(new GISField(fieldtype, fieldname));
            }
            return fields;
        }

        public static void WriteFile(GISLayer layer, string filename)
        {
            FileStream fsr = new FileStream(filename, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fsr);
            WriteFileHeader(layer, bw);
            GISTools.WriteString(layer.Name, bw);
            WriteFields(layer.Fields, bw);
            WriteFeatures(layer, bw);
            bw.Close();
            fsr.Close();
        }

        public static GISLayer ReadFile(string filename)
        {
            FileStream fsr = new FileStream(filename, FileMode.Open);
            BinaryReader br = new BinaryReader(fsr);
            MyFileHeader mfh = (MyFileHeader)(GISTools.FromBytes(br, typeof(MyFileHeader)));
            SHAPETYPE ShapeType = (SHAPETYPE) Enum.Parse(typeof(SHAPETYPE),mfh.ShapeType.ToString());
            GISExtent Extent = new GISExtent(mfh.MinX, mfh.MaxX, mfh.MinY, mfh.MaxY);
            string layername = GISTools.ReadString(br);
            List<GISField> Fields = ReadFields(br, mfh.FieldCount);
            GISLayer layer = new GISLayer(layername, ShapeType, Extent, Fields);
            ReadFeatures(layer, br, mfh.FeatureCount);
            br.Close();
            fsr.Close();
            return layer;
        }
    }

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
        public static double PointToSegment(GISVertex A, GISVertex B, GISVertex C)
        {
            double dot1 = Dot3Product(A, B, C);
            if (dot1 > 0) return B.Distance(C);
            double dot2 = Dot3Product(B, A, C);
            if (dot2 > 0) return A.Distance(C);
            double dist = Cross3Product(A, B, C) / A.Distance(B);
            return Math.Abs(dist);
        }

        static double Dot3Product(GISVertex A, GISVertex B, GISVertex C)
        {
            GISVertex AB = new GISVertex(B.x - A.x, B.y - A.y);
            GISVertex BC = new GISVertex(C.x - B.x, C.y - B.y);
            return AB.x * BC.x + AB.y * BC.y;
        }

        static double Cross3Product(GISVertex A, GISVertex B, GISVertex C)
        {
            GISVertex AB = new GISVertex(B.x - A.x, B.y - A.y);
            GISVertex AC = new GISVertex(C.x - A.x, C.y - A.y);
            return VectorProduct(AB, AC);
        }

        public static Object FromBytes(BinaryReader br, Type type)
        {
            byte[] buff = br.ReadBytes(Marshal.SizeOf(type));
            GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);
            Object result = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), type);
            handle.Free();
            return result;
        }

        public static string ReadString(BinaryReader br)
        {
            int length = br.ReadInt32();
            byte[] sbytes = br.ReadBytes(length);
            return Encoding.Default.GetString(sbytes);
        }

        public static int TypeToInt(Type type)
        {
            ALLTYPES onetype = (ALLTYPES)Enum.Parse(typeof(ALLTYPES), type.ToString().Replace(".", "_"));
            return (int)onetype;
        }

        public static Type IntToType(int index)
        {
            string typestring = Enum.GetName(typeof(ALLTYPES), index);
            typestring = typestring.Replace("_", ".");
            return Type.GetType(typestring);
        }

        public static void WriteString(string s, BinaryWriter bw)
        {
            bw.Write(StringLength(s));
            byte[] sbytes = Encoding.Default.GetBytes(s);
            bw.Write(sbytes);
        }

        public static int StringLength(string s)
        {
            int ChineseCount = 0;
            //将字符串转换为以ASCII来编码的字节数组
            byte[] bs = new ASCIIEncoding().GetBytes(s);
            foreach (byte b in bs)
                //所有双字节中文都会被转换成单字节的0X3F
                if (b == 0X3F) ChineseCount++;
            return ChineseCount + bs.Length;
        }

        public static byte[] ToBytes(object c)
        {
            byte[] bytes = new byte[Marshal.SizeOf(c.GetType())];
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            Marshal.StructureToPtr(c, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return bytes;
        }

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
        public bool DrawAttributeOrNot = false;
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
        public List<GISFeature> GetAllFeatures()
        {
            return Features;
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
            return (RecordHeader)GISTools.FromBytes(br, typeof(RecordHeader));
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
            return (ShapeFileHeader)GISTools.FromBytes(br, typeof(ShapeFileHeader));
        }

        public static object ReadStructure(Object o, BinaryReader br)
        {
            byte[] buff = br.ReadBytes(Marshal.SizeOf(o.GetType()));
            GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);
            Object result = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), o.GetType());
            handle.Free();
            return result;
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

        public double ToScreenDistance(GISVertex v1, GISVertex v2)
        {
            Point p1 = ToScreenPoint(v1);
            Point p2 = ToScreenPoint(v2);
            return Math.Sqrt((double)((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y)));
        }

        public GISVertex ToMapVertex(Point point)
        {
            double MapX = ScaleX * point.X + MapMinX;
            double MapY = ScaleY * (WinH - point.Y) + MapMinY;
            return new GISVertex(MapX, MapY);
        }

        public double ToScreenDistance(double distance)
        {
            return ToScreenDistance(new GISVertex(0, 0), new GISVertex(0, distance));
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

        public bool InsertectOrNot(GISExtent extent)
        {
           return !(getMaxX() < extent.getMinX() || getMinX() > extent.getMaxX() ||
                getMaxY() < extent.getMinY() || getMinY() > extent.getMaxY());            
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

        public GISVertex(BinaryReader br)
        {
            x = br.ReadDouble();
            y = br.ReadDouble();
        }

        public void WriteVertex(BinaryWriter bw)
        {
            bw.Write(x);
            bw.Write(y);
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
        public List<GISVertex> Vertexes;
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

        public double Distance(GISVertex vertex)
        {
            double distance = Double.MaxValue;
            for (int i = 0; i < Vertexes.Count - 1; i++)
            {
                distance = Math.Min(GISTools.PointToSegment(Vertexes[i], Vertexes[i + 1], vertex), distance);
            }
            return distance;
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
        public List<GISVertex> Vertexes;
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

        public int ValueCount()
        {
            return values.Count;
        }
    }

}