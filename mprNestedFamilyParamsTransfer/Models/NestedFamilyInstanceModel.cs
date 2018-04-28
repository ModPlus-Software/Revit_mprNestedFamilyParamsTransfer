using Autodesk.Revit.DB;
using mprNestedFamilyParamsTransfer.Helpers;

namespace mprNestedFamilyParamsTransfer.Models
{
    /// <summary>Вложенное (экземпляр) семейство</summary>
    public class NestedFamilyInstanceModel : VmBase
    {
        public NestedFamilyInstanceModel(FamilyInstance familyInstance)
        {
            Name = familyInstance.Name;
        }

        public string Name { get; }
    }
}
