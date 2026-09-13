using Xunit;

// WebApplicationFactory<Program> reflectively invokes the minimal-hosting entry point's Main via
// HostFactoryResolver, which relies on a process-wide DiagnosticListener subscription. Running
// multiple factories concurrently (the shared fixture plus RedisFailureFallbackTests' own factory)
// races that mechanism and can crash the test host, so all tests in this assembly run sequentially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
