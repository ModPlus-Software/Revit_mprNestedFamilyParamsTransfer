using Autodesk.Revit.DB;
using mprNestedFamilyParamsTransfer.Helpers;

namespace mprNestedFamilyParamsTransfer.Models
{
    /// <summary>Параметр вложенного семейства</summary>
    public class NestedFamilyParameterModel : VmBase
    {
        public NestedFamilyParameterModel(Parameter parameter, NestedFamilyInstanceModel nestedFamilyInstance)
        {
            Parameter = parameter;
            NestedFamilyInstance = nestedFamilyInstance;
        }

        public Parameter Parameter;
        public NestedFamilyInstanceModel NestedFamilyInstance;

        public string Name => Parameter.Definition.Name;

        public StorageType StorageType => Parameter.StorageType;

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
                        case StorageType.Integer: return Parameter.AsInteger() == 0 ? "Выкл" : "Вкл";
                        case StorageType.ElementId: return RevitInterop.Document.GetElement(Parameter.AsElementId()).Name;
                    }
                }

                return string.Empty;
            }
        }

        private bool _isLinked;
        /// <summary>Есть ли связь этого параметра в текущем семействе (семейство, в котором запущена функция)</summary>
        public bool IsLinked
        {
            get => _isLinked;
            set
            {
                if (value == _isLinked) return;
                _isLinked = value;
                OnPropertyChanged();
            }
        }

        public AssociatedParameterModel AssociatedParameter { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                if (AssociatedParameter != null)
                    AssociatedParameter.IsSelected = value;
                OnPropertyChanged();
            }
        }
    }
}
