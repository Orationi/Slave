using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Discovery;
using System.Threading;
using System.Threading.Tasks;
using Orationi.CommunicationCore.Interfaces;
using Orationi.CommunicationCore.Model;
using Orationi.ModuleCore.Providers;

namespace Orationi.Slave
{
    /// <summary>
    /// Orationi slave
    /// </summary>
    public class OrationiSlave : IOrationiSlaveCallback, IDisposable
    {
        private readonly IOrationiMasterService _masterService = null;
        private readonly ICommunicationObject _communicationObject = null;
        private readonly AutoResetEvent _ping = new AutoResetEvent(false);
        private readonly TimeSpan _pingInterval = TimeSpan.FromSeconds(5);
        readonly CancellationTokenSource _shutdown = new CancellationTokenSource();

        public OrationiSlave()
        {
            Binding binding = new NetTcpBinding(SecurityMode.None);
            EndpointAddress defaultEndpointAddress = new EndpointAddress("net.tcp://master.orationi.org:57344/Orationi/Master/v1/");
            EndpointAddress discoveredEndpointAddress = DiscoverMaster();
            ContractDescription contractDescription = ContractDescription.GetContract(typeof(IOrationiMasterService));
            ServiceEndpoint serviceEndpoint = new ServiceEndpoint(contractDescription, binding, discoveredEndpointAddress ?? defaultEndpointAddress);
            var channelFactory = new DuplexChannelFactory<IOrationiMasterService>(this, serviceEndpoint);

            try
            {
                channelFactory.Open();
            }
            catch (Exception ex)
            {
                channelFactory?.Abort();
                Console.WriteLine(ex.Message);
            }

            try
            {
                _masterService = channelFactory.CreateChannel();
                _communicationObject = (ICommunicationObject)_masterService;
                _communicationObject.Open();
            }
            catch (Exception ex)
            {
                if (_communicationObject != null && _communicationObject.State == CommunicationState.Faulted)
                    _communicationObject.Abort();
                Console.WriteLine(ex.Message);
            }
        }

        public void Connect()
        {
            SlaveConfiguration configuration = _masterService.Connect();
            StartPinging();
        }

        public EndpointAddress DiscoverMaster()
        {
            DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
            var orationiServices = discoveryClient.Find(new FindCriteria(typeof(IOrationiMasterService)));
            discoveryClient.Close();
            return orationiServices.Endpoints.Count == 0 ? null : orationiServices.Endpoints[0].Address;
        }

        public void Disconnect()
        {
            _shutdown.Cancel();
            _masterService.Disconnect();
        }

        public void AbortConnection()
        {
            _shutdown.Cancel();
            Dispose();
        }

        public void PushModule(Stream moduleStream)
        {
            throw new NotImplementedException();
        }

        public void UndeployModule(int moduleId)
        {
            throw new NotImplementedException();
        }

        public void ExecutePowerShell(string script)
        {
            PowerShellProvider powerShellProvider = new PowerShellProvider();
            powerShellProvider.InvokeScriptAsync(script);
        }

        private void StartPinging()
        {
            var token = _shutdown.Token;
            Task.Run(
                () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        _masterService.Ping();

                        _ping.WaitOne(_pingInterval);
                    }
                },
                token
            );
        }

        public void Dispose()
        {
            if (_communicationObject != null && _communicationObject.State != CommunicationState.Closed)
                _communicationObject.Close();
        }
    }
}
