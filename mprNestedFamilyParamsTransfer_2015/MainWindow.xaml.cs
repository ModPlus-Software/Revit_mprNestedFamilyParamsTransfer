namespace mprNestedFamilyParamsTransfer
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using ModPlusAPI.Windows.Helpers;

    public partial class MainWindow
    {
        private const string LangItem = "mprNestedFamilyParamsTransfer";

        public MainWindow()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem(LangItem, "h1");
            CbIsInstanceParameterCreate.ItemsSource = new[]
            {
               ModPlusAPI.Language.GetItem(LangItem, "p1"), //  "Параметр типа",
               ModPlusAPI.Language.GetItem(LangItem, "p2") //"Параметр экземпляра"
            };
        }
        private void OnMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            // Обработчик события повешен на текст так как при нажатии на кнопку не будет работать как надо
            if (sender is TextBlock tb)
            {
                var lbi = tb.TryFindParent<ListBoxItem>();
                if (lbi != null && lbi.IsSelected)
                {
                    lbi.IsSelected = false;
                    e.Handled = true;
                }
            }
        }
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            if (parentObject is T parent)
                return parent;
            return FindParent<T>(parentObject);
        }

        private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            NestedParametersScrollViewer.ScrollToVerticalOffset(NestedParametersScrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;

        }
    }
}
