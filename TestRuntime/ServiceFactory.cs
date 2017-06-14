using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace R4nd0mApps.TddStud10.TestRuntime
{
    public class ServiceInfo<TSvc> 
    {
        public Uri Address { get; set; }
        public TSvc Service { get; set; }
    }

    public static class ServiceFactory
    {
        public static int GetFreePortOnLoopback()
        {
            var server = new TcpListener(IPAddress.Loopback, 0);
            server.Start();
            var port = ((IPEndPoint)server.LocalEndpoint).Port;
            server.Stop();
            return port;
        }

        public static ServiceInfo<TSvc> CreateService<TSvc, TISvc>(TSvc svcObj)
            where TSvc : class
        {
            Trace.TraceInformation("{0}: Creating Coverage Data service.", typeof(TSvc).Name);

            var serviceHost = new ServiceHost(svcObj);
            var binding = Marker.OnMono ? new NetTcpBinding() as Binding : new NetNamedPipeBinding(NetNamedPipeSecurityMode.None) as Binding;
            var address = Marker.OnMono
                ? $"net.tcp://127.0.0.1:{GetFreePortOnLoopback()}/r4nd0mapps/tddstud10/{typeof(TSvc).Name}/{Process.GetCurrentProcess().Id}"
                : $"net.pipe://localhost/r4nd0mapps/tddstud10/{typeof(TSvc).Name}/{Process.GetCurrentProcess().Id}";
            var endPoint = serviceHost.AddServiceEndpoint(typeof(TISvc), binding, address);
            serviceHost.Open();

            var epUri = serviceHost.ChannelDispatchers[0].Listener.Uri;
            Trace.TraceInformation("{0}: Bound coverage data service to: {1}", typeof(TSvc).Name, epUri);
            serviceHost.Faulted += (sender, e) => Trace.TraceError("{0}: Service host faulted: {1}", typeof(TSvc).Name, e);

            return new ServiceInfo<TSvc> { Address = epUri, Service = svcObj };
        }
    }
}
