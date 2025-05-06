using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement;
using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement.Core;
using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement.Rules;
using Moq;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Resources;
using System.Globalization;
using MultiFactor.SelfService.Linux.Portal.Core;

namespace MultiFactor.SelfService.Linux.Portal.Tests.Settings.PasswordRequirement
{
    public class PasswordRequirementsServiceTests
    {
        private readonly PasswordRequirementsService _service;
        private readonly List<PasswordRequirementRule> _rules;
        private readonly PasswordRequirementLocalizer _localizer;

        public PasswordRequirementsServiceTests()
        {
            var mockStringLocalizer = new Mock<IStringLocalizer<PasswordRequirementResource>>();
            
            mockStringLocalizer.Setup(x => x[It.IsAny<string>()])
                .Returns<string>(key => new LocalizedString(key, GetLocalizedValue(key)));

            _localizer = new PasswordRequirementLocalizer(mockStringLocalizer.Object);

            _rules = new List<PasswordRequirementRule>
            {
                CreateRule(Constants.PasswordRequirements.MIN_LENGTH),
                CreateRule(Constants.PasswordRequirements.MAX_LENGTH),
                CreateRule(Constants.PasswordRequirements.UPPER_CASE_LETTERS),
                CreateRule(Constants.PasswordRequirements.LOWER_CASE_LETTERS),
                CreateRule(Constants.PasswordRequirements.DIGITS),
                CreateRule(Constants.PasswordRequirements.SPECIAL_SYMBOLS)
            };

            var settings = new PortalSettings
            {
                PasswordRequirements = new PasswordRequirementsSection
                {
                    PwdRequirement = new List<PasswordRequirementItem>()
                }
            };

            _service = new PasswordRequirementsService(settings, _rules);
        }

        private string GetLocalizedValue(string key)
        {
            return key switch
            {
                Constants.PasswordRequirements.UPPER_CASE_LETTERS => "Password must contain at least one uppercase letter",
                Constants.PasswordRequirements.LOWER_CASE_LETTERS => "Password must contain at least one lowercase letter",
                Constants.PasswordRequirements.DIGITS => "Password must contain at least one digit",
                Constants.PasswordRequirements.SPECIAL_SYMBOLS => "Password must contain at least one special symbol",
                Constants.PasswordRequirements.MIN_LENGTH => "Password must be at least {0} characters long",
                Constants.PasswordRequirements.MAX_LENGTH => "Password must not exceed {0} characters",
                _ => key
            };
        }

        private PasswordRequirementRule CreateRule(string condition, int? value = null, string descriptionEn = null, string descriptionRu = null)
        {
            return condition switch
            {
                Constants.PasswordRequirements.MIN_LENGTH => new MinLengthRule(value ?? 8, condition: condition, localizer: _localizer, descriptionEn: descriptionEn, descriptionRu: descriptionRu),
                Constants.PasswordRequirements.MAX_LENGTH => new MaxLengthRule(value ?? 25, condition: condition, localizer: _localizer, descriptionEn: descriptionEn, descriptionRu: descriptionRu),
                Constants.PasswordRequirements.UPPER_CASE_LETTERS => new UpperCaseLetterRule(condition: condition, localizer: _localizer, descriptionEn: descriptionEn, descriptionRu: descriptionRu),
                Constants.PasswordRequirements.LOWER_CASE_LETTERS => new LowerCaseLetterRule(condition: condition, localizer: _localizer, descriptionEn: descriptionEn, descriptionRu: descriptionRu),
                Constants.PasswordRequirements.DIGITS => new DigitRule(condition: condition, localizer: _localizer, descriptionEn: descriptionEn, descriptionRu: descriptionRu),
                Constants.PasswordRequirements.SPECIAL_SYMBOLS => new SpecialSymbolRule(condition: condition, localizer: _localizer, descriptionEn: descriptionEn, descriptionRu: descriptionRu),
                _ => throw new ArgumentException($"Unknown condition: {condition}")
            };
        }

        [Fact]
        public void ValidatePassword_NoRequirements_ReturnsValid()
        {
            // Arrange
            var password = "any-password";

            // Act
            var result = _service.ValidatePassword(password);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ToString());
        }

