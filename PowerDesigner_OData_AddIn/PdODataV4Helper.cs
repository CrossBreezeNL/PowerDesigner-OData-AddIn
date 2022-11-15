using System;
using System.Collections.Generic;
using System.Xml;

using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Validation;

namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData
{
    public static class PdODataV4Helper
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
            if (CsdlReader.TryParse(oDataMetadataXmlReader, out IEdmModel model, out IEnumerable<EdmError> errors))
            {
                // Loop through the schema elements in the model.
                //_logger.Debug(string.Format("EntityContainer[Name={0}; Location={1}]", model.EntityContainer.Name, model.EntityContainer.Location()));
                // Loop through the schema elements in the model.
                foreach (IEdmEntityContainerElement entityContainerElement in model.EntityContainer.Elements)
                {
                    logger.Debug(string.Format(" EntityContainerElement[ContainerElementKind={0}; Name={1}; Location={2}]", Enum.GetName(typeof(EdmContainerElementKind), entityContainerElement.ContainerElementKind), entityContainerElement.Name, entityContainerElement.Location()));
                    if (entityContainerElement.ContainerElementKind.Equals(EdmContainerElementKind.EntitySet))
                    {
                        IEdmEntitySet edmEntitySet = (IEdmEntitySet)entityContainerElement;
                        logger.Debug(string.Format(" =EntitySet[ContainerElementKind={0}; Name={1}]", Enum.GetName(typeof(EdmContainerElementKind), edmEntitySet.ContainerElementKind), edmEntitySet.Name));
                        IEdmEntityType entityType = edmEntitySet.EntityType();
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
                                logger.Debug(string.Format(" -Property[Name={0}; PropertyKind={1}; Type={2}]", edmProperty.Name, Enum.GetName(typeof(EdmPropertyKind), edmProperty.PropertyKind), edmProperty.Type.FullName()));

                                // Only add columns for properties which are structural.
                                if (edmProperty.PropertyKind.Equals(EdmPropertyKind.Structural))
                                {
                                    // Create a new columns for the property.
                                    PdPDM.Column pdmColumn = (PdPDM.Column)pdmTable.Columns.CreateNew();
                                    pdmColumn.Name = edmProperty.Name;
                                    pdmColumn.SetNameToCode();

                                    // If the type is set, update the column.
                                    if (edmProperty.Type != null)
                                    {
                                        SetColumnType(pdmColumn, edmProperty.Type);
                                    }

                                    // Add the new columns to the columns collection.
                                    pdmTable.Columns.Add(pdmColumn);
                                }

                            }
                            // Add the new table to the tables collection.
                            pdmModel.Tables.Add(pdmTable);
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

        /// <summary>
        /// Function to get the Sql data type based on a Edm primitive kind.
        /// Used: https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/ef/sqlclient-for-ef-types
        /// </summary>
        /// <param name="edmPrimitiveType"></param>
        /// <returns></returns>
        public static void SetColumnType(PdPDM.Column pdmColumn, IEdmTypeReference edmType)
        {
            // Set the Mandatory property as the inverse of IsNullable.
            pdmColumn.Mandatory = !edmType.IsNullable;

            // Translate the DataType if the edm type is a primitive.
            if (edmType.IsPrimitive())
            {
                switch (edmType.PrimitiveKind())
                {
                    case EdmPrimitiveTypeKind.Binary:
                        pdmColumn.DataType = "varbinary";
                        IEdmBinaryTypeReference edmBinaryType = edmType.AsBinary();
                        // Set the length, if known.
                        if (edmBinaryType.MaxLength.HasValue)
                            pdmColumn.Length = edmBinaryType.MaxLength.Value;
                        break;

                    case EdmPrimitiveTypeKind.Boolean:
                        pdmColumn.DataType = "bit";
                        break;

                    case EdmPrimitiveTypeKind.Byte:
                        pdmColumn.DataType = "tinyint";
                        break;

                    case EdmPrimitiveTypeKind.DateTimeOffset:
                        pdmColumn.DataType = "datetimeoffset";
                        break;

                    case EdmPrimitiveTypeKind.Date:
                        pdmColumn.DataType = "date";
                        break;

                    case EdmPrimitiveTypeKind.Decimal:
                        pdmColumn.DataType = "decimal";
                        // If the precision is set, copy it.
                        if (edmType.AsDecimal().Precision.HasValue)
                            pdmColumn.Precision = (short)edmType.AsDecimal().Precision.Value;
                        // If the scale is set, copy it.
                        if (edmType.AsDecimal().Scale.HasValue)
                            pdmColumn.Length = edmType.AsDecimal().Scale.Value;
                        break;

                    case EdmPrimitiveTypeKind.Double:
                        pdmColumn.DataType = "float";
                        break;

                    case EdmPrimitiveTypeKind.Guid:
                        pdmColumn.DataType = "uniqueidentifier";
                        break;

                    case EdmPrimitiveTypeKind.Int16:
                        pdmColumn.DataType = "smallint";
                        break;

                    case EdmPrimitiveTypeKind.Int32:
                        pdmColumn.DataType = "int";
                        break;

                    case EdmPrimitiveTypeKind.Int64:
                        pdmColumn.DataType = "bigint";
                        break;

                    case EdmPrimitiveTypeKind.SByte:
                        pdmColumn.DataType = "tinyint";
                        break;

                    case EdmPrimitiveTypeKind.String:
                        pdmColumn.DataType = "nvarchar";
                        IEdmStringTypeReference stringType = edmType.AsString();
                        // Set the length, if known.
                        if (stringType.MaxLength.HasValue)
                            pdmColumn.Length = stringType.MaxLength.Value;
                        break;

                    case EdmPrimitiveTypeKind.TimeOfDay:
                        pdmColumn.DataType = "time";
                        break;
                }
            }
        }
    }
}
