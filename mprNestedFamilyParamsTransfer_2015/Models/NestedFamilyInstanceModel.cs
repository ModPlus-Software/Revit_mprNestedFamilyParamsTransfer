using System.Collections.Generic;
using System.Windows.Input;
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
            ShowFamilyInstanceCommand = new RelayCommand(ShowFamilyInstance);
            InstanceParameters = new List<NestedFamilyParameterModel>();
            TypeParameters = new List<NestedFamilyParameterModel>();
            GetParameters();
        }

        public FamilyInstance NestedFamilyInstance;
        public string Name => NestedFamilyInstance.Name;
        
        public List<NestedFamilyParameterModel> InstanceParameters { get; }
        public List<NestedFamilyParameterModel> TypeParameters { get; }

        public List<NestedFamilyParameterModel> AllParameters
        {
            get
            {
                List<NestedFamilyParameterModel> list = new List<NestedFamilyParameterModel>();
                foreach (var nestedFamilyParameterModel in InstanceParameters)
                    list.Add(nestedFamilyParameterModel);
                foreach (var nestedFamilyParameterModel in TypeParameters)
                    list.Add(nestedFamilyParameterModel);

                return list;
            }
        }

        #region Methods

        private void GetParameters()
        {
            // get instance parameters
            foreach (Parameter parameter in NestedFamilyInstance.Parameters)
            {
                if (NestedFamilyInstance.Document.FamilyManager.CanElementParameterBeAssociated(parameter))
                    InstanceParameters.Add(new NestedFamilyParameterModel(parameter, this));
            }
            // get type parameters
            foreach (Parameter parameter in NestedFamilyInstance.Symbol.Parameters)
            {
                if (NestedFamilyInstance.Document.FamilyManager.CanElementParameterBeAssociated(parameter))
                    TypeParameters.Add(new NestedFamilyParameterModel(parameter, this));
            }
        }

        public ICommand ShowFamilyInstanceCommand { get; }

        private void ShowFamilyInstance(object o)
        {
            RevitInterop.UiDocument.Selection.SetElementIds(new List<ElementId>{NestedFamilyInstance.Id});
        }

        #endregion
    }
}
