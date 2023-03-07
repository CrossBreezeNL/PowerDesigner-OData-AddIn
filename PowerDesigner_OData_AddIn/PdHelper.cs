
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
                // Create a new package and set the Name and Code.
                PdPDM.Package pdmPackage = (PdPDM.Package)pdmModel.Packages.CreateNew();
                pdmPackage.Name = packageNameToFind;
                pdmPackage.SetNameToCode();

                // Add the new package to the model.
                pdmModel.Packages.Add(pdmPackage);

                // Return the new package.
                return pdmPackage;
            }

        }

        public static PdPDM.User GetOrCreateUser(PdPDM.Model pdmModel, string userNameToFind)
        {
            var pdmUserObject = pdmModel.FindChildByName(userNameToFind, (int)PdPDM.PdPDM_Classes.cls_User);
            // If the user is found, return it.
            if (pdmUserObject != null)
            {
                return (PdPDM.User)pdmUserObject;
            }
            // If the user wasn't found, create it.
            else
            {
                // Create a new user and set the Name and Code.
                PdPDM.User pdmUser = (PdPDM.User)pdmModel.Users.CreateNew();
                pdmUser.Name = userNameToFind;
                pdmUser.SetNameToCode();

                // Add the new package to the model.
                pdmModel.Users.Add(pdmUser);

                // Return the new package.
                return pdmUser;
            }

        }

        public static void UpdateDiagramResursively(PdPDM.BasePackage packageOrModel)
        {
            // First update all diagrams in child-packages.
            foreach (PdPDM.BasePackage childPackage in packageOrModel.Packages)
            {
                UpdateDiagramResursively(childPackage);
            }

            // Set the name of the default diagram to the package name.
            PdPDM.PhysicalDiagram defautlDiagram = (PdPDM.PhysicalDiagram)packageOrModel.DefaultDiagram;
            defautlDiagram.Name = packageOrModel.Name;
            defautlDiagram.SetNameToCode();

            // Creates a symbol for each object in package which can be displayed in current diagram.
            defautlDiagram.AttachAllObjects();
            // Perform auto-layout on the diagram.
            defautlDiagram.AutoLayout();
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
