using System.Windows.Controls;
using System.Windows.Input;

namespace mprNestedFamilyParamsTransfer
{
    public partial class MainWindow 
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = "Nested Family InstanceParameters Transfer";
            CbIsInstanceParameterCreate.ItemsSource = new[]
            {
                "Параметр типа", "Параметр экземпляра"
            };
        }
        private void OnMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem lbi)
            {
                if (lbi.IsSelected)
                {
                    lbi.IsSelected = false;
                    e.Handled = true;
                }
            }
        }
    }
}
