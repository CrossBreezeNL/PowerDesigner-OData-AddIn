
namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData
{
    public static class PdHelper
    {
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
    }
}
