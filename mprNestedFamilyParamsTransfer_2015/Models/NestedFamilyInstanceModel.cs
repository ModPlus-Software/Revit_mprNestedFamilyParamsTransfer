using System;
using System.Collections.ObjectModel;
using Autodesk.Revit.DB;
using mprNestedFamilyParamsTransfer.Helpers;

namespace mprNestedFamilyParamsTransfer.Models
{
    /// <summary>Вложенное (экземпляр) семейство</summary>
    public class NestedFamilyInstanceModel : VmBase
    {
        public NestedFamilyInstanceModel(FamilyInstance familyInstance)
        {
            NestedFamilyInstance = familyInstance;
            InstanceParameters = new ObservableCollection<NestedFamilyParameterModel>();
            TypeParameters = new ObservableCollection<NestedFamilyParameterModel>();
            GetParameters();
        }

        public FamilyInstance NestedFamilyInstance;
        public string Name => NestedFamilyInstance.Name;

        private int _count = 1;
        public int Count
        {
            get => _count;
            set
            {
                if (value == _count) return;
                _count = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<NestedFamilyParameterModel> InstanceParameters { get; }
        public ObservableCollection<NestedFamilyParameterModel> TypeParameters { get; }

        #region Methods

        private void GetParameters()
        {
            // get instance parameters
            foreach (Parameter parameter in NestedFamilyInstance.Parameters)
            {
                if (NestedFamilyInstance.Document.FamilyManager.CanElementParameterBeAssociated(parameter))
                    InstanceParameters.Add(new NestedFamilyParameterModel(parameter));
            }
            // get type parameters
            foreach (Parameter parameter in NestedFamilyInstance.Symbol.Parameters)
            {
                if (NestedFamilyInstance.Document.FamilyManager.CanElementParameterBeAssociated(parameter))
                    TypeParameters.Add(new NestedFamilyParameterModel(parameter));
            }
        }


        #endregion
    }
}
