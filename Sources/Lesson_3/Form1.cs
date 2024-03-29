﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MyGIS;

namespace Lesson_3
{
    public partial class Form1 : Form
    {
        List<GISFeature> features = new List<GISFeature>();
        GISView view = null;

        public Form1()
        {
            InitializeComponent();
            view = new GISView(new GISExtent(new GISVertex(0, 0), new GISVertex(100, 100)), ClientRectangle);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //获取空间信息
            double x = Convert.ToDouble(textBox1.Text);
            double y = Convert.ToDouble(textBox2.Text);
            GISVertex onevertex = new GISVertex(x, y);
            GISPoint onepoint = new GISPoint(onevertex);
            //获取属性信息
            string attribute = textBox3.Text;
            GISAttribute oneattribute = new GISAttribute();
            oneattribute.AddValue(attribute);
            //新建一个GISFeature，并添加到数组"features"中
            GISFeature onefeature = new GISFeature(onepoint, oneattribute);
            features.Add(onefeature);
            //把这个新的GISFeature画出来
            Graphics graphics = this.CreateGraphics();
            onefeature.draw(graphics, view, true, 0);
        }


        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            //计算点击位置的地图坐标位置
            GISVertex mouselocation = view.ToMapVertex(new Point(e.X, e.Y));
            double mindistance = Double.MaxValue;
            int id = -1;
            //寻找据鼠标点击位置最近的空间实体对象
            for (int i = 0; i < features.Count; i++)
            {
                double onedistance = features[i].spatialpart.centroid.Distance(mouselocation);
                if (onedistance < mindistance)
                {
                    id = i;
                    mindistance = onedistance;
                }
            }
            //判断是否存在空间对象
            if (id == -1)
            {
                MessageBox.Show("没有任何空间对象!");
                return;
            }
            //判断其屏幕距离是否在给定范围以内
            Point nearestpoint = view.ToScreenPoint(features[id].spatialpart.centroid);
            int screendistance = Math.Abs(nearestpoint.X - e.X) + Math.Abs(nearestpoint.Y - e.Y);
            if (screendistance > 5)
            {
                MessageBox.Show("请靠近空间对象点击!");
                return;
            }
            MessageBox.Show("该空间对象属性是 " + features[id].getAttribute(0));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //从文本框中获取新的地图范围
            double minx = Double.Parse(textBox4.Text);
            double miny = Double.Parse(textBox5.Text);
            double maxx = Double.Parse(textBox6.Text);
            double maxy = Double.Parse(textBox7.Text);
            //更新view
            view.Update(new GISExtent(minx, maxx, miny, maxy), ClientRectangle);
            Graphics graphics = CreateGraphics();
            //用黑色填充整个窗口
            graphics.FillRectangle(new SolidBrush(Color.Black),
                    ClientRectangle);
            //根据新的view在绘图窗口中画上数组中的每个空间对象
            for (int i = 0; i < features.Count; i++)
            {
                features[i].draw(graphics, view, true, 0);
            }
        }

    }
}