        [Fact]
        public void ValidatePassword_MinLengthRule_ValidatesCorrectly()
        {
            // Arrange
            var settings = new PortalSettings
            {
                PasswordRequirements = new PasswordRequirementsSection
                {
                    PwdRequirement = new List<PasswordRequirementItem>
                    {
                        new PasswordRequirementItem
                        {
                            Enabled = true,
                            Condition = Constants.PasswordRequirements.MIN_LENGTH,
                            Value = "8"
                        }
                    }
                }
            };
            var service = new PasswordRequirementsService(settings, _rules);

            // Act & Assert
            Assert.True(service.ValidatePassword("password123").IsValid);
            Assert.False(service.ValidatePassword("pass").IsValid);
        }

        [Fact]
        public void ValidatePassword_MaxLengthRule_ValidatesCorrectly()
        {
            // Arrange
            var settings = new PortalSettings
            {
                PasswordRequirements = new PasswordRequirementsSection
                {
                    PwdRequirement = new List<PasswordRequirementItem>
                    {
                        new PasswordRequirementItem
                        {
                            Enabled = true,
                            Condition = Constants.PasswordRequirements.MAX_LENGTH,
                            Value = "10"
                        }
                    }
                }
            };
            
            var rules = new List<PasswordRequirementRule>
            {
                CreateRule(Constants.PasswordRequirements.MAX_LENGTH, 10)
            };

            var service = new PasswordRequirementsService(settings, rules);

            // Act & Assert
            Assert.True(service.ValidatePassword("password12").IsValid);
            Assert.False(service.ValidatePassword("password123456").IsValid);
        }

        [Fact]
        public void ValidatePassword_UpperCaseRule_ValidatesCorrectly()
        {
            // Arrange
            var settings = new PortalSettings
            {
                PasswordRequirements = new PasswordRequirementsSection
                {
                    PwdRequirement = new List<PasswordRequirementItem>
                    {
                        new PasswordRequirementItem
                        {
                            Enabled = true,
                            Condition = Constants.PasswordRequirements.UPPER_CASE_LETTERS
                        }
                    }
                }
            };
            var service = new PasswordRequirementsService(settings, _rules);

            // Act & Assert
            Assert.True(service.ValidatePassword("Password123").IsValid);
            Assert.False(service.ValidatePassword("password123").IsValid);
        }

        [Fact]
        public void ValidatePassword_LowerCaseRule_ValidatesCorrectly()
        {
            // Arrange
            var settings = new PortalSettings
            {
                PasswordRequirements = new PasswordRequirementsSection
                {
                    PwdRequirement = new List<PasswordRequirementItem>
                    {
                        new PasswordRequirementItem
                        {
                            Enabled = true,
                            Condition = Constants.PasswordRequirements.LOWER_CASE_LETTERS
                        }
                    }
                }
            };
            var service = new PasswordRequirementsService(settings, _rules);

            // Act & Assert
            Assert.True(service.ValidatePassword("Password123").IsValid);
            Assert.False(service.ValidatePassword("PASSWORD123").IsValid);
        }

        [Fact]
        public void ValidatePassword_DigitRule_ValidatesCorrectly()
        {
            // Arrange
            var settings = new PortalSettings
            {
                PasswordRequirements = new PasswordRequirementsSection
                {
                    PwdRequirement = new List<PasswordRequirementItem>
                    {
                        new PasswordRequirementItem
                        {
                            Enabled = true,
                            Condition = Constants.PasswordRequirements.DIGITS
                        }
                    }
                }
            };
            var service = new PasswordRequirementsService(settings, _rules);

            // Act & Assert
            Assert.True(service.ValidatePassword("Password123").IsValid);
            Assert.False(service.ValidatePassword("Password").IsValid);
        }

        [Fact]
        public void ValidatePassword_SpecialSymbolRule_ValidatesCorrectly()
        {
            // Arrange
            var settings = new PortalSettings
            {
                PasswordRequirements = new PasswordRequirementsSection
                {
                    PwdRequirement = new List<PasswordRequirementItem>
                    {
                        new PasswordRequirementItem
                        {
                            Enabled = true,
                            Condition = Constants.PasswordRequirements.SPECIAL_SYMBOLS
                        }
                    }
                }
            };
            var service = new PasswordRequirementsService(settings, _rules);

            // Act & Assert
            Assert.True(service.ValidatePassword("Password123!").IsValid);
            Assert.False(service.ValidatePassword("Password123").IsValid);
        }

