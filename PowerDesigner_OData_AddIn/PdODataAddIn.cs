using System;
using System.Runtime.InteropServices;
using PdAddInTypLib;

namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData
{
    [ComVisible(true)]
    [Guid("87284DA7-93AC-4D80-9F83-0D5F46539B74")]
    public class PdODataAddIn : IPdAddIn
    {
        // A reference to the PowerDesigner Application object.
        private PdCommon.Application _app;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PdODataAddIn()
        {
            this._app = null;
        }

        /// <summary>
        /// From: https://help.sap.com/docs/SAP_POWERDESIGNER/31c48596e34446a68956e0aa7e700a2e/c7d62f7d6e1b1014b6b5cd497c583efb.html
        /// The Initialize() method initializes communication between PowerDesigner and the add-in.
        /// PowerDesigner provides a pointer to its application object, defined in the PdCommon type library,
        /// which allows you to access the PowerDesigner environment (output window, active model etc.).
        /// </summary>
        /// <param name="pApplication"></param>
        public void Initialize(object pApplication)
        {
            try
            {
                this._app = (PdCommon.Application)pApplication;
                Info("Succesfully initialized OData AddIn");
            }
            catch (Exception e)
            {
                // Write exception in the PowerDesigner output window.
                Error(String.Format("An exception occurred during intitialize of OData AddIn: {0}", e.Message));
            }
        }

        public string ProvideMenuItems(string sMenu, object pObject)
        {
            throw new NotImplementedException();
        }

        public int IsCommandSupported(string sMenu, object pObject, string sCommandName)
        {
            throw new NotImplementedException();
        }

        public void DoCommand(string sMenu, object pObject, string sCommandName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// From: https://help.sap.com/docs/SAP_POWERDESIGNER/31c48596e34446a68956e0aa7e700a2e/c7d62f7d6e1b1014b6b5cd497c583efb.html
        /// The Uninitialize() method is called when PowerDesigner is closed to release all global variables and clean all references to PowerDesigner objects.
        /// </summary>
        public void Uninitialize()
        {
            Debug("Uninitialize []");
            // Unset the reference to the PowerDesigner Application object.
            // Not sure if this is needed, but just to be sure it can't be used anymore.
            this._app = null;
        }

        /// <summary>
        /// Helper method to write a info message in the PowerDesigner output window.
        /// </summary>
        /// <param name="message">The info message to write in the PowerDesigner output window</param>
        private void Info(String message)
        {
            this.Log(message, "INFO");
        }

        /// <summary>
        /// Helper method to write a error message in the PowerDesigner output window.
        /// </summary>
        /// <param name="message">The error message to write in the PowerDesigner output window</param>
        private void Error(String message)
        {
            this.Log(message, "ERROR");
        }

        /// <summary>
        /// Helper method to write a debug message in the PowerDesigner output window.
        /// </summary>
        /// <param name="message">The debug message to write in the PowerDesigner output window</param>
        private void Debug(String message)
        {
            this.Log(message, "DEBUG");
        }

        /// <summary>
        /// Helper method to write a log message in the PowerDesigner output window.
        /// </summary>
        /// <param name="message">The message to write in the PowerDesigner output window</param>
        private void Log(String message, String level)
        {
            // Only write to the Output if we have a handle on the PowerDesigner application.
            if (this._app != null)
                this._app.Output(String.Format("[OData-AddIn {0}] {1}", level, message));
            else
                Console.WriteLine(String.Format("[OData-AddIn {0}] {1}", level, message));
        }
    }
}
