using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using R4nd0mApps.TddStud10.Common;
using R4nd0mApps.TddStud10.Common.Domain;
using R4nd0mApps.TddStud10.Logger;
using R4nd0mApps.TddStud10.TestExecution.Adapters;
using R4nd0mApps.TddStud10.TestRuntime;
using R4nd0mApps.XTestPlatform.Api;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace R4nd0mApps.TddStud10.TestHost
{
    public static class Program
    {
        private static ILogger Logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger;

        private static bool _debuggerAttached = Debugger.IsAttached;
        private static void LogInfo(string format, params object[] args)
        {
            Logger.LogInfo(format, args);
        }

        private static void LogError(string format, params object[] args)
        {
            Logger.LogError(format, args);
        }

        [LoaderOptimization(LoaderOptimization.MultiDomain)]
        public static int Main(string[] args)
        {
            try
            {
                return MainImpl(args);
            }
            catch
            {
                return 1;
            }
        }

        private static int MainImpl(string[] args)
        {
            if (File.Exists(@"c:\debug.testhost.txt"))
            {
                while (true)
                {
                    System.Threading.Thread.Sleep(2000);
                }
            }

            LogInfo("TestHost: Entering Main.");
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomainUnhandledException);
            var command = args[0];
            var buildRoot = args[1];
            var codeCoverageStore = args[2];
            var testResultsStore = args[3];
            var discoveredUnitTestsStore = args[4];
            var testFailureInfoStore = args[5];
            var timeFilter = args[6];
            var slnPath = args[7];
            var slnSnapPath = args[8];
            var discoveredUnitDTestsStore = args[9];
            var ignoredTests = (args[10] ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var searchPath = FilePath.NewFilePath(Path.Combine(Path.GetDirectoryName(slnSnapPath), "packages"));
            if (command == "discover")
            {
                var tds = AdapterLoader.LoadDiscoverers(searchPath.Item);
                DiscoverUnitTests(tds, slnPath, slnSnapPath, discoveredUnitTestsStore, discoveredUnitDTestsStore, buildRoot, new DateTime(long.Parse(timeFilter)), ignoredTests);
                LogInfo("TestHost: Exiting Main.");
                return 0;
            }
            else
            {
                var tes = AdapterLoader.LoadExecutors(searchPath.Item);
                var allTestsPassed = false;
                if (_debuggerAttached)
                {
                    allTestsPassed = RunTests(tes, slnPath, slnSnapPath, buildRoot, testResultsStore, testFailureInfoStore, GetTestToDebug(discoveredUnitTestsStore, discoveredUnitDTestsStore));
                }
                else
                {
                    allTestsPassed = ExecuteTestWithCoverageDataCollection(() => RunTests(tes, slnPath, slnSnapPath, buildRoot, testResultsStore, testFailureInfoStore, PerDocumentLocationXTestCases.Deserialize(FilePath.NewFilePath(discoveredUnitTestsStore))), codeCoverageStore);
                }

                LogInfo("TestHost: Exiting Main.");
                return allTestsPassed ? 0 : 1;
            }
        }

        private static PerDocumentLocationXTestCases GetTestToDebug(string discoveredUnitTestsStore, string discoveredUnitDTestsStore)
        {
            var discoveredUnitTests = PerDocumentLocationXTestCases.Deserialize(FilePath.NewFilePath(discoveredUnitTestsStore));
            var discoveredUnitDTests = PerDocumentLocationDTestCases.Deserialize(FilePath.NewFilePath(discoveredUnitDTestsStore));

            var dtc = discoveredUnitDTests.Values.First().First();
            var dl = new DocumentLocation(dtc.CodeFilePath, dtc.LineNumber);

            var testToDebug = new PerDocumentLocationXTestCases();
            testToDebug.TryAdd(dl, discoveredUnitTests[dl]);

            return testToDebug;
        }

        private static void FindAndExecuteForEachAssembly(string buildOutputRoot, DateTime timeFilter, Action<string> action, int? maxThreads = null)
        {
            int madDegreeOfParallelism = maxThreads.HasValue ? maxThreads.Value : Environment.ProcessorCount;
            Logger.LogInfo("FindAndExecuteForEachAssembly: Running with {0} threads.", madDegreeOfParallelism);
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".dll", ".exe" };
            Parallel.ForEach(
                Directory.EnumerateFiles(buildOutputRoot, "*").Where(s => extensions.Contains(Path.GetExtension(s))),
                new ParallelOptions { MaxDegreeOfParallelism = madDegreeOfParallelism },
                assemblyPath =>
                {
                    if (!File.Exists(Path.ChangeExtension(assemblyPath, ".pdb")))
                    {
                        return;
                    }

                    var lastWriteTime = File.GetLastWriteTimeUtc(assemblyPath);
                    if (lastWriteTime < timeFilter)
                    {
                        return;
                    }

                    Logger.LogInfo("FindAndExecuteForEachAssembly: Running for assembly {0}. LastWriteTime: {1}.", assemblyPath, lastWriteTime.ToLocalTime());
                    action(assemblyPath);
                });
        }

        private static void DiscoverUnitTests(IEnumerable<IXTestDiscoverer> tds, string slnPath, string slnSnapPath, string discoveredUnitTestsStore, string discoveredUnitDTestsStore, string buildOutputRoot, DateTime timeFilter, string[] ignoredTests)
        {
            Logger.LogInfo("DiscoverUnitTests: starting discovering.");
            var testsPerAssembly = new PerDocumentLocationXTestCases();
            var dtestsPerAssembly = new PerDocumentLocationDTestCases();
            FindAndExecuteForEachAssembly(
                buildOutputRoot,
                timeFilter,
                (string assemblyPath) =>
                {
                    var disc = new XUnitTestDiscoverer();
                    disc.TestDiscovered.AddHandler(
                        new FSharpHandler<XTestCase>(
                            (o, ea) =>
                            {
                                if (ea.CodeFilePath != null)
                                {
                                    var cfp = PathBuilder.rebaseCodeFilePath(FilePath.NewFilePath(slnPath), FilePath.NewFilePath(slnSnapPath), FilePath.NewFilePath(ea.CodeFilePath));
                                    ea.CodeFilePath = cfp.Item;
                                }
                                var dl = new DocumentLocation { document = FilePath.NewFilePath(ea.CodeFilePath), line = DocumentCoordinate.NewDocumentCoordinate(ea.LineNumber) };
                                var tests = testsPerAssembly.GetOrAdd(dl, _ => new ConcurrentBag<XTestCase>());
                                tests.Add(ea);
                                var dtests = dtestsPerAssembly.GetOrAdd(dl, _ => new ConcurrentBag<DTestCase>());
                                dtests.Add(FromXTestCase(ea));
                            }));
                    disc.DiscoverTests(tds, FilePath.NewFilePath(assemblyPath), ignoredTests);
                });

            testsPerAssembly.Serialize(FilePath.NewFilePath(discoveredUnitTestsStore));
            dtestsPerAssembly.Serialize(FilePath.NewFilePath(discoveredUnitDTestsStore));
            Logger.LogInfo("Written discovered unit tests to {0} & {1}.", discoveredUnitTestsStore, discoveredUnitDTestsStore);
        }

        private static DTestCase FromXTestCase(XTestCase ea)
        {
            return new DTestCase
            {
                CodeFilePath = FilePath.NewFilePath(ea.CodeFilePath),
                DisplayName = ea.DisplayName,
                DtcId = ea.Id,
                FullyQualifiedName = ea.FullyQualifiedName,
                LineNumber = DocumentCoordinate.NewDocumentCoordinate(ea.LineNumber),
                Source = FilePath.NewFilePath(ea.Source)
            };
        }

        private static bool ExecuteTestWithCoverageDataCollection(Func<bool> runTests, string codeCoverageStore)
        {
            bool allTestsPassed = true;
            var ccServer = new CoverageDataCollector();
            using (System.ServiceModel.ServiceHost serviceHost = new System.ServiceModel.ServiceHost(ccServer))
            {
                LogInfo("TestHost: Created Service Host.");
                string address = MarkerV1.CreateCodeCoverageDataCollectorEndpointAddress();
                NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
                serviceHost.AddServiceEndpoint(typeof(ICoverageDataCollector), binding, address);
                serviceHost.Open();
                LogInfo("TestHost: Opened _channel.");

                allTestsPassed = runTests();
                LogInfo("TestHost: Finished running test cases.");
            }
            ccServer.CoverageData.Serialize(FilePath.NewFilePath(codeCoverageStore));
            return allTestsPassed;
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogError("Exception thrown in InvokeEngine: {0}.", e.ExceptionObject);
        }

        private static bool RunTests(IEnumerable<IXTestExecutor> tes, string slnPath, string slnSnapPath, string buildRoot, string testResultsStore, string testFailureInfoStore, PerDocumentLocationXTestCases discoveredUnitTests)
        {
            Stopwatch stopWatch = new Stopwatch();

            LogInfo("TestHost executing tests...");
            stopWatch.Start();
            var testResults = new PerTestIdDResults();
            var testFailureInfo = new PerDocumentLocationTestFailureInfo();
            var tests = from dc in discoveredUnitTests.Keys
                        from t in discoveredUnitTests[dc]
                        group t by t.Source;
            Parallel.ForEach(
                tests,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                test =>
                {
                    LogInfo("Executing tests in {0}: Start.", test.Key);
                    var exec = new XUnitTestExecutor();
                    exec.TestExecuted.AddHandler(
                        new FSharpHandler<XTestResult>(
                            (o, ea) =>
                            {
                                NoteTestResults(testResults, ea, slnPath, slnSnapPath);
                                NoteTestFailureInfo(testFailureInfo, ea);
                            }));
                    exec.ExecuteTests(tes, test);
                    LogInfo("Executing tests in {0}: Done.", test.Key);
                });

            if (!_debuggerAttached)
            {
                testResults.Serialize(FilePath.NewFilePath(testResultsStore));
                testFailureInfo.Serialize(FilePath.NewFilePath(testFailureInfoStore));
            }

            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            var elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
            LogInfo("Done TestHost executing tests! [" + elapsedTime + "]");
            LogInfo("");

            var rrs =
                from tr in testResults
                from rr in tr.Value
                where rr.Outcome == DTestOutcome.TOFailed
                select rr;

            return !rrs.Any();
        }

        private static void NoteTestFailureInfo(PerDocumentLocationTestFailureInfo pdtfi, XTestResult tr)
        {
            LogInfo("Noting Test Failure Info: {0} - {1}", tr.DisplayName, tr.Outcome);

            TestFailureInfoExtensions.create(tr)
            .Aggregate(
                pdtfi,
                (acc, e) =>
                {
                    acc
                    .GetOrAdd(e.Item1, _ => new ConcurrentBag<TestFailureInfo>())
                    .Add(e.Item2);
                    return acc;
                });
        }

        private static void NoteTestResults(PerTestIdDResults testResults, XTestResult tr, string slnPath, string slnSnapPath)
        {
            LogInfo("Noting Test Result: {0} - {1}", tr.DisplayName, tr.Outcome);

            if (tr.TestCase.CodeFilePath != null)
            {
                var cfp = PathBuilder.rebaseCodeFilePath(FilePath.NewFilePath(slnPath), FilePath.NewFilePath(slnSnapPath), FilePath.NewFilePath(tr.TestCase.CodeFilePath));
                tr.TestCase.CodeFilePath = cfp.Item;
            }

            var testId = new TestId(
                FilePath.NewFilePath(tr.TestCase.Source),
                new DocumentLocation(
                    FilePath.NewFilePath(tr.TestCase.CodeFilePath),
                    DocumentCoordinate.NewDocumentCoordinate(tr.TestCase.LineNumber)));

            var results = testResults.GetOrAdd(testId, _ => new ConcurrentBag<DTestResult>());
            results.Add(FromXTestResult(tr));
        }

        private static DTestResult FromXTestResult(XTestResult tr)
        {
            Func<XStackFrame, string> sf2Str = 
                sf => 
                {
                    if (sf.IsXUnparsedFrame)
                    {
                        return (sf as XStackFrame.XUnparsedFrame).Item;
                    }
                    else if (sf.IsXParsedFrame)
                    {
                        var psf = (sf as XStackFrame.XParsedFrame);
                        return $"    at {psf.Item1} in {psf.Item2}:line {psf.Item3}";
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("sf");
                    }
                };
            Func<XStackFrame[], string> cs2Str = cs => cs.Aggregate(new StringBuilder(), (acc, e) => acc.AppendLine(sf2Str(e))).ToString();
            return new DTestResult
            {
                DisplayName = tr.DisplayName,
                ErrorMessage = tr.FailureInfo == FSharpOption<XTestFailureInfo>.None ? null : tr.FailureInfo.Value.Message,
                ErrorStackTrace = tr.FailureInfo == FSharpOption<XTestFailureInfo>.None ? null : cs2Str(tr.FailureInfo.Value.CallStack),
                Outcome = FromXTestOutcome(tr.Outcome),
                TestCase = FromXTestCase(tr.TestCase)
            };
        }

        private static readonly Dictionary<XTestOutcome, DTestOutcome>  outcomeMap = new Dictionary<XTestOutcome, DTestOutcome>
        {
            { XTestOutcome.Failed, DTestOutcome.TOFailed },
            { XTestOutcome.NoOutcome, DTestOutcome.TONone },
            { XTestOutcome.NotFound, DTestOutcome.TONotFound },
            { XTestOutcome.Passed, DTestOutcome.TOPassed },
            { XTestOutcome.Skipped, DTestOutcome.TOSkipped },
        };

        private static DTestOutcome FromXTestOutcome(XTestOutcome outcome)
        {
            return outcomeMap[outcome];
        }
    }
}
