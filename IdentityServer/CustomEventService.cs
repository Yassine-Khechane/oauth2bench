using System;
using System.Threading.Tasks;
using IdentityServer3.Core.Events;
using IdentityServer3.Core.Services;
using Newtonsoft.Json;
using IdentityServer3.Core.Logging;

namespace IdentityServer
{
    internal class CustomEventService : IEventService
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();


        public async Task RaiseAsync<T>(Event<T> evt)
        {
            var obj = new { evt.Id, evt.Category, evt.EventType, evt.Name, evt.Message, evt.Details, evt.Context };
            Logger.Info(JsonConvert.SerializeObject(obj, Formatting.Indented));
        }
    }
}