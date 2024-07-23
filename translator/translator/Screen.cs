using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace translator
{
    internal class Screen : Form
    {
        int id;
        List<Control> toDraw;

        public Screen(int id, List<Control> toDraw)
        {
            this.id = id;
            this.toDraw = toDraw;
        }

        public int getId() { return id; }
        public List<Control> getToDraw() { return toDraw; }

        public void DrawScrean(Control.ControlCollection control)
        {

            foreach (var item in toDraw)
            {
                control.Add(item);
            }

        }
        
        public void ClearScrean(Control.ControlCollection control)
        {
            foreach (var item in toDraw)
            {
                control.Remove(item);
            }
           
        }
    }
}
