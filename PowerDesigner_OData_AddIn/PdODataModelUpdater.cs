using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Validation;

namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData
{
    public class PdODataModelUpdater
    {
        // Name of the metadata file.
        const string ODATA_METADATA_FILE_NAME = "OData Metadata";
        const string ODATA_METADATA_FILE_CODE = "ODATA_METADATA";

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
        /// <param name="pdmModelName"></param>
        /// <param name="oDataMetadataUri"></param>
        /// <returns></returns>
        public PdPDM.Model CreatePdmModelFromODataMetadata(string pdmModelName, string oDataMetadataUri, bool hiddenModel = true)
        {
            _logger.Info(string.Format("CreatePdmModelFromODataMetadata[pdmModelName={0}; oDataMetadataUri={1}; hiddenModel={2}]", pdmModelName, oDataMetadataUri, hiddenModel));

            // Set protocol to Tls 1.2 to solve SSL/TLS error.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Derive on how to open the model, it's hidden by default.
            PdCommon.OpenModelFlags openFlags = PdCommon.OpenModelFlags.omf_Default;
            if (hiddenModel)
                openFlags = PdCommon.OpenModelFlags.omf_Hidden;

            // Create a new PDM model.
            PdPDM.Model oImportDataModel = (PdPDM.Model)this._app.CreateModel(((int)PdPDM.PdPDM_Classes.cls_Model), "", openFlags);
            // Set the name of the model.
            oImportDataModel.Name = pdmModelName;
            oImportDataModel.SetNameToCode();

            // Store the metadata uri in a file object in the model.
            PdCommon.FileObject metaDataFile = (PdCommon.FileObject)oImportDataModel.Files.CreateNew();
            metaDataFile.SetNameAndCode(ODATA_METADATA_FILE_NAME, ODATA_METADATA_FILE_CODE);
            // Set the file type to URI.
            metaDataFile.LocationType = 2;
            // Set the URI value.
            metaDataFile.Location = oDataMetadataUri;
            // Disable generation of the file.
            metaDataFile.Generated = false;
            // Add the file to the model.
            oImportDataModel.Files.Add((PdPDM.BaseObject)metaDataFile);

            // Create an xml reader on the metadata uri.
            using (XmlReader oDataMetadataXmlReader = XmlReader.Create(oDataMetadataUri))
            {
                // Try to parse the uri to a edm model.
                if (CsdlReader.TryParse(oDataMetadataXmlReader, out IEdmModel model, out IEnumerable<EdmError> errors))
                {
                    // Loop through the schema elements in the model.
                    //_logger.Debug(string.Format("EntityContainer[Name={0}; Location={1}]", model.EntityContainer.Name, model.EntityContainer.Location()));
                    // Loop through the schema elements in the model.
                    foreach (IEdmEntityContainerElement entityContainerElement in model.EntityContainer.Elements)
                    {
                        _logger.Debug(string.Format(" EntityContainerElement[ContainerElementKind={0}; Name={1}; Location={2}]", Enum.GetName(typeof(EdmContainerElementKind), entityContainerElement.ContainerElementKind), entityContainerElement.Name, entityContainerElement.Location()));
                        if (entityContainerElement.ContainerElementKind.Equals(EdmContainerElementKind.EntitySet))
                        {
                            IEdmEntitySet edmEntitySet = (IEdmEntitySet)entityContainerElement;
                            _logger.Debug(string.Format(" =EntitySet[ContainerElementKind={0}; Name={1}]", Enum.GetName(typeof(EdmContainerElementKind), edmEntitySet.ContainerElementKind), edmEntitySet.Name));
                            IEdmEntityType entityType = edmEntitySet.EntityType();
                            _logger.Debug(string.Format(" =EntityType[Name={0}; IsAbstract={1}]", entityType.Name, entityType.IsAbstract));

                            // If the entity type is not abstract, create a table for it.
                            if (!entityType.IsAbstract)
                            {
                                // Create a new PDM table object for the EntityType.
                                PdPDM.Table pdmTable = (PdPDM.Table)oImportDataModel.Tables.CreateNew();
                                pdmTable.Name = entityType.Name;
                                pdmTable.SetNameToCode();

                                // Loop overthe declared properties.
                                foreach (IEdmProperty edmProperty in entityType.DeclaredProperties)
                                {
                                    // Create a new columns for the property.
                                    PdPDM.Column pdmColumn = (PdPDM.Column)pdmTable.Columns.CreateNew();
                                    pdmColumn.Name = edmProperty.Name;
                                    pdmColumn.SetNameToCode();
                                    // Add the new columns to the columns collection.
                                    pdmTable.Columns.Add(pdmColumn);

                                    _logger.Debug(string.Format(" -Property[Name={0}; PropertyKind={1}; Type={2}]", edmProperty.Name, Enum.GetName(typeof(EdmPropertyKind), edmProperty.PropertyKind), edmProperty.Type.FullName()));
                                }
                                // Add the new table to the tables collection.
                                oImportDataModel.Tables.Add(pdmTable);
                            }
                        }
                    }
                }
                // If an error occurred while trying to parse the edm model, log the error(s).
                else
                {
                    _logger.Error("Errors occured while parsing the OData metadata:");
                    foreach (EdmError edmError in errors)
                    {
                        _logger.Error(string.Format(" [{0}] {1} @{2}", edmError.ErrorCode, edmError.ErrorMessage, edmError.ErrorLocation));
                    }
                }

            }

            return oImportDataModel;
        }

        /// <summary>
        /// Update a Pdm Model from a OData $metadata Uri.
        /// </summary>
        /// <param name="pdmModel"></param>
        public void UpdatePdmModel(PdPDM.Model pdmModel, string oDataMetadataUri)
        {
            _logger.Info(string.Format("Updating the model '{0}' from OData metadata", pdmModel.DisplayName));

            // Create an in-memory model based on the OData metadata.
            PdPDM.Model oImportDataModel = CreatePdmModelFromODataMetadata(pdmModel.Name, oDataMetadataUri);

            // Merge the created OData import data model into the target pdm model.
            _logger.Debug("Merging the OData metadata changes into the PDM model");
            pdmModel.Merge(oImportDataModel, ((int)PdCommon.MergeActions.mrg_allchanges - (int)PdCommon.MergeActions.mrg_deleted - (int)PdCommon.MergeActions.mrg_remove));

            // Delete the the temporary model.
            oImportDataModel.delete();
        }
    }
}
