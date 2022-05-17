using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyGIS
{
    public partial class NetworkForm : Form
    {
        GISNetwork network;
        GISVectorLayer StopsLayer;
        GISVectorLayer LineLayer;

        public NetworkForm()
        {
            InitializeComponent();
            //生成起止点图层
            List<GISField> fields = new List<GISField>();
            fields.Add(new GISField(typeof(Int32), "Index"));
            StopsLayer = new GISVectorLayer("stops"+DateTime.Now.Ticks, SHAPETYPE.point, null, fields);
            //令起止点图层自动标注序号
            StopsLayer.LabelIndex = 0;
            StopsLayer.DrawAttributeOrNot = true;
            //设置控件可用性
            checkBox1.Checked = false;
            checkBox1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
        }

        private void comboBox1_MouseClick(object sender, MouseEventArgs e)
        {
            comboBox1.Items.Clear();
            for(int i=0;i<gisPanel1.document.layers.Count;i++)
            {
                GISLayer layer = gisPanel1.document.layers[i];
                //如果非矢量图层，则继续
                if (layer.LayerType != LAYERTYPE.VectorLayer) continue;
                //如果非线图层，则继续
                if (((GISVectorLayer)layer).ShapeType != SHAPETYPE.line) continue;
                comboBox1.Items.Add(layer.Name);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //获得基础线图层
            LineLayer = (GISVectorLayer)gisPanel1.document.getLayer(comboBox1.SelectedItem.ToString());
            //获取不到线图层，退出
            if (LineLayer == null) return;
            //构造网络结构
            network = new GISNetwork(LineLayer);
            //初始化相关设置
            Init();
            MessageBox.Show("成功！");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Init();
        }

        private void CheckLayers()
        {
            if (gisPanel1.document.getLayer(LineLayer.Name) == null)
                gisPanel1.document.AddLayer(LineLayer);
            if (gisPanel1.document.getLayer(StopsLayer.Name) == null)
                gisPanel1.document.AddLayer(StopsLayer);

        }
        private void Init()
        {
            //检查图层是否存在于地图窗口
            CheckLayers();
            //清空起止点图层和listBox1
            StopsLayer.DeleteAllFeatures();
            listBox1.Items.Clear();
            //清空基础线图层选择集
            LineLayer.ClearSelection();
            //设置控件的可用性
            checkBox1.Checked = true;
            checkBox1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button5.Enabled = true;
            button6.Enabled = true;
            //更新地图
            gisPanel1.UpdateMap();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //不足两个点，不能计算最短路径
            if (StopsLayer.FeatureCount() < 2) return;
            //检查图层是否存在于地图窗口
            CheckLayers();
            //清空基础线图层选择集
            LineLayer.ClearSelection();
            //逐对计算最短路径
            for (int i=1; i<StopsLayer.FeatureCount();i++)
            {
                GISVertex vfrom = StopsLayer.GetFeature(i - 1).spatialpart.centroid;
                GISVertex vto = StopsLayer.GetFeature(i).spatialpart.centroid;
                List<GISFeature> fs = network.FindRoute(vfrom, vto);
                //令涉及的空间对象被选中
                LineLayer.Select(fs);
            }
            //重绘地图
            gisPanel1.UpdateMap();
        }

        private void gisPanel1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //如果添加起止点开关打开了
            if (checkBox1.Checked)
            {
                //检查所需图层是否已经加载
                CheckLayers();
                //获得点击处的地图坐标
                GISVertex v = gisPanel1.view.ToMapVertex(new Point(e.X, e.Y));
                //添加到listBox1中
                listBox1.Items.Add(v.x + "," + v.y);
                //添加到起止点图层中
                GISAttribute a = new GISAttribute();
                a.AddValue(listBox1.Items.Count);
                StopsLayer.AddFeature(new GISFeature(new GISPoint(v), a),true);
                //更新地图
                gisPanel1.UpdateMap();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //打开一个扩展名为GISConst.NETFILE的文件
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "GIS Files (*." + GISConst.NETFILE + ")|*." +
                            GISConst.NETFILE;
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;
            //恢复网络结构
            network=new GISNetwork(openFileDialog.FileName);
            //提取基础线图层
            LineLayer = network.LineLayer;
            //初始化相关设置
            Init();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "GIS file (*." + GISConst.NETFILE + ")|*." + GISConst.NETFILE;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                network.Write(saveFileDialog1.FileName);
                MessageBox.Show("成功！");
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "GIS Files (*." + GISConst.SHPFILE + ", *." + GISConst.MYFILE + ")|*." +
                            GISConst.SHPFILE + ";*." + GISConst.MYFILE;
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;
            GISLayer layer = GISTools.GetLayer(openFileDialog.FileName);
            //如果非矢量图层，则退出
            if (layer.LayerType != LAYERTYPE.VectorLayer) return;
            //如果非点图层，则退出
            if (((GISVectorLayer)layer).ShapeType != SHAPETYPE.point) return;
            //初始化相关设置
            Init();
            GISVectorLayer pointlayer = (GISVectorLayer)layer;
            for (int i = 0; i < pointlayer.FeatureCount(); i++)
            {
                //获得点击处的地图坐标
                GISVertex v = pointlayer.GetFeature(i).spatialpart.centroid;
                //添加到listBox1中
                listBox1.Items.Add(v.x + "," + v.y);
                //添加到起止点图层中
                GISAttribute a = new GISAttribute();
                a.AddValue(listBox1.Items.Count);
                StopsLayer.AddFeature(new GISFeature(new GISPoint(v), a), true);
            }
            //更新地图
            gisPanel1.UpdateMap();
            MessageBox.Show("成功！");
        }
    }
}
