using System;
using System.Collections.Generic;
using System.Linq;
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

                // Create a list of type definitions.
                IEnumerable<IEdmSchemaType> schemaTypeDefinitions = model.SchemaElements.Where(schemaElement => schemaElement.SchemaElementKind.Equals(EdmSchemaElementKind.TypeDefinition)).Cast<IEdmSchemaType>();
                // Create the Domains based on the enum types.
                logger.Debug("Adding domains from enums:");
                foreach (IEdmEnumType enumType in schemaTypeDefinitions.Where(typeDefinition => typeDefinition.TypeKind.Equals(EdmTypeKind.Enum)))
                {
                    logger.Debug(string.Format(" Enum[Name={0}; Namespace={1}]", enumType.Name, enumType.Namespace));

                    // Create a new PDM domain object for the Enum.
                    PdPDM.PhysicalDomain pdmEnumDomain = (PdPDM.PhysicalDomain)pdmModel.Domains.CreateNew();
                    pdmEnumDomain.Name = enumType.FullName();
                    pdmEnumDomain.SetNameToCode();
                    // An Enum type has integer values with labels, so we set the datatype to int.
                    pdmEnumDomain.DataType = "int";

                    // Create the ListOfValues of the Domain based on the Enum members.
                    // The list of values is stored in a formatted text.
                    // The format is the following: Value1 '\t' Label1 '\n' Value2 '\t' Label2 '\n' ...
                    // The Tabulation is used as a separator between the value and the value label.
                    // The separator between each line is: '\n'.
                    foreach (IEdmEnumMember enumMember in enumType.Members)
                    {
                        pdmEnumDomain.ListOfValues += string.Format("{0}\t{1}\n", enumMember.Value.Value, enumMember.Name);
                    }

                    // Add the domain to the model.
                    pdmModel.Domains.Add(pdmEnumDomain);
                }

                // Create all tables based on the Entity and Complex type definitions.
                logger.Debug("Adding tables from entity and complex types:");
                IEnumerable<IEdmSchemaType> structuredTypeElements = schemaTypeDefinitions.Where(typeDefinition => typeDefinition.TypeKind.Equals(EdmTypeKind.Entity) || typeDefinition.TypeKind.Equals(EdmTypeKind.Complex));
                foreach (IEdmSchemaType structuredTypeElement in structuredTypeElements)
                {
                    string edmTypeKindName = Enum.GetName(typeof(EdmTypeKind), structuredTypeElement.TypeKind);
                    logger.Debug(string.Format(" {0}[Name={1}; Namespace={2}]", edmTypeKindName, structuredTypeElement.Name, structuredTypeElement.Namespace));

                    // Get or create the package for the entity type.
                    PdPDM.Package pdmTypePackage = PdHelper.GetOrCreatePackage(pdmModel, structuredTypeElement.Namespace);
                    // Get or create the user for the entity type.
                    PdPDM.User pdmUser = PdHelper.GetOrCreateUser(pdmModel, structuredTypeElement.Namespace);

                    // Create a new PDM table object for the EntityType.
                    PdPDM.Table pdmTypeTable = (PdPDM.Table)pdmTypePackage.Tables.CreateNew();
                    pdmTypeTable.Name = string.Format("{0}.{1}", structuredTypeElement.Namespace, structuredTypeElement.Name);
                    pdmTypeTable.Comment = structuredTypeElement.Name;
                    pdmTypeTable.SetNameToCode();
                    // Set the stereotype to the OData EDM Type Kind.
                    pdmTypeTable.Stereotype = edmTypeKindName;
                    // Set the owner of the table.
                    pdmTypeTable.Owner = pdmUser;
                    // Disable generation of the type table.
                    pdmTypeTable.Generated = false;

                    // Add the columns to the table based on the declared properties.
                    AddColumsToTable(pdmTypeTable, (IEdmStructuredType)structuredTypeElement, logger);

                    // Add the table to the package.
                    pdmTypePackage.Tables.Add(pdmTypeTable);
                }

                // Create references between the tables.
                logger.Debug("Adding tables references:");
                foreach (IEdmSchemaType structuredTypeElement in structuredTypeElements)
                {
                    // Find the table which represents the underlying type.
                    PdPDM.Package pdmEntityTypePackage = PdHelper.GetOrCreatePackage(pdmModel, structuredTypeElement.Namespace);
                    if (pdmEntityTypePackage == null)
                    {
                        logger.Error(string.Format("The type package '{0}' was not found!", structuredTypeElement.Namespace));
                        throw new PdODataException("The type package was not found!");
                    }
                    string tableName = string.Format("{0}.{1}", structuredTypeElement.Namespace, structuredTypeElement.Name);
                    PdPDM.Table typeTable = PdHelper.GetTable(pdmEntityTypePackage, tableName);
                    if (typeTable == null)
                    {
                        logger.Error(string.Format("The type table '{0}' was not found!", tableName));
                        throw new PdODataException("The type table was not found!");
                    }

                    // Loop over the navigation properties of the schema type element.
                    foreach (IEdmNavigationProperty navProp in ((IEdmStructuredType)structuredTypeElement).NavigationProperties())
                    {
                        logger.Debug(string.Format(" -IEdmNavigationProperty[Name={0}; PropertyKind={1}]", navProp.Name, Enum.GetName(typeof(EdmPropertyKind), navProp.PropertyKind)));

                        IEdmEntityType targetedEntityType = navProp.ToEntityType();
                        // Find the table which represents the targeted type.
                        PdPDM.Package pdmTargetEntityTypePackage = PdHelper.GetOrCreatePackage(pdmModel, targetedEntityType.Namespace);
                        if (pdmTargetEntityTypePackage == null)
                        {
                            logger.Error(string.Format("The targeted type package '{0}' was not found!", targetedEntityType.Namespace));
                            throw new PdODataException("The targeted type package was not found!");
                        }
                        string targetedTableName = string.Format("{0}.{1}", targetedEntityType.Namespace, targetedEntityType.Name);
                        PdPDM.Table targetedTypeTable = PdHelper.GetTable(pdmTargetEntityTypePackage, targetedTableName);
                        if (targetedTypeTable == null)
                        {
                            logger.Error(string.Format("The targeted type table '{0}' was not found!", targetedTableName));
                            throw new PdODataException("The targeted type table was not found!");
                        }

                        logger.Debug("  -Creating table reference");

                        PdPDM.Reference tableRef = (PdPDM.Reference)((PdPDM.BasePhysicalPackage)typeTable.Package).References.CreateNew();
                        // Set the name of the reference based on the child, parent and parent role.
                        tableRef.Name = string.Format("{0} > {1} : {2}", typeTable.Name, targetedTypeTable.Name, navProp.Name);
                        tableRef.SetNameToCode();
                        // Set the parent table and role.
                        tableRef.ParentTable = targetedTypeTable;
                        tableRef.ParentRole = navProp.Name;
                        // Set the foreign key constraint name.
                        tableRef.ForeignKeyConstraintName = string.Format("FK_{0}_{1}_{2}", typeTable.Code, targetedTypeTable.Code, tableRef.ParentRole);

                        // Add the reference to the child table.
                        typeTable.OutReferences.Add(tableRef);

                        // Empty the joins list (by default it is populated.
                        tableRef.Joins.Clear();

                        // Disable generation of the reference.
                        tableRef.Generated = false;

                    }
                }


                // Create replica-tables for all entity sets.
                logger.Debug("Adding replica-tables from entity sets:");
                IEnumerable<IEdmEntitySet> edmEntitySets = model.EntityContainer.Elements.Where(entityContainerElement => entityContainerElement.ContainerElementKind.Equals(EdmContainerElementKind.EntitySet)).Cast<IEdmEntitySet>();
                foreach (IEdmEntitySet edmEntitySet in edmEntitySets)
                {
                    logger.Debug(string.Format(" EntitySet[ContainerElementKind={0}; Name={1}; Namespace={2}]", Enum.GetName(typeof(EdmContainerElementKind), edmEntitySet.ContainerElementKind), edmEntitySet.Name, edmEntitySet.Container.Namespace));

                    IEdmEntityType entityType = edmEntitySet.EntityType();
                    logger.Debug(string.Format(" =EntityType[Name={0}; IsAbstract={1}; Namespace={2}]", entityType.Name, entityType.IsAbstract, entityType.Namespace));

                    // Now a table is created for the EntityType, we create a replica-table for the EntitySet.
                    // Find the schema to entity type belongs to, so the table can be added in the right package.
                    PdPDM.Package pdmEntitySetPackage = PdHelper.GetOrCreatePackage(pdmModel, edmEntitySet.Container.Namespace);
                    // Get or create the user for the entity type.
                    PdPDM.User pdmUser = PdHelper.GetOrCreateUser(pdmModel, edmEntitySet.Container.Namespace);

                    // Find the table which represents the underlying type.
                    PdPDM.Package pdmEntityTypePackage = PdHelper.GetOrCreatePackage(pdmModel, entityType.Namespace);
                    if (pdmEntityTypePackage == null)
                    {
                        logger.Error(string.Format("The type package '{0}' was not found!", entityType.Namespace));
                        throw new PdODataException("The type package was not found!");
                    }
                    string typeTableName = string.Format("{0}.{1}", entityType.Namespace, entityType.Name);
                    PdPDM.Table typeTable = PdHelper.GetTable(pdmEntityTypePackage, typeTableName);
                    if (typeTable == null)
                    {
                        logger.Error(string.Format("The type table '{0}' was not found!", typeTableName));
                        throw new PdODataException("The type table was not found!");
                    }

                    // Make the new table a replica of the type table.
                    // For docs on replica, read: https://help.sap.com/docs/SAP_POWERDESIGNER/abd3434b4987485c92057ab9392aadbe/c7e0de496e1b1014aa2ee40d2fca2b30.html?locale=en-US&version=16.6.6
                    logger.Debug("  - Creating replicated table for entity set");
                    PdPDM.Table pdmTable = (PdPDM.Table)typeTable.CreateReplica(pdmEntitySetPackage);

                    // Get the replica object.
                    logger.Debug("  - Setting replication settings");
                    PdCommon.BaseReplication baseReplication = (PdCommon.BaseReplication)pdmTable.SourceReplication;
                    // BaseReplication.SetAttributeReplicated(ByVal Attribute As String, ByVal Replicated As Boolean = True, ByVal Replicate As Boolean = True) As Boolean
                    // Disable replication of Name, Code, Stereptype, Owner and Generate (since this are different for the replicated table).
                    baseReplication.SetAttributeReplicated("Name", false, false);
                    baseReplication.SetAttributeReplicated("Code", false, false);
                    baseReplication.SetAttributeReplicated("Stereotype", false, false);
                    baseReplication.SetAttributeReplicated("Owner", false, false);
                    baseReplication.SetAttributeReplicated("Generated", false, false);
                    // Don't replicate InReferences, OutReferences and all ExtendedDependency collections from the parent table.
                    baseReplication.SetCollectionReplicated("InReferences", false, false);
                    baseReplication.SetCollectionReplicated("OutReferences", false, false);
                    baseReplication.SetCollectionReplicated("IncomingTraceabilityLinks", false, false);
                    baseReplication.SetCollectionReplicated("ExtendedInfluences", false, false);
                    baseReplication.SetCollectionReplicated("OutgoingTraceabilityLinks", false, false);
                    baseReplication.SetCollectionReplicated("ExtendedDependencies", false, false);

                    // Create a traceability link between the entity set table and it's underlying type.
                    logger.Debug("  - Creating traceability link from entity set to entity.");
                    PdCommon.ExtendedDependency extendedDependency = (PdCommon.ExtendedDependency)pdmModel.ChildTraceabilityLinks.CreateNew();
                    extendedDependency.SourceObject = (PdCommon.NamedObject)pdmTable;
                    extendedDependency.Stereotype = "EntitySet of Entity";
                    extendedDependency.LinkedObject = (PdCommon.NamedObject)typeTable;

                    // Set the Name, Code, Stereotype, Owner and Generated properties of the repica.
                    logger.Debug("  - Setting Name, Code, Stereotype, Owner and Generated on replica-table");
                    pdmTable.Name = edmEntitySet.Name;
                    pdmTable.SetNameToCode();
                    pdmTable.Stereotype = "EntitySet";
                    pdmTable.Owner = pdmUser;
                    pdmTable.Generated = true;
                }

                // Create the table references between the replica-tables.
                logger.Debug("Adding replica-table references:");
                foreach (IEdmEntitySet edmEntitySet in edmEntitySets)
                {
                    logger.Debug(string.Format(" EntitySet[ContainerElementKind={0}; Name={1}; Namespace={2}]", Enum.GetName(typeof(EdmContainerElementKind), edmEntitySet.ContainerElementKind), edmEntitySet.Name, edmEntitySet.Container.Namespace));

                    // Get the package for the table.
                    PdPDM.Package pdmEntitySetPackage = PdHelper.GetPackage(pdmModel, edmEntitySet.Container.Namespace);
                    if (pdmEntitySetPackage == null)
                    {
                        logger.Error(string.Format("The current package '{0}' was not found!", edmEntitySet.Container.Namespace));
                        throw new PdODataException("The current package was not found!");
                    } else
                    {
                        logger.Debug(string.Format(" -Package={0}", pdmEntitySetPackage.Name));
                    }

                    PdPDM.Table currentTable = PdHelper.GetTable(pdmEntitySetPackage, edmEntitySet.Name);
                    if (currentTable == null)
                    {
                        logger.Error(string.Format("The current table '{0}' was not found!", edmEntitySet.Name));
                        throw new PdODataException("The current table was not found!");
                    } else
                    {
                        logger.Debug(string.Format(" -Table={0}", currentTable.Name));
                    }

                    // Loop throught the navigation property bindings.
                    foreach (IEdmNavigationPropertyBinding navigationPropertyBinding in edmEntitySet.NavigationPropertyBindings)
                    {
                        logger.Debug(string.Format(" -IEdmNavigationPropertyBinding[Name={0}; Target={1}; TargetType={2}]", navigationPropertyBinding.NavigationProperty.Name, navigationPropertyBinding.Target.Name, Enum.GetName(typeof(EdmTypeKind), navigationPropertyBinding.Target.Type.TypeKind)));

                        // Represents a type implementing Microsoft.OData.Edm.IEdmCollectionType.
                        if (navigationPropertyBinding.Target.Type.TypeKind.Equals(EdmTypeKind.Collection))
                        {
                            PdPDM.Table targetTable = PdHelper.GetTable(pdmEntitySetPackage, navigationPropertyBinding.Target.Name);
                            if (targetTable == null)
                            {
                                logger.Error(string.Format("The target table '{0}' was not found!", navigationPropertyBinding.Target.Name));
                                throw new PdODataException("The target table was not found!");
                            }
                            else
                            {
                                logger.Debug(" -Creating replica-table reference:");
                                logger.Debug(string.Format("  -TargetTable={0}", targetTable.Name));
                                PdPDM.Reference tableReference = (PdPDM.Reference)pdmEntitySetPackage.References.CreateNew();
                                tableReference.Name = string.Format("{0} > {1} : {2}", currentTable.Name, targetTable.Name, navigationPropertyBinding.NavigationProperty.Name);
                                tableReference.SetNameToCode();
                                // Set the parent to the target table.
                                tableReference.Object2 = targetTable;
                                tableReference.ParentRole = navigationPropertyBinding.NavigationProperty.Name;
                                // Set the foreign key constraint name.
                                tableReference.ForeignKeyConstraintName = string.Format("FK_{0}_{1}_{2}", currentTable.Code, targetTable.Code, tableReference.ParentRole);

                                // Add the reference to the current table.
                                currentTable.OutReferences.Add(tableReference);

                                // Empty the joins list (by default it is populated.
                                tableReference.Joins.Clear();
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

        public static void AddColumsToTable(PdPDM.Table pdmTable, IEdmStructuredType edmStructuredType, PdLogger logger)
        {
            // If the current structred type has a base type, add it's base poperties first.
            if (edmStructuredType.BaseType != null)
            {
                logger.Debug(string.Format(" Adding properties from base type {0}", edmStructuredType.BaseType.FullTypeName()));
                AddColumsToTable(pdmTable, edmStructuredType.BaseType, logger);
            }

            // Loop over the declared properties.
            foreach (IEdmProperty edmProperty in edmStructuredType.DeclaredProperties)
            {
                logger.Debug(string.Format(" -Property[Name={0}; PropertyKind={1}; Type={2}]", edmProperty.Name, Enum.GetName(typeof(EdmPropertyKind), edmProperty.PropertyKind), edmProperty.Type.FullName()));

                // Only add columns for properties which are structural.
                if (edmProperty.PropertyKind.Equals(EdmPropertyKind.Structural))
                {
                    // Create a new columns for the property.
                    PdPDM.Column pdmColumn = (PdPDM.Column)pdmTable.Columns.CreateNew();
                    pdmColumn.Name = edmProperty.Name;
                    pdmColumn.SetNameToCode();

                    // If the EDM property is part of the key, set the Primary indicator to true.
                    if (edmProperty.IsKey())
                        pdmColumn.Primary = true;

                    // If the type is set, update the column.
                    if (edmProperty.Type != null)
                    {
                        SetColumnType(pdmColumn, edmProperty.Type, logger);
                    }

                    // Add the new columns to the columns collection.
                    pdmTable.Columns.Add(pdmColumn);
                }
            }
        }

        /// <summary>
        /// Function to get the Sql data type based on a Edm primitive kind.
        /// Used: https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/ef/sqlclient-for-ef-types
        /// </summary>
        /// <param name="pdmColumn"></param>
        /// <param name="edmType"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static void SetColumnType(PdPDM.Column pdmColumn, IEdmTypeReference edmType, PdLogger logger)
        {
            // Set the Mandatory property as the inverse of IsNullable.
            pdmColumn.Mandatory = !edmType.IsNullable;

            // Translate the DataType if the edm type is a primitive.
            if (edmType.IsPrimitive())
            {
                switch (edmType.PrimitiveKind())
                {
                    // In OData V4 there is no "IsFixedLength" property on the binary type, so we can't differentiate a varbinary from binary.
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
            // If the type is an Enum, set the corresponding domain.
            else if (edmType.IsEnum())
            {
                logger.Debug("The datatype is an Enum type, so translating to PDM domain.");
                string enumFullName = edmType.FullName();
                logger.Debug(string.Format(" -Enum={0}", enumFullName));
                // Find the domain for the enum type.
                PdPDM.PhysicalDomain enumDomain = (PdPDM.PhysicalDomain)((PdPDM.Model)pdmColumn.Model).FindChildByName(enumFullName, (int)PdPDM.PdPDM_Classes.cls_PhysicalDomain);
                if (enumDomain == null)
                {
                    logger.Error(string.Format("The domain for Enum '{0}' was not found!", enumFullName));
                    throw new PdODataException("The domain for Enum was not found!");
                }

                // Assign the domain to the column.
                pdmColumn.Domain = enumDomain;
            }
        }
    }
}
