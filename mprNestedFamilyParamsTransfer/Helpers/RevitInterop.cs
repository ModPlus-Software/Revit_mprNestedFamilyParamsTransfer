﻿namespace mprNestedFamilyParamsTransfer.Helpers
{
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    public static class RevitInterop
    {
        public static UIApplication UiApplication { get; set; }

        public static UIDocument UiDocument => UiApplication.ActiveUIDocument;

        public static Document Document => UiApplication.ActiveUIDocument.Document;

        // events
        public static RevitEvent RevitEvent;

        public static void InitEvents()
        {
            RevitEvent = new RevitEvent();
        }
    }
}
