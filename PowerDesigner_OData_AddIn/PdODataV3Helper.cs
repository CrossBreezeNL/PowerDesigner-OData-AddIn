using System;
using System.Collections.Generic;
using System.Xml;

using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Csdl;
using Microsoft.Data.Edm.Validation;


namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData
{
    public static class PdODataV3Helper
    {
        /// <summary>
        /// Create the PDM objects based on the metadata found in the V4 OData metadata.
        /// </summary>
        /// <param name="oDataMetadataXmlReader">The XmlReader on the OData metadata feed.</param>
        /// <param name="pdmModel">The PDM model to create the objects in.</param>
        /// <param name="logger">The PdLogger to log messages to.</param>
        public static void CreatePdmObjects(XmlReader oDataMetadataXmlReader, PdPDM.Model pdmModel, PdLogger logger)
        {
            // Try to parse the uri to a edm model.
            if (EdmxReader.TryParse(oDataMetadataXmlReader, out IEdmModel model, out IEnumerable<EdmError> errors))
            {
                // Loop through the schema elements in the model.
                //_logger.Debug(string.Format("EntityContainer[Name={0}; Location={1}]", model.EntityContainer.Name, model.EntityContainer.Location()));
                // Loop through the schema elements in the model.
                foreach (IEdmEntityContainer entityContainer in model.EntityContainers())
                {
                    foreach (IEdmEntityContainerElement entityContainerElement in entityContainer.Elements)
                    {
                        logger.Debug(string.Format(" EntityContainerElement[ContainerElementKind={0}; Name={1}; Location={2}]", Enum.GetName(typeof(EdmContainerElementKind), entityContainerElement.ContainerElementKind), entityContainerElement.Name, entityContainerElement.Location()));
                        if (entityContainerElement.ContainerElementKind.Equals(EdmContainerElementKind.EntitySet))
                        {
                            IEdmEntitySet edmEntitySet = (IEdmEntitySet)entityContainerElement;
                            logger.Debug(string.Format(" =EntitySet[ContainerElementKind={0}; Name={1}]", Enum.GetName(typeof(EdmContainerElementKind), edmEntitySet.ContainerElementKind), edmEntitySet.Name));
                            IEdmEntityType entityType = edmEntitySet.ElementType;
                            logger.Debug(string.Format(" =EntityType[Name={0}; IsAbstract={1}]", entityType.Name, entityType.IsAbstract));

                            // If the entity type is not abstract, create a table for it.
                            if (!entityType.IsAbstract)
                            {
                                // Create a new PDM table object for the EntityType.
                                PdPDM.Table pdmTable = (PdPDM.Table)pdmModel.Tables.CreateNew();
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

                                    logger.Debug(string.Format(" -Property[Name={0}; PropertyKind={1}; Type={2}]", edmProperty.Name, Enum.GetName(typeof(EdmPropertyKind), edmProperty.PropertyKind), edmProperty.Type.FullName()));
                                }
                                // Add the new table to the tables collection.
                                pdmModel.Tables.Add(pdmTable);
                            }
                        }
                    }
                }
            }
            // If an error occurred while trying to parse the edm model, log the error(s).
            else
            {
                logger.Error("Errors occured while parsing the OData metadata:");
                foreach (EdmError edmError in errors)
                {
                    logger.Error(string.Format(" [{0}] {1} @{2}", edmError.ErrorCode, edmError.ErrorMessage, edmError.ErrorLocation));
                }
            }
        }
    }
}
