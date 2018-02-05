using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace ContactTracing.Controls
{
    public class LabResultsSummaryView : Selector
    {
        static LabResultsSummaryView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LabResultsSummaryView), new FrameworkPropertyMetadata(typeof(LabResultsSummaryView)));
        }
    }
}
