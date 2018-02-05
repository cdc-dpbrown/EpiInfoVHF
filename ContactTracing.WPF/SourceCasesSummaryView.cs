using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace ContactTracing.Controls
{
    public class SourceCasesSummaryView : Selector
    {
        static SourceCasesSummaryView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SourceCasesSummaryView), new FrameworkPropertyMetadata(typeof(SourceCasesSummaryView)));
        }
    }
}
