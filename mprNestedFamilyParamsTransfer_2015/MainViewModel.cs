using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            _familyManager = RevitInterop.Document.FamilyManager;
            GetNestedFamilyInstances();
        }
        #endregion

        #region Fields

        private FamilyManager _familyManager;

        #endregion

        #region Properties

        public ObservableCollection<NestedFamilyInstanceModel> NestedFamilyInstanceModels { get; set; }

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
                    if (!CheckIsCopyInstanse(familyInstance))
                        NestedFamilyInstanceModels.Add(new NestedFamilyInstanceModel(familyInstance));
                }
            }
        }

        private bool CheckIsCopyInstanse(FamilyInstance familyInstance)
        {
            foreach (NestedFamilyInstanceModel nestedFamilyInstanceModel in NestedFamilyInstanceModels)
            {
                if (nestedFamilyInstanceModel.NestedFamilyInstance.GetTypeId().IntegerValue ==
                    familyInstance.GetTypeId().IntegerValue)
                {
                    nestedFamilyInstanceModel.Count += 1;
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
