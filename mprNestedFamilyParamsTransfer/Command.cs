using System;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using mprNestedFamilyParamsTransfer.Helpers;

namespace mprNestedFamilyParamsTransfer
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var doc = commandData.Application.ActiveUIDocument.Document;
                if (!doc.IsFamilyDocument)
                {
                    MessageBox.Show("Запуск плагина доступен только в редакторе семейств");
                    return Result.Cancelled;
                }
                
                RevitInterop.UiApplication = commandData.Application;
                MainWindow window = new MainWindow();
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                return Result.Failed;
            }
        }
    }
}
