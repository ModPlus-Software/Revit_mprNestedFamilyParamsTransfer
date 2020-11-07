namespace mprNestedFamilyParamsTransfer
{
    using System;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Helpers;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        private const string LangItem = "mprNestedFamilyParamsTransfer";

        /// <inheritdoc />
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
#if !DEBUG
                Statistic.SendCommandStarting(new ModPlusConnector());
#endif

                var doc = commandData.Application.ActiveUIDocument.Document;
                if (!doc.IsFamilyDocument)
                {
                    MessageBox.Show(Language.GetItem(LangItem, "msg1")); // Запуск функции доступен только в редакторе семейств
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
                                Language.GetItem(LangItem, "msg2") + // "Внимание! В текущем семействе отсутствуют типоразмеры! Корректная работа Функции не возможна!" +
                                Environment.NewLine +
                                Language.GetItem(LangItem, "msg3") + " " + // Вы можете выбрать вариант \"Продолжить и создать типоразмер\" - тогда Функция создаст типоразмер с именем:
                                doc.Title + Environment.NewLine +
                                Language.GetItem(LangItem, "msg4") + // "Если Вы не хотите создавать новый типоразмер, то Вам нужно выполнить следующие действия:" +
                                Environment.NewLine +
                                Language.GetItem(LangItem, "msg5") + Environment.NewLine + // 1. Выберите вариант \"Вернуться в редактор семейства\"
                                Language.GetItem(LangItem, "msg6") + // 2. Откройте стандартное диалоговое окно \"Типоразмеры в семействе\"
                                Environment.NewLine +
                                Language.GetItem(LangItem, "msg7") + Environment.NewLine + // 3. Создайте новый типоразмер и сразу его удалите
                                Language.GetItem(LangItem, "msg8") // 4. Еще раз запустите данную Функцию
                        };
                        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, Language.GetItem(LangItem, "c1")); // "Продолжить и создать типоразмер"
                        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, Language.GetItem(LangItem, "c2")); // Вернуться в редактор семейства
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
                        else
                        {
                            return Result.Succeeded;
                        }
                    }
                    else
                    {
                        fm.CurrentType = fm.Types.GetEnumerator().Current as FamilyType;
                    }
                }

                RevitInterop.UiApplication = commandData.Application;
                RevitInterop.InitEvents();
                PluginStarter.Start();

                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                return Result.Failed;
            }
        }
    }
}
