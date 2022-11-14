using System;
using System.Windows.Forms;

namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData
{
    // A helper class with functions for the PowerDesigner application object.
    public static class PdAppHelper
    {
        /// <summary>
        /// Get a pointer to the PowerDesigner main window so we can show message boxes on top of it.
        /// </summary>
        /// <param name="app">The PowerDesigner Application object.</param>
        /// <returns></returns>
        public static NativeWindow GetPDWindow(PdCommon.Application app)
        {
            return NativeWindow.FromHandle((IntPtr)app.MainWindowHandle);
        }
    }
}
