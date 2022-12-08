using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.AdditionalClaimValueSources;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Tests
{
    public class AdditionalClaimDescriptorFactoryTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Create_InvalidName_ShouldThrow(string name)
        {
            var claim = new Claim
            {
                Name = name
            };
            
            var ex = Assert.Throws<InvalidClaimDescriptionException>(() => AdditionalClaimDescriptorFactory.Create(claim));
            Assert.Equal($"Invalid claim '{name}' description", ex.Message);
        }

        [Fact]
        public void Create_WitnNoSources_ShouldThrow()
        {
            var claim = new Claim
            {
                Name = "Class"
            };

            var ex = Assert.Throws<InvalidClaimDescriptionException>(() => AdditionalClaimDescriptorFactory.Create(claim));
            Assert.Equal("Invalid claim 'Class' description", ex.Message);
        }

        [Fact]
        public void Create_WithValue_ShouldReturnDescriptorWithLiteralSource()
        {
            var claim = new Claim
            {
                Name = "Class",
                Value = "888"
            };

            var desc = AdditionalClaimDescriptorFactory.Create(claim);
            Assert.IsType<LiteralClaimValueSource>(desc.Source);
        }

        [Fact]
        public void Create_WithValueAndFrom_ShouldReturnDescriptorWithLiteralSource()
        {
            var claim = new Claim
            {
                Name = "Class",
                Value = "888",
                From = "UserName"
            };

            var desc = AdditionalClaimDescriptorFactory.Create(claim);
            Assert.IsType<LiteralClaimValueSource>(desc.Source);
        }
        
        [Fact]
        public void Create_WithFrom_ShouldReturnDescriptorWithReservedSource()
        {
            var claim = new Claim
            {
                Name = "Class",
                From = "UserName"
            };

            var desc = AdditionalClaimDescriptorFactory.Create(claim);
            Assert.IsType<ReservedValueClaimValueSource>(desc.Source);
        }

        [Fact]
        public void Create_WithFrom_ShouldReturnDescriptorWithAttributeSource()
        {
            var claim = new Claim
            {
                Name = "Class",
                From = "samaccountname"
            };

            var desc = AdditionalClaimDescriptorFactory.Create(claim);
            Assert.IsType<AttributeClaimValueSource>(desc.Source);
        }

        [Fact]
        public void Create_WithWHen_ShouldReturnDescriptorWithCondition()
        {
            var claim = new Claim
            {
                Name = "Class",
                From = "samaccountname",
                When = "samaccountname='user.name'"
            };

            var desc = AdditionalClaimDescriptorFactory.Create(claim);
            Assert.NotNull(desc.Condition);
        }
    }
}
