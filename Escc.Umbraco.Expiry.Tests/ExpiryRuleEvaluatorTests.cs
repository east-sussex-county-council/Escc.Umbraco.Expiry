using System;
using Moq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Umbraco.Core.Models;

namespace Escc.Umbraco.Expiry.Tests
{
    [TestFixture]
    public class ExpiryRuleEvaluatorTests
    {
        [Test]
        public void NoDefaultNoRulesNoChange()
        {
            var ruleEvaluator = new ExpiryRuleEvaluator();
            var publicationTime = new DateTime(2018, 1, 1);
            var documentTypeMatcher = new Mock<IDocumentTypeRuleMatcher>();
            var pathMatcher = new Mock<IPathRuleMatcher>();
            var content = new Mock<IContent>();
            content.SetupGet(x => x.ContentType).Returns(new Mock<IContentType>().Object);
            var urlBuilder = new Mock<INodeUrlBuilder>();
            urlBuilder.Setup(x => x.GetNodeUrl(content.Object)).Returns("/example");
            
            var result = ruleEvaluator.ApplyExpiryRules(publicationTime, null, documentTypeMatcher.Object, pathMatcher.Object, content.Object, urlBuilder.Object);

            Assert.IsNull(result.ExpireDate);
            Assert.IsNull(result.ExpireDateChangedMessage);
            Assert.IsNull(result.CancellationMessage);
        }

        [Test]
        public void DefaultAppliedIfNoRulesAndDateIsTooLong()
        {
            var ruleEvaluator = new ExpiryRuleEvaluator();
            var publicationTime = new DateTime(2018, 1, 1);
            var defaultExpiry = new TimeSpan(30, 0, 0, 0);
            var documentTypeMatcher = new Mock<IDocumentTypeRuleMatcher>();
            var pathMatcher = new Mock<IPathRuleMatcher>();
            var content = new Mock<IContent>();
            content.SetupGet(x => x.ContentType).Returns(new Mock<IContentType>().Object);
            content.SetupGet(x => x.ExpireDate).Returns(publicationTime.AddDays(60));
            var urlBuilder = new Mock<INodeUrlBuilder>();
            urlBuilder.Setup(x => x.GetNodeUrl(content.Object)).Returns("/example");

            var result = ruleEvaluator.ApplyExpiryRules(publicationTime, defaultExpiry, documentTypeMatcher.Object, pathMatcher.Object, content.Object, urlBuilder.Object);

            Assert.AreEqual(publicationTime.Add(defaultExpiry), result.ExpireDate);
            Assert.IsNotNull(result.ExpireDateChangedMessage);
            Assert.IsNull(result.CancellationMessage);
        }

        [Test]
        public void DefaultNotAppliedIfNoRulesAndDateIsOK()
        {
            var ruleEvaluator = new ExpiryRuleEvaluator();
            var publicationTime = new DateTime(2018, 1, 1);
            var defaultExpiry = new TimeSpan(30, 0, 0, 0);
            var userSelectedExpiry = publicationTime.AddDays(10);
            var documentTypeMatcher = new Mock<IDocumentTypeRuleMatcher>();
            var pathMatcher = new Mock<IPathRuleMatcher>();
            var content = new Mock<IContent>();
            content.SetupGet(x => x.ContentType).Returns(new Mock<IContentType>().Object);
            content.SetupGet(x => x.ExpireDate).Returns(userSelectedExpiry);
            var urlBuilder = new Mock<INodeUrlBuilder>();
            urlBuilder.Setup(x => x.GetNodeUrl(content.Object)).Returns("/example");

            var result = ruleEvaluator.ApplyExpiryRules(publicationTime, defaultExpiry, documentTypeMatcher.Object, pathMatcher.Object, content.Object, urlBuilder.Object);

            Assert.AreEqual(userSelectedExpiry, result.ExpireDate);
            Assert.IsNull(result.ExpireDateChangedMessage);
            Assert.IsNull(result.CancellationMessage);
        }

        [Test]
        public void RuleForcesCancellationWhenContentShouldNeverExpireAndADateIsSet()
        {
            var ruleEvaluator = new ExpiryRuleEvaluator();
            var publicationTime = new DateTime(2018, 1, 1);
            var defaultExpiry = new TimeSpan(30, 0, 0, 0);
            var userSelectedExpiry = publicationTime.AddDays(10);
            var documentTypeMatcher = new Mock<IDocumentTypeRuleMatcher>();
            var matchedRule = new Mock<IExpiryRule>();
            documentTypeMatcher.Setup(x => x.MatchRule("example", 0)).Returns(matchedRule.Object);
            var pathMatcher = new Mock<IPathRuleMatcher>();
            var content = new Mock<IContent>();
            var documentType = new Mock<IContentType>();
            documentType.Setup(x => x.Alias).Returns("example");
            content.SetupGet(x => x.ContentType).Returns(documentType.Object);
            content.SetupGet(x => x.ExpireDate).Returns(userSelectedExpiry);
            var urlBuilder = new Mock<INodeUrlBuilder>();
            urlBuilder.Setup(x => x.GetNodeUrl(content.Object)).Returns("/example");

            var result = ruleEvaluator.ApplyExpiryRules(publicationTime, defaultExpiry, documentTypeMatcher.Object, pathMatcher.Object, content.Object, urlBuilder.Object);

            Assert.AreEqual(null, result.ExpireDate);
            Assert.IsNull(result.ExpireDateChangedMessage);
            Assert.IsNotNull(result.CancellationMessage);
        }

