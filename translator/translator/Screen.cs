
namespace translator
{
    public partial class Screen : Form
    {
        int id;
        List<Control> toDraw;
        Form1 form1;

        public Screen(int id = 0, List<Control> toDraw = null, Form1 form1 = null)
        {
            this.id = id;
            this.toDraw = toDraw;
            this.form1 = form1;
        }

        public int GetId() { return id; }
        public List<Control> GetToDraw() { return toDraw; }
        public Form1 GetForm1() { return form1; }

        public void DrawScrean(Control.ControlCollection control)
        {
            foreach (var item in toDraw)
                control.Add(item);
        }
        
        public void ClearScrean(Control.ControlCollection control)
        {
            foreach (var item in toDraw)
                control.Remove(item);
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }
    }
}