        [Fact]
        public void ValidatePassword_MultipleRules_ValidatesCorrectly()
        {
            // Arrange
            var settings = new PortalSettings
            {
                PasswordRequirements = new PasswordRequirementsSection
                {
                    PwdRequirement = new List<PasswordRequirementItem>
                    {
                        new PasswordRequirementItem
                        {
                            Enabled = true,
                            Condition = Constants.PasswordRequirements.MIN_LENGTH,
                            Value = "8"
                        },
                        new PasswordRequirementItem
                        {
                            Enabled = true,
                            Condition = Constants.PasswordRequirements.UPPER_CASE_LETTERS
                        },
                        new PasswordRequirementItem
                        {
                            Enabled = true,
                            Condition = Constants.PasswordRequirements.DIGITS
                        }
                    }
                }
            };
            var service = new PasswordRequirementsService(settings, _rules);

            // Act & Assert
            Assert.True(service.ValidatePassword("Password123").IsValid);
            Assert.False(service.ValidatePassword("pass").IsValid); // Too short
            Assert.False(service.ValidatePassword("password123").IsValid); // No uppercase
            Assert.False(service.ValidatePassword("Password").IsValid); // No digits
        }

        [Fact]
        public void ValidatePassword_DisabledRules_AreIgnored()
        {
            // Arrange
            var settings = new PortalSettings
            {
                PasswordRequirements = new PasswordRequirementsSection
                {
                    PwdRequirement = new List<PasswordRequirementItem>
                    {
                        new PasswordRequirementItem
                        {
                            Enabled = false,
                            Condition = Constants.PasswordRequirements.MIN_LENGTH,
                            Value = "8"
                        },
                        new PasswordRequirementItem
                        {
                            Enabled = true,
                            Condition = Constants.PasswordRequirements.UPPER_CASE_LETTERS
                        }
                    }
                }
            };
            var service = new PasswordRequirementsService(settings, _rules);

            // Act & Assert
            Assert.True(service.ValidatePassword("Pass").IsValid); // Short but has uppercase
            Assert.False(service.ValidatePassword("pass").IsValid); // No uppercase
        }

        [Fact]
        public void ValidatePassword_InvalidCondition_IsIgnored()
        {
            // Arrange
            var settings = new PortalSettings
            {
                PasswordRequirements = new PasswordRequirementsSection
                {
                    PwdRequirement = new List<PasswordRequirementItem>
                    {
                        new PasswordRequirementItem
                        {
                            Enabled = true,
                            Condition = "invalid-rule"
                        },
                        new PasswordRequirementItem
                        {
                            Enabled = true,
                            Condition = Constants.PasswordRequirements.UPPER_CASE_LETTERS
                        }
                    }
                }
            };
            var service = new PasswordRequirementsService(settings, _rules);

            // Act & Assert
            Assert.True(service.ValidatePassword("Pass").IsValid); // Valid because invalid rule is ignored
            Assert.False(service.ValidatePassword("pass").IsValid); // Fails uppercase rule
        }

        [Fact]
        public void GetRule_ReturnsCorrectRule()
        {
            // Act & Assert
            Assert.NotNull(_service.GetRule(Constants.PasswordRequirements.MIN_LENGTH));
            Assert.NotNull(_service.GetRule(Constants.PasswordRequirements.MAX_LENGTH));
            Assert.NotNull(_service.GetRule(Constants.PasswordRequirements.UPPER_CASE_LETTERS));
            Assert.NotNull(_service.GetRule(Constants.PasswordRequirements.LOWER_CASE_LETTERS));
            Assert.NotNull(_service.GetRule(Constants.PasswordRequirements.DIGITS));
            Assert.NotNull(_service.GetRule(Constants.PasswordRequirements.SPECIAL_SYMBOLS));
            Assert.Null(_service.GetRule("invalid-rule"));
        }

