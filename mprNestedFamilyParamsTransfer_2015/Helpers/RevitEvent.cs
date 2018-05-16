using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace mprNestedFamilyParamsTransfer.Helpers
{
    public class RevitEvent : IExternalEventHandler
    {
        private Action _doAction;
        private Document _doc;
        private readonly ExternalEvent _exEvent;
        private bool _skipFailures;
        private string _transactionName;

        public RevitEvent()
        {
            _exEvent = ExternalEvent.Create(this);
        }

        public void Run(Action doAction, bool skipFailures, Document doc = null, string transactionName = null)
        {
            _doAction = doAction;
            _skipFailures = skipFailures;
            _doc = doc;
            _transactionName = transactionName;
            _exEvent.Raise();
        }

        public void Execute(UIApplication app)
        {
            if (_doAction != null)
            {
                if (_doc == null) _doc = app.ActiveUIDocument.Document;
                if (_skipFailures)
                    app.Application.FailuresProcessing += Application_FailuresProcessing;
                using (Transaction t = new Transaction(_doc, _transactionName ?? "RevitEvent"))
                {
                    t.Start();
                    _doAction();
                    t.Commit();
                }
                if (_skipFailures)
                    app.Application.FailuresProcessing -= Application_FailuresProcessing;
            }
        }

        private void Application_FailuresProcessing(object sender, Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            // Inside event handler, get all warnings
            var failList = e.GetFailuresAccessor().GetFailureMessages();
            if (failList.Any())
            {
                // skip all failures
                e.GetFailuresAccessor().DeleteAllWarnings();
                e.SetProcessingResult(FailureProcessingResult.Continue);
            }
        }

        public string GetName()
        {
            return "RevitEvent";
        }
    }

}
