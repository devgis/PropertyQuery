using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyGIS;

namespace Lesson_20
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //假定第一个图层就是一个线图层
            GISVectorLayer layer = (GISVectorLayer)gisPanel1.document.layers[0];
            //构建网络结构
            GISNetwork network = new GISNetwork(layer);
            //清空图层的选择集
            layer.ClearSelection();
            //获得指定两点间的最短路径
            List<GISFeature> fs = network.FindRoute(new GISVertex(-115, 33), new GISVertex(-88, 14));
            //令路径上的空间对象被选中
            foreach (GISFeature f in fs)
            {
                f.Selected = true;
                layer.Selection.Add(f);
            }
            //重绘地图
            gisPanel1.UpdateMap();            
        }
    }
}
