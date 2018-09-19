using System;
using NUnit.Framework;

namespace Escc.Umbraco.Expiry.Tests
{
    [TestFixture]
    public class DocumentTypeRuleMatcherTests
    {
        [Test]
        public void AliasIsMatched()
        {
            var rules = new[] { new DocumentTypeExpiryRule() { Alias = "example" } };
            var matcher = new DocumentTypeRuleMatcher(rules);

            var result= matcher.MatchRule("example", null);

            Assert.IsNotNull(result);
        }

        [Test]
        public void AliasMatchIsCaseSensitive()
        {
            var rules = new[] { new DocumentTypeExpiryRule() { Alias = "Example" } };
            var matcher = new DocumentTypeRuleMatcher(rules);

            var result = matcher.MatchRule("example", null);

            Assert.IsNull(result);
        }

        [Test]
        public void LevelIsMatched()
        {
            var rules = new[] { new DocumentTypeExpiryRule() { Alias = "example", Level = 2 } };
            var matcher = new DocumentTypeRuleMatcher(rules);

            var result = matcher.MatchRule("example", 2);

            Assert.IsNotNull(result);
        }

        [Test]
        public void DifferentLevelIsNotMatched()
        {
            var rules = new[] { new DocumentTypeExpiryRule() { Alias = "example", Level = 2 } };
            var matcher = new DocumentTypeRuleMatcher(rules);

            var result = matcher.MatchRule("example", 3);

            Assert.IsNull(result);
        }

        [Test]
        public void WildcardLevelIsMatched()
        {
            var rules = new[] { new DocumentTypeExpiryRule() { Alias = "example", Level = null } };
            var matcher = new DocumentTypeRuleMatcher(rules);

            var result = matcher.MatchRule("example", 2);

            Assert.IsNotNull(result);
        }
    }
}
