
using PdPDM;
using System.Reflection;

namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData
{
    public static class PdHelper
    {
        public static PdPDM.Package GetOrCreatePackage(PdPDM.BasePackage pdmModel, string packageNameToFind, PdLogger logger)
        {
            var pdmPackageObject = pdmModel.FindChildByName(packageNameToFind, (int)PdPDM.PdPDM_Classes.cls_Package);
            // If the package is found, return it.
            if (pdmPackageObject != null)
            {
                logger.Debug(string.Format("[GetOrCreatePackage] Package '{0}' already exists, returning.", packageNameToFind));
                return (PdPDM.Package)pdmPackageObject;
            }
            // If the package wasn't found, create it.
            else
            {
                logger.Debug(string.Format("[GetOrCreatePackage] Creating package '{0}'.", packageNameToFind));
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

        public static PdPDM.User GetOrCreateUser(PdPDM.Model pdmModel, string userNameToFind, PdLogger logger)
        {
            var pdmUserObject = pdmModel.FindChildByName(userNameToFind, (int)PdPDM.PdPDM_Classes.cls_User);
            // If the user is found, return it.
            if (pdmUserObject != null)
            {
                logger.Debug(string.Format("[GetOrCreateUser] User '{0}' already exists, returning.", userNameToFind));
                return (PdPDM.User)pdmUserObject;
            }
            // If the user wasn't found, create it.
            else
            {
                logger.Debug(string.Format("[GetOrCreateUser] Creating user '{0}'.", userNameToFind));
                // Create a new user and set the Name and Code.
                PdPDM.User pdmUser = (PdPDM.User)pdmModel.Users.CreateNew();
                pdmUser.Name = userNameToFind;
                pdmUser.SetNameToCode();
                // Set the stereotype to User (for certain DBMS'es the stereotype will automatically be set to 'User', so on update this gives issues otherwise).
                pdmUser.Stereotype = "User";

                // Add the new package to the model.
                pdmModel.Users.Add(pdmUser);

                // Return the new package.
                return pdmUser;
            }

        }

        public static void UpdateDiagramRecursively(PdPDM.BasePackage packageOrModel)
        {
            // First update all diagrams in child-packages.
            foreach (PdPDM.BasePackage childPackage in packageOrModel.Packages)
            {
                UpdateDiagramRecursively(childPackage);
            }

            // Set the name of the default diagram to the package name.
            PdPDM.PhysicalDiagram defaultDiagram = (PdPDM.PhysicalDiagram)packageOrModel.DefaultDiagram;
            // Add check whether default diagram is set.
            if (defaultDiagram != null)
            {
                defaultDiagram.Name = packageOrModel.Name;
                defaultDiagram.SetNameToCode();

                // Creates a symbol for each object in package which can be displayed in current diagram.
                defaultDiagram.AttachAllObjects();
                // Perform auto-layout on the diagram.
                defaultDiagram.AutoLayout();
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
