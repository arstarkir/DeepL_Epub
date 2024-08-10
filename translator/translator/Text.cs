using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace translator
{
    public partial class Text : Screen
    {
        public Text(int id, List<Control> toDraw, Form1 form1) : base(id, toDraw, form1)
        {
            InitializeComponent();

            toDraw.Add(button1);
            toDraw.Add(button2);
            toDraw.Add(button3);
            toDraw.Add(checkedListBox1);
            toDraw.Add(checkBox2);
        }
    }
}
