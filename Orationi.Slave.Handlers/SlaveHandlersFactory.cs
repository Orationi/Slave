using System;
using Orationi.Slave.Handlers.GacHandler;
using Orationi.Slave.Handlers.IisHandler;

namespace Orationi.Slave.Handlers
{
    public static class SlaveHandlersFactory
    {
        public static SlaveHandlerBase GetHandler(string handlerName)
        {
            switch (handlerName)
            {
                case SlaveHandlers.GAC:
                    return new SlaveGacHandler();
                case SlaveHandlers.IIS:
                    return new SlaveIisHandler();
                case SlaveHandlers.Custom:
                    throw new NotImplementedException();
                default:
                    return new SlaveHandlerBase();
            }
        }
    }
}
