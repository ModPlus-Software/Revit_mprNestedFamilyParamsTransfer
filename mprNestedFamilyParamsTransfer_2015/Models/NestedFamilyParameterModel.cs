using Autodesk.Revit.DB;
using mprNestedFamilyParamsTransfer.Helpers;

namespace mprNestedFamilyParamsTransfer.Models
{
    /// <summary>Параметр вложенного семейства</summary>
    public class NestedFamilyParameterModel : VmBase
    {
        public NestedFamilyParameterModel(Parameter parameter)
        {
            Parameter = parameter;
            _isLinked = true;
        }

        public Parameter Parameter;

        public string Name => Parameter.Definition.Name;

        public StorageType StorageType => Parameter.StorageType;

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
    }
}
