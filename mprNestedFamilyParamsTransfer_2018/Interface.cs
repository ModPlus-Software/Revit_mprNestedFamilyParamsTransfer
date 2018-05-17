using System;
using System.Collections.Generic;
using ModPlusAPI.Interfaces;

namespace mprNestedFamilyParamsTransfer
{
    /// <summary>Интерфейс функции ModPlus</summary>
    public class Interface : IModPlusFunctionInterface
    {
        public SupportedProduct SupportedProduct => SupportedProduct.Revit;
        public string Name => "mprNestedFamilyParamsTransfer";
        public string AvailProductExternalVersion => "2018";
        public string FullClassName => "mprNestedFamilyParamsTransfer.Command";
        public string AppFullClassName => string.Empty;
        public Guid AddInId => Guid.Empty;
        public string LName => "Связь параметров вложенных семейств";
        public string Description => "Функция позволяет упростить работу по установке связи между параметрами вложенных семейств с параметрами родительского семейства";
        public string Author => "Пекшев Александр aka Modis";
        public string Price => "0";
        public bool CanAddToRibbon => true;
        public string FullDescription => "С помощью функции можно создавать новые параметры в родительском семействе на основе одного или нескольких параметров вложенных семейств. Присутствует возможность задать маску для имени создаваемого параметра путём добавления префикса или суффикса. Функция также позволяет устанавливать и удалять связь с существующими параметрами родительского семейства";
        public string ToolTipHelpImage => "";
        public List<string> SubFunctionsNames => new List<string>();
        public List<string> SubFunctionsLames => new List<string>();
        public List<string> SubDescriptions => new List<string>();
        public List<string> SubFullDescriptions => new List<string>();
        public List<string> SubHelpImages => new List<string>();
        public List<string> SubClassNames => new List<string>();
    }
}
