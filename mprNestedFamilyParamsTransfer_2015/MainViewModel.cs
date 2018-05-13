using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Autodesk.Revit.DB;
using mprNestedFamilyParamsTransfer.Helpers;
using mprNestedFamilyParamsTransfer.Models;

namespace mprNestedFamilyParamsTransfer
{
    public class MainViewModel : VmBase
    {
        #region Constructor

        public MainViewModel()
        {
            // commands
            RemoveLinksCommand = new RelayCommand(RemoveLink);
            DeleteParametersCommand = new RelayCommand(DeleteParameter);
            CreateParametersCommand = new RelayCommand(CreateParameters);
            // get families and parameters
            GetNestedFamilyInstances();
            GetAssociatedParameters();
        }
        #endregion

        #region Properties

        public ObservableCollection<NestedFamilyInstanceModel> NestedFamilyInstanceModels { get; set; }
        public ObservableCollection<AssociatedParameterModel> AssociatedParameters { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        /// <summary>0 - создавать параметр типа. 1 - создавать параметр экземпляра</summary>
        public int IsInstanceParameterCreate { get; set; }
        #endregion

        #region Methods

        private void GetNestedFamilyInstances()
        {
            NestedFamilyInstanceModels = new ObservableCollection<NestedFamilyInstanceModel>();
            var collector = new FilteredElementCollector(RevitInterop.Document)
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance));
            foreach (Element element in collector)
            {
                if (element is FamilyInstance familyInstance)
                {
                    NestedFamilyInstanceModels.Add(new NestedFamilyInstanceModel(familyInstance));
                }
            }
        }

        private void GetAssociatedParameters()
        {
            /* Парметры экземпляра могут быть связаны с разными параметрами текущего семейства
             * А вот параметры типа могут быть связаны только с одним параметром текущего семейства!
             */
            AssociatedParameters = new ObservableCollection<AssociatedParameterModel>();
            foreach (var nestedFamily in NestedFamilyInstanceModels)
            {
                foreach (var nestedFamilyInstanceParameter in nestedFamily.InstanceParameters)
                {
                    FamilyParameter associatedParameter = RevitInterop.Document.FamilyManager
                        .GetAssociatedFamilyParameter(nestedFamilyInstanceParameter.Parameter);
                    if (associatedParameter != null)
                    {
                        // нужно найти параметр с которым уже может быть установлена связь, иначе создать новый
                        if (!IsAlreadyUseAssociatedParameter(associatedParameter,
                            nestedFamilyInstanceParameter))
                        {
                            var associatedParameterModel = new AssociatedParameterModel(associatedParameter,
                                nestedFamilyInstanceParameter);
                            AssociatedParameters.Add(associatedParameterModel);
                            // также параметру вложенного семейства сообщу об связанном параметре
                            nestedFamilyInstanceParameter.AssociatedParameter = associatedParameterModel;
                            nestedFamilyInstanceParameter.IsLinked = true;
                        }
                    }
                }
                foreach (var nestedFamilyInstanceParameter in nestedFamily.TypeParameters)
                {
                    FamilyParameter associatedParameter = RevitInterop.Document.FamilyManager
                        .GetAssociatedFamilyParameter(nestedFamilyInstanceParameter.Parameter);
                    if (associatedParameter != null)
                    {
                        // нужно найти параметр с которым уже может быть установлена связь, иначе создать новый
                        if (!IsAlreadyUseAssociatedParameter(associatedParameter,
                            nestedFamilyInstanceParameter))
                        {
                            var associatedParameterModel = new AssociatedParameterModel(associatedParameter,
                                nestedFamilyInstanceParameter);
                            AssociatedParameters.Add(associatedParameterModel);
                            // также параметру вложенного семейства сообщу об связанном параметре
                            nestedFamilyInstanceParameter.AssociatedParameter = associatedParameterModel;
                            nestedFamilyInstanceParameter.IsLinked = true;
                        }
                    }
                }
            }
        }

