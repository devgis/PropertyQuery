using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MyGIS;

namespace Lesson_15
{
    public partial class Form3 : Form
    {
        GISDocument Document;
        Form1 MapWindow;

        public Form3(GISDocument document, Form1 mapwindow)
        {
            InitializeComponent();
            Document = document;
            MapWindow = mapwindow;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            GISLayer layer = Document.getLayer(listBox1.SelectedItem.ToString());
            checkBox1.Checked = layer.Selectable;
            checkBox2.Checked = layer.Visible;
            checkBox3.Checked = layer.DrawAttributeOrNot;
            comboBox1.Items.Clear();
            for (int i = 0; i < layer.Fields.Count; i++)
                    comboBox1.Items.Add(layer.Fields[i].name);
                comboBox1.SelectedIndex = layer.LabelIndex;
            label1.Text = layer.Path;
            textBox1.Text = layer.Name;
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
            openFileDialog.Filter = "GIS Files (*."+GISConst.SHPFILE+", *."+GISConst.MYFILE+")|*."+
                            GISConst.SHPFILE+";*."+GISConst.MYFILE;
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
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "GIS file (*." + GISConst.MYFILE + ")|*." + GISConst.MYFILE;
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = false;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                GISLayer layer = Document.getLayer(listBox1.SelectedItem.ToString());
                GISMyFile.WriteFile(layer, saveFileDialog1.FileName);
                MessageBox.Show("Done!");
            }
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
            GISLayer layer=Document.getLayer(listBox1.SelectedItem.ToString());
            layer.Selectable = checkBox1.Checked;
            layer.Visible = checkBox2.Checked;
            layer.DrawAttributeOrNot = checkBox3.Checked;
            layer.LabelIndex = comboBox1.SelectedIndex;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            GISLayer layer = Document.getLayer(listBox1.SelectedItem.ToString());
            MapWindow.OpenAttributeWindow(layer);
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

    }
}
