using System;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Windows.Forms;
using CrossBreeze.Tools.PowerDesigner.AddIn.OData.Forms;
using PdCommon;

namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData
{
    public class PdODataModelUpdater
    {
        // Name of the metadata file.
        const string ODATA_METADATA_FILE_NAME = "OData Metadata";
        const string ODATA_METADATA_FILE_CODE = "ODATA_METADATA";

        /// <summary>
        /// A list of supported OData authentication methods.
        /// </summary>
        public enum ODataAutenticationType
        {
            None,
            Basic
        }

        // A reference to the PowerDesigner logger object.
        private PdLogger _logger;

        // A reference to the PowerDesigner Application object.
        private PdCommon.Application _app;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger">PdLogger reference to log in PowerDesigner.</param>
        public PdODataModelUpdater(PdLogger logger, PdCommon.Application app)
        {
            this._logger = logger;
            this._app = app;
        }

        /// <summary>
        /// Function to retrieve the OData Metadata file from a PDM model (if it exists, otherwise null).
        /// </summary>
        /// <param name="pdmModel">The PDM model to check</param>
        /// <returns></returns>
        public static PdCommon.FileObject GetODataMetadataFile(PdPDM.Model pdmModel)
        {
            var oDataMetadataFile = pdmModel.FindChildByCode(PdODataModelUpdater.ODATA_METADATA_FILE_CODE, (int)PdCommon.PdCommon_Classes.cls_FileObject);
            if (oDataMetadataFile == null)
            {
                return null;
            }
            return (PdCommon.FileObject)oDataMetadataFile;
        }

        /// <summary>
        /// Method to chech whether a PDM model has a OData Metadata file.
        /// </summary>
        /// <param name="pdmModel"></param>
        /// <returns></returns>
        public static bool HasODataMetadataFile(PdPDM.Model pdmModel)
        {
            return GetODataMetadataFile(pdmModel) != null;
        }

        /// <summary>
        /// Create a new PdPDM model with the passed name based on the oData metadata feed.
        /// </summary>
        /// <param name="pdmModelName">The name of the PDM model to create.</param>
        /// <param name="oDataMetadataUri">The URI of the OData metadata feed.</param>
        /// <param name="oDataAuthentication">The authentication method to use.</param>
        /// <param name="hiddenModel">Whether the model should be created as hidden.</param>
        /// <returns></returns>
        public void UpdatePdmModelFromODataMetadata(PdPDM.Model oImportDataModel, string oDataMetadataUri, ODataAutenticationType oDataAuthentication)
        {
            _logger.Info(string.Format("UpdatePdmModelFromODataMetadata[pdmModel.Name={0}; oDataMetadataUri={1}; oDataAuthentication={2}]", oImportDataModel.Name, oDataMetadataUri, oDataAuthentication));

            // Store the metadata uri in a file object in the model.
            PdCommon.FileObject metaDataFile = (PdCommon.FileObject)oImportDataModel.Files.CreateNew();
            metaDataFile.SetNameAndCode(ODATA_METADATA_FILE_NAME, ODATA_METADATA_FILE_CODE);
            // Set the file type to URI.
            metaDataFile.LocationType = 2;
            // Set the URI value.
            metaDataFile.Location = oDataMetadataUri;
            // Disable generation of the file.
            metaDataFile.Generated = false;
            // Set the following based on authentication setting from dialog box.
            metaDataFile.Comment = Enum.GetName(typeof(ODataAutenticationType), oDataAuthentication);
            // Add the file to the model.
            oImportDataModel.Files.Add((PdPDM.BaseObject)metaDataFile);

            // Set protocol to Tls 1.2 to solve SSL/TLS error.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Create a web request to download the OData metadata.
            WebRequest metadataRequest = WebRequest.Create(oDataMetadataUri);
            // Set the credentials to use.
            if (oDataAuthentication.Equals(ODataAutenticationType.Basic)) {
                using (PdODataBasicAuthenticationForm authForm = new PdODataBasicAuthenticationForm())
                {
                    // Open the authentication form with the PowerDesigner windows being the owner.
                    // If the result of the dialog was OK, we can fetch the authentication information.
                    if (authForm.ShowDialog(PdAppHelper.GetPDWindow(this._app)) == DialogResult.OK) {
                        metadataRequest.Credentials = new NetworkCredential(authForm.Username, authForm.Password);
                        _logger.Debug("Succesfully set authentication information for web-request.");
                    }
                    // If in the dialog the cancel button was pressed, we stop the reverse engineering process.
                    else
                    {
                        _logger.Info("The authentication dialog was cancelled, so stopping reverse engineering.");
                    }
                }
            }
            // The only other authentication method is NONE, for which we don't have to do anything.

            // Get the response from the metadata request.
            using (WebResponse metadataResponse = metadataRequest.GetResponse())
            {
                // Load the OData metadata into an XDocument.
                XDocument oDataMetadataDocument = XDocument.Load(metadataResponse.GetResponseStream());

                // Move to the content, so we can read the root element information.
                //_logger.Debug(string.Format("XML root element: LocalName='{0}';NamespaceURI='{1}';HasAttributes={2}", oDataMetadataDocument.Root.Name.LocalName, oDataMetadataDocument.Root.Name.NamespaceName, oDataMetadataDocument.Root.HasAttributes));

                // The root element local name must be Edmx, if now, throw an exception.
                if (!oDataMetadataDocument.Root.Name.LocalName.Equals("Edmx", StringComparison.CurrentCultureIgnoreCase))
                    throw new PdODataException(string.Format("The document at '{0}' doesn't have a Edmx root element, which is required for OData $metadata! The root element is '{1}'.", oDataMetadataUri, oDataMetadataDocument.Root.Name.LocalName));

                // If the root element local-name is Edmx we take the namespace of that element.
                string edmxNamespaceUri = oDataMetadataDocument.Root.Name.NamespaceName;

                // Find the version attribute on the root element.
                XAttribute versionAttribute = oDataMetadataDocument.Root.Attribute(XName.Get("Version"));
                if (versionAttribute == null)
                    throw new PdODataException("The root Edmx element doesn't have the required 'Version' attribute!");

                // Get the Edmx Version of the document.
                string oDataVersionString = versionAttribute.Value;

                // If the version string is empty, the OData feed is invalid.
                if (oDataVersionString == null)
                    throw new PdODataException("The root Edmx element doesn't have a value for the 'Version' attribute!");

                // Log which version was retrieved from the OData feed.
                _logger.Info(string.Format("Succesfully read OData version from feed (Version='{0}').", oDataVersionString));

                // Try to parse the retrieved version number to an int.
                double oDataVersion;
                if (!Double.TryParse(oDataVersionString, out oDataVersion))
                    throw new PdODataException(string.Format("The OData metadata version format is not supported! The retrieved format is '{0}', expected a numeric value between 1.0 and 4.0.", oDataVersionString));

                // Create an xml reader on the metadata uri.
                using (XmlReader oDataMetadataXmlReader = oDataMetadataDocument.CreateReader())
                {
                    // For the V4 OData Metadata we use the Microsoft.OData.Edm package.
                    if (((int)oDataVersion) == 4)
                    {
                        // Create the pdm objects based on the OData v4 feed.
                        PdODataV4Helper.CreatePdmObjects(oDataMetadataXmlReader, oImportDataModel, this._logger);
                    }
                    // For the V1 - V3 OData Metadata we use the Microsoft.Data.Edm package.
                    else if ((int)oDataVersion >= 1 && (int)oDataVersion <= 3)
                    {
                        // Create the pdm objects based on the OData v1-3 feed.
                        PdODataV3Helper.CreatePdmObjects(oDataMetadataXmlReader, oImportDataModel, this._logger);
                    }
                    else
                    {
                        throw new PdODataException(string.Format("The OData metadata version is not supported! The retrieved version is {0}, expected a value between 1 and 4.", oDataVersion));
                    }
                }
            }

            // Update the all diagrams in the model.
            PdHelper.UpdateDiagramRecursively(oImportDataModel);
        }

        /// <summary>
        /// Create a Pdm Model using a form where the user specifies the information needed.
        /// </summary>
        public void CreatePdmModel()
        {
            // Open a form where the user can entier the name, URI and authentication type for the OData service.
            using (PdODataReversePropertiesForm reverseEngineerForm = new PdODataReversePropertiesForm())
            {
                if (reverseEngineerForm.ShowDialog(PdAppHelper.GetPDWindow(this._app)) == DialogResult.OK)
                {
                    // Create a new PDM model.
                    PdPDM.Model oImportDataModel = (PdPDM.Model)this._app.CreateModel(((int)PdPDM.PdPDM_Classes.cls_Model));
                    // Set base model options to be case senstive and mixed case.
                    PdPDM.ModelOptions modelOptions = (PdPDM.ModelOptions)oImportDataModel.GetModelOptions();
                    modelOptions.NameCodeCaseSensitive = true;
                    // Set all Code naming conventions character case to Mixed (M).
                    foreach (PdCommon.NamingConvention namingConvention in modelOptions.CodeNamingConventions)
                    {
                        namingConvention.CharacterCase = "M";
                    }
                    // Save the model options (otherwise it's not persisted).
                    modelOptions.Save();
                    // Set the name of the model.
                    oImportDataModel.Name = reverseEngineerForm.ModelName;
                    oImportDataModel.SetNameToCode();

                    // Create the PDM model.
                    UpdatePdmModelFromODataMetadata(oImportDataModel, reverseEngineerForm.ODataMetadataUri, reverseEngineerForm.AuthenticationType);
                }
                else
                {
                    // If the result is not OK, stop the process.
                    _logger.Debug("The reverse engineer form was cancelled, so stopping.");
                }
            }
        }

        /// <summary>
        /// Update a Pdm Model from a OData $metadata Uri.
        /// </summary>
        /// <param name="pdmModel"></param>
        public void UpdatePdmModel(PdPDM.Model pdmModel)
        {
            _logger.Info(string.Format("Updating the model '{0}' from OData metadata", pdmModel.DisplayName));

            PdCommon.FileObject oDataMetadataFile = PdODataModelUpdater.GetODataMetadataFile(pdmModel);
            if (oDataMetadataFile == null)
            {
                _logger.Error(string.Format("The model {0} doesn't have a OData metadata file!", pdmModel.DisplayName));
                return;
            }

            ODataAutenticationType oDataAuthType;
            // If the Comment is empty, no authentication is set, so we default to NoAuthentication.
            if (oDataMetadataFile.Comment.Length == 0)
            {
                oDataAuthType = ODataAutenticationType.None;
            }
            // If there is a value in Comment, parse it into a ODataAutenticationType object.
            else if (!Enum.TryParse(oDataMetadataFile.Comment, out oDataAuthType))
            {
                _logger.Error(string.Format("The model {0} OData metadata file doesn't contain a valid authentication type in the Comment field!", pdmModel.DisplayName));
                return;
            }

            // Create an in-memory model based on the existing PDM model.
            PdPDM.Model oImportDataModel = (PdPDM.Model)this._app.CreateModel(((int)PdPDM.PdPDM_Classes.cls_Model), "", OpenModelFlags.omf_Hidden);
            // Set the name of the model.
            oImportDataModel.Name = pdmModel.Name;
            oImportDataModel.SetNameToCode();
            // Copy the model options from the existing model to the new model.
            oImportDataModel.ModelOptionsText = pdmModel.ModelOptionsText;
            // Update the new model from the metadata feed.
            UpdatePdmModelFromODataMetadata(oImportDataModel, oDataMetadataFile.Location, oDataAuthType);

            // Merge the created OData import data model into the target pdm model.
            _logger.Debug("Merging the OData metadata changes into the PDM model");
            if (!pdmModel.Merge(oImportDataModel, ((int)PdCommon.MergeActions.mrg_allchanges - (int)PdCommon.MergeActions.mrg_deleted - (int)PdCommon.MergeActions.mrg_remove)))
            {
                _logger.Error("The merge was aborted!");
            } else
            {
                _logger.Info("The merge is completed succesfully");
            }

            // Delete the the temporary model.
            oImportDataModel.delete();
        }
    }
}
