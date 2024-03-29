﻿using Microsoft.Extensions.Logging;
using Moq;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.AdditionalClaimValueSources;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description.Conditions;

namespace MultiFactor.SelfService.Linux.Portal.Tests
{
    public class ClaimConditionEvaluatorTests
    {
        [Theory]
        [InlineData("", "")]
        [InlineData(null, null)]
        [InlineData(" ", " ")]
        [InlineData("value", "value")]
        public void Evaluate_EqOperationWithLiterals_ShouldReturnTrue(string left, string right)
        {
            var ctxMock = new Mock<IApplicationValuesContext>();
            ctxMock.Setup(x => x[It.IsAny<string>()]).Returns(new List<string>());

            var loggerMock = new Mock<ILogger<ClaimConditionEvaluator>>();
            var evaluator = new ClaimConditionEvaluator(ctxMock.Object, loggerMock.Object);
            var condition = new ClaimCondition(
                new LiteralClaimValueSource(left), 
                new LiteralClaimValueSource(right), 
                ClaimsConditionOperation.Eq);

            var result = evaluator.Evaluate(condition);

            Assert.True(result);
        }
        
        [Theory]
        [InlineData("uid", "", new[] { "" })]
        [InlineData("uid", "u.user", new[] { "u.user" })]
        [InlineData("memberOf", "VPN Users", new[] { "Users", "VPN Users", "Domain Users" })]
        public void Evaluate_EqOperationWithLiteralAndAttr_ShouldReturnTrue(string attr, string lit, IReadOnlyList<string> attrValues)
        {
            var ctxMock = new Mock<IApplicationValuesContext>();
            ctxMock.Setup(x => x[It.Is<string>(v => v == attr)]).Returns(attrValues);

            var loggerMock = new Mock<ILogger<ClaimConditionEvaluator>>();
            var evaluator = new ClaimConditionEvaluator(ctxMock.Object, loggerMock.Object);
            var condition = new ClaimCondition( 
                new AttributeClaimValueSource(attr),
                new LiteralClaimValueSource(lit),
                ClaimsConditionOperation.Eq);

            var result = evaluator.Evaluate(condition);

            Assert.True(result);
        }
        
        [Theory]
        [InlineData(new[] { "VPN Users", "Users", "Domain Users" }, new[] {  "Domain Users", "VPN Users", "Users" })]
        [InlineData(new[] { "VPN Users" }, new[] {  "Domain Users", "VPN Users" })]
        public void Evaluate_EqOperationWithAttrsAndAttrs_ShouldReturnTrue(IReadOnlyList<string> leftValues, IReadOnlyList<string> rightValues)
        {
            var ctxMock = new Mock<IApplicationValuesContext>();
            ctxMock.Setup(x => x[It.Is<string>(v => v == "left")]).Returns(leftValues);
            ctxMock.Setup(x => x[It.Is<string>(v => v == "right")]).Returns(rightValues);

            var loggerMock = new Mock<ILogger<ClaimConditionEvaluator>>();
            var evaluator = new ClaimConditionEvaluator(ctxMock.Object, loggerMock.Object);
            var condition = new ClaimCondition(
                new AttributeClaimValueSource("left"),
                new AttributeClaimValueSource("right"),
                ClaimsConditionOperation.Eq);

            var result = evaluator.Evaluate(condition);

            Assert.True(result);
        }
        
        [Theory]
        [InlineData(new[] { "VPN Users", "Users", "Domain Users" }, new[] {  "Domain Users", "VPN Users" })]
        [InlineData(new[] { "VPN Users" }, new[] {  "Domain Users" })]
        public void Evaluate_EqOperationWithAttrsAndAttrs_ShouldReturnFalse(IReadOnlyList<string> leftValues, IReadOnlyList<string> rightValues)
        {
            var ctxMock = new Mock<IApplicationValuesContext>();
            ctxMock.Setup(x => x[It.Is<string>(v => v == "left")]).Returns(leftValues);
            ctxMock.Setup(x => x[It.Is<string>(v => v == "right")]).Returns(rightValues);

            var loggerMock = new Mock<ILogger<ClaimConditionEvaluator>>();
            var evaluator = new ClaimConditionEvaluator(ctxMock.Object, loggerMock.Object);
            var condition = new ClaimCondition(
                new AttributeClaimValueSource("left"),
                new AttributeClaimValueSource("right"),
                ClaimsConditionOperation.Eq);

            var result = evaluator.Evaluate(condition);

            Assert.False(result);
        }
    }
}
