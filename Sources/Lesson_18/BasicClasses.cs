﻿using System.Collections.Generic;
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
    public enum SHAPETYPE
    {
        point = 1,
        line = 3,
        polygon = 5
    };

    public enum THEMATICTYPE
    {
        UnifiedValue, UniqueValue, GradualColor
    };

    public enum MOUSECOMMAND
    {
        Unused, Select, ZoomIn, ZoomOut, Pan
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

    public class GISThematic
    {
        public Color OutsideColor;
        public int Size;
        public Color InsideColor;

        public GISThematic(Color outsideColor, int size, Color insideColor)
        {
            Update(outsideColor, size, insideColor);
        }

        public void Update(Color outsideColor, int size, Color insideColor)
        {
            OutsideColor = outsideColor;
            Size = size;
            InsideColor = insideColor;
        }

        public GISThematic(SHAPETYPE _shapetype)
        {
            if (_shapetype == SHAPETYPE.point)
                Update(GISTools.GetRandomColor(), GISConst.PointSize, GISTools.GetRandomColor());
            else if (_shapetype == SHAPETYPE.line)
                Update(GISTools.GetRandomColor(), GISConst.LineWidth, GISTools.GetRandomColor());
            else if (_shapetype == SHAPETYPE.polygon)
                Update(GISTools.GetRandomColor(), GISConst.PolygonBoundaryWidth, GISTools.GetRandomColor());
        }
    }

    public class GISDocument
    {
        public List<GISLayer> layers = new List<GISLayer>();
        public GISExtent Extent;
        public GISLayer AddLayer(string path)
        {
            GISLayer layer = null;
            string filetype = System.IO.Path.GetExtension(path).ToLower();
            if (filetype == "." + GISConst.SHPFILE) 
                layer = GISShapefile.ReadShapeFile(path);
            else if (filetype == "." + GISConst.MYFILE) 
                layer = GISMyFile.ReadFile(path);
            layer.Path = path;
            getUniqueName(layer);
            layers.Add(layer);
            UpdateExtent();
            return layer;
        }

        private void getUniqueName(GISLayer layer)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < layers.Count; i++) names.Add(layers[i].Name);
            names.Sort();
            for (int i = 0; i < names.Count; i++)
                if (layer.Name == names[i])
                    layer.Name = names[i] + "1";
        }

        public void RemoveLayer(string layername)
        {
            layers.Remove(getLayer(layername));
            UpdateExtent();
        }
        public void UpdateExtent()
        {
            Extent = null;
            if (layers.Count == 0) return;
            Extent = new GISExtent(layers[0].Extent);
            for (int i = 1; i < layers.Count; i++)
                Extent.Merge(layers[i].Extent);
        }

        public void draw(Graphics graphics, GISView view)
        {
            if (layers.Count == 0) return;
            GISExtent displayextent = view.getRealExtent();
            for (int i = 0; i < layers.Count; i++)
                if (layers[i].Visible)
                    layers[i].draw(graphics, view, displayextent);
        }

        public GISLayer getLayer(string layername)
        {
            for (int i = 0; i < layers.Count; i++)
                if (layers[i].Name == layername) return layers[i];
            return null;
        }

        public void SwitchLayer(string name1, string name2)
        {
            GISLayer layer1 = getLayer(name1);
            GISLayer layer2 = getLayer(name2);
            int index1 = layers.IndexOf(layer1);
            int index2 = layers.IndexOf(layer2);
            layers[index1] = layer2;
            layers[index2] = layer1;
        }

        public void Write(string filename)
        {
            FileStream fsr = new FileStream(filename, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fsr);
            for (int i = 0; i < layers.Count; i++)
            {
                GISTools.WriteString(layers[i].Path, bw);
                bw.Write(layers[i].DrawAttributeOrNot);
                bw.Write(layers[i].LabelIndex);
                bw.Write(layers[i].Selectable);
                bw.Write(layers[i].Visible);
            }
            bw.Close();
            fsr.Close();
        }

        public void Read(string filename)
        {
            layers.Clear();
            FileStream fsr = new FileStream(filename, FileMode.Open);
            BinaryReader br = new BinaryReader(fsr);
            while (br.PeekChar() != -1)
            {
                string path = GISTools.ReadString(br);
                GISLayer layer = AddLayer(path);
                layer.Path = path;
                layer.DrawAttributeOrNot = br.ReadBoolean();
                layer.LabelIndex = br.ReadInt32();
                layer.Selectable = br.ReadBoolean();
                layer.Visible = br.ReadBoolean();
            }
            br.Close();
            fsr.Close();
        }

        public bool IsEmpty()
        {
            return (layers.Count == 0);
        }

        public void ClearSelection()
        {
            for (int i = 0; i < layers.Count; i++)
                layers[i].ClearSelection();
        }

        public SelectResult Select(GISVertex v, GISView view)
        {
            SelectResult sr = SelectResult.TooFar;
            for (int i = 0; i < layers.Count; i++)
                if (layers[i].Selectable)
                    if (layers[i].Select(v, view) == SelectResult.OK)
                        sr = SelectResult.OK;
            return sr;
        }

        public SelectResult Select(GISExtent extent)
        {
            SelectResult sr = SelectResult.TooFar;
            for (int i = 0; i < layers.Count; i++)
                if (layers[i].Selectable)
                    if (layers[i].Select(extent) == SelectResult.OK)
                        sr = SelectResult.OK;
            return sr;
        }
    }

    public class GISConst
    {
        public static double MinScreenDistance=5;
        //点的颜色和半径
        //public static Color PointColor = Color.Pink;
        public static int PointSize = 3;
        //线的颜色与宽度
        //public static Color LineColor = Color.CadetBlue;
        public static int LineWidth = 2;
        //面的边框颜色、填充颜色及边框宽度
        //public static Color PolygonBoundaryColor = Color.White;
        //public static Color PolygonFillColor = Color.Gray;
        public static int PolygonBoundaryWidth = 2;
        //被选中的点的颜色
        public static Color SelectedPointColor = Color.Red;
        //被选中的线的颜色
        public static Color SelectedLineColor = Color.Blue;
        //被选中的面的填充颜色
        public static Color SelecedPolygonFillColor = Color.Yellow;
        //绘制选择或缩放范围框时的填充颜色
        public static Color ZoomSelectBoxColor = Color.FromArgb(50, 0, 0, 0);
        //地图放大系数
        public static double ZoomInFactor = 0.8;
        //地图缩小系数
        public static double ZoomOutfactor = 0.8;
        //Shapefile文件扩展名
        public static string SHPFILE = "shp";
        //自定义文件扩展名
        public static string MYFILE = "gis";
        //地图文档扩展名
        public static string MYDOC = "mydoc";
    }

    public class GISSelect
    {
        public GISFeature SelectedFeature=null;
        public List<GISFeature> SelectedFeatures = new List<GISFeature>();
        public SelectResult Select(GISVertex vertex, List<GISFeature> features, SHAPETYPE shapetype, GISView view)
        {
            SelectedFeature = null;
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
            SelectedFeatures.Clear();
            for (int i = 0; i < features.Count; i++)
            {
                if (MinSelectExtent.InsertectOrNot(features[i].spatialpart.extent) == false) continue;
                GISPolygon polygon = (GISPolygon)(features[i].spatialpart);
                if (polygon.Include(vertex))
                    SelectedFeatures.Add(features[i]);
            }
            return (SelectedFeatures.Count > 0) ? SelectResult.OK : SelectResult.TooFar;
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

        public SelectResult Select(GISExtent extent, List<GISFeature> Features)
        {
            SelectedFeatures.Clear();
            for (int i = 0; i < Features.Count; i++)
            {
                if (extent.Include(Features[i].spatialpart.extent))
                    SelectedFeatures.Add(Features[i]);
            }
            return (SelectedFeatures.Count > 0) ? SelectResult.OK : SelectResult.TooFar;
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
        public static Random rand = new Random();

        public static List<double> FindLevels(List<double> values, int levelNumber)
        {
            if (values.Count == 0) return null;
            //确定每个级别的属性值数量
            int ValueNumber = values.Count / levelNumber;
            values.Sort();
            List<double> Levels = new List<double>();
            //寻找分割点
            for (int i = 0; i < values.Count; i+=ValueNumber)
            {
                Levels.Add(values[i]);
            }
            //如果分割点多于分组数量，就去掉最后一个分割点
            if (Levels.Count > levelNumber) Levels.RemoveAt(Levels.Count - 1);
            return Levels;
        }

        public static int WhichLevel(List<double> Levels, double value)
        {
            //先判断是否属于除最后一组之外的其它组
            for (int i = 0; i < Levels.Count-1; i ++)
                if (value>=Levels[i]&&value<Levels[i+1])
                    return i;
            //否则就是属于最后一组
            return Levels.Count-1;
        }

        public static List<Object> FindUniqueValues(List<Object> values)
        {
            if (values.Count == 0) return null;
            values.Sort();
            List<Object> UniqueValues = new List<object>();
            UniqueValues.Add(values[0]);
            for (int i = 1; i < values.Count; i++)
                if (values[i].Equals(values[i - 1])==false)
                {
                    UniqueValues.Add(values[i]);
                }
            return UniqueValues;
        }

        public static Color GetRandomColor()
        {
            return Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
        }
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

        public static Color GetGradualColor(int levelIndex, int levelNumber)
        {
            int ColorLevel = (int)(255 - (float)levelIndex / levelNumber * 255);
            return Color.FromArgb(ColorLevel, ColorLevel, ColorLevel);
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

        public bool Selectable = true;
        public bool Visible = true;
        public string Path = "";

        public THEMATICTYPE ThematicType;
        public Dictionary<Object, GISThematic> Thematics;
        public int ThematicFieldIndex;
        public List<int> LevelIndexes = new List<int>();

        public GISLayer(string _name, SHAPETYPE _shapetype, GISExtent _extent, List<GISField> _fields = null)
        {
            Name = _name;
            ShapeType = _shapetype;
            Extent = _extent;
            Fields = (_fields == null) ? new List<GISField>() : _fields;
            Thematics = new Dictionary<object, GISThematic>();
            MakeUnifiedValueMap();
        }

        public void MakeUnifiedValueMap()
        {
            ThematicType = THEMATICTYPE.UnifiedValue;
            Thematics.Clear();
            Thematics.Add(ThematicType, new GISThematic(ShapeType));
        }

        public void draw(Graphics graphics, GISView view, GISExtent extent = null)
        {
            extent = (extent == null) ? view.getRealExtent() : extent;
            if (ThematicType == THEMATICTYPE.UnifiedValue)
            {
                GISThematic Thematic = Thematics[ThematicType];
                for (int i = 0; i < Features.Count; i++)
                {
                    if (extent.InsertectOrNot(Features[i].spatialpart.extent))
                        Features[i].draw(graphics, view, DrawAttributeOrNot, LabelIndex, Thematic);
                }
            }
            else if (ThematicType == THEMATICTYPE.UniqueValue)
            {                
                for (int i = 0; i < Features.Count; i++)
                {
                    GISThematic Thematic = Thematics[Features[i].getAttribute(ThematicFieldIndex)];
                    if (extent.InsertectOrNot(Features[i].spatialpart.extent))
                        Features[i].draw(graphics, view, DrawAttributeOrNot, LabelIndex, Thematic);
                }
            }
            else if (ThematicType == THEMATICTYPE.GradualColor)
            {
                for (int i = 0; i < Features.Count; i++)
                {
                    GISThematic Thematic = Thematics[LevelIndexes[i]];
                    if (extent.InsertectOrNot(Features[i].spatialpart.extent))
                        Features[i].draw(graphics, view, DrawAttributeOrNot, LabelIndex, Thematic);
                }
            }
        }
        public bool MakeGradualColor(int FieldIndex,int levelNumber)
        {
            List<double> values=new List<double>();
            //尝试把属性值转成double类型的列表
            try
            {
                for (int i = 0; i < Features.Count; i++)
                {
                    values.Add(Convert.ToDouble(Features[i].getAttribute(ThematicFieldIndex).ToString()));
                }
            }
            //如果不成功，说明属性值为非数值型的
            catch
            {
                return false;
            }
            //修改专题地图样式
            ThematicType = THEMATICTYPE.GradualColor;
            //确定专题地图的属性字段
            ThematicFieldIndex = FieldIndex;
            //获取分级关键点
            List<double> levels = GISTools.FindLevels(values, levelNumber);
            //清空每个空间对象的分级序号
            LevelIndexes.Clear();  
            //计算每个属性值的分级序号
            for (int i = 0; i < Features.Count; i++)
            {
                int LevelIndex = GISTools.WhichLevel(levels,
                    Convert.ToDouble(Features[i].getAttribute(ThematicFieldIndex).ToString()));
                LevelIndexes.Add(LevelIndex);
            }
            //获取目前的一些显示设置，这些设置将保持不变
            Color OutsideColor = Color.Beige;
            int Size = 0;
            foreach (GISThematic Thematic in Thematics.Values)
            {
                OutsideColor = Thematic.OutsideColor;
                Size = Thematic.Size;
                break;
            }
            //构建Thematics，为每个级别确定一个绘图样式
            Thematics.Clear();
            for (int i = 0; i < levelNumber; i++)
                Thematics.Add(i, new GISThematic(OutsideColor, Size, 
                    GISTools.GetGradualColor(i, levelNumber)));
            return true;    
        }
        public void MakeUniqueValueMap(int FieldIndex)
        {
            //修改专题地图样式
            ThematicType = THEMATICTYPE.UniqueValue;
            //确定专题地图的属性字段
            ThematicFieldIndex = FieldIndex;
            //获取属性值
            List<object> values = new List<object>();
            for (int i = 0; i < Features.Count; i++)
            {
                values.Add(Features[i].getAttribute(ThematicFieldIndex));
            }
            //获取独立值
            List<object> UniqueValues = GISTools.FindUniqueValues(values);
            //获取目前的一些显示设置，这些设置将保持不变
            Color OutsideColor = Color.Beige;
            int Size = 0;
            foreach (GISThematic Thematic in Thematics.Values)
            {
                OutsideColor = Thematic.OutsideColor;
                Size = Thematic.Size;
                break;
            }
            //构建Thematics，其中用InsideColor来区别具有不同独立值的空间对象
            Thematics.Clear();
            foreach (Object o in UniqueValues)
            {
                GISThematic Thematic = new GISThematic(OutsideColor, Size,
                    GISTools.GetRandomColor());
                Thematics.Add(o, Thematic);
            }
        }

        public void AddFeature(GISFeature feature)
        {
            if (Features.Count == 0) feature.ID = 0;
            else feature.ID = Features[Features.Count - 1].ID+1;
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

        public List<GISFeature> Selection = new List<GISFeature>();
        public SelectResult Select(GISVertex vertex, GISView view)
        {
            GISSelect gs = new GISSelect();
            SelectResult sr = gs.Select(vertex, Features, ShapeType, view);
            if (sr == SelectResult.OK)
            {
                if (ShapeType == SHAPETYPE.polygon)
                {
                    for (int i = 0; i < gs.SelectedFeatures.Count; i++)
                        if (gs.SelectedFeatures[i].Selected == false)
                        {
                            gs.SelectedFeatures[i].Selected = true;
                            Selection.Add(gs.SelectedFeatures[i]);
                        }
                }
                else
                    if (gs.SelectedFeature.Selected == false)
                    {
                        gs.SelectedFeature.Selected = true;
                        Selection.Add(gs.SelectedFeature);
                    }
            }
            return sr;
        }
        public SelectResult Select(GISExtent extent)
        {
            GISSelect gs = new GISSelect();
            SelectResult sr = gs.Select(extent, Features);
            if (sr == SelectResult.OK)
            {
                for (int i = 0; i < gs.SelectedFeatures.Count; i++)
                        if (gs.SelectedFeatures[i].Selected == false)
                        {
                            gs.SelectedFeatures[i].Selected = true;
                            Selection.Add(gs.SelectedFeatures[i]);
                        }
            }
            return sr;
        }
        public void ClearSelection()
        {
            for (int i = 0; i < Selection.Count; i++)
                Selection[i].Selected = false;
            Selection.Clear();
        }

        public void AddSelectedFeatureByID(int id)
        {
            GISFeature feature = GetFeatureByID(id);
            feature.Selected = true;
            Selection.Add(feature);
        }

        public GISFeature GetFeatureByID(int id)
        {
            foreach (GISFeature feature in Features)
                if (feature.ID == id) return feature;
            return null;
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
            WinW = MapWindowSize.Width;
            WinH = MapWindowSize.Height;
            ScaleX = CurrentMapExtent.getWidth() / WinW;
            ScaleY = CurrentMapExtent.getHeight() / WinH;
            ScaleX = Math.Max(ScaleX, ScaleY);
            ScaleY = ScaleX;
            MapW=MapWindowSize.Width*ScaleX;
            MapH=MapWindowSize.Height*ScaleY;
            GISVertex center = CurrentMapExtent.getCenter();
            MapMinX = center.x - MapW / 2;
            MapMinY = center.y - MapH / 2;
        }

        public void UpdateExtent(GISExtent extent)
        {
            CurrentMapExtent.CopyFrom(extent);
            Update(CurrentMapExtent, MapWindowSize);
        }

        public void UpdateRectangle(Rectangle rect)
        {
            MapWindowSize = rect;
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

        public GISExtent getRealExtent()
        {
            return new GISExtent(MapMinX, MapMinX + MapW, MapMinY, MapMinY + MapH);
        }

        public GISExtent RectToExtent(int x1, int x2, int y1, int y2)
        {
            GISVertex v1 = ToMapVertex(new Point(x1, y1));
            GISVertex v2 = ToMapVertex(new Point(x2, y2));
            return new GISExtent(v1.x, v2.x, v1.y, v2.y);
        }
    }


    public class GISFeature
    {
        public GISSpatial spatialpart;
        public GISAttribute attributepart;
        public bool Selected = false;
        public int ID;

        public GISFeature(GISSpatial spatial, GISAttribute attribute)
        {
            spatialpart = spatial;
            attributepart = attribute;
        }

        public void draw(Graphics graphics, GISView view, bool DrawAttributeOrNot, int index, GISThematic Thematic)
        {
            spatialpart.draw(graphics, view, Selected, Thematic);
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

        public abstract void draw(Graphics graphics, GISView view, bool Selected, GISThematic Thematic);
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
        public GISExtent(GISExtent extent)
        {
            upright = new GISVertex(extent.upright);
            bottomleft = new GISVertex(extent.bottomleft);
        }

        public void Merge(GISExtent extent)
        {
            upright.x = Math.Max(upright.x, extent.upright.x);
            upright.y = Math.Max(upright.y, extent.upright.y);
            bottomleft.x = Math.Min(bottomleft.x, extent.bottomleft.x);
            bottomleft.y = Math.Min(bottomleft.y, extent.bottomleft.y);
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

        public GISVertex getCenter()
        {
            return new GISVertex((upright.x + bottomleft.x) / 2, (upright.y + bottomleft.y) / 2);
        }

        public bool Include(GISExtent extent)
        {
            return (getMaxX() >= extent.getMaxX() && getMinX() <= extent.getMinX() &&
                  getMaxY() >= extent.getMaxY() && getMinY() <= extent.getMinY());
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

        public GISVertex(GISVertex v)
        {
            CopyFrom(v);
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

        public bool IsSame(GISVertex vertex)
        {
            return x == vertex.x && y == vertex.y;
        }
    }



    public class GISPoint : GISSpatial
    {
        public GISPoint(GISVertex onevertex)
        {
            centroid = onevertex;
            extent = new GISExtent(onevertex, onevertex);
        }

        public override void draw(Graphics graphics, GISView view, bool Selected, GISThematic Thematic)
        {
            Point screenpoint = view.ToScreenPoint(centroid);
            graphics.FillEllipse(new SolidBrush(Selected ? GISConst.SelectedPointColor : Thematic.InsideColor),
                new Rectangle(screenpoint.X - Thematic.Size, screenpoint.Y - Thematic.Size,
                    Thematic.Size * 2, Thematic.Size * 2));
            graphics.DrawEllipse(new Pen(new SolidBrush(Thematic.OutsideColor)),
                new Rectangle(screenpoint.X - Thematic.Size, screenpoint.Y - Thematic.Size,
                    Thematic.Size * 2, Thematic.Size * 2));
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
        public override void draw(Graphics graphics, GISView view, bool Selected, GISThematic Thematic)
        {
            Point[] points = GISTools.GetScreenPoints(Vertexes, view);
            graphics.DrawLines(new Pen(Selected ? GISConst.SelectedLineColor : Thematic.InsideColor,
                Thematic.Size), points);
        }
        public GISVertex FromNode()
        {
            return Vertexes[0];
        }
        public GISVertex ToNode()
        {
            return Vertexes[Vertexes.Count - 1];
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
        public override void draw(Graphics graphics, GISView view, bool Selected, GISThematic Thematic)
        {
            Point[] points = GISTools.GetScreenPoints(Vertexes, view);
            graphics.FillPolygon(new SolidBrush(Selected ? GISConst.SelecedPolygonFillColor : Thematic.InsideColor), points);
            graphics.DrawPolygon(new Pen(Thematic.OutsideColor, Thematic.Size), points);
        }
        public bool Include(GISVertex vertex)
        {
            int count = 0;
            for (int i = 0; i < Vertexes.Count; i++)
            {
                //满足情况3，直接返回false
                if (Vertexes[i].IsSame(vertex)) return false;
                //由序号为i及next的两个节点构成一条线段，一般情况下next为i+1，而针对最后一条线段，i为Vertexes.Count-1，next为0
                int next = (i + 1) % Vertexes.Count;
                //确定线段的坐标极值
                double minX = Math.Min(Vertexes[i].x, Vertexes[next].x);
                double minY = Math.Min(Vertexes[i].y, Vertexes[next].y);
                double maxX = Math.Max(Vertexes[i].x, Vertexes[next].x);
                double maxY = Math.Max(Vertexes[i].y, Vertexes[next].y);
                //如果线段是平行于射线的。
                if (minY == maxY)
                {
                    //满足情况2，直接返回false
                    if (minY == vertex.y && vertex.x >= minX && vertex.x <= maxX) return false;
                    //满足情况1或者射线与线段平行无交点
                    else continue;
                }
                //点在线段坐标极值之外，不可能有交点
                if (vertex.x > maxX || vertex.y > maxY || vertex.y < minY) continue;
                //计算交点横坐标，纵坐标无需计算，就是vertex.y
                double X0 = Vertexes[i].x + (vertex.y - Vertexes[i].y) * (Vertexes[next].x - Vertexes[i].x) / (Vertexes[next].y - Vertexes[i].y);
                //交点在射线反方向，按无交点计算
                if (X0 < vertex.x) continue;
                //交点即为vertex，且在线段上，按不包括处理
                if (X0 == vertex.x) return false;
                //射线穿过线段下端点，不记数
                if (vertex.y == minY) continue;
                //其它情况下，交点数加一
                count++;
            }
            //根据交点数量确定面是否包括点
            return count % 2 != 0;
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