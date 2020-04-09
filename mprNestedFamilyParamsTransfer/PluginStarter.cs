namespace mprNestedFamilyParamsTransfer
{
    using System;
    using JetBrains.Annotations;

    public static class PluginStarter
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