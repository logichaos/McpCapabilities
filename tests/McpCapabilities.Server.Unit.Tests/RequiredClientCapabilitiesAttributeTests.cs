using McpCapabilities.Server;

namespace McpCapabilities.Server.Unit.Tests;

public class RequiredClientCapabilitiesAttributeTests
{
  [Test]
  public async Task AttributeUsage_IsRestrictedToMethods()
  {
    var attributeUsage = typeof(RequiredClientCapabilitiesAttribute)
        .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
        .Cast<AttributeUsageAttribute>()
        .First();

    await Assert.That(attributeUsage.ValidOn).IsEqualTo(AttributeTargets.Method);
  }

  [Test]
  public async Task AttributeUsage_IsNotInheritable()
  {
    var attributeUsage = typeof(RequiredClientCapabilitiesAttribute)
        .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
        .Cast<AttributeUsageAttribute>()
        .First();

    await Assert.That(attributeUsage.Inherited).IsFalse();
  }

  [Test]
  public async Task AttributeUsage_DoesNotAllowMultiple()
  {
    var attributeUsage = typeof(RequiredClientCapabilitiesAttribute)
        .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
        .Cast<AttributeUsageAttribute>()
        .First();

    await Assert.That(attributeUsage.AllowMultiple).IsFalse();
  }

  [Test]
  public async Task Required_PropertyIsInitOnly()
  {
    var prop = typeof(RequiredClientCapabilitiesAttribute).GetProperty("Required");

    await Assert.That(prop).IsNotNull();
    await Assert.That(prop!.PropertyType).IsEqualTo(typeof(CapabilityFlag));
    await Assert.That(prop.CanWrite).IsTrue();
    await Assert.That(prop.CanRead).IsTrue();
  }

  [Test]
  public async Task Message_PropertyIsOptionalString()
  {
    var prop = typeof(RequiredClientCapabilitiesAttribute).GetProperty("Message");

    await Assert.That(prop).IsNotNull();
    await Assert.That(prop!.PropertyType).IsEqualTo(typeof(string));
    await Assert.That(prop.CanWrite).IsTrue();
    await Assert.That(prop.CanRead).IsTrue();
  }

  [Test]
  public async Task CreateInstance_WithRequiredOnly_SetsRequired()
  {
    var attr = new RequiredClientCapabilitiesAttribute
    {
      Required = CapabilityFlag.Sampling | CapabilityFlag.Elicitation,
    };

    await Assert.That(attr.Required).IsEqualTo(CapabilityFlag.Sampling | CapabilityFlag.Elicitation);
    await Assert.That(attr.Message).IsNull();
  }

  [Test]
  public async Task CreateInstance_WithRequiredAndMessage_SetsBoth()
  {
    var attr = new RequiredClientCapabilitiesAttribute
    {
      Required = CapabilityFlag.Sampling,
      Message = "Needs LLM support",
    };

    await Assert.That(attr.Required).IsEqualTo(CapabilityFlag.Sampling);
    await Assert.That(attr.Message).IsEqualTo("Needs LLM support");
  }

  [Test]
  public async Task Attribute_IsSealed()
  {
    await Assert.That(typeof(RequiredClientCapabilitiesAttribute).IsSealed).IsTrue();
  }
}