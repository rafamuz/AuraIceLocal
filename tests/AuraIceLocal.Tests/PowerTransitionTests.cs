namespace AuraIceLocal.Tests;

public sealed class PowerTransitionTests
{
    [Fact]
    public void RunningMonitoringCreatesResumeRequest()
    {
        var coordinator = new PowerTransitionCoordinator();

        coordinator.Suspend(monitoringWasRunning: true);

        PowerResumeRequest request = Assert.IsType<PowerResumeRequest>(coordinator.Resume());
        Assert.True(coordinator.IsPending(request));
    }

    [Fact]
    public void StoppedMonitoringStaysStoppedAfterResume()
    {
        var coordinator = new PowerTransitionCoordinator();

        coordinator.Suspend(monitoringWasRunning: false);

        Assert.Null(coordinator.Resume());
    }

    [Fact]
    public void NewSuspendInvalidatesOlderResumeAttempt()
    {
        var coordinator = new PowerTransitionCoordinator();
        coordinator.Suspend(monitoringWasRunning: true);
        PowerResumeRequest oldRequest = Assert.IsType<PowerResumeRequest>(coordinator.Resume());

        coordinator.Suspend(monitoringWasRunning: true);
        PowerResumeRequest currentRequest = Assert.IsType<PowerResumeRequest>(coordinator.Resume());

        Assert.False(coordinator.IsPending(oldRequest));
        Assert.True(coordinator.IsPending(currentRequest));
    }

    [Fact]
    public void CompletionOrManualCancellationPreventsAnotherResume()
    {
        var coordinator = new PowerTransitionCoordinator();
        coordinator.Suspend(monitoringWasRunning: true);
        PowerResumeRequest completedRequest = Assert.IsType<PowerResumeRequest>(coordinator.Resume());
        coordinator.Complete(completedRequest);

        Assert.Null(coordinator.Resume());

        coordinator.Suspend(monitoringWasRunning: true);
        coordinator.Cancel();

        Assert.Null(coordinator.Resume());
    }

    [Fact]
    public void CompletingOldRequestDoesNotCancelCurrentResume()
    {
        var coordinator = new PowerTransitionCoordinator();
        coordinator.Suspend(monitoringWasRunning: true);
        PowerResumeRequest oldRequest = Assert.IsType<PowerResumeRequest>(coordinator.Resume());
        coordinator.Suspend(monitoringWasRunning: true);
        PowerResumeRequest currentRequest = Assert.IsType<PowerResumeRequest>(coordinator.Resume());

        coordinator.Complete(oldRequest);

        Assert.True(coordinator.IsPending(currentRequest));
    }
}
