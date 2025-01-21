using MultiFactor.SelfService.Linux.Portal.Settings.PrivacyMode;

namespace MultiFactor.SelfService.Linux.Portal.Tests;

public class PrivacyModeDescriptorTests
{
    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public void CreatePrivacyModeDescriptor_EmptyValue_ShouldReturnNoneMode(string emptyString)
    {
        var descriptor = PrivacyModeDescriptor.Create(emptyString);
        Assert.Equal(PrivacyMode.None, descriptor.Mode);
    }
    
    [Fact]
    public void CreatePrivacyModeDescriptor_ShouldReturnFullMode()
    {
        var descriptor = PrivacyModeDescriptor.Create("Full");
        Assert.Equal(PrivacyMode.Full, descriptor.Mode);
    }
    
    [Fact]
    public void CreatePrivacyModeDescriptor_ShouldReturnPartialModeWithoutFields()
    {
        var descriptor = PrivacyModeDescriptor.Create("Partial");
        Assert.Equal(PrivacyMode.Partial, descriptor.Mode);
    }
    
    [Theory]
    [InlineData("Name")]
    [InlineData("Phone")]
    [InlineData("Email")]
    [InlineData("000")]
    [InlineData("666")]
    [InlineData("@fsaq2fdsa2")]
    public void CreatePrivacyModeDescriptor_ShouldReturnPartialModeWithField(string fieldName)
    {
        var descriptor = PrivacyModeDescriptor.Create($"Partial:{fieldName}");
        Assert.Equal(PrivacyMode.Partial, descriptor.Mode);
        Assert.True(descriptor.HasField(fieldName));
    }
    
    [Fact]
    public void CreatePrivacyModeDescriptor_ShouldReturnPartialModeWithFields()
    {
        var name = "name";
        var phone = "phone";
        var email = "email";
        var descriptor = PrivacyModeDescriptor.Create($"Partial:{name},{phone},{email}");
        Assert.Equal(PrivacyMode.Partial, descriptor.Mode);
        Assert.True(descriptor.HasField(name));
        Assert.True(descriptor.HasField(phone));
        Assert.True(descriptor.HasField(email));
    }
}