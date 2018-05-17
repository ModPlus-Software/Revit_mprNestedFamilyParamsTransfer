using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using mprNestedFamilyParamsTransfer.Annotations;
using mprNestedFamilyParamsTransfer.Helpers;
using ModPlusAPI.Windows;

namespace mprNestedFamilyParamsTransfer
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                ModPlusAPI.Statistic.SendCommandStarting(new Interface());

                var doc = commandData.Application.ActiveUIDocument.Document;
                if (!doc.IsFamilyDocument)
                {
                    MessageBox.Show("Запуск плагина доступен только в редакторе семейств");
                    return Result.Cancelled;
                }
                var fm = doc.FamilyManager;
                // check for familyType
                if (fm.CurrentType == null)
                {
                    if (fm.Types.IsEmpty)
                    {
                        TaskDialog taskDialog = new TaskDialog("ModPlus")
                        {
                            MainContent =
                                "Внимание! В текущем семействе отсутствуют типоразмеры! Корректная работа Функции не возможна!" +
                                Environment.NewLine +
                                "Вы можете выбрать вариант \"Продолжить и создать типоразмер\" - тогда Функция создаст типоразмер с именем: " +
                                doc.Title + Environment.NewLine +
                                "Если Вы не хотите создавать новый типоразмер, то Вам нужно выполнить следующие действия:" +
                                Environment.NewLine +
                                "1. Выберите вариант \"Вернуться в редактор семейства\"" + Environment.NewLine +
                                "2. Откройте стандартное диалоговое окно \"Типоразмеры в семействе\"" +
                                Environment.NewLine +
                                "3. Создайте новый типоразмер и сразу его удалите" + Environment.NewLine +
                                "4. Еще раз запустите данную Функцию"
                        };
                        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1,
                            "Продолжить и создать типоразмер");
                        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2,
                            "Вернуться в редактор семейства");
                        var result = taskDialog.Show();
                        if (result == TaskDialogResult.CommandLink1)
                        {
                            using (Transaction tr = new Transaction(doc, "Create Type"))
                            {
                                tr.Start();
                                fm.NewType(doc.Title);
                                tr.Commit();
                            }
                            
                        }
                        else return Result.Succeeded;
                    }
                    else fm.CurrentType = fm.Types.GetEnumerator().Current as FamilyType;
                }

                RevitInterop.UiApplication = commandData.Application;
                RevitInterop.InitEvents();
                FunctionStarter.Start();

                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                return Result.Failed;
            }
        }
    }

    public static class FunctionStarter
    {
        [CanBeNull]
        public static MainWindow MainWindow;

        public static void Start()
        {
            if (MainWindow != null)
            {
                MainWindow.Activate();
                MainWindow.Focus();
            }
            else
            {
                MainWindow = new MainWindow();
                MainWindow.Closed += MainWindow_Closed;
                MainWindow.ShowDialog();
            }
        }

        private static void MainWindow_Closed(object sender, EventArgs e)
        {
            MainWindow = null;
        }
    }
}
