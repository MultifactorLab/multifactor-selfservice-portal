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
        [InlineData("~value")]
        [InlineData("value~")]
        [InlineData("~")]
        [InlineData("$attr=422")]
        public void Parse_InvalidValue_ShouldThrow(string value)
        {
            var ex = Assert.Throws<InvalidClaimConditionException>(() => ClaimConditionParser.Parse(value));
            Assert.StartsWith($"Invalid expression: ", ex.Message);
        }

        [Theory]
        [InlineData("value=value")]
        [InlineData("$username=123")]
        [InlineData("@username=def")]
        [InlineData("@username=$username")]
        public void Parse_CorrectValue_ShouldReturnConditionObject(string expression)
        {
            var cond = ClaimConditionParser.Parse(expression);
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
        [InlineData("literal", typeof(LiteralClaimValueSource))]
        [InlineData("UserName", typeof(LiteralClaimValueSource))]
        [InlineData("$UserName", typeof(ReservedValueClaimValueSource))]
        [InlineData("$userName", typeof(ReservedValueClaimValueSource))]
        [InlineData("$UserGroup", typeof(ReservedValueClaimValueSource))]
        [InlineData("@attr", typeof(AttributeClaimValueSource))]
        public void Parse_CorrectValue_ShouldReturnCorrectValueSource(string value, Type valueSourceType)
        {
            var cond = ClaimConditionParser.Parse($"{value}={value}");
            Assert.IsType(valueSourceType, cond.LeftOperand);
            Assert.IsType(valueSourceType, cond.RightOperand);
        }
    }
}