        [Test]
        public void RuleAppliedIfDateIsTooLong()
        {
            var ruleEvaluator = new ExpiryRuleEvaluator();
            var publicationTime = new DateTime(2018, 1, 1);
            var defaultExpiry = new TimeSpan(30, 0, 0, 0);
            var ruleExpiry = new TimeSpan(40, 0, 0, 0);
            var userSelectedExpiry = publicationTime.AddDays(60);
            var documentTypeMatcher = new Mock<IDocumentTypeRuleMatcher>();
            var matchedRule = new Mock<IExpiryRule>();
            matchedRule.Setup(x => x.MaximumExpiry).Returns(ruleExpiry);
            documentTypeMatcher.Setup(x => x.MatchRule("example", 0)).Returns(matchedRule.Object);
            var pathMatcher = new Mock<IPathRuleMatcher>();
            var content = new Mock<IContent>();
            var documentType = new Mock<IContentType>();
            documentType.Setup(x => x.Alias).Returns("example");
            content.SetupGet(x => x.ContentType).Returns(documentType.Object);
            content.SetupGet(x => x.ExpireDate).Returns(userSelectedExpiry);
            var urlBuilder = new Mock<INodeUrlBuilder>();
            urlBuilder.Setup(x => x.GetNodeUrl(content.Object)).Returns("/example");

            var result = ruleEvaluator.ApplyExpiryRules(publicationTime, defaultExpiry, documentTypeMatcher.Object, pathMatcher.Object, content.Object, urlBuilder.Object);

            Assert.AreEqual(publicationTime.Add(ruleExpiry), result.ExpireDate);
            Assert.IsNotNull(result.ExpireDateChangedMessage);
            Assert.IsNull(result.CancellationMessage);
        }

        [Test]
        public void RuleMatchedButNotAppliedIfDateIsOK()
        {
            var ruleEvaluator = new ExpiryRuleEvaluator();
            var publicationTime = new DateTime(2018, 1, 1);
            var defaultExpiry = new TimeSpan(30, 0, 0, 0);
            var ruleExpiry = new TimeSpan(40, 0, 0, 0);
            var userSelectedExpiry = publicationTime.AddDays(35);
            var documentTypeMatcher = new Mock<IDocumentTypeRuleMatcher>();
            var matchedRule = new Mock<IExpiryRule>();
            matchedRule.Setup(x => x.MaximumExpiry).Returns(ruleExpiry);
            documentTypeMatcher.Setup(x => x.MatchRule("example", 0)).Returns(matchedRule.Object);
            var pathMatcher = new Mock<IPathRuleMatcher>();
            var content = new Mock<IContent>();
            var documentType = new Mock<IContentType>();
            documentType.Setup(x => x.Alias).Returns("example");
            content.SetupGet(x => x.ContentType).Returns(documentType.Object);
            content.SetupGet(x => x.ExpireDate).Returns(userSelectedExpiry);
            var urlBuilder = new Mock<INodeUrlBuilder>();
            urlBuilder.Setup(x => x.GetNodeUrl(content.Object)).Returns("/example");

            var result = ruleEvaluator.ApplyExpiryRules(publicationTime, defaultExpiry, documentTypeMatcher.Object, pathMatcher.Object, content.Object, urlBuilder.Object);

            Assert.AreEqual(userSelectedExpiry, result.ExpireDate);
            Assert.IsNull(result.ExpireDateChangedMessage);
            Assert.IsNull(result.CancellationMessage);
        }

        [Test]
        public void RuleAppliedIfNoDateSet()
        {
            var ruleEvaluator = new ExpiryRuleEvaluator();
            var publicationTime = new DateTime(2018, 1, 1);
            var defaultExpiry = new TimeSpan(30, 0, 0, 0);
            var ruleExpiry = new TimeSpan(40, 0, 0, 0);
            DateTime? userSelectedExpiry = null;
            var documentTypeMatcher = new Mock<IDocumentTypeRuleMatcher>();
            var matchedRule = new Mock<IExpiryRule>();
            matchedRule.Setup(x => x.MaximumExpiry).Returns(ruleExpiry);
            documentTypeMatcher.Setup(x => x.MatchRule("example", 0)).Returns(matchedRule.Object);
            var pathMatcher = new Mock<IPathRuleMatcher>();
            var content = new Mock<IContent>();
            var documentType = new Mock<IContentType>();
            documentType.Setup(x => x.Alias).Returns("example");
            content.SetupGet(x => x.ContentType).Returns(documentType.Object);
            content.SetupGet(x => x.ExpireDate).Returns(userSelectedExpiry);
            var urlBuilder = new Mock<INodeUrlBuilder>();
            urlBuilder.Setup(x => x.GetNodeUrl(content.Object)).Returns("/example");

            var result = ruleEvaluator.ApplyExpiryRules(publicationTime, defaultExpiry, documentTypeMatcher.Object, pathMatcher.Object, content.Object, urlBuilder.Object);

            Assert.AreEqual(publicationTime.Add(ruleExpiry), result.ExpireDate);
            Assert.IsNotNull(result.ExpireDateChangedMessage);
            Assert.IsNull(result.CancellationMessage);
        }
    }
}
