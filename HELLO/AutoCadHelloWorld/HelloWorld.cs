using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(AutoCadHelloWorld.HelloWorld))]

namespace AutoCadHelloWorld
{
    public class HelloWorld
    {
        [CommandMethod("HelloWorld")]
        public void SayHello()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            ed.WriteMessage("\nHello World!");
        }
    }
}