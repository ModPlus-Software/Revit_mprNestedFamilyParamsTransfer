using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace mprNestedFamilyParamsTransfer.Helpers
{
    public static class RevitInterop
    {
        public static UIApplication UiApplication { get; set; }
        public static UIDocument UiDocument => UiApplication.ActiveUIDocument;
        public static Document Document => UiApplication.ActiveUIDocument.Document;
    }
}
