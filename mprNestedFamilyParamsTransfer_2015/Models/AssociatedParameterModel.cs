using System.Collections.ObjectModel;
using System.Windows.Input;
using Autodesk.Revit.DB;
using mprNestedFamilyParamsTransfer.Helpers;

namespace mprNestedFamilyParamsTransfer.Models
{
    /// <summary>Связанный параметр в текущем семействе (параметр который используется в параметрах вложенных семейств)</summary>
    public class AssociatedParameterModel : VmBase
    {
        public AssociatedParameterModel(
            FamilyParameter familyParameter,
            NestedFamilyParameterModel nestedFamilyParameter)
        {
            FamilyParameter = familyParameter;
            NestedFamilyParameters = new ObservableCollection<NestedFamilyParameterModel> { nestedFamilyParameter };
            
            _isInstanceParameter = familyParameter.IsInstance ? 1 : 0;
            // commands
            SelectNestedParametersCommand = new RelayCommand(SelectNestedParameters);
        }

        public FamilyParameter FamilyParameter;

        public ObservableCollection<NestedFamilyParameterModel> NestedFamilyParameters { get; }
        
        public string Name => FamilyParameter.Definition.Name;

        private bool _isSelected;
        private bool _isUnliked;
        private int _isInstanceParameter;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public bool IsUnliked
        {
            get => _isUnliked;
            set
            {
                if (value == _isUnliked) return;
                _isUnliked = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public bool IsEnabled => !IsUnliked;

        public int IsInstanceParameter
        {
            get => _isInstanceParameter;
            set
            {
                if (value == _isInstanceParameter) return;
                _isInstanceParameter = value;
                using (Transaction tr = new Transaction(RevitInterop.Document, "Change IsInstance parameter"))
                {
                    tr.Start();
                    if (value == 1)
                        RevitInterop.Document.FamilyManager.MakeInstance(FamilyParameter);
                    else RevitInterop.Document.FamilyManager.MakeType(FamilyParameter);
                    tr.Commit();
                }
                OnPropertyChanged();
            }
        }

        public ICommand SelectNestedParametersCommand { get; }
        private bool _isAllSelected;
        private void SelectNestedParameters(object o)
        {
            if (_isAllSelected)
            {
                foreach (var p in NestedFamilyParameters)
                    p.IsSelected = false;
                _isAllSelected = false;
            }
            else
            {
                foreach (var p in NestedFamilyParameters)
                    p.IsSelected = true;
                _isAllSelected = true;
            }
        }
    }
}
