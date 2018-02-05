using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Markup;
using System.IO;
using ContactTracing.Core;

namespace ContactTracing.CaseView
{
    class AnalysisPaginator : DocumentPaginator
    {

        StackPanel mainPanel;

        public AnalysisPaginator(StackPanel pnl)
        {
            mainPanel = pnl;
            CloneVisual(mainPanel);
        }


        public override DocumentPage GetPage(int pageNumber)
        {
            StackPanel panel = (StackPanel)CloneVisual(mainPanel);

            Canvas pageCanvas = new Canvas();
            pageCanvas = new Canvas();
            pageCanvas.Measure(PageSize);
            pageCanvas.Arrange(new Rect(new Point(), PageSize));
            pageCanvas.Children.Add(panel);
            Canvas.SetLeft(panel, 50);
            Canvas.SetTop(panel, pageNumber * -1 * Constants.A4_PAGE_LENGTH);
            pageCanvas.UpdateLayout();

            return new DocumentPage(pageCanvas);
        }

        private object CloneVisual(object o)
        {
            string xamlCode = XamlWriter.Save(o).Replace("Name=","Tag=");
            return XamlReader.Load(new System.Xml.XmlTextReader(new StringReader(xamlCode)));
        }

        public override bool IsPageCountValid
        {
            get { return true; }
        }

        public override int PageCount
        {
            get { return (int)Math.Ceiling(mainPanel.ActualHeight / Constants.A4_PAGE_LENGTH); }
        }

        public override System.Windows.Size PageSize
        {
            get
            {
                return new Size(Constants.A4_PAGE_WIDTH, Constants.A4_PAGE_LENGTH);
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override IDocumentPaginatorSource Source
        {
            get { return null; }
        }
    }
}
