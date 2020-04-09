namespace mprNestedFamilyParamsTransfer
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using Autodesk.Revit.DB;
    using Helpers;
    using ModPlusAPI.Windows;

    public partial class SelectExistParameter
    {
        private const string LangItem = "mprNestedFamilyParamsTransfer";

        public SelectExistParameter(Parameter parameter)
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem(LangItem, "h18"); //"Назначение параметра семейства"; 
            // get data from parameter
            TbParameterName.Text = parameter.Definition.Name;
            TbParameterType.Text = LabelUtils.GetLabelFor(parameter.Definition.ParameterType);
            // get allowable parameters
            var fm = RevitInterop.Document.FamilyManager;
            // sort
            List<Tuple<string, int>> parameters = new List<Tuple<string, int>>();
            foreach (FamilyParameter fmParameter in fm.Parameters)
            {
                if (fmParameter.Definition.ParameterType == parameter.Definition.ParameterType)
                    parameters.Add(new Tuple<string, int>(fmParameter.Definition.Name, fmParameter.Id.IntegerValue));
            }
            parameters.Sort((i1,i2) => string.Compare(i1.Item1, i2.Item1, StringComparison.Ordinal));
            parameters.ForEach(p =>
            {
                LbParameters.Items.Add(new ListBoxItem
                {
                    Content = p.Item1,
                    Tag = p.Item2
                });
            });
        }

        private void BtAccept_OnClick(object sender, RoutedEventArgs e)
        {
            if (LbParameters.SelectedIndex == -1)
            {
                ModPlusAPI.Windows.MessageBox.Show(ModPlusAPI.Language.GetItem(LangItem, "msg12"),  //"Вы не выбрали параметр в списке!"
                MessageBoxIcon.Alert);
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
