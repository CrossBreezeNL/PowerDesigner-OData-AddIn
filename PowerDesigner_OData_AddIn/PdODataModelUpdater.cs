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
        // A reference to the PowerDesigner logger object.
        private PdLogger _logger;

        // Store the reference to the OData metadata Uri.
        // List of public OData APIs: https://pragmatiqa.com/xodata/odatadir.html
        private string _odataMetadataUri = "https://denhaag.incijfers.nl/jiveservices/odata/$metadata";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger">PdLogger reference to log in PowerDesigner.</param>
        public PdODataModelUpdater(PdLogger logger)
        {
            this._logger = logger;
        }

        /// <summary>
        /// Update a Pdm Model from a OData $metadata Uri.
        /// </summary>
        /// <param name="pdmModel"></param>
        public void UpdatePdmModel(PdPDM.Model pdmModel)
        {
            _logger.Info(string.Format("Updating the model '{0}' from OData metadata", pdmModel.DisplayName));

            // Set protocol to Tls 1.2 to solve SSL/TLS error.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Create an xml reader on the metadata uri.
            using (XmlReader oDataMetadataXmlReader = XmlReader.Create(_odataMetadataUri))
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

                            // Loop overthe declared properties.
                            foreach (IEdmProperty edmProperty in entityType.DeclaredProperties)
                            {
                                _logger.Debug(string.Format(" -Property[Name={0}; PropertyKind={1}; Type={2}]", edmProperty.Name, Enum.GetName(typeof(EdmPropertyKind), edmProperty.PropertyKind), edmProperty.Type.FullName()));
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
        }
    }
}
