namespace condo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;

    public sealed class ScrollableTabs : ContentControl
    {
        static ScrollableTabs()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ScrollableTabs), new FrameworkPropertyMetadata(typeof(ScrollableTabs)));
        }

        private const int ScrollerCount = 2;
        private const int ScrollUpIndex = 0;
        private const int ScrollDownIndex = 1;
        private ContentPresenter scrollUpContent;
        private ContentPresenter scrollDownContent;

        public ScrollableTabs() : base()
        {
            this.scrollUpContent = new ContentPresenter();
            this.scrollDownContent = new ContentPresenter();
            this.AddVisualChild(this.scrollUpContent);
            this.AddVisualChild(this.scrollDownContent);

        }
    }
}
