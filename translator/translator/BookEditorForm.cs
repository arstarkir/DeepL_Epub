using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace translator
{
    public partial class BookEditorForm : Form
    {

        public BookEditorForm()
        {
            InitializeComponent();

            imageList1.Images.Add("Text", Image.FromFile("C:\\Git\\DeepL_Epub\\translator\\translator\\bin\\Debug\\net8.0-windows\\Text.jpg"));
            pictureBox1.Image = imageList1.Images[0];
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle rect = new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height);

            // Creating a circle clipping region using a GraphicsPath
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(rect);  // AddEllipse creates a circular clipping region
            g.SetClip(path);

            // Draw the image clipped to the circular region
            g.DrawImage(pictureBox1.Image, rect);
        }

    }
}
