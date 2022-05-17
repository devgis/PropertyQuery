using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MyGIS;

namespace Lesson_1
{
    public partial class Form1 : Form
    {
        List<GISPoint> points = new List<GISPoint>();
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            double x = Convert.ToDouble(textBox1.Text);
            double y = Convert.ToDouble(textBox2.Text);
            string attribute = textBox3.Text;
            GISVertex onevertex = new GISVertex(x, y);
            GISPoint onepoint = new GISPoint(onevertex, attribute);
            Graphics graphics = this.CreateGraphics();
            onepoint.DrawPoint(graphics);
            onepoint.DrawAttribute(graphics);
            points.Add(onepoint);
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            GISVertex onevertex = new GISVertex((double)e.X, (double)e.Y);
            double mindistance = Double.MaxValue;
            int findid = -1;
            for (int i = 0; i < points.Count; i++)
            {
                double distance = points[i].Distance(onevertex);
                if (distance < mindistance)
                {
                    mindistance = distance;
                    findid = i;
                }
            }
            if (mindistance > 5 || findid == -1)
            {
                MessageBox.Show("没有点实体或者鼠标点击位置不准确！");
            }
            else
                MessageBox.Show(points[findid].Attribute);
        }

    }
}
