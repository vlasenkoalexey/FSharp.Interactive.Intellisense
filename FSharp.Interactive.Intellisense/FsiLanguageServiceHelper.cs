using FSharp.Interactive.Intellisense.Lib;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace FSharp.Interactive.Intellisense
{
    internal class FsiLanguageServiceHelper
    {
        public const string FsiToolWindowClassName = "Microsoft.VisualStudio.FSharp.Interactive.FsiToolWindow";

        private Assembly fsiAssembly;
        private Type fsiLanguageServiceType;
        private Type sessionsType;
        private Type fsiWindowType;
        private System.Timers.Timer timer;
        private Object sessionCache;

        public FsiLanguageServiceHelper()
        {
            fsiAssembly = Assembly.Load("FSharp.VS.FSI, Version=12.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            fsiLanguageServiceType = fsiAssembly.GetType("Microsoft.VisualStudio.FSharp.Interactive.FsiLanguageService");
            sessionsType = fsiAssembly.GetType("Microsoft.VisualStudio.FSharp.Interactive.Session.Sessions");
            fsiWindowType = fsiAssembly.GetType(FsiToolWindowClassName);
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                timer.Stop();
                TryRegisterAutocompleteServer();
            }
            finally
            {
                timer.Start();
            }
        }

        private Object GetSession()
        {
            var providerGlobal = (IOleServiceProvider)Package.GetGlobalService(typeof(IOleServiceProvider));
            var provider = new ServiceProvider(providerGlobal);
            dynamic fsiLanguageService = ExposedObject.From(provider.GetService(fsiLanguageServiceType));
            dynamic sessions = fsiLanguageService.sessions;
            if (sessions != null)
            { 
                dynamic sessionsValue = ExposedObject.From(sessions).Value;
                dynamic sessionR = ExposedObject.From(sessionsValue).sessionR;
                dynamic sessionRValue = ExposedObject.From(sessionR).Value;
                dynamic sessionRValueValue = ExposedObject.From(sessionRValue).Value;
                dynamic exitedE = ExposedObject.From(sessionRValueValue).exitedE;
                try
                {
                    MethodInfo methodInfo = fsiAssembly.GetType("Microsoft.VisualStudio.FSharp.Interactive.Session+Session").GetMethod("get_Exited");
                    IObservable<EventArgs> exited = methodInfo.Invoke(sessionRValueValue, null);
                    IObserver<EventArgs> obsvr = Observer.Create<EventArgs>(
                        x => { Debug.WriteLine("OnNext: {0}", x); },
                        ex => { },
                        () => { });

                    exited.Subscribe(obsvr);
                }
                catch
                {
                }
                
                return sessionRValueValue;
            }

            return null;
        }

        public void StartRegisterAutocompleteWatchdogLoop()
        {
            if (timer == null)
            {
                timer = new System.Timers.Timer(3000);
                timer.Elapsed += timer_Elapsed;
                timer.Start();
            }
            else
            {
                timer.Start();
            }
        }

        private bool TryRegisterAutocompleteServer()
        {
            Object sessionRValueValue = GetSession();
            bool returnValue = false;

            if (sessionRValueValue != null && sessionRValueValue != sessionCache)
            {
                sessionCache = sessionRValueValue;
                MethodInfo methodInfo = fsiAssembly.GetType("Microsoft.VisualStudio.FSharp.Interactive.Session+Session").GetMethod("get_Input");
                dynamic fsiProcess = methodInfo.Invoke((Object)sessionRValueValue, null);

                fsiProcess.Invoke("printfn \"Registering Autocomplete provider\";;");
                fsiProcess.Invoke(String.Format("#r \"{0}\";;", typeof(AutocompleteServer).Assembly.Location));
                fsiProcess.Invoke("FSharp.Interactive.Intellisense.Lib.AutocompleteServer.StartServer(\"channel\");;");
                returnValue = true;

                System.Threading.Tasks.Task.Delay(2500).ContinueWith((t) =>
                {
                    // activate session
                    try
                    {
                        AutocompleteService autocomplteService = AutocompleteClient.GetAutocompleteService();
                        autocomplteService.Test();
                    }
                    catch
                    {
                    }
                });

            }

            return returnValue;
        }
    }
}
