using System;
using System.Runtime.InteropServices;
using PdAddInTypLib;
using System.Windows.Forms;


namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData
{
    [ComVisible(true)]
    [Guid("87284DA7-93AC-4D80-9F83-0D5F46539B74")]
    public class PdODataAddIn : IPdAddIn
    {
        // A reference to the PowerDesigner Application object.
        private PdCommon.Application _app;

        // A reference to the PowerDesigner logger object.
        private PdLogger _logger;

        const string MENU_REVERSE_ENGINEER = "Reverse";
        const string MENU_OBJECT = "Object";

        const string REVERSE_ENGINEER_ODATA_METADATA_COMMAND = "ReverseEngineerFromOdataMetadata";
        const string REVERSE_ENGINEER_ODATA_METADATA_CAPTION = "OData...";

        const string UPDATE_MODEL_FROM_ODATA_METADATA_COMMAND = "UpdateModelFromOdataMetadata";
        const string UPDATE_MODEL_FROM_ODATA_METADATA_CAPTION = "Update model from OData metadata";

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
                this._logger = new PdLogger(this._app);
                _logger.Info("Succesfully initialized OData AddIn");
            }
            catch (Exception e)
            {
                // Write exception in the PowerDesigner output window.
                _logger.Error(String.Format("An exception occurred during intitialize of OData AddIn: {0}", e.Message));
            }
        }

        /// <summary>
        /// From: https://help.sap.com/docs/SAP_POWERDESIGNER/31c48596e34446a68956e0aa7e700a2e/c7d62f7d6e1b1014b6b5cd497c583efb.html
        /// This functions is invoked each time PowerDesigner needs to display a menu, and returns an XML text that describes the menu items to display.
        /// It is called once without an object parameter at the initialization of PowerDesigner to fill the Import and Reverse menus.
        /// When you right-click a symbol in a diagram, this method is called twice: once for the object and once for the symbol.
        /// Thus, you can create a method that is only called on graphical contextual menus.
        /// </summary>
        /// <param name="sMenu">This can be BrowserNode or Object</param>
        /// <param name="pObject"></param>
        /// <returns></returns>
        public string ProvideMenuItems(string sMenu, object pObject)
        {
            // Wrap the whole ProvideMenuItems in a generic Try-Catch since exceptions are not handled by the Com Add-In automatically. The Add-In will stop working at that point.
            try
            {
                //_logger.Debug(String.Format("ProvideMenuItems [sMenu='{0}'; pObject={1}]", sMenu, _logger.PDObjectToString(pObject)));

                // If the pObject is not set the Import or Reverse menu items are requested.
                if (pObject == null)
                {
                    // If the requested menu is the 'Reverse Engineer' menu, return the OData reverse engineer option.
                    if (sMenu.Equals(MENU_REVERSE_ENGINEER))
                    {
                        return String.Format("<Menu><Command Name=\"{0}\" Caption=\"{1}\" /></Menu>", REVERSE_ENGINEER_ODATA_METADATA_COMMAND, REVERSE_ENGINEER_ODATA_METADATA_CAPTION);
                    }
                }
                // If the pObject is set we provide menu items based on the object type.
                else if(sMenu.Equals(MENU_OBJECT))
                {
                    // Get the class kind of the current object.
                    int pObjectClassKind = ((PdCommon.BaseObject)pObject).ClassKind;

                    // If the current object is a PDM model, return the menu option.
                    if (pObjectClassKind == (int)PdPDM.PdPDM_Classes.cls_Model)
                    {
                        // The update is only possible if the model has a OData Metadata file.
                        if (PdODataModelUpdater.HasODataMetadataFile((PdPDM.Model)pObject))
                        {
                            return String.Format("<Menu><Command Name=\"{0}\" Caption=\"{1}\" /></Menu>", UPDATE_MODEL_FROM_ODATA_METADATA_COMMAND, UPDATE_MODEL_FROM_ODATA_METADATA_CAPTION);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // When an Exception is thrown in C# via ActiveX it is not handled, so this way we log an error in the PowerDesigner Script output.
                _logger.Error(string.Format("Exception was thrown during ProvideMenuItems [sMenu='{0}'; pObject={1}]: {2}", sMenu, _logger.PDObjectToString(pObject), e.Message));
            }

            // If nothing has return yet, return an empty string.
            return "";
        }

        /// <summary>
        /// From: https://help.sap.com/docs/SAP_POWERDESIGNER/31c48596e34446a68956e0aa7e700a2e/c7d62f7d6e1b1014b6b5cd497c583efb.html
        /// This function allows you to dynamically disable commands defined in a menu. The method must return true to enable a command and false to disable it.
        /// </summary>
        /// <param name="sMenu"></param>
        /// <param name="pObject"></param>
        /// <param name="sCommandName"></param>
        /// <returns></returns>
        public int IsCommandSupported(string sMenu, object pObject, string sCommandName)
        {
            // Wrap the whole IsCommandSupported in a generic Try-Catch since exceptions are not handled by the Com Add-In automatically. The Add-In will stop working at that point.
            try
            {
                //_logger.Debug(String.Format("IsCommandSupported [sMenu='{0}'; pObject={1}; sCommandName='{2}']", sMenu, _logger.PDObjectToString(pObject), sCommandName));

                // If the object is null it is either for the Import or Reverse menu.
                if (pObject == null)
                {
                    // Check whether the request command is reverse engineering OData.
                    if (sMenu.Equals(MENU_REVERSE_ENGINEER) && sCommandName.Equals(REVERSE_ENGINEER_ODATA_METADATA_COMMAND))
                    {
                        return 1;
                    }
                }
                else if (sMenu.Equals(MENU_OBJECT))
                {
                    // Get the class kind of the current object.
                    int pObjectClassKind = ((PdCommon.BaseObject)pObject).ClassKind;

                    // If the current object is a PDM model, return the menu option.
                    if (pObjectClassKind == (int)PdPDM.PdPDM_Classes.cls_Model && sCommandName.Equals(UPDATE_MODEL_FROM_ODATA_METADATA_COMMAND))
                    {
                        // The update is only possible if the model has a OData Metadata file.
                        if (PdODataModelUpdater.HasODataMetadataFile((PdPDM.Model)pObject)) {
                            return 1;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // When an Exception is thrown in C# via ActiveX it is not handled, so this way we log an error in the PowerDesigner Script output.
                _logger.Error(string.Format("Exception was thrown during IsCommandSupported [sMenu='{0}'; pObject={1}; sCommandName='{2}']: {3}", sMenu, _logger.PDObjectToString(pObject), sCommandName, e.Message));
            }

            // If nothing has return yet, return an false.
            return 0;
        }

        /// <summary>
        /// From: https://help.sap.com/docs/SAP_POWERDESIGNER/31c48596e34446a68956e0aa7e700a2e/c7d62f7d6e1b1014b6b5cd497c583efb.html
        /// This function implements the execution of a command designated by its name.
        /// </summary>
        /// <param name="sMenu"></param>
        /// <param name="pObject"></param>
        /// <param name="sCommandName"></param>
        public void DoCommand(string sMenu, object pObject, string sCommandName)
        {
            // Wrap the whole DoCommand in a generic Try-Catch since exceptions are not handled by the Com Add-In automatically. The Add-In will stop working at that point.
            try
            {
                _logger.Debug(String.Format("DoCommand [sMenu='{0}'; pObject={1}; sCommandName='{2}']", sMenu, _logger.PDObjectToString(pObject), sCommandName));

                // If the pObject is null it is either and Import or Reverse command.
                if (pObject == null)
                {
                    // Check whether the request command is reverse engineering OData.
                    if (sMenu.Equals(MENU_REVERSE_ENGINEER) && sCommandName.Equals(REVERSE_ENGINEER_ODATA_METADATA_COMMAND))
                    {
                        _logger.Debug("Reverse engineer OData command is invoked...");

                        // Get a pointer to the PowerDesigner main window so we can show message boxes on top of it.
                        NativeWindow pdWindow = NativeWindow.FromHandle((IntPtr)this._app.MainWindowHandle);

                        // Show the Pd OData Reverse form.
                        new PdODataReversePropertiesForm(new PdODataModelUpdater(this._logger, this._app)).Show(pdWindow);
                    }
                }
                // All other commands are pObject specific, and thus not null.
                else if (sMenu.Equals(MENU_OBJECT))
                {
                    // Get the class kind of the current object.
                    int pObjectClassKind = ((PdCommon.BaseObject)pObject).ClassKind;

                    // If the current object is a PDM model, return the menu option.
                    if (pObjectClassKind == (int)PdPDM.PdPDM_Classes.cls_Model && sCommandName.Equals(UPDATE_MODEL_FROM_ODATA_METADATA_COMMAND))
                    {
                        // Update the model here...
                        PdPDM.Model pdmModel = (PdPDM.Model)pObject;

                        // Update the current model from the Uri from the file reference.
                        new PdODataModelUpdater(this._logger, this._app).UpdatePdmModel(pdmModel);
                    }
                }

            }
            catch (Exception e)
            {
                // When an Exception is thrown in C# via ActiveX it is not handled, so this way we log an error in the PowerDesigner Script output.
                _logger.Error(string.Format("Exception was thrown during DoCommand [sMenu='{0}'; pObject={1}; sCommandName='{2}']: {3}", sMenu, _logger.PDObjectToString(pObject), sCommandName, e.Message));
            }
        }

        /// <summary>
        /// From: https://help.sap.com/docs/SAP_POWERDESIGNER/31c48596e34446a68956e0aa7e700a2e/c7d62f7d6e1b1014b6b5cd497c583efb.html
        /// The Uninitialize() method is called when PowerDesigner is closed to release all global variables and clean all references to PowerDesigner objects.
        /// </summary>
        public void Uninitialize()
        {
            _logger.Debug("Uninitialize []");
            // Unset the reference to the PowerDesigner Application object.
            // Not sure if this is needed, but just to be sure it can't be used anymore.
            this._app = null;
            this._logger = null;
        }
    }
}