        private bool IsAlreadyUseAssociatedParameter(
            FamilyParameter associatedParameter,
            NestedFamilyParameterModel nestedFamilyParameter)
        {
            foreach (var associatedParameterModel in AssociatedParameters)
            {
                if (associatedParameterModel.FamilyParameter.Id.IntegerValue == associatedParameter.Id.IntegerValue)
                {
                    associatedParameterModel.NestedFamilyParameters.Add(nestedFamilyParameter);
                    nestedFamilyParameter.AssociatedParameter = associatedParameterModel;
                    nestedFamilyParameter.IsLinked = true;
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Commands

        public ICommand RemoveLinksCommand { get; }
        /// <summary>Удалить связь, не удаляя параметр</summary>
        private void RemoveLink(object o)
        {
            var selectedInstanceParameters = new List<NestedFamilyParameterModel>();
            var selectedTypeParameters = new List<NestedFamilyParameterModel>();
            foreach (var nestedFamilyInstanceModel in NestedFamilyInstanceModels)
            {
                nestedFamilyInstanceModel.InstanceParameters.ForEach(p => { if (p.IsSelected && p.IsLinked) selectedInstanceParameters.Add(p); });
                nestedFamilyInstanceModel.TypeParameters.ForEach(p => { if (p.IsSelected && p.IsLinked) selectedTypeParameters.Add(p); });
            }

            if (selectedInstanceParameters.Any() || selectedTypeParameters.Any())
                using (Transaction tr = new Transaction(RevitInterop.Document, "Remove links"))
                {
                    tr.Start();
                    selectedInstanceParameters.ForEach(p =>
                    {
                        RevitInterop.Document.FamilyManager.AssociateElementParameterToFamilyParameter(p.Parameter, null);
                        p.AssociatedParameter.NestedFamilyParameters.Remove(p);
                        if (p.AssociatedParameter.NestedFamilyParameters.Count == 0)
                            p.AssociatedParameter.IsUnliked = true;
                        p.IsLinked = false;
                    });
                    // Параметры типа - это один и тот-же параметр в Ревите, но разные в плагине
                    // Поэтому после "отвязки" нужно пройти по всем параметрам всех семейств и "отвязать" тот-же самый параметр
                    var idsOfSameParameter = new List<int>();
                    foreach (var p in selectedTypeParameters)
                    {
                        if (idsOfSameParameter.Contains(p.Parameter.Id.IntegerValue)) continue;
                        idsOfSameParameter.Add(p.Parameter.Id.IntegerValue);
                        RevitInterop.Document.FamilyManager.AssociateElementParameterToFamilyParameter(p.Parameter, null);
                        foreach (var nestedFamilyInstanceModel in NestedFamilyInstanceModels)
                        {
                            foreach (var nestedFamilyParameterModel in nestedFamilyInstanceModel.TypeParameters)
                            {
                                if(nestedFamilyParameterModel.Parameter.Id.IntegerValue != p.Parameter.Id.IntegerValue) continue;
                                if (nestedFamilyParameterModel.AssociatedParameter.NestedFamilyParameters.Count > 0)
                                    nestedFamilyParameterModel.AssociatedParameter.NestedFamilyParameters.Remove(nestedFamilyParameterModel);
                                if (nestedFamilyParameterModel.AssociatedParameter.NestedFamilyParameters.Count == 0)
                                    nestedFamilyParameterModel.AssociatedParameter.IsUnliked = true;
                                nestedFamilyParameterModel.IsLinked = false;
                            }
                        }
                    }
                    tr.Commit();
                }
        }

        public ICommand DeleteParametersCommand { get; }

        private void DeleteParameter(object o)
        {
            var selectedAssociatedParameters = AssociatedParameters.Where(p => p.IsSelected).ToList();
            if (selectedAssociatedParameters.Any())
                using (Transaction tr = new Transaction(RevitInterop.Document, "Delete parameters"))
                {
                    tr.Start();
                    selectedAssociatedParameters.ForEach(p =>
                    {
                        foreach (var nestedFamilyParameter in p.NestedFamilyParameters)
                        {
                            nestedFamilyParameter.IsLinked = false;
                            nestedFamilyParameter.AssociatedParameter = null;
                        }
                        RevitInterop.Document.Delete(p.FamilyParameter.Id);
                        AssociatedParameters.Remove(p);
                    });
                    tr.Commit();
                }
        }

        public ICommand CreateParametersCommand { get; }

        private void CreateParameters(object o)
        {
            var fm = RevitInterop.Document.FamilyManager;
            var selectedInstanceParameters = new List<NestedFamilyParameterModel>();
            var selectedTypeParameters = new List<NestedFamilyParameterModel>();
            foreach (var nestedFamilyInstanceModel in NestedFamilyInstanceModels)
            {
                nestedFamilyInstanceModel.InstanceParameters.ForEach(p =>
                {
                    if (p.IsSelected && !p.IsLinked) selectedInstanceParameters.Add(p);
                });
                nestedFamilyInstanceModel.TypeParameters.ForEach(p =>
                {
                    if (p.IsSelected && !p.IsLinked) selectedTypeParameters.Add(p);
                });
            }

            if (selectedInstanceParameters.Any() || selectedTypeParameters.Any())
            {
                using (Transaction tr = new Transaction(RevitInterop.Document, "Create parameters"))
                {
                    tr.Start();
                    if (fm.CurrentType == null)
                    {
                        if (fm.Types.IsEmpty)
                            fm.NewType(RevitInterop.Document.Title);
                        else
                            fm.CurrentType = fm.Types.GetEnumerator().Current as FamilyType;
                    }

                    selectedInstanceParameters.ForEach(p =>
                    {
                        FamilyParameter newFamilyParameter = fm.AddParameter(
                            GetNameForNewFamilyParameter(p.Name), p.Parameter.Definition.ParameterGroup,
                            p.Parameter.Definition.ParameterType, IsInstanceParameterCreate != 0);
                        if (p.Parameter.HasValue)
                            switch (p.Parameter.StorageType)
                            {
                                case StorageType.Double:
                                    fm.Set(newFamilyParameter, p.Parameter.AsDouble());
                                    break;
                                case StorageType.ElementId:
                                    fm.Set(newFamilyParameter, p.Parameter.AsElementId());
                                    break;
                                case StorageType.Integer:
                                    fm.Set(newFamilyParameter, p.Parameter.AsInteger());
                                    break;
                                case StorageType.String:
                                    fm.Set(newFamilyParameter, p.Parameter.AsString());
                                    break;
                            }
                        fm.AssociateElementParameterToFamilyParameter(p.Parameter, newFamilyParameter);
                        var associatedParameterModel = new AssociatedParameterModel(newFamilyParameter, p);
                        p.IsLinked = true;
                        AssociatedParameters.Add(associatedParameterModel);
                    });
                    tr.Commit();
                }
            }
        }

        private string GetNameForNewFamilyParameter(string pName)
        {
            var newName = Prefix + pName + Suffix;
            var baseName = newName;
            var index = 1;
            while (HasSameNameParameter(newName))
            {
                newName = baseName + "_" + index;
                index++;
            }
            return newName;
        }

        private bool HasSameNameParameter(string name)
        {
            foreach (FamilyParameter familyParameter in RevitInterop.Document.FamilyManager.GetParameters())
            {
                if (familyParameter.Definition.Name.Equals(name)) return true;
            }
            return false;
        }

        #endregion
    }
}
