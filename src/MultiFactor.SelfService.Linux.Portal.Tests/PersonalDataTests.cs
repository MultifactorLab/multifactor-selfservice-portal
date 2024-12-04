using MultiFactor.SelfService.Linux.Portal.Settings.PrivacyMode;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;

namespace MultiFactor.SelfService.Linux.Portal.Tests;

public class PersonalDataTests
{
    [Fact]
    public void CreatePersonalData_NonePrivacyMode_ShouldFillAllProperties()
    {
        var name = "name";
        var email = "email";
        var phone = "phone";
        var descriptor = PrivacyModeDescriptor.Create(null);
        var personalData = new PersonalData(name, email, phone, descriptor);
        Assert.Equal(name, personalData.Name);
        Assert.Equal(email, personalData.Email);
        Assert.Equal(phone, personalData.Phone);
    }
    
    [Fact]
    public void CreatePersonalData_FullPrivacyMode_AllPropertiesShouldBeNull()
    {
        var name = "name";
        var email = "email";
        var phone = "phone";
        var descriptor = PrivacyModeDescriptor.Create("Full");
        var personalData = new PersonalData(name, email, phone, descriptor);
        Assert.Null(personalData.Name);
        Assert.Null(personalData.Email);
        Assert.Null( personalData.Phone);
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("Phone")]
    [InlineData("Email")]
    public void CreatePersonalData_PartialPrivacyMode_ProvidedElementNotNul(string element)
    {
        var name = "name";
        var email = "email";
        var phone = "phone";
        var descriptor = PrivacyModeDescriptor.Create($"Partial:{element}");
        var personalData = new PersonalData(name, email, phone, descriptor);
        var properties = personalData.GetType().GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(personalData) as string;
            if (property.Name == element)
            {
                Assert.NotNull(value);
                Assert.Equal(element.ToLower(), value);
            }
            else
            {
                Assert.Null(value);
            }
        }
    }
}