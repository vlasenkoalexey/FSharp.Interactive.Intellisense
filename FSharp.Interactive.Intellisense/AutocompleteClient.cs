using FSharp.Interactive.Intellisense.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSharp.Interactive.Intellisense
{
    public class AutocompleteClient
    {
        private static volatile AutocompleteService autocompleteService;
        private static Object syncRoot = new object();

        public static AutocompleteService GetAutocompleteService()
        {
            if (autocompleteService == null)
            {
                lock(syncRoot)
                {
                    if (autocompleteService == null)
                    {
                        try
                        {
                            autocompleteService = AutocompleteServer.StartClient("channel");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Can't instantiate ipc service channel: " + ex.ToString());
                        }
                    }
                }
            }

            return autocompleteService;
        }
    }
}
