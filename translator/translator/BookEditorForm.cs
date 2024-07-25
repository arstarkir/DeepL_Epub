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
    }
}
