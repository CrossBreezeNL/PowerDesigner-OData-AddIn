using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

                // First create the Domains based on the enum types.
                logger.Debug("Adding domains from enums:");
                foreach (IEdmEnumType enumType in schemaTypeDefinitions.Where(typeDefinition => typeDefinition.TypeKind.Equals(EdmTypeKind.Enum)))
                {
                    logger.Debug(string.Format(" Enum[Name={0}; Namespace={1}]", enumType.Name, enumType.Namespace));

                    // Create a new PDM domain object for the Enum.
                    PdPDM.PhysicalDomain pdmEnumDomain = (PdPDM.PhysicalDomain)pdmModel.Domains.CreateNew();
                    pdmEnumDomain.Name = enumType.FullName();
                    pdmEnumDomain.SetNameToCode();

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

                // Then create all tables based on the Entity and Complex type definitions.
                logger.Debug("Adding tables from entity and complex types:");
                IEnumerable<IEdmSchemaType> structuredTypeElements = schemaTypeDefinitions.Where(typeDefinition => typeDefinition.TypeKind.Equals(EdmTypeKind.Entity) || typeDefinition.TypeKind.Equals(EdmTypeKind.Complex));
                foreach (IEdmSchemaType structuredTypeElement in structuredTypeElements)
                {
                    logger.Debug(string.Format(" {0}[Name={1}; Namespace={2}]", Enum.GetName(typeof(EdmTypeKind), structuredTypeElement.TypeKind), structuredTypeElement.Name, structuredTypeElement.Namespace));

                    // Get or create the package for the entity type.
                    PdPDM.Package pdmTypePackage = GetOrCreatePackage(pdmModel, structuredTypeElement.Namespace);

                    // Create a new PDM table object for the EntityType.
                    PdPDM.Table pdmTypeTable = (PdPDM.Table)pdmTypePackage.Tables.CreateNew();
                    pdmTypeTable.Name = structuredTypeElement.Name;
                    pdmTypeTable.SetNameToCode();

                    // Add the columns to the table based on the declared properties.
                    AddColumsToTable(pdmTypeTable, (IEdmStructuredType)structuredTypeElement, logger);

                    // Add the table to the package.
                    pdmTypePackage.Tables.Add(pdmTypeTable);
                }

                logger.Debug("Adding tables references:");
                foreach (IEdmSchemaType structuredTypeElement in structuredTypeElements)
                {
                    // Find the table which represents the underlying type.
                    PdPDM.Package pdmEntityTypePackage = GetOrCreatePackage(pdmModel, structuredTypeElement.Namespace);
                    if (pdmEntityTypePackage == null)
                    {
                        logger.Error(string.Format("The type package '{0}' was not found!", structuredTypeElement.Namespace));
                        throw new PdODataException("The type package was not found!");
                    }
                    PdPDM.Table typeTable = GetTable(pdmEntityTypePackage, structuredTypeElement.Name);
                    if (typeTable == null)
                    {
                        logger.Error(string.Format("The type table '{0}' was not found!", structuredTypeElement.Name));
                        throw new PdODataException("The type table was not found!");
                    }

                    // Loop over the navigation properties of the schema type element.
                    foreach (IEdmNavigationProperty navProp in ((IEdmStructuredType)structuredTypeElement).NavigationProperties())
                    {
                        logger.Debug(string.Format(" -IEdmNavigationProperty[Name={0}; PropertyKind={1}]", navProp.Name, Enum.GetName(typeof(EdmPropertyKind), navProp.PropertyKind)));

                        IEdmEntityType targetedEntityType = navProp.ToEntityType();
                        // Find the table which represents the targeted type.
                        PdPDM.Package pdmTargetEntityTypePackage = GetOrCreatePackage(pdmModel, targetedEntityType.Namespace);
                        if (pdmTargetEntityTypePackage == null)
                        {
                            logger.Error(string.Format("The targeted type package '{0}' was not found!", targetedEntityType.Namespace));
                            throw new PdODataException("The targeted type package was not found!");
                        }
                        PdPDM.Table targetedTypeTable = GetTable(pdmTargetEntityTypePackage, targetedEntityType.Name);
                        if (targetedTypeTable == null)
                        {
                            logger.Error(string.Format("The targeted type table '{0}' was not found!", targetedEntityType.Name));
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
                        // Add the reference to the child table.
                        typeTable.OutReferences.Add(tableRef);
                    }
                }


                //logger.Debug(string.Format("EntityContainer[Name={0}; Location={1}]", model.EntityContainer.Name, model.EntityContainer.Location()));
                // Loop through the entity elements in the model.
                logger.Debug("Adding views from entity sets:");
                IEnumerable<IEdmEntitySet> edmEntitySets = model.EntityContainer.Elements.Where(entityContainerElement => entityContainerElement.ContainerElementKind.Equals(EdmContainerElementKind.EntitySet)).Cast<IEdmEntitySet>();
                foreach (IEdmEntitySet edmEntitySet in edmEntitySets)
                {
                    logger.Debug(string.Format(" EntitySet[ContainerElementKind={0}; Name={1}; Namespace={2}]", Enum.GetName(typeof(EdmContainerElementKind), edmEntitySet.ContainerElementKind), edmEntitySet.Name, edmEntitySet.Container.Namespace));

                    IEdmEntityType entityType = edmEntitySet.EntityType();
                    logger.Debug(string.Format(" =EntityType[Name={0}; IsAbstract={1}; Namespace={2}]", entityType.Name, entityType.IsAbstract, entityType.Namespace));

                    // Now a table is created for the EntityType, we create a view for the EntitySet.
                    // Find the schema to entity type belongs to, so the table can be added in the right package.
                    PdPDM.Package pdmEntitySetPackage = GetOrCreatePackage(pdmModel, edmEntitySet.Container.Namespace);
                    PdPDM.View pdmView = (PdPDM.View)pdmEntitySetPackage.Views.CreateNew();
                    pdmView.Name = edmEntitySet.Name;
                    pdmView.SetNameToCode();

                    // Find the table which represents the underlying type.
                    PdPDM.Package pdmEntityTypePackage = GetOrCreatePackage(pdmModel, entityType.Namespace);
                    if (pdmEntityTypePackage == null)
                    {
                        logger.Error(string.Format("The type package '{0}' was not found!", entityType.Namespace));
                        throw new PdODataException("The type package was not found!");
                    }
                    PdPDM.Table typeTable = GetTable(pdmEntityTypePackage, entityType.Name);
                    if (typeTable == null)
                    {
                        logger.Error(string.Format("The type table '{0}' was not found!", entityType.Name));
                        throw new PdODataException("The type table was not found!");
                    }
                    // Get a list of the column names.
                    var colNames = from PdPDM.Column col in typeTable.Columns.Cast<PdPDM.Column>()
                                    select string.Format("\"{0}\"", col.Name);
                    pdmView.SQLQuery = string.Format("SELECT {0} FROM \"{1}\"", string.Join(",", colNames), typeTable.Name);
                    pdmEntitySetPackage.Views.Add(pdmView);
                }

                // After we created the views we can create the view references between them.
                logger.Debug("Adding view references:");
                foreach (IEdmEntitySet edmEntitySet in edmEntitySets)
                {
                    logger.Debug(string.Format(" EntitySet[ContainerElementKind={0}; Name={1}; Namespace={2}]", Enum.GetName(typeof(EdmContainerElementKind), edmEntitySet.ContainerElementKind), edmEntitySet.Name, edmEntitySet.Container.Namespace));

                    // Get the package for the view.
                    PdPDM.Package pdmEntitySetPackage = GetPackage(pdmModel, edmEntitySet.Container.Namespace);
                    if (pdmEntitySetPackage == null)
                    {
                        logger.Error(string.Format("The current package '{0}' was not found!", edmEntitySet.Container.Namespace));
                        throw new PdODataException("The current package was not found!");
                    } else
                    {
                        logger.Debug(string.Format(" -Package={0}", pdmEntitySetPackage.Name));
                    }

                    PdPDM.View currentView = GetView(pdmEntitySetPackage, edmEntitySet.Name);
                    if (currentView == null)
                    {
                        logger.Error(string.Format("The current view '{0}' was not found!", edmEntitySet.Name));
                        throw new PdODataException("The current view was not found!");
                    } else
                    {
                        logger.Debug(string.Format(" -View={0}", currentView.Name));
                    }

                    // Loop throught the navigation property bindings.
                    foreach (IEdmNavigationPropertyBinding navigationPropertyBinding in edmEntitySet.NavigationPropertyBindings)
                    {
                        logger.Debug(string.Format(" -IEdmNavigationPropertyBinding[Name={0}; Target={1}; TargetType={2}]", navigationPropertyBinding.NavigationProperty.Name, navigationPropertyBinding.Target.Name, Enum.GetName(typeof(EdmTypeKind), navigationPropertyBinding.Target.Type.TypeKind)));

                        // Represents a type implementing Microsoft.OData.Edm.IEdmCollectionType.
                        if (navigationPropertyBinding.Target.Type.TypeKind.Equals(EdmTypeKind.Collection))
                        {
                            PdPDM.View targetView = GetView(pdmEntitySetPackage, navigationPropertyBinding.Target.Name);
                            if (targetView == null)
                            {
                                logger.Error(string.Format("The target view '{0}' was not found!", navigationPropertyBinding.Target.Name));
                                throw new PdODataException("The target view was not found!");
                            }
                            else
                            {
                                logger.Debug(" -Creating table reference:");
                                logger.Debug(string.Format("  -TargetView={0}", targetView.Name));
                                PdPDM.ViewReference viewReference = (PdPDM.ViewReference)pdmEntitySetPackage.ViewReferences.CreateNew();
                                viewReference.Name = string.Format("{0} > {1} : {2}", currentView.Name, targetView.Name, navigationPropertyBinding.NavigationProperty.Name);
                                viewReference.SetNameToCode();
                                // Set the parent to the view.
                                viewReference.Object2 = targetView;
                                viewReference.ParentRole = navigationPropertyBinding.NavigationProperty.Name;
                                // Add the reference to the view.
                                currentView.OutViewReferences.Add(viewReference);
                            }
                        }
                    }
                }

                //foreach (Tuple<IEdmEntityContainerElement, IEdmNavigationPropertyBinding> edmNavigationPropertyBindingTuple in model.EntityContainer.GetNavigationPropertyBindings())
                //{
                //    IEdmNavigationPropertyBinding edmNavigationPropertyBinding = edmNavigationPropertyBindingTuple.Item2;
                //    IEdmNavigationProperty edmNavigationProperty = edmNavigationPropertyBinding.NavigationProperty;
                //    string targetViewName = edmNavigationPropertyBinding.Target.Name;
                //    logger.Debug(string.Format("Found navigation property binding '{0}'; Target={1}; Type={2}", edmNavigationProperty.Name, targetViewName, Enum.GetName(typeof(EdmTypeKind), edmNavigationPropertyBinding.Target.Type.TypeKind)));

                //    // Represents a type implementing Microsoft.OData.Edm.IEdmCollectionType.
                //    if (edmNavigationPropertyBinding.Target.Type.TypeKind.Equals(EdmTypeKind.Collection))
                //    {
                //        IEdmCollectionType collectionType = (IEdmCollectionType)edmNavigationPropertyBinding.Target.Type;
                //        logger.Debug(string.Format(" =IEdmCollection[ElementType={0}", Enum.GetName(typeof(EdmTypeKind), collectionType.ElementType.TypeKind())));
                //    }

                    //PdPDM.View targetView = GetView(pdmModel, targetViewName);
                    //if (targetView == null)
                    //{
                    //    logger.Error(string.Format("The target view '{0}' was not found!", targetViewName));
                    //    throw new PdODataException("The target view was not found!");
                    //}

                    //// If the type is not set, skip this property.
                    //string foreignEntityName = edmNavigationProperty.ToEntityType().Name;
                    //logger.Debug(string.Format("Found foreign entity {0}", foreignEntityName));

                    //PdPDM.View foreignView = GetView(pdmModel, foreignEntityName);
                    //if (foreignView == null)
                    //{
                    //    logger.Error(string.Format("The foreign view '{0}' was not found!", foreignEntityName));
                    //    throw new PdODataException("The foreign view was not found!");
                    //}

                    //PdPDM.ViewReference fkReference = (PdPDM.ViewReference)targetView.OutViewReferences.CreateNew();
                    //fkReference.Name = edmNavigationProperty.Name;
                    //fkReference.SetNameToCode();

                    //foreach (EdmReferentialConstraintPropertyPair referentialConstraintPropertyPair in ((IEdmNavigationProperty)edmProperty).ReferentialConstraint.PropertyPairs)
                    //{
                    //    IEdmStructuralProperty localProperty = referentialConstraintPropertyPair.DependentProperty;
                    //    IEdmStructuralProperty foreignProperty = referentialConstraintPropertyPair.PrincipalProperty;
                    //    logger.Debug(string.Format("Found referential constraint property pair for local property {0} to foreign property {1}", localProperty.Name, foreignProperty.Name));

                    //    PdPDM.Column localColumn = null;
                    //    // Find the local PDM column.
                    //    foreach (PdPDM.Column pdmColumn in pdmTable.Columns)
                    //    {
                    //        if (pdmColumn.Name.Equals(localProperty.Name))
                    //        {
                    //            localColumn = pdmColumn;
                    //            break;
                    //        }
                    //    }
                    //    if (localColumn == null)
                    //    {
                    //        logger.Error("The local column was not found!");
                    //        throw new PdODataException("The local column was not found!");
                    //    }

                    //    PdPDM.Column foreignColumn = null;
                    //    // Find the foreign PDM column.
                    //    foreach (PdPDM.Column pdmColumn in foreignPdmTable.Columns)
                    //    {
                    //        if (pdmColumn.Name.Equals(foreignProperty.Name))
                    //        {
                    //            foreignColumn = pdmColumn;
                    //            break;
                    //        }
                    //    }
                    //    if (foreignColumn == null)
                    //    {
                    //        logger.Error("The foreign column was not found!");
                    //        throw new PdODataException("The foreign column was not found!");
                    //    }

                    //    PdPDM.ReferenceJoin referenceJoin = (PdPDM.ReferenceJoin)fkReference.Joins.CreateNew();
                    //    referenceJoin.ChildTableColumn = localColumn;
                    //    referenceJoin.ParentTableColumn = foreignColumn;
                    //    fkReference.Joins.Add(referenceJoin);
                    //}

                    //targetView.OutViewReferences.Add(fkReference);
                //}

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

        public static PdPDM.Package GetOrCreatePackage(PdPDM.BasePackage pdmModel, string packageNameToFind)
        {
            var pdmPackageObject = pdmModel.FindChildByName(packageNameToFind, (int)PdPDM.PdPDM_Classes.cls_Package);
            // If the package is found, return it.
            if (pdmPackageObject != null)
            {
                return (PdPDM.Package)pdmPackageObject;
            }
            // If the package wasn't found, create it.
            else
            {
                PdPDM.Package pdmPackage = (PdPDM.Package)pdmModel.Packages.CreateNew();
                pdmPackage.Name = packageNameToFind;
                pdmPackage.SetNameToCode();
                pdmModel.Packages.Add(pdmPackage);
                return pdmPackage;
            }

        }

        public static PdPDM.Package GetPackage(PdPDM.BasePackage pdmModel, string packageNameToFind)
        {
            var pdmPackageObject = pdmModel.FindChildByName(packageNameToFind, (int)PdPDM.PdPDM_Classes.cls_Package);
            // If the package is found, return it.
            if (pdmPackageObject != null)
            {
                return (PdPDM.Package)pdmPackageObject;
            }
            // If the package wasn't found, return null.
            return null;

        }

        public static PdPDM.Table GetTable(PdPDM.BasePackage model, string tableNameToFind)
        {
            // Find the table in the model.
            var tableObject = model.FindChildByName(tableNameToFind, (int)PdPDM.PdPDM_Classes.cls_Table);
            if (tableObject != null)
                return (PdPDM.Table)tableObject;

            // If the table wasn't found, return null.
            return null;
        }

        public static PdPDM.View GetView(PdPDM.BasePackage model, string viewNameToFind)
        {
            // Find the view in the model.
            var viewObject = model.FindChildByName(viewNameToFind, (int)PdPDM.PdPDM_Classes.cls_View);
            if (viewObject != null)
                return (PdPDM.View)viewObject;

            // If the view wasn't found, return null.
            return null;
        }

        /// <summary>
        /// Function to get the Sql data type based on a Edm primitive kind.
        /// Used: https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/ef/sqlclient-for-ef-types
        /// </summary>
        /// <param name="edmPrimitiveType"></param>
        /// <returns></returns>
        public static void SetColumnType(PdPDM.Column pdmColumn, IEdmTypeReference edmType, PdLogger logger)
        {
            // Set the Mandatory property as the inverse of IsNullable.
            pdmColumn.Mandatory = !edmType.IsNullable;

            // Translate the DataType if the edm type is a primitive.
            if (edmType.IsPrimitive())
            {
                logger.Debug("The datatype is a primitive type, so translating to PDM datatype.");
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
            // If the type is an Enum, set the corresponding domain.
            //else if (edmType.IsEnum())
            //{
            //    logger.Debug("The datatype is an Enum type, so translating to PDM domain.");
            //    logger.Debug(string.Format(" -Enum={0}", edmType.FullName()));
            //}
        }
    }
}
