using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FSharp.Interactive.Intellisense
{
    internal class CommandChainNodeWrapper
    {
        private const int LevelDeepMax = 20;

        public IOleCommandTarget FilterObject
        {
            get
            {
                return commandChainNode.FilterObject;
            }
        }

        public CommandChainNodeWrapper Next
        {
            get
            {
                if (commandChainNode.Next != null)
                {
                    return new CommandChainNodeWrapper(commandChainNode.Next);
                }
                else
                {
                    return null;
                }
            }
        }

        private dynamic commandChainNode;

        private CommandChainNodeWrapper(Object commandChainNode)
        {
            this.commandChainNode = ExposedObject.From(commandChainNode);
        }

        private CommandChainNodeWrapper(IVsTextView textAdapter)
        {
            FieldInfo commandChainFieldInfo = textAdapter.GetType().BaseType.GetField("_commandChain", BindingFlags.NonPublic | BindingFlags.Instance);
            this.commandChainNode = new CommandChainNodeWrapper(commandChainFieldInfo.GetValue(textAdapter));
        }

        public static IOleCommandTarget GetFilterByFullClassName(IVsTextView textAdapter, string className)
        {
            CommandChainNodeWrapper commandChainWrapper = new CommandChainNodeWrapper(textAdapter);
            return GetFilterByFullClassName(commandChainWrapper, className, 0);
        }

        private static IOleCommandTarget GetFilterByFullClassName(CommandChainNodeWrapper commandChainWrapper, string className, int levelDeep)
        {
            if (commandChainWrapper.FilterObject != null && commandChainWrapper.FilterObject.GetType().FullName == className)
            {
                return commandChainWrapper.FilterObject as IOleCommandTarget;
            }

            if (levelDeep >= LevelDeepMax)
            {
                return null;
            }

            if (commandChainWrapper.Next != null)
            {
                try
                {
                    return GetFilterByFullClassName(commandChainWrapper.Next, className, ++levelDeep);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    return null;
                }

            }

            return null;
        }
    }

}
