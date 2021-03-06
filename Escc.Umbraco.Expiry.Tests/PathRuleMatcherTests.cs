﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Escc.Umbraco.Expiry.Tests
{
    [TestFixture]
    public class PathRuleMatcherTests
    {
        [Test]
        public void PathWithoutTrailingSlashIsMatchedWithTrailingSlashInRule()
        {
            var rules = new[] { new PathExpiryRule() { Path = "/example/" } };
            var matcher = new PathRuleMatcher(rules);

            var result = matcher.MatchRule("/example");

            Assert.IsNotNull(result);
        }

        [Test]
        public void PathWithoutTrailingSlashIsMatchedWithoutTrailingSlashInRule()
        {
            var rules = new[] { new PathExpiryRule() { Path = "/example" } };
            var matcher = new PathRuleMatcher(rules);

            var result = matcher.MatchRule("/example");

            Assert.IsNotNull(result);
        }

        [Test]
        public void PathWithTrailingSlashIsMatchedWithTrailingSlashInRule()
        {
            var rules = new[] { new PathExpiryRule() { Path = "/example/" } };
            var matcher = new PathRuleMatcher(rules);

            var result = matcher.MatchRule("/example/");

            Assert.IsNotNull(result);
        }

        [Test]
        public void PathWithTrailingSlashIsMatchedWithoutTrailingSlashInRule()
        {
            var rules = new[] { new PathExpiryRule() { Path = "/example" } };
            var matcher = new PathRuleMatcher(rules);

            var result = matcher.MatchRule("/example/");

            Assert.IsNotNull(result);
        }

        [Test]
        public void ChildPathIsMatchedWithWildcard()
        {
            var rules = new[] { new PathExpiryRule() { Path = "/example/", ApplyToDescendantPages = true } };
            var matcher = new PathRuleMatcher(rules);

            var result = matcher.MatchRule("/example/child/");

            Assert.IsNotNull(result);
        }

        [Test]
        public void ChildPathIsNotMatchedWithoutWildcard()
        {
            var rules = new[] { new PathExpiryRule() { Path = "/example/", ApplyToDescendantPages = false } };
            var matcher = new PathRuleMatcher(rules);

            var result = matcher.MatchRule("/example/child/");

            Assert.IsNull(result);
        }

        [Test]
        public void DifferentPathIsNotMatched()
        {
            var rules = new[] { new PathExpiryRule() { Path = "/example/" } };
            var matcher = new PathRuleMatcher(rules);

            var result = matcher.MatchRule("/different/");

            Assert.IsNull(result);
        }
    }
}
