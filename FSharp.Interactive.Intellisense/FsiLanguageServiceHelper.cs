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
        // https://github.com/Microsoft/visualfsharp/blob/275b832e9dd1a4bd64ed3accd218384b901be1d2/vsintegration/src/vs/FsPkgs/FSharp.VS.FSI/fsiSessionToolWindow.fs
        public const string FsiToolWindowClassName = "Microsoft.VisualStudio.FSharp.Interactive.FsiToolWindow";
        public const string FsiViewFilterClassName = "Microsoft.VisualStudio.FSharp.Interactive.FsiViewFilter";

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

        private Object GetSession()
        {
            try
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

                        MethodInfo methodInfo = fsiAssembly.GetType("Microsoft.VisualStudio.FSharp.Interactive.Session+Session").GetMethod("get_Exited");
                        IObservable<EventArgs> exited = methodInfo.Invoke(sessionRValueValue, null);
                        IObserver<EventArgs> obsvr = Observer.Create<EventArgs>(
                            x => { RegisterAutocompleteServer(); },
                            ex => { },
                            () => { });

                        exited.Subscribe(obsvr);

                
                    return sessionRValueValue;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
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

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            RegisterAutocompleteServer();
        }

        private void RegisterAutocompleteServer()
        {
            try
            {
                if (timer != null)
                {
                    timer.Stop();
                }
                TryRegisterAutocompleteServer();
            }
            finally
            {
                if (timer != null)
                {
                    timer.Start();
                }
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
                fsiProcess.Invoke(String.Format("FSharp.Interactive.Intellisense.Lib.AutocompleteServer.StartServer({0});;", sessionRValueValue.GetHashCode()));
                returnValue = true;

                System.Threading.Tasks.Task.Delay(2500).ContinueWith((t) =>
                {
                    // activate session
                    try
                    {
                        AutocompleteService autocomplteService = AutocompleteClient.SetAutocompleteServiceChannel(sessionRValueValue.GetHashCode());
                        autocomplteService.Ping();
                        fsiProcess.Invoke("printfn \"Autocomplete provider registration complete\";;");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                });

            }

            return returnValue;
        }
    }
}
