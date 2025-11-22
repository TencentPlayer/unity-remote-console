using System;
using System.Collections.Generic;
using RConsole.Common;
using Unity.VisualScripting;

namespace RConsole.Runtime
{
    public static class HandlerFactory
    {

        private static List<IHandler> _handlers = new List<IHandler>();

        public static void RegisterHandler(IHandler handler)
        {
            _handlers.Add(handler);
        }

        public static void Initialize()
        {
            RegisterHandler(new LookinHandler());
            RegisterHandler(new FileHandler());
        }

        public static void OnEnable()
        {
            foreach (var handler in _handlers)
            {
                handler.OnEnable();
            }
        }

        public static void OnDisable()
        {
            foreach (var handler in _handlers)
            {
                handler.OnDisable();
            }
        }
    }
}