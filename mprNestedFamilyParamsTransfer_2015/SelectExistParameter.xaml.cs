using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using mprNestedFamilyParamsTransfer.Helpers;
using ModPlusAPI.Windows;

namespace mprNestedFamilyParamsTransfer
{
    public partial class SelectExistParameter
    {
        public SelectExistParameter(Parameter parameter)
        {
            InitializeComponent();
            Title = "Назначение параметра семейства"; // Associate global parameter
            // get data from parameter
            TbParameterName.Text = parameter.Definition.Name;
            TbParameterType.Text = LabelUtils.GetLabelFor(parameter.Definition.ParameterType);
            // get allowable parameters
            var fm = RevitInterop.Document.FamilyManager;
            foreach (FamilyParameter fmParameter in fm.Parameters)
            {
                if (fmParameter.Definition.ParameterType == parameter.Definition.ParameterType)
                    LbParameters.Items.Add(new ListBoxItem
                    {
                        Content = fmParameter.Definition.Name,
                        Tag = fmParameter.Id.IntegerValue
                    });
            }
        }

        private void BtAccept_OnClick(object sender, RoutedEventArgs e)
        {
            if (LbParameters.SelectedIndex == -1)
            {
                ModPlusAPI.Windows.MessageBox.Show("Вы не выбрали параметр в списке!", MessageBoxIcon.Alert);
                return;
            }
            DialogResult = true;
        }

        private void BtCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
