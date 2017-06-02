using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Remoting.Messaging;
using System.ServiceModel;

namespace R4nd0mApps.TddStud10.TestRuntime
{
    public abstract class Marker
    {
        public const string CoverageDataCollectorAddressEnvVarName = "TDDSTUDIO_COVERAGEDATACOLLECTORADDRESS";

        private static LazyObject<Marker> instance = new LazyObject<Marker>(VersionedMarkerFactory);

        private static Marker VersionedMarkerFactory()
        {
            var address = Environment.GetEnvironmentVariable(CoverageDataCollectorAddressEnvVarName);
            if (string.IsNullOrEmpty(address))
            {
                return MarkerV1.Create();
            }
            else
            {
                return MarkerV2.Create(address.Trim());
            }
        }

        public static void SafeExec(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Trace.TraceInformation("Marker: SafeExec crash: {0}.", e.Message);
            }
        }

        protected bool IsDebuggerAttached { get; set; }

        public Marker(bool isDebuggerAttached)
        {
            IsDebuggerAttached = isDebuggerAttached;
        }

        public abstract void RegisterEnterSequencePoint(string assemblyId, string methodMdRid, string spId);

        public abstract void RegisterExitUnitTest(string source, string document, string line);

        [DebuggerNonUserCode]
        public static void EnterSequencePoint(string assemblyId, string methodMdRid, string spId)
        {
            instance.Value.RegisterEnterSequencePoint(assemblyId, methodMdRid, spId);
        }

        [DebuggerNonUserCode]
        public static void ExitUnitTest(string source, string document, string line)
        {
            instance.Value.RegisterExitUnitTest(source, document, line);
        }
    }

    public sealed class MarkerV1 : Marker
    {
        private const string TESTRUNID_SLOTNAME = "Marker.TestRunId";

        private Func<string, object> _ccGetData;
        private Action<string, object> _ccSetData;
        private LazyObject<ICoverageDataCollector> _channel;

        private string TestRunId
        {
            get { return _ccGetData(TESTRUNID_SLOTNAME) as string; }
            set { _ccSetData(TESTRUNID_SLOTNAME, value); }
        }

        public static Marker Create()
        {
            return new MarkerV1(CreateChannel, Debugger.IsAttached, CallContext.LogicalGetData, CallContext.LogicalSetData);
        }

        public MarkerV1(Func<ICoverageDataCollector> channelCreator, bool isDebuggerAttached, Func<string, object> ccGetData, Action<string, object> ccSetData)
            : base(isDebuggerAttached)
        {
            _channel = new LazyObject<ICoverageDataCollector>(channelCreator);
            _ccGetData = ccGetData;
            _ccSetData = ccSetData;
        }

        public override void RegisterEnterSequencePoint(string assemblyId, string methodMdRid, string spId)
        {
            if (IsDebuggerAttached)
            {
                Trace.TraceInformation("Marker: Ignoring call as debugger is attached.");
                return;
            }

            if (TestRunId == null)
            {
                TestRunId = new object().GetHashCode().ToString(CultureInfo.InvariantCulture);
            }

            SafeExec(() => _channel.Value.EnterSequencePoint(TestRunId, assemblyId, methodMdRid, spId));
        }

        public override void RegisterExitUnitTest(string source, string document, string line)
        {
            if (IsDebuggerAttached)
            {
                Trace.TraceInformation("Marker: Ignoring call as debugger is attached.");
                return;
            }

            if (TestRunId == null)
            {
                Trace.TraceError("Marker: Appears we did not have any sequence points for {0},{1},{2}.", source, document, line);
            }

            SafeExec(() => _channel.Value.ExitUnitTest(TestRunId, source, document, line));
            TestRunId = null;
        }

        public static string CreateCodeCoverageDataCollectorEndpointAddress()
        {
            return string.Format(
                "net.pipe://localhost/r4nd0mapps/tddstud10/CodeCoverageDataCollector/{0}",
                Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));
        }

        private static ICoverageDataCollector CreateChannel()
        {
            string address = CreateCodeCoverageDataCollectorEndpointAddress();

            Trace.TraceInformation("Marker: Initiating connection to {0} ...", address);
            NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            EndpointAddress epa = new EndpointAddress(address);
            var ret = ChannelFactory<ICoverageDataCollector>.CreateChannel(binding, epa);
            ret.Ping();
            Trace.TraceInformation("Marker: Connected to server.", address);

            return ret;
        }
    }

    public sealed class MarkerV2 : Marker
    {
        private readonly ICoverageDataCollector2 _channel;

        internal static Marker Create(string channelAddress)
        {
            return new MarkerV2(CreateChannel(channelAddress), Debugger.IsAttached);
        }

        public MarkerV2(ICoverageDataCollector2 channel, bool isDebuggerAttached) : base(isDebuggerAttached)
        {
            _channel = channel;
        }

        public override void RegisterEnterSequencePoint(string assemblyId, string methodMdRid, string spNum)
        {
            _channel.EnterSequencePoint(assemblyId, methodMdRid, spNum);
        }

        public override void RegisterExitUnitTest(string source, string document, string line)
        {
            throw new NotImplementedException();
        }

        private static ICoverageDataCollector2 CreateChannel(string address)
        {
            Trace.TraceInformation("Marker: Initiating connection to {0} ...", address);
            NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            EndpointAddress epa = new EndpointAddress(address);
            var ret = ChannelFactory<ICoverageDataCollector2>.CreateChannel(binding, epa);
            Trace.TraceInformation("Marker: Connected to server.", address);
            return ret;
        }
    }
}
