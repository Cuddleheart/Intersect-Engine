using System;
using System.Diagnostics;
using System.Globalization;

namespace Intersect.Server
{
    /// <summary>
    /// Please do not modify this without JC's approval! If namespaces are referenced that are not SYSTEM.* then the server won't run cross platform.
    /// If you want to add startup instructions see Classes/ServerStart.cs
    /// </summary>
    public static class MainClass
    {
        [STAThread]
        public static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            try
            {
                Type
                    .GetType("Intersect.Server.Core.Bootstrapper")?
                    .GetMethod("Start")?
                    .Invoke(null, new object[] {args});
            }
            catch (Exception exception)
            {
                var type = Type.GetType("Intersect.Server.Core.Bootstrapper", true);
                Debug.Assert(type != null, "type != null");

                var staticMethodInfo = type.GetMethod("OnUnhandledException");
                Debug.Assert(staticMethodInfo != null, nameof(staticMethodInfo) + " != null");

                staticMethodInfo.Invoke(null, new object[]
                {
                    null,
                    new UnhandledExceptionEventArgs(exception.InnerException ?? exception, true)
                });
            }
        }
    }
}