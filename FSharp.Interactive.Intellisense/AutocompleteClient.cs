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
            return autocompleteService;
        }

        public static AutocompleteService SetAutocompleteServiceChannel(int channelId)
        {
            lock (syncRoot)
            {
                try
                {
                    autocompleteService = AutocompleteServer.StartClient(channelId);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Can't instantiate ipc service channel: " + ex.ToString());
                }
            }

            return autocompleteService;
        }
    }
}
