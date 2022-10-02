# PowerDesigner-OData-AddIn
An AddIn for PowerDesigner to synchronize OData metadata with a PDM model

# Getting Started
To use the plugin (for now):

- Open this solution in Visual Studio which is running as Administator (so it can register new DLLs)
- Build the project in Debug.
- Run the RegisterAddIn - Debug.reg file to register the path in the registry (You only need to do this once, not on every build).

Now you can enable the AddIn in PowerDesigner in Tools -> General Options -> Add-Ins

# Resources

## PowerDesigner
- https://help.sap.com/docs/SAP_POWERDESIGNER/31c48596e34446a68956e0aa7e700a2e/c7d62f7d6e1b1014b6b5cd497c583efb.html?locale=en-US&version=16.6.10
- https://help.sap.com/docs/SAP_POWERDESIGNER/abd3434b4987485c92057ab9392aadbe/c7e194046e1b101492b38124129e7841.html?locale=en-US&version=16.6.10
- https://help.sap.com/docs/SAP_POWERDESIGNER/31c48596e34446a68956e0aa7e700a2e/c7d0294d6e1b1014b766cb40bbc4f211.html?version=16.7.02&locale=en-US

## OData
- OData Examples: https://pragmatiqa.com/xodata/odatadir.html
- https://learn.microsoft.com/nl-nl/odata/