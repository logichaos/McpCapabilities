using FluentResults;
using McpCapabilities.Server;

namespace McpCapabilities.Server.Unit.Tests;

public class CapabilityNotMetErrorTests
{
    [Test]
    public async Task Error_ImplementsIError()
    {
        var error = new CapabilityNotMetError(
            CapabilityFlag.Sampling | CapabilityFlag.Elicitation,
            CapabilityFlag.Elicitation,
            "ai_summarize",
            "Missing elicitation");

        await Assert.That(error).IsTypeOf<IError>();
    }

    [Test]
    public async Task Error_CarriesStructuredData()
    {
        var error = new CapabilityNotMetError(
            required: CapabilityFlag.Sampling | CapabilityFlag.Elicitation,
            missing: CapabilityFlag.Elicitation,
            primitiveName: "ai_summarize",
            message: "Missing elicitation");

        await Assert.That(error.Required).IsEqualTo(CapabilityFlag.Sampling | CapabilityFlag.Elicitation);
        await Assert.That(error.Missing).IsEqualTo(CapabilityFlag.Elicitation);
        await Assert.That(error.PrimitiveName).IsEqualTo("ai_summarize");
        await Assert.That(error.Message).IsEqualTo("Missing elicitation");
    }

    [Test]
    public async Task Error_HasMetadataForLogging()
    {
        var error = new CapabilityNotMetError(
            CapabilityFlag.Sampling,
            CapabilityFlag.Sampling,
            "test_tool");

        await Assert.That(error.Metadata).IsNotNull();
        await Assert.That(error.Metadata).ContainsKey("RequiredFlags");
        await Assert.That(error.Metadata).ContainsKey("MissingFlags");
        await Assert.That(error.Metadata).ContainsKey("PrimitiveName");

        await Assert.That(error.Metadata["RequiredFlags"]).IsEqualTo("Sampling");
        await Assert.That(error.Metadata["MissingFlags"]).IsEqualTo("Sampling");
        await Assert.That(error.Metadata["PrimitiveName"]).IsEqualTo("test_tool");
    }

    [Test]
    public async Task Error_DefaultMessage_IsGenerated()
    {
        var error = new CapabilityNotMetError(
            CapabilityFlag.Sampling | CapabilityFlag.Roots,
            CapabilityFlag.Roots,
            "test_tool");

        await Assert.That(error.Message).IsNotEmpty();
        await Assert.That(error.Message).Contains("test_tool");
        await Assert.That(error.Message).Contains("Sampling, Roots");
        await Assert.That(error.Message).Contains("Roots");
    }

    [Test]
    public async Task Error_IsCompatibleWithResultFail()
    {
        var error = new CapabilityNotMetError(
            CapabilityFlag.Sampling,
            CapabilityFlag.Sampling,
            "test_tool");

        var result = Result.Fail<string>(error);

        await Assert.That(result.IsFailed).IsTrue();
        await Assert.That(result.Errors).Contains(error);
    }

    [Test]
    public async Task Error_InResult_HasErrorsCount()
    {
        var error = new CapabilityNotMetError(
            CapabilityFlag.Sampling,
            CapabilityFlag.Sampling,
            "test_tool");

        var result = Result.Fail<string>(error);

        await Assert.That(result.Errors).Count().IsEqualTo(1);
    }
}
