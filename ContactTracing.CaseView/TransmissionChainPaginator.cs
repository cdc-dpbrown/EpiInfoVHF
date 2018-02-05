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
    class TransmissionChainPaginator : DocumentPaginator
    {

        Dictionary<string, Canvas> pages;

        public TransmissionChainPaginator(Canvas transmissionCanvas, DateTime firstOnset)
        {
            int requiredPagesPerColumn = (int)Math.Ceiling(transmissionCanvas.Height / Constants.A4_PAGE_WIDTH);
            int requiredColumns = (int)Math.Ceiling(transmissionCanvas.Width / Constants.A4_PAGE_LENGTH);
            pages = new Dictionary<string, Canvas>();

            for (int y = 0; y < requiredColumns; y++)
            {
                for (int x = 0; x < requiredPagesPerColumn; x++)
                {
                    Canvas pageCanvas = new Canvas();
                    pageCanvas = new Canvas();
                    pageCanvas.Measure(PageSize);
                    pageCanvas.Arrange(new Rect(new Point(), PageSize));
                    pages.Add(x + "," + y, pageCanvas);

                    if (x == 0)
                    {
                        for (int days = 0; days < 256; days += 7)
                        {
                            TextBlock tb = new TextBlock();
                            tb.Text = "|" + firstOnset.AddDays(days).ToString("dd-MMM");
                            tb.FontWeight = FontWeights.SemiBold;
                            tb.Foreground = System.Windows.Media.Brushes.DarkBlue;
                            tb.FontSize = 14;
                            Canvas.SetTop(tb, 0);
                            Canvas.SetLeft(tb, (days * Constants.TRANS_CHAIN_SCALE) + 10 - (y * Constants.A4_PAGE_LENGTH));
                            Canvas.SetZIndex(tb, 50);
                            pages[x + "," + y].Children.Add(tb);
                        }
                    }
                }

                foreach (FrameworkElement element in transmissionCanvas.Children)
                {
                    if (element is Line)
                    {
                        for (int x = requiredPagesPerColumn - 1; x >= 0; x--)
                        {
                            Line line = (Line)Clone(element);
                            line.Y1 = line.Y1 - (x * Constants.A4_PAGE_WIDTH) + 20;
                            line.Y2 = line.Y2 - (x * Constants.A4_PAGE_WIDTH) + 20;

                            line.X1 = line.X1 - (y * Constants.A4_PAGE_LENGTH);
                            line.X2 = line.X2 - (y * Constants.A4_PAGE_LENGTH);

                            pages[x + "," + y].Children.Add(line);
                        }
                    }
                    else
                    {
                        double left = Canvas.GetLeft(element);
                        double top = Canvas.GetTop(element);
                        for (int x = requiredPagesPerColumn - 1; x >= 0; x--)
                        {
                            if (top > x * Constants.A4_PAGE_WIDTH)
                            {
                                FrameworkElement clonedElement = Clone(element);
                                pages[x + "," + y].Children.Add(clonedElement);
                                Canvas.SetLeft(clonedElement, left - (y * Constants.A4_PAGE_LENGTH));
                                Canvas.SetTop(clonedElement, top - (x * Constants.A4_PAGE_WIDTH) + 20);

                                if (Canvas.GetTop(clonedElement) + 100 > Constants.A4_PAGE_WIDTH && x < requiredPagesPerColumn - 1)
                                {
                                    FrameworkElement clonedElement2 = Clone(element);
                                    pages[(x + 1) + "," + y].Children.Add(clonedElement2);
                                    Canvas.SetLeft(clonedElement2, left - (y * Constants.A4_PAGE_LENGTH));
                                    Canvas.SetTop(clonedElement2, top - ((x + 1) * Constants.A4_PAGE_WIDTH) + 20);
                                }

                                break;
                            }
                        }
                    }
                }
            }
            
            foreach (Canvas canvas in pages.Values)
            {                
                canvas.UpdateLayout();
            }
        }

        private FrameworkElement Clone(FrameworkElement element)
        {
            if (element is Rectangle)
            {
                return GetRectangle((Rectangle)element);
            }
            else if (element is TextBlock)
            {
                return GetTextBlock((TextBlock)element);
            }
            else if (element is Line)
            {
                return GetLine((Line)element);
            }
            else
            {
                return new Canvas();
            }
        }

        private Line GetLine(Line original)
        {
            Line line = new Line();
            line.X1 = original.X1;
            line.Y1 = original.Y1;
            line.X2 = original.X2;
            line.Y2 = original.Y2;
            line.Stroke = original.Stroke;
            line.StrokeThickness = original.StrokeThickness;
            line.StrokeEndLineCap = original.StrokeEndLineCap;
            line.StrokeDashArray = original.StrokeDashArray;
            Canvas.SetZIndex(line, 10);
            return line;
        }

        private TextBlock GetTextBlock(TextBlock original)
        {
            TextBlock tb = new TextBlock();
            tb.Text = original.Text;
            tb.FontWeight = original.FontWeight;
            tb.FontSize = original.FontSize;
            Canvas.SetZIndex(tb, 30);
            return tb;
        }

        private Rectangle GetRectangle(Rectangle original)
        {
            Rectangle rect = new Rectangle();
            rect.RadiusX = original.RadiusX;
            rect.RadiusY = original.RadiusY;
            rect.StrokeThickness = 0.5;
            rect.Stroke = System.Windows.Media.Brushes.Black;
            rect.StrokeDashArray = original.StrokeDashArray;
            rect.StrokeThickness = original.StrokeThickness;
            rect.Fill = System.Windows.Media.Brushes.GhostWhite;// GetDefaultBrush();
            rect.Width = original.Width;
            rect.Height = original.Height;
            Canvas.SetZIndex(rect, 20);
            return rect;
        }

        public override DocumentPage GetPage(int pageNumber)
        {
            return new DocumentPage(pages[pages.Keys.ToArray()[pageNumber]]);
        }

        public override bool IsPageCountValid
        {
            get { return true; }
        }

        public override int PageCount
        {
            get { return pages.Count; }
        }

        public override System.Windows.Size PageSize
        {
            get
            {
                return new Size(Constants.A4_PAGE_LENGTH, Constants.A4_PAGE_WIDTH);
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
