using McpCapabilities.Server;

namespace McpCapabilities.Server.Unit.Tests;

public class CapabilityFlagTests
{
    [Test]
    public async Task None_HasValueZero()
    {
        var value = (int)CapabilityFlag.None;
        await Assert.That(value).IsEqualTo(0);
    }

    [Test]
    public async Task Enum_HasFlagsAttribute()
    {
        var type = typeof(CapabilityFlag);
        var hasFlags = type.GetCustomAttributes(typeof(FlagsAttribute), inherit: false).Length > 0;

        await Assert.That(hasFlags).IsTrue();
    }

    [Test]
    public async Task Enum_ContainsAllClientCapabilityValues()
    {
        var values = Enum.GetValues<CapabilityFlag>();

        await Assert.That(values).Contains(CapabilityFlag.Sampling);
        await Assert.That(values).Contains(CapabilityFlag.Roots);
        await Assert.That(values).Contains(CapabilityFlag.Elicitation);
        await Assert.That(values).Contains(CapabilityFlag.ElicitationForm);
        await Assert.That(values).Contains(CapabilityFlag.ElicitationUrl);
        await Assert.That(values).Contains(CapabilityFlag.Tasks);
        await Assert.That(values).Contains(CapabilityFlag.TaskList);
        await Assert.That(values).Contains(CapabilityFlag.TaskCancel);
        await Assert.That(values).Contains(CapabilityFlag.TaskAugmentedSampling);
        await Assert.That(values).Contains(CapabilityFlag.TaskAugmentedElicitation);
    }

    [Test]
    public async Task IsSatisfied_AllRequiredPresent_ReturnsTrue()
    {
        var required = CapabilityFlag.Sampling | CapabilityFlag.Elicitation;
        var available = CapabilityFlag.Sampling | CapabilityFlag.Elicitation | CapabilityFlag.Roots;

        var result = CapabilityFlags.IsSatisfied(required, available);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsSatisfied_PartialFlagsMissing_ReturnsFalse()
    {
        var required = CapabilityFlag.Sampling | CapabilityFlag.Elicitation;
        var available = CapabilityFlag.Sampling;

        var result = CapabilityFlags.IsSatisfied(required, available);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsSatisfied_NoneRequired_ReturnsTrue()
    {
        var required = CapabilityFlag.None;
        var available = CapabilityFlag.Sampling;

        var result = CapabilityFlags.IsSatisfied(required, available);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsSatisfied_NoneAvailableButSomeRequired_ReturnsFalse()
    {
        var required = CapabilityFlag.Sampling;
        var available = CapabilityFlag.None;

        var result = CapabilityFlags.IsSatisfied(required, available);

        await Assert.That(result).IsFalse();
    }
}
