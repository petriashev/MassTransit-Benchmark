namespace MassTransitBenchmark.RequestResponse
{
    using System;
    using System.Threading.Tasks;
    using MassTransit;
    using MassTransit.AzureServiceBusTransport;


    public class ServiceBusRequestResponseTransport :
        IRequestResponseTransport
    {
        readonly ServiceBusHostSettings _hostSettings;
        readonly IRequestResponseSettings _settings;

        Task<ISendEndpoint> _targetEndpoint;
        Uri _targetEndpointAddress;

        public ServiceBusRequestResponseTransport(ServiceBusHostSettings hostSettings, IRequestResponseSettings settings)
        {
            _hostSettings = hostSettings;
            _settings = settings;
        }

        public Task<ISendEndpoint> TargetEndpoint => _targetEndpoint;
        public Uri TargetEndpointAddress => _targetEndpointAddress;

        public IBusControl GetBusControl(Action<IReceiveEndpointConfigurator> callback)
        {
            var busControl = Bus.Factory.CreateUsingAzureServiceBus(x =>
            {
                var host = x.Host(_hostSettings);

                x.ReceiveEndpoint(host, "rpc_consumer" + (_settings.Durable ? "" : "_express"), e =>
                {
                    e.EnableExpress = !_settings.Durable;
                    e.PrefetchCount = _settings.PrefetchCount;
                    e.MaxConcurrentCalls = _settings.ConcurrencyLimit;

                    callback(e);

                    _targetEndpointAddress = e.InputAddress;
                });
            });

            busControl.Start();

            _targetEndpoint = busControl.GetSendEndpoint(_targetEndpointAddress);

            return busControl;
        }
    }
}