using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.AdditionalClaimValueSources;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description.Conditions;

namespace MultiFactor.SelfService.Linux.Portal.Tests
{
    public class AdditionalClaimConditionParserTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("value")]
        [InlineData("value=")]
        [InlineData("=")]
        [InlineData("=value")]
        public void Parse_InvalidValue_ShouldThrow(string value)
        {
            var ex = Assert.Throws<InvalidClaimConditionException>(() => ClaimConditionParser.Parse(value));
            Assert.Equal($"Invalid expression: {value}", ex.Message);
        }

        [Fact]
        public void Parse_CorrectValue_ShouldReturnConditionObject()
        {
            var cond = ClaimConditionParser.Parse("value=value");
            Assert.NotNull(cond);
        }
        
        [Theory]
        [InlineData("=", ClaimsConditionOperation.Eq)]
        public void Parse_CorrectValue_ShouldReturnCorrectOperation(string opString, ClaimsConditionOperation parsed)
        {
            var cond = ClaimConditionParser.Parse($"value{opString}value");
            Assert.Equal(parsed, cond.Operation);
        }

        [Theory]
        [InlineData("'literal'", typeof(LiteralClaimValueSource))]
        [InlineData("'UserName'", typeof(LiteralClaimValueSource))]
        [InlineData("UserName", typeof(ReservedValueClaimValueSource))]
        [InlineData("userName", typeof(ReservedValueClaimValueSource))]
        [InlineData("UserGroup", typeof(ReservedValueClaimValueSource))]
        [InlineData("attr", typeof(AttributeClaimValueSource))]
        public void Parse_CorrectValue_ShouldReturnCorrectValueSource(string value, Type valueSourceType)
        {
            var cond = ClaimConditionParser.Parse($"{value}={value}");
            Assert.IsType(valueSourceType, cond.LeftOperand);
            Assert.IsType(valueSourceType, cond.RightOperand);
        }
    }
}
