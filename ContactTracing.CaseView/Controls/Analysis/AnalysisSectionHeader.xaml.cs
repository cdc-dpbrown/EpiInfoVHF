using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ContactTracing.CaseView.Controls.Analysis
{
    /// <summary>
    /// Interaction logic for AnalysisSectionHeader.xaml
    /// </summary>
    public partial class AnalysisSectionHeader : UserControl
    {
        public AnalysisSectionHeader()
        {
            InitializeComponent();
        }

        public string Heading
        {
            get
            {
                return this.tblockSummaryHeading.Text;
            }
            set
            {
                this.tblockSummaryHeading.Text = value;
            }
        }
    }
}
