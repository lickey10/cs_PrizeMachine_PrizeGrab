using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SCTV
{
    public partial class ResultDetails : Form
    {
        public ResultDetails(string details)
        {
            string splitString = "|";

            InitializeComponent();

            //load details into listbox
            string[] detailsArray = details.Split(new string[] { splitString }, StringSplitOptions.RemoveEmptyEntries);

            foreach(string detail in detailsArray)
            {
                lbDetails.Items.Add(detail);
            }
        }
    }
}
