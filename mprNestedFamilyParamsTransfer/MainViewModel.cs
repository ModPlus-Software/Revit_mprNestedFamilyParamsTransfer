namespace mprNestedFamilyParamsTransfer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Helpers;
    using JetBrains.Annotations;
    using Models;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.Windows;

    public class MainViewModel : VmBase
    {
        private const string LangItem = "mprNestedFamilyParamsTransfer";

        #region Constructor

        public MainViewModel()
        {
            // commands
            RemoveLinksCommand = new RelayCommandWithoutParameter(RemoveLink);
            DeleteParametersCommand = new RelayCommandWithoutParameter(DeleteParameter);
            CreateParametersCommand = new RelayCommandWithoutParameter(CreateParameters);

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
        public int IsInstanceParameterCreate
        {
            get => int.TryParse(
                UserConfigFile.GetValue(LangItem, nameof(IsInstanceParameterCreate)), out var i) ? i : 0;
            set => UserConfigFile.SetValue(LangItem, nameof(IsInstanceParameterCreate), value.ToString(), true);
        }
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
                    NestedFamilyInstanceModels.Add(new NestedFamilyInstanceModel(familyInstance, this));
                }
            }
        }

        private void GetAssociatedParameters()
        {
            /* Параметры экземпляра могут быть связаны с разными параметрами текущего семейства
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
                        if (!IsAlreadyUseAssociatedParameter(
                            associatedParameter,
                            nestedFamilyInstanceParameter))
                        {
                            var associatedParameterModel = new AssociatedParameterModel(
                                associatedParameter,
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
                        if (!IsAlreadyUseAssociatedParameter(
                            associatedParameter,
                            nestedFamilyInstanceParameter))
                        {
                            var associatedParameterModel = new AssociatedParameterModel(
                                associatedParameter,
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
        private void RemoveLink()
        {
            try
            {
                var selectedInstanceParameters = new List<NestedFamilyParameterModel>();
                var selectedTypeParameters = new List<NestedFamilyParameterModel>();
                foreach (var nestedFamilyInstanceModel in NestedFamilyInstanceModels)
                {
                    nestedFamilyInstanceModel.InstanceParameters.ForEach(p =>
                    {
                        if (p.IsSelected && p.IsLinked)
                            selectedInstanceParameters.Add(p);
                    });
                    nestedFamilyInstanceModel.TypeParameters.ForEach(p =>
                    {
                        if (p.IsSelected && p.IsLinked)
                            selectedTypeParameters.Add(p);
                    });
                }

                if (selectedInstanceParameters.Any() || selectedTypeParameters.Any())
                {
                    using (Transaction tr = new Transaction(RevitInterop.Document, "Remove links"))
                    {
                        tr.Start();
                        selectedInstanceParameters.ForEach(p =>
                        {
                            RevitInterop.Document.FamilyManager.AssociateElementParameterToFamilyParameter(p.Parameter, null);
                            p.AssociatedParameter.NestedFamilyParameters.Remove(p);
                            if (p.AssociatedParameter.NestedFamilyParameters.Count == 0)
                                p.AssociatedParameter.IsUnlinked = true;
                            p.IsLinked = false;
                        });

                        // Параметры типа - это один и тот-же параметр в Ревите, но разные в плагине
                        // Поэтому после "отвязки" нужно пройти по всем параметрам всех семейств и "отвязать" тот-же самый параметр
                        var idsOfSameParameter = new List<int>();
                        foreach (var p in selectedTypeParameters)
                        {
                            if (idsOfSameParameter.Contains(p.Parameter.Id.IntegerValue))
                                continue;
                            idsOfSameParameter.Add(p.Parameter.Id.IntegerValue);
                            RevitInterop.Document.FamilyManager.AssociateElementParameterToFamilyParameter(p.Parameter, null);
                            foreach (var nestedFamilyInstanceModel in NestedFamilyInstanceModels)
                            {
                                foreach (var nestedFamilyParameterModel in nestedFamilyInstanceModel.TypeParameters)
                                {
                                    if (nestedFamilyParameterModel.Parameter.Id.IntegerValue !=
                                        p.Parameter.Id.IntegerValue)
                                        continue;
                                    if (nestedFamilyParameterModel.AssociatedParameter.NestedFamilyParameters.Count > 0)
                                    {
                                        nestedFamilyParameterModel.AssociatedParameter.NestedFamilyParameters.Remove(
                                            nestedFamilyParameterModel);
                                    }

                                    if (nestedFamilyParameterModel.AssociatedParameter.NestedFamilyParameters.Count == 0)
                                        nestedFamilyParameterModel.AssociatedParameter.IsUnlinked = true;
                                    nestedFamilyParameterModel.IsLinked = false;
                                }
                            }
                        }

                        tr.Commit();
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        public ICommand DeleteParametersCommand { get; }

        private void DeleteParameter()
        {
            var selectedAssociatedParameters = AssociatedParameters.Where(p => p.IsSelected).ToList();
            if (selectedAssociatedParameters.Any())
            {
                using (Transaction tr = new Transaction(RevitInterop.Document, "Delete parameters"))
                {
                    tr.Start();
                    selectedAssociatedParameters.ForEach(p =>
                    {
                        foreach (var nestedFamilyParameter in p.NestedFamilyParameters)
                        {
                            RevitInterop.Document.FamilyManager.AssociateElementParameterToFamilyParameter(nestedFamilyParameter.Parameter, null);
                            nestedFamilyParameter.IsLinked = false;
                            nestedFamilyParameter.AssociatedParameter = null;
                        }

                        try
                        {
                            RevitInterop.Document.Delete(p.FamilyParameter.Id);

                            // if no exception
                            AssociatedParameters.Remove(p);
                        }
                        catch
                        {
                            MessageBox.Show(
                                $"{Language.GetItem(LangItem, "msg9")} {p.FamilyParameter.Definition.Name}", // Can not remove parameter
                                MessageBoxIcon.Alert);
                            p.NestedFamilyParameters.Clear();
                            p.IsUnlinked = true;
                        }
                    });
                    tr.Commit();
                }
            }
        }

        public ICommand CreateParametersCommand { get; }

        private void CreateParameters()
        {
            var fm = RevitInterop.Document.FamilyManager;
            var selectedInstanceParameters = new List<NestedFamilyParameterModel>();
            var selectedTypeParameters = new List<NestedFamilyParameterModel>();
            foreach (var nestedFamilyInstanceModel in NestedFamilyInstanceModels)
            {
                nestedFamilyInstanceModel.InstanceParameters.ForEach(p =>
                {
                    if (p.IsSelected && !p.IsLinked)
                        selectedInstanceParameters.Add(p);
                });
                nestedFamilyInstanceModel.TypeParameters.ForEach(p =>
                {
                    if (p.IsSelected && !p.IsLinked)
                        selectedTypeParameters.Add(p);
                });
            }

            try
            {
                if (selectedInstanceParameters.Any() || selectedTypeParameters.Any())
                {
                    using (Transaction tr = new Transaction(RevitInterop.Document, "Create parameters"))
                    {
                        tr.Start();

                        // При создании нового параметра в текущем семействе для параметра экземпляра, выбранного
                        // в списке создается всего один параметр независимо от количества экземпляров семейств
                        selectedInstanceParameters.ForEach(p =>
                        {
                            // Сначала проверю на наличие существующего параметра с таким-же именем
                            var existFamilyParameter = CheckForExistAllowableParameter(p.Parameter);
                            if (existFamilyParameter != null)
                            {
                                // связать с существующим
                                // сначала проверить - вдруг он уже есть в окне плагина
                                var hasInWindow = false;
                                foreach (var associatedParameterModel in AssociatedParameters)
                                {
                                    if (associatedParameterModel.FamilyParameter.Id.IntegerValue ==
                                        existFamilyParameter.Id.IntegerValue)
                                    {
                                        fm.AssociateElementParameterToFamilyParameter(
                                            p.Parameter,
                                            associatedParameterModel.FamilyParameter);
                                        associatedParameterModel.IsUnlinked = false;
                                        associatedParameterModel.NestedFamilyParameters.Add(p);
                                        p.AssociatedParameter = associatedParameterModel;
                                        p.IsLinked = true;
                                        hasInWindow = true;
                                    }
                                }

                                if (!hasInWindow)
                                {
                                    var associatedParameterModel =
                                        new AssociatedParameterModel(existFamilyParameter, p);
                                    p.AssociatedParameter = associatedParameterModel;
                                    p.IsLinked = true;
                                    AssociatedParameters.Add(associatedParameterModel);
                                }
                            }
                            else
                            {
                                // создать новый
                                FamilyParameter newFamilyParameter = fm.AddParameter(
                                    GetNameForNewFamilyParameter(p.Name), p.Parameter.Definition.ParameterGroup,
                                    p.Parameter.Definition.ParameterType, IsInstanceParameterCreate != 0);
                                SetValueForCreatedParameter(p, fm, newFamilyParameter);
                                fm.AssociateElementParameterToFamilyParameter(p.Parameter, newFamilyParameter);
                                var associatedParameterModel = new AssociatedParameterModel(newFamilyParameter, p);
                                p.AssociatedParameter = associatedParameterModel;
                                p.IsLinked = true;
                                AssociatedParameters.Add(associatedParameterModel);
                            }
                        });

                        // При создании нового параметра в текущем семействе для параметра типа, выбранного в 
                        // списке создается один параметр, но связь устанавливается для всех экземпляров семейства
                        // (так как параметр типа содержится в типе, а не в экземпляре), поэтому в списках "моих классов"
                        // нужно отыскать нужные параметры и установить нужные данные
                        var idsOfSameParameter = new List<int>();
                        foreach (var p in selectedTypeParameters)
                        {
                            if (idsOfSameParameter.Contains(p.Parameter.Id.IntegerValue))
                                continue;
                            idsOfSameParameter.Add(p.Parameter.Id.IntegerValue);
                            AssociatedParameterModel associatedParameterModel = null;

                            // Сначала проверю на наличие существующего параметра с таким-же именем
                            var existFamilyParameter = CheckForExistAllowableParameter(p.Parameter);
                            if (existFamilyParameter != null)
                            {
                                // связать с существующим
                                // сначала проверить - вдруг он уже есть в окне плагина
                                var hasInWindow = false;
                                foreach (var parameterModel in AssociatedParameters)
                                {
                                    if (parameterModel.FamilyParameter.Id.IntegerValue ==
                                        existFamilyParameter.Id.IntegerValue)
                                    {
                                        fm.AssociateElementParameterToFamilyParameter(
                                            p.Parameter,
                                            parameterModel.FamilyParameter);
                                        parameterModel.IsUnlinked = false;
                                        parameterModel.NestedFamilyParameters.Add(p);
                                        p.AssociatedParameter = parameterModel;
                                        p.IsLinked = true;
                                        hasInWindow = true;
                                    }
                                }

                                if (!hasInWindow)
                                {
                                    associatedParameterModel = new AssociatedParameterModel(existFamilyParameter, p);
                                    p.AssociatedParameter = associatedParameterModel;
                                    p.IsLinked = true;
                                    AssociatedParameters.Add(associatedParameterModel);
                                }
                            }
                            else
                            {
                                // create new
                                FamilyParameter newFamilyParameter = fm.AddParameter(
                                    GetNameForNewFamilyParameter(p.Name), p.Parameter.Definition.ParameterGroup,
                                    p.Parameter.Definition.ParameterType, IsInstanceParameterCreate != 0);
                                SetValueForCreatedParameter(p, fm, newFamilyParameter);
                                fm.AssociateElementParameterToFamilyParameter(p.Parameter, newFamilyParameter);
                                associatedParameterModel = new AssociatedParameterModel(newFamilyParameter, p);
                                p.AssociatedParameter = associatedParameterModel;
                                p.IsLinked = true;
                                AssociatedParameters.Add(associatedParameterModel);
                            }

                            // Искать такие-же параметры в любом случае (создали ли новый или привязали к существующему)
                            if (associatedParameterModel != null)
                            {
                                foreach (var nestedFamilyInstanceModel in NestedFamilyInstanceModels)
                                {
                                    if (p.NestedFamilyInstance == nestedFamilyInstanceModel)
                                        continue;
                                    foreach (var nestedFamilyParameterModel in nestedFamilyInstanceModel.TypeParameters)
                                    {
                                        if (nestedFamilyParameterModel.Parameter.Id.IntegerValue !=
                                            p.Parameter.Id.IntegerValue)
                                            continue;
                                        nestedFamilyParameterModel.IsLinked = true;
                                        nestedFamilyParameterModel.AssociatedParameter = associatedParameterModel;
                                        associatedParameterModel.NestedFamilyParameters.Add(nestedFamilyParameterModel);
                                    }
                                }
                            }
                        }

                        tr.Commit();
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static void SetValueForCreatedParameter(
            NestedFamilyParameterModel p,
            FamilyManager fm,
            FamilyParameter newFamilyParameter)
        {
            if (p.Parameter.HasValue)
            {
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
            }
        }

        [CanBeNull]
        private FamilyParameter CheckForExistAllowableParameter(Parameter p)
        {
            TaskDialog taskDialog = new TaskDialog("ModPlus")
            {
                MainInstruction = $"{Language.GetItem(LangItem, "msg10")} {p.Definition.Name}" + // В текущем семействе уже существует параметр с именем
                                  Environment.NewLine +
                                  Language.GetItem(LangItem, "msg11") // Выберите дальнейшее действие:
            };
            taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, Language.GetItem(LangItem, "c3")); // "Установить связь с существующим параметром"
            taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, Language.GetItem(LangItem, "c4")); // "Создать новый параметр"
            var fm = RevitInterop.Document.FamilyManager;
            foreach (FamilyParameter fmParameter in fm.Parameters)
            {
                if (fmParameter.Definition.Name.Equals(p.Definition.Name) &&
                    fmParameter.StorageType == p.StorageType &&
#if R2017 || R2018 || R2019 || R2020 || R2021
                    fmParameter.Definition.ParameterType == p.Definition.ParameterType)
#else
                    fmParameter.Definition.GetDataType() == p.Definition.GetDataType())
#endif
                {
                    var userSet = taskDialog.Show();
                    PluginStarter.MainWindow?.Focus();
                    if (userSet == TaskDialogResult.CommandLink1)
                        return fmParameter;
                    break;
                }
            }

            return null;
        }

        private string GetNameForNewFamilyParameter(string pName)
        {
            var newName = RemoveNotAllowableSymbols(Prefix + pName + Suffix);
            var baseName = newName;
            var index = 1;
            while (HasSameNameParameter(newName))
            {
                newName = baseName + "_" + index;
                index++;
            }

            return newName;
        }

        private string RemoveNotAllowableSymbols(string name)
        {
            var bracketsOpen = new[] { "{", "[", "<", };
            var bracketsClose = new[] { "}", "]", ">" };
            var symbols = new[] { "\\", ":", "|", ";", "?", "`", "~" };
            foreach (var s in bracketsOpen)
                name = name.Replace(s, "(");
            foreach (var s in bracketsClose)
                name = name.Replace(s, ")");
            foreach (var s in symbols)
                name = name.Replace(s, "");
            return name;
        }

        private bool HasSameNameParameter(string name)
        {
            foreach (FamilyParameter familyParameter in RevitInterop.Document.FamilyManager.GetParameters())
            {
                if (familyParameter.Definition.Name.Equals(name))
                    return true;
            }

            return false;
        }

        public void AssociateToExistFamilyParameter(NestedFamilyParameterModel nestedFamilyParameterModel)
        {
            try
            {
                SelectExistParameter selectExistParameter = new SelectExistParameter(nestedFamilyParameterModel.Parameter);
                if (selectExistParameter.ShowDialog() == true)
                {
                    var id = (int)((ListBoxItem)selectExistParameter.LbParameters.SelectedItem).Tag;
                    var fm = RevitInterop.Document.FamilyManager;
                    using (Transaction tr = new Transaction(RevitInterop.Document, "Associate parameter"))
                    {
                        tr.Start();
                        AssociatedParameterModel associatedParameterModel = null;

                        // сначала проверить - вдруг он уже есть в окне плагина
                        var hasInWindow = false;
                        foreach (var parameterModel in AssociatedParameters)
                        {
                            if (parameterModel.FamilyParameter.Id.IntegerValue == id)
                            {
                                if (parameterModel.FamilyParameter.StorageType == StorageType.Double &&
                                   !fm.CurrentType.HasValue(parameterModel.FamilyParameter))
                                    SetValueForCreatedParameter(nestedFamilyParameterModel, fm, parameterModel.FamilyParameter);
                                fm.AssociateElementParameterToFamilyParameter(nestedFamilyParameterModel.Parameter, parameterModel.FamilyParameter);
                                parameterModel.IsUnlinked = false;
                                parameterModel.NestedFamilyParameters.Add(nestedFamilyParameterModel);
                                nestedFamilyParameterModel.AssociatedParameter = parameterModel;
                                nestedFamilyParameterModel.IsLinked = true;
                                hasInWindow = true;
                            }
                        }

                        if (!hasInWindow)
                        {
                            foreach (FamilyParameter fmParameter in fm.GetParameters())
                            {
                                if (fmParameter.Id.IntegerValue == id)
                                {
                                    var oldValue = GetFamilyParameterValue(fm, fmParameter);
                                    SetTemporaryValueToFamilyParameter(fm, fmParameter);
                                    fm.AssociateElementParameterToFamilyParameter(nestedFamilyParameterModel.Parameter, fmParameter);
                                    SetSavedValueToFamilyParameter(oldValue, fm, fmParameter);
                                    associatedParameterModel = new AssociatedParameterModel(fmParameter, nestedFamilyParameterModel);
                                    nestedFamilyParameterModel.AssociatedParameter = associatedParameterModel;
                                    nestedFamilyParameterModel.IsLinked = true;
                                    AssociatedParameters.Add(associatedParameterModel);
                                    break;
                                }
                            }
                        }

                        // Искать такие-же параметры в любом случае (создали ли новый или привязали к существующему)
                        if (!nestedFamilyParameterModel.IsInstance && associatedParameterModel != null)
                        {
                            foreach (var nestedFamilyInstanceModel in NestedFamilyInstanceModels)
                            {
                                if (nestedFamilyInstanceModel == nestedFamilyParameterModel.NestedFamilyInstance)
                                    continue;
                                foreach (var parameterModel in nestedFamilyInstanceModel.TypeParameters)
                                {
                                    if (parameterModel.Parameter.Id.IntegerValue != nestedFamilyParameterModel.Parameter.Id.IntegerValue)
                                        continue;
                                    parameterModel.IsLinked = true;
                                    parameterModel.AssociatedParameter = associatedParameterModel;
                                    associatedParameterModel.NestedFamilyParameters.Add(parameterModel);
                                }
                            }
                        }

                        tr.Commit();
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        /// <summary>Получить текущее значение параметра семейства</summary>
        private static object GetFamilyParameterValue(FamilyManager fm, FamilyParameter familyParameter)
        {
            switch (familyParameter.StorageType)
            {
                case StorageType.Double: return fm.CurrentType.AsDouble(familyParameter);
                case StorageType.ElementId: return fm.CurrentType.AsElementId(familyParameter);
                case StorageType.Integer: return fm.CurrentType.AsInteger(familyParameter);
                case StorageType.String: return fm.CurrentType.AsString(familyParameter);
            }

            return null;
        }

        /// <summary>Установить временное значение-заглушку для параметра семейства</summary>
        private static void SetTemporaryValueToFamilyParameter(FamilyManager fm, FamilyParameter familyParameter)
        {
            switch (familyParameter.StorageType)
            {
                case StorageType.Double: fm.Set(familyParameter, 1); break;
                case StorageType.ElementId: fm.Set(familyParameter, ElementId.InvalidElementId); break;
                case StorageType.Integer: fm.Set(familyParameter, 1); break;
                case StorageType.String: fm.Set(familyParameter, "temp"); break;
            }
        }

        /// <summary>Установить сохраненное значение для параметра семейства</summary>
        private static void SetSavedValueToFamilyParameter(object value, FamilyManager fm, FamilyParameter familyParameter)
        {
            switch (familyParameter.StorageType)
            {
                case StorageType.Double:
                    if (value != null)
                        fm.Set(familyParameter, (double)value);
                    else
                        fm.Set(familyParameter, 0);
                    break;
                case StorageType.ElementId:
                    if (value != null)
                        fm.Set(familyParameter, (ElementId)value);
                    else
                        fm.Set(familyParameter, ElementId.InvalidElementId);
                    break;
                case StorageType.Integer:
                    if (value != null)
                        fm.Set(familyParameter, (int)value);
                    else
                        fm.Set(familyParameter, 0);
                    break;
                case StorageType.String:
                    if (value != null)
                        fm.Set(familyParameter, (string)value);
                    else
                        fm.Set(familyParameter, string.Empty);
                    break;
            }
        }

        #endregion
    }
}
