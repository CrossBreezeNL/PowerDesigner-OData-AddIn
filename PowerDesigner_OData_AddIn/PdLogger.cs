using System;

namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData
{
    public class PdLogger
    {
        // A reference to the PowerDesigner Application object.
        private PdCommon.Application _app;

        public PdLogger(PdCommon.Application app)
        {
            this._app = app;
        }

        /// <summary>
        /// Helper method to write a debug message in the PowerDesigner output window with a PowerDesigner object class name.
        /// </summary>
        /// <param name="message">The debug message to write in the PowerDesigner output window</param>
        public string PDObjectToString(object pObject)
        {
            PdCommon.BaseObject pdBaseObject = (PdCommon.BaseObject)pObject;
            if (pdBaseObject is PdCommon.NamedObject pdNamedObject)
            {
                return String.Format("PDObject[ClassName='{0}'; Code='{1}'; Name='{1}'; ObjectLocation='{3}']", pdNamedObject.ClassName, pdNamedObject.Code, pdNamedObject.name, pdNamedObject.ObjectLocation);
            }
            else
            {
                return String.Format("PDObject[ClassName='{0}']", pdBaseObject.ClassName);
            }
        }

        /// <summary>
        /// Helper method to write a info message in the PowerDesigner output window.
        /// </summary>
        /// <param name="message">The info message to write in the PowerDesigner output window</param>
        public void Info(String message)
        {
            this.Log(message, "INFO");
        }

        /// <summary>
        /// Helper method to write a error message in the PowerDesigner output window.
        /// </summary>
        /// <param name="message">The error message to write in the PowerDesigner output window</param>
        public void Error(String message)
        {
            this.Log(message, "ERROR");
        }

        /// <summary>
        /// Helper method to write a debug message in the PowerDesigner output window.
        /// </summary>
        /// <param name="message">The debug message to write in the PowerDesigner output window</param>
        public void Debug(String message)
        {
            this.Log(message, "DEBUG");
        }

        /// <summary>
        /// Helper method to write a log message in the PowerDesigner output window.
        /// </summary>
        /// <param name="message">The message to write in the PowerDesigner output window</param>
        private void Log(String message, String level)
        {
            // Only write to the Output if we have a handle on the PowerDesigner application.
            if (this._app != null)
                this._app.Output(String.Format("[OData-AddIn {0}] {1}", level, message));
            else
                Console.WriteLine(String.Format("[OData-AddIn {0}] {1}", level, message));
        }
    }
}