        [Fact]
        public void RuleLocalizedDescriptions_AreCorrectlySet()
        {
            // Arrange & Act
            var minLengthRule = CreateRule(Constants.PasswordRequirements.MIN_LENGTH, 8,
                "Password must be at least {0} characters long",
                "Пароль должен содержать не менее {0} символов");
            var maxLengthRule = CreateRule(Constants.PasswordRequirements.MAX_LENGTH, 25,
                "Password must not exceed {0} characters",
                "Пароль не должен превышать {0} символов");
            var upperCaseRule = CreateRule(Constants.PasswordRequirements.UPPER_CASE_LETTERS,
                descriptionEn: "Password must contain at least one uppercase letter",
                descriptionRu: "Пароль должен содержать хотя бы одну заглавную букву");
            var lowerCaseRule = CreateRule(Constants.PasswordRequirements.LOWER_CASE_LETTERS,
                descriptionEn: "Password must contain at least one lowercase letter",
                descriptionRu: "Пароль должен содержать хотя бы одну строчную букву");
            var digitRule = CreateRule(Constants.PasswordRequirements.DIGITS,
                descriptionEn: "Password must contain at least one digit",
                descriptionRu: "Пароль должен содержать хотя бы одну цифру");
            var specialSymbolRule = CreateRule(Constants.PasswordRequirements.SPECIAL_SYMBOLS,
                descriptionEn: "Password must contain at least one special symbol",
                descriptionRu: "Пароль должен содержать хотя бы один специальный символ");

            // Assert English culture
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Assert.Equal(string.Format("Password must be at least {0} characters long", 8), minLengthRule.GetLocalizedDescription());
            Assert.Equal(string.Format("Password must not exceed {0} characters", 25), maxLengthRule.GetLocalizedDescription());
            Assert.Equal("Password must contain at least one uppercase letter", upperCaseRule.GetLocalizedDescription());
            Assert.Equal("Password must contain at least one lowercase letter", lowerCaseRule.GetLocalizedDescription());
            Assert.Equal("Password must contain at least one digit", digitRule.GetLocalizedDescription());
            Assert.Equal("Password must contain at least one special symbol", specialSymbolRule.GetLocalizedDescription());

            // Assert Russian culture
            Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");
            Assert.Equal(string.Format("Пароль должен содержать не менее {0} символов", 8), minLengthRule.GetLocalizedDescription());
            Assert.Equal(string.Format("Пароль не должен превышать {0} символов", 25), maxLengthRule.GetLocalizedDescription());
            Assert.Equal("Пароль должен содержать хотя бы одну заглавную букву", upperCaseRule.GetLocalizedDescription());
            Assert.Equal("Пароль должен содержать хотя бы одну строчную букву", lowerCaseRule.GetLocalizedDescription());
            Assert.Equal("Пароль должен содержать хотя бы одну цифру", digitRule.GetLocalizedDescription());
            Assert.Equal("Пароль должен содержать хотя бы один специальный символ", specialSymbolRule.GetLocalizedDescription());
        }

        [Fact]
        public void RuleDescriptions_WithCustomValues_AreCorrectlyFormatted()
        {
            // Arrange & Act
            var minLengthRule = CreateRule(Constants.PasswordRequirements.MIN_LENGTH, 12,
                "Password must be at least 12 characters long",
                "Пароль должен содержать не менее 12 символов");
            var maxLengthRule = CreateRule(Constants.PasswordRequirements.MAX_LENGTH, 20,
                "Password must not exceed 20 characters",
                "Пароль не должен превышать 20 символов");

            // Assert
            Assert.Equal("Password must be at least 12 characters long", minLengthRule.DescriptionEn);
            Assert.Equal("Password must not exceed 20 characters", maxLengthRule.DescriptionEn);
            Assert.Equal("Пароль должен содержать не менее 12 символов", minLengthRule.DescriptionRu);
            Assert.Equal("Пароль не должен превышать 20 символов", maxLengthRule.DescriptionRu);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Qwerty")]
        public void ValidatePassword_DefaultRequirements_ReturnsSuccess(string password)
        {
            // Arrange
            var settings = new PortalSettings
            {
                PasswordRequirements = new PasswordRequirementsSection
                {
                    PwdRequirement = new List<PasswordRequirementItem>()
                }
            };
            var service = new PasswordRequirementsService(settings, _rules);

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ToString());
        }
    }
} 