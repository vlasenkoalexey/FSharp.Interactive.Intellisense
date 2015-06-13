using FSharp.Interactive.Intellisense.Lib;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace FSharp.Interactive.Intellisense
{
    internal class FsiLanguageServiceHelper
    {
        private Assembly fsiAssembly;
        private Type fsiLanguageServiceType;
        private Type sessionsType;
        private Type fsiWindow;
        private bool isRegistered;
        private System.Timers.Timer timer;
        private Object sessionCache;

        public FsiLanguageServiceHelper()
        {
            fsiAssembly = Assembly.Load("FSharp.VS.FSI, Version=12.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            fsiLanguageServiceType = fsiAssembly.GetType("Microsoft.VisualStudio.FSharp.Interactive.FsiLanguageService");
            sessionsType = fsiAssembly.GetType("Microsoft.VisualStudio.FSharp.Interactive.Session.Sessions");
            fsiWindow = fsiAssembly.GetType("Microsoft.VisualStudio.FSharp.Interactive.FsiToolWindow");
            isRegistered = false;
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

        void sessionRestarted(object sender, System.EventArgs e)
        {
            this.isRegistered = false;
            this.TryRegisterAutocompleteServer();
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
                //dynamic input = sessionRValueValue.Input;
                //exitedE.Add(new EventHandler(sessionRestarted)); //TODO: figure out how to bind here
                try
                {
                    MethodInfo methodInfo = fsiAssembly.GetType("Microsoft.VisualStudio.FSharp.Interactive.Session+Session").GetMethod("get_Exited");
                    IObservable<EventArgs> exited = methodInfo.Invoke(sessionRValueValue, null);
                    //IObserver<int> obsvr = Observer.Create<int>(
                    //    x => Console.WriteLine("OnNext: {0}", x),
                    //    ex => Console.WriteLine("OnError: {0}", ex.Message),
                    //    () => Console.WriteLine("OnCompleted"));

                    //exited.Subscribe(complete => { };, ex => { };, () => { };);
                }
                catch(Exception ex)
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

                fsiProcess.Invoke("(* Registering Autocomplete provider *);;");
                fsiProcess.Invoke(String.Format("#r \"{0}\";;", typeof(AutocompleteServer).Assembly.Location));
                fsiProcess.Invoke("FSharp.Interactive.Intellisense.Lib.AutocompleteServer.StartServer(\"channel\");;");
                returnValue = true;

                // TODO: activate session
                //try
                //{
                //    AutocompleteService autocomplteService = AutocompleteServer.StartClient("channel");
                //    autocomplteService.Test();
                //}
                //catch (Exception ex)
                //{
                //}
            }

            return returnValue;
        }
    }
}
