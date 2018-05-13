using System;
using System.Collections.Generic;
using ModPlusAPI.Interfaces;

namespace mprNestedFamilyParamsTransfer
{
    public class Interface : IModPlusFunctionInterface
    {
        public SupportedProduct SupportedProduct => SupportedProduct.Revit;
        public string Name => "mprNestedFamilyParamsTransfer";
        public string AvailProductExternalVersion => "2015";
        public string FullClassName => "mprNestedFamilyParamsTransfer.Command";
        public string AppFullClassName => string.Empty;
        public Guid AddInId => Guid.Empty;
        public string LName => "Связь параметров вложенных семейств";
        public string Description => "";
        public string Author => "Пекшев Александр aka Modis";
        public string Price => "0";
        public bool CanAddToRibbon => true;
        public string FullDescription => "";
        public string ToolTipHelpImage => "";
        public List<string> SubFunctionsNames => new List<string>();
        public List<string> SubFunctionsLames => new List<string>();
        public List<string> SubDescriptions => new List<string>();
        public List<string> SubFullDescriptions => new List<string>();
        public List<string> SubHelpImages => new List<string>();
        public List<string> SubClassNames => new List<string>();
    }
}
