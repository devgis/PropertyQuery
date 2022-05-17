using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MyGIS
{
    public partial class LayerDialog : Form
    {
        GISDocument Document;
        GISPanel MapWindow;

        public LayerDialog(GISDocument document, GISPanel mapwindow)
        {
            InitializeComponent();
            Document = document;
            MapWindow = mapwindow;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            GISLayer onelayer = Document.getLayer(listBox1.SelectedItem.ToString());
            //初始化与共享属性相关的各个控件
            label11.Text = onelayer.CSType.ToString() + "," + onelayer.ZoneNumber + "," + (onelayer.NorthOrSouth ? "North" : "South");
            label1.Text = onelayer.Path;
            textBox1.Text = onelayer.Name;
            checkBox2.Checked = onelayer.Visible;
            //根据图层类型更新控件的可用性
            button15.Visible = onelayer.LayerType == LAYERTYPE.VectorLayer;
            checkBox1.Visible = onelayer.LayerType == LAYERTYPE.VectorLayer;
            checkBox3.Visible = onelayer.LayerType == LAYERTYPE.VectorLayer;
            comboBox1.Visible = onelayer.LayerType == LAYERTYPE.VectorLayer;
            button2.Visible = onelayer.LayerType == LAYERTYPE.VectorLayer;
            button5.Visible = onelayer.LayerType == LAYERTYPE.VectorLayer;
            groupBox1.Visible = onelayer.LayerType == LAYERTYPE.VectorLayer;
            checkBox4.Visible = onelayer.LayerType == LAYERTYPE.VectorLayer;
            //栅格图层的操作到此结束，可以退出
            if (onelayer.LayerType == LAYERTYPE.RasterLayer) return;
            //以下为原有矢量图层的操作
            GISVectorLayer layer = (GISVectorLayer)onelayer;
            checkBox4.Checked = layer.rtree != null;
            checkBox1.Checked = layer.Selectable;
            checkBox3.Checked = layer.DrawAttributeOrNot;
            comboBox1.Items.Clear();
            comboBox3.Items.Clear();
            for (int i = 0; i < layer.Fields.Count; i++)
            {
                comboBox1.Items.Add(layer.Fields[i].name);
                comboBox3.Items.Add(layer.Fields[i].name);
            }
            comboBox1.SelectedIndex = (layer.Fields.Count > 0) ? layer.LabelIndex : -1;
            comboBox3.SelectedIndex = (layer.Fields.Count > 0) ? layer.ThematicFieldIndex : -1;

            if (layer.ThematicType == THEMATICTYPE.UnifiedValue)
            {
                comboBox2.SelectedIndex = 0;
                GISThematic Thematic = layer.Thematics[layer.ThematicType];
                button11.BackColor = Thematic.InsideColor;
                textBox2.Text = Thematic.Size.ToString();
                button12.BackColor = Thematic.OutsideColor;
            }
            else if (layer.ThematicType == THEMATICTYPE.UniqueValue)
            {
                comboBox2.SelectedIndex = 1;
            }
            else if (layer.ThematicType == THEMATICTYPE.GradualColor)
            {
                comboBox2.SelectedIndex = 2;
                textBox3.Text = layer.Thematics.Count.ToString();
            }
        }

        private void Form3_Shown(object sender, EventArgs e)
        {
            for (int i = 0; i < Document.layers.Count; i++)
                listBox1.Items.Insert(0, Document.layers[i].Name);
            if (Document.layers.Count > 0)
                listBox1.SelectedIndex = 0;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "GIS Files (*."+GISConst.SHPFILE+", *."+GISConst.MYFILE+", *."+GISConst.RASTER+")|*."+
                            GISConst.SHPFILE + ";*." + GISConst.MYFILE + ";*." + GISConst.RASTER;
            openFileDialog.RestoreDirectory = false;
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;
            GISLayer layer=Document.AddLayer(openFileDialog.FileName);
            listBox1.Items.Insert(0,layer.Name);
            listBox1.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;            
            Document.RemoveLayer(listBox1.SelectedItem.ToString());
            listBox1.Items.Remove(listBox1.SelectedItem);
            if (listBox1.Items.Count > 0) listBox1.SelectedIndex = 0;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //无选择
            if (listBox1.SelectedItem == null) return;
            //当前选择无法上移
            if (listBox1.SelectedIndex == 0) return;
            //当前图层名
            string selectedname = listBox1.SelectedItem.ToString();
            //需要调换的图层名
            string uppername = listBox1.Items[listBox1.SelectedIndex - 1].ToString();
            //在listBox1中完成调换
            listBox1.Items[listBox1.SelectedIndex - 1] = selectedname;
            listBox1.Items[listBox1.SelectedIndex] = uppername;
            //在Document中完成调换
            Document.SwitchLayer(selectedname, uppername);
            listBox1.SelectedIndex--;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            if (listBox1.Items.Count == 1) return;
            if (listBox1.SelectedIndex == listBox1.Items.Count-1) return;
            string selectedname = listBox1.SelectedItem.ToString();
            string lowername = listBox1.Items[listBox1.SelectedIndex + 1].ToString();
            listBox1.Items[listBox1.SelectedIndex + 1] = selectedname;
            listBox1.Items[listBox1.SelectedIndex] = lowername;
            Document.SwitchLayer(selectedname, lowername);
            listBox1.SelectedIndex++;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
                GISVectorLayer layer = (GISVectorLayer)Document.getLayer(listBox1.SelectedItem.ToString());   
            if (layer.Selection.Count>0)
            {
                DialogResult dialogResult = MessageBox.Show(
                    "当前有" + layer.Selection.Count+"个对象被选中，是否只导出被选中的空间对象", "导出图层", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    GISTools.SaveLayer(layer, true, true);
                }
                else if (dialogResult == DialogResult.No)
                {
                    GISTools.SaveLayer(layer, true, false);
                }
            }
            else
                GISTools.SaveLayer(layer, true, false);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            for (int i = 0; i < listBox1.Items.Count; i++)
                if (i != listBox1.SelectedIndex)
                    if (listBox1.Items[i].ToString() == textBox1.Text)
                    {
                        MessageBox.Show("不能与已有图层名重复！");
                        return;
                    }
            GISLayer layer = Document.getLayer(listBox1.SelectedItem.ToString());
            layer.Name = textBox1.Text;
            listBox1.Items[listBox1.SelectedIndex] = textBox1.Text;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "GIS Document (*." + GISConst.MYDOC + ")|*." + GISConst.MYDOC;
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = false;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Document.Write(saveFileDialog1.FileName);
                MessageBox.Show("Done!");
            }
        }

        private void Clicked(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            GISLayer onelayer = Document.getLayer(listBox1.SelectedItem.ToString());
            onelayer.Visible = checkBox2.Checked;
            //栅格图层的操作到此结束，可以退出
            if (onelayer.LayerType == LAYERTYPE.RasterLayer) return;
            GISVectorLayer layer = (GISVectorLayer)onelayer;
            layer.Selectable = checkBox1.Checked;
            layer.DrawAttributeOrNot = checkBox3.Checked;
            layer.LabelIndex = comboBox1.SelectedIndex;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            GISLayer layer = Document.getLayer(listBox1.SelectedItem.ToString());
            MapWindow.OpenAttributeWindow((GISVectorLayer)layer);
        }
        //应用按钮
        private void button4_Click(object sender, EventArgs e)
        {
            MapWindow.UpdateMap();
        }
        //关闭按钮
        private void button6_Click(object sender, EventArgs e)
        {
            MapWindow.UpdateMap();
            Close();
        }

        private void SettingColor_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            colorDialog.Color = ((Button)sender).BackColor;
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                ((Button)sender).BackColor = colorDialog.Color;
                Clicked(sender, e);
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            //唯一值地图
            if (comboBox2.SelectedIndex == 0)
            {
                comboBox3.Visible = false;
                textBox3.Visible = false;
                button11.Visible = true;
                textBox2.Visible = true;
                button12.Visible = true;
            }
            //独立值地图
            else if (comboBox2.SelectedIndex == 1)
            {
                comboBox3.Visible = true;
                textBox3.Visible = false;
                button11.Visible = false;
                textBox2.Visible = false;
                button12.Visible = false;
            }
            //分级设色地图
            else if (comboBox2.SelectedIndex == 2)
            {
                comboBox3.Visible = true;
                textBox3.Visible = true;
                button11.Visible = false;
                textBox2.Visible = false;
                button12.Visible = false;
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            GISVectorLayer layer = (GISVectorLayer)Document.getLayer(listBox1.SelectedItem.ToString());
            //唯一值地图
            if (comboBox2.SelectedIndex == 0)
            {
                layer.MakeUnifiedValueMap();
                GISThematic Thematic = layer.Thematics[layer.ThematicType];
                Thematic.InsideColor = button11.BackColor;
                Thematic.OutsideColor = button12.BackColor;
                Thematic.Size = (textBox2.Text == "") ?
                    Thematic.Size : Int32.Parse(textBox2.Text);
            }
            //独立值地图
            else if (comboBox2.SelectedIndex == 1)
            {
                layer.MakeUniqueValueMap(comboBox3.SelectedIndex);
            }
            //分级设色地图
            else if (comboBox2.SelectedIndex == 2)
            {
                if (layer.MakeGradualColor(comboBox3.SelectedIndex,Int32.Parse(textBox3.Text))==false)
                {
                    MessageBox.Show("基于该属性无法绘制分级设色地图！");
                    return;
                }
            }
            //更新地图绘制
            MapWindow.UpdateMap();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "GIS Document (*." + GISConst.MYDOC + ")|*." +
                            GISConst.MYDOC;
            openFileDialog.RestoreDirectory = false;
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;
            //读入文档
            MapWindow.document.Read(openFileDialog.FileName);
            //更新地图窗口
            MapWindow.UpdateMap();
        }

        private void LayerDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            MapWindow.UpdateMap();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            GISVectorLayer layer = (GISVectorLayer)Document.getLayer(listBox1.SelectedItem.ToString());
            GISTools.SaveLayer(layer, false, false);
            //更新文件路径
            label1.Text = layer.Path;
        }

        private void checkBox4_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            GISVectorLayer layer = (GISVectorLayer)Document.getLayer(listBox1.SelectedItem.ToString());
            if (checkBox4.Checked)
                layer.BuildRTree();
            else
                layer.rtree = null;
        }

        private void button16_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            GISLayer layer = Document.getLayer(listBox1.SelectedItem.ToString());
            if (layer.CSType == CSTYPE.wgs84)
            {
                if (layer.LayerType == LAYERTYPE.RasterLayer) CS_UTM.WGS2UTM((GISRasterLayer)layer);
                else CS_UTM.WGS2UTM((GISVectorLayer)layer);
            }
            else if (layer.CSType == CSTYPE.utm)
            {
                if (layer.LayerType == LAYERTYPE.RasterLayer) CS_UTM.UTM2WGS((GISRasterLayer)layer);
                else CS_UTM.UTM2WGS((GISVectorLayer)layer);
            }
            Document.UpdateExtent();
            label11.Text = layer.CSType.ToString() + "," + layer.ZoneNumber + "," + (layer.NorthOrSouth ? "North" : "South");
        }

    }
}
