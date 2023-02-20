using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MoveButton
{
    public partial class Form1 : Form
    {
        Random random = new Random();
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_MouseMove(object sender, MouseEventArgs e)
        {
            Size formSize = this.Size;
            Size buttonSize = this.button1.Size;
            int newX = random.Next(formSize.Width - buttonSize.Width);
            int newY = random.Next(formSize.Height - buttonSize.Height);

            Point newPos = new Point(newX, newY);
            this.button1.Location = newPos;
        }

        private void button2_MouseClick(object sender, MouseEventArgs e)
        {
            string val1 = this.textBox1.Text;
            try
            {
                int intVal1 = Convert.ToInt32(val1);

                Color color = Color.FromArgb(intVal1, intVal1, intVal1);
                this.BackColor = color;
            }
            catch (Exception exception)
            {
                MessageBox.Show("Invalid color params");

            }
        }
    }
}
