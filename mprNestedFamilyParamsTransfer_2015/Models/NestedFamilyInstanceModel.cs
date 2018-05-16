using System;
using System.Collections.Generic;
using System.Windows.Input;
using Autodesk.Revit.DB;
using mprNestedFamilyParamsTransfer.Helpers;

namespace mprNestedFamilyParamsTransfer.Models
{
    /// <summary>Вложенное (экземпляр) семейство</summary>
    public class NestedFamilyInstanceModel : VmBase
    {
        public NestedFamilyInstanceModel(FamilyInstance familyInstance, MainViewModel mainViewModel)
        {
            NestedFamilyInstance = familyInstance;
            ShowFamilyInstanceCommand = new RelayCommand(ShowFamilyInstance);
            InstanceParameters = new List<NestedFamilyParameterModel>();
            TypeParameters = new List<NestedFamilyParameterModel>();
            _mainViewModel = mainViewModel;
            GetParameters();
        }

        private readonly MainViewModel _mainViewModel;

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
            List<Parameter> instanceParameters = new List<Parameter>();
            foreach (Parameter parameter in NestedFamilyInstance.Parameters)
            {
                if (NestedFamilyInstance.Document.FamilyManager.CanElementParameterBeAssociated(parameter))
                    instanceParameters.Add(parameter);
            }
            instanceParameters.Sort((p1,p2) => String.Compare(p1.Definition.Name, p2.Definition.Name, StringComparison.Ordinal));
            instanceParameters.ForEach(parameter =>
            {
                InstanceParameters.Add(new NestedFamilyParameterModel(parameter, this, true, _mainViewModel));
            });
            // get type parameters
            List<Parameter> typeParameters = new List<Parameter>();
            foreach (Parameter parameter in NestedFamilyInstance.Symbol.Parameters)
            {
                if (NestedFamilyInstance.Document.FamilyManager.CanElementParameterBeAssociated(parameter))
                    typeParameters.Add(parameter);
            }
            typeParameters.Sort((p1,p2) => string.Compare(p1.Definition.Name, p2.Definition.Name, StringComparison.Ordinal));
            typeParameters.ForEach(parameter =>
            {
                TypeParameters.Add(new NestedFamilyParameterModel(parameter, this, false, _mainViewModel));
            });
        }

        public ICommand ShowFamilyInstanceCommand { get; }

        private void ShowFamilyInstance(object o)
        {
            RevitInterop.UiDocument.Selection.SetElementIds(new List<ElementId>{NestedFamilyInstance.Id});
        }

        #endregion
    }
}
