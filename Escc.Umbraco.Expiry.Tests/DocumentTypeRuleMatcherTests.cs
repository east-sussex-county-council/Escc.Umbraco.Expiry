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
            var matcher = new DocumentTypeRuleMatcher(rules, "example", "*");

            var result= matcher.MatchRule();

            Assert.IsTrue(result);
        }

        [Test]
        public void AliasMatchIsCaseSensitive()
        {
            var rules = new[] { new DocumentTypeExpiryRule() { Alias = "Example" } };
            var matcher = new DocumentTypeRuleMatcher(rules, "example", "*");

            var result = matcher.MatchRule();

            Assert.IsFalse(result);
        }

        [Test]
        public void LevelIsMatched()
        {
            var rules = new[] { new DocumentTypeExpiryRule() { Alias = "example", Level = "2" } };
            var matcher = new DocumentTypeRuleMatcher(rules, "example", "2");

            var result = matcher.MatchRule();

            Assert.IsTrue(result);
        }

        [Test]
        public void DifferentLevelIsNotMatched()
        {
            var rules = new[] { new DocumentTypeExpiryRule() { Alias = "example", Level = "2" } };
            var matcher = new DocumentTypeRuleMatcher(rules, "example", "3");

            var result = matcher.MatchRule();

            Assert.IsFalse(result);
        }

        [Test]
        public void WildcardLevelIsMatched()
        {
            var rules = new[] { new DocumentTypeExpiryRule() { Alias = "example", Level = "*" } };
            var matcher = new DocumentTypeRuleMatcher(rules, "example", "2");

            var result = matcher.MatchRule();

            Assert.IsTrue(result);
        }
    }
}
