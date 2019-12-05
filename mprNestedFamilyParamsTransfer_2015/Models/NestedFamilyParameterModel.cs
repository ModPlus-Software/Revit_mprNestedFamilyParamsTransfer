namespace mprNestedFamilyParamsTransfer.Models
{
    using System.Windows.Input;
    using Autodesk.Revit.DB;
    using Helpers;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;

    /// <summary>Параметр вложенного семейства</summary>
    public class NestedFamilyParameterModel : VmBase
    {
        private const string LangItem = "mprNestedFamilyParamsTransfer";

        public NestedFamilyParameterModel(
            Parameter parameter,
            NestedFamilyInstanceModel nestedFamilyInstance, 
            bool isInstanceParameter,
            MainViewModel mainViewModel)
        {
            Parameter = parameter;
            NestedFamilyInstance = nestedFamilyInstance;
            IsInstance = isInstanceParameter;
            AssociateToExistFamilyParameterCommand = new RelayCommand(AssociateToExistFamilyParameter);
            _mainViewModel = mainViewModel;
        }

        private readonly MainViewModel _mainViewModel;

        public Parameter Parameter;
        public NestedFamilyInstanceModel NestedFamilyInstance;
        public bool IsInstance;

        public string Name => Parameter.Definition.Name;

        public string DisplayValue
        {
            get
            {
                if (Parameter != null && Parameter.HasValue)
                {
                    switch (Parameter.StorageType)
                    {
                        case StorageType.String: return Parameter.AsString();
                        case StorageType.Double: return Parameter.AsValueString();
                        case StorageType.Integer:
                            return Parameter.AsInteger() == 0
                                ? Language.GetItem(LangItem, "off") // "Выкл"
                                : Language.GetItem(LangItem, "on"); // "Вкл";
                        case StorageType.ElementId: return RevitInterop.Document.GetElement(Parameter.AsElementId()).Name;
                    }
                }

                return string.Empty;
            }
        }

        private bool _isLinked;
        
        /// <summary>
        /// Есть ли связь этого параметра в текущем семействе (семейство, в котором запущена функция)
        /// </summary>
        public bool IsLinked
        {
            get => _isLinked;
            set
            {
                if (value == _isLinked)
                    return;
                _isLinked = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanLink));
            }
        }

        public bool CanLink => !IsLinked;

        public AssociatedParameterModel AssociatedParameter { get; set; }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected)
                    return;
                _isSelected = value;
                if (AssociatedParameter != null)
                    AssociatedParameter.IsSelected = value;
                OnPropertyChanged();
            }
        }

        public ICommand AssociateToExistFamilyParameterCommand { get; }

        private void AssociateToExistFamilyParameter(object o)
        {
            _mainViewModel.AssociateToExistFamilyParameter(this);
        }
    }
}
