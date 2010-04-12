using System.Windows;
using System.Windows.Media;

namespace Editor
{
    public class Zoom
    {
        private readonly FrameworkElement _elementToZoom;
        private double _x;
        private double _y;
        private const double Factor = 0.2;

        public Zoom(FrameworkElement elementToZoom)
        {
            _elementToZoom = elementToZoom;
            _x = _y = 1;
        }

        public void ZoomIn()
        {
            _y = (_x += Factor);
            _elementToZoom.LayoutTransform = new ScaleTransform(_x, _y);
        }
        public void ZoomOut()
        {
            _y = (_x -= Factor);
            _elementToZoom.LayoutTransform = new ScaleTransform(_x, _y);
        }
        public void UpZoomBy(double factor)
        {
            _y = (_x += factor);
            _elementToZoom.LayoutTransform = new ScaleTransform(_x, _y);
        }
        public void SetZoomTo(double factor)
        {
            _y = _x = factor;
            _elementToZoom.LayoutTransform = new ScaleTransform(_x, _y);
        }
    }
}