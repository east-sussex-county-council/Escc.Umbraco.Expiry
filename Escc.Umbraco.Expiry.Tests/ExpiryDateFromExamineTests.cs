using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Examine;
using Lucene.Net.Store;
using Moq;
using NUnit.Framework;

namespace Escc.Umbraco.Expiry.Tests
{
    [TestFixture]
    public class ExpiryDateFromExamineTests
    {
        [Test]
        public void AlreadyClosedExceptionReturnsNull()
        {
            var searcher = new Mock<ISearcher>();
            searcher.Setup(x => x.CreateSearchCriteria()).Throws(new AlreadyClosedException("this IndexReader is closed"));
            var cache = new Mock<ICacheStrategy>();
            var dateFromExamine = new ExpiryDateFromExamine(1, searcher.Object, cache.Object);

            var date = dateFromExamine.ExpiryDate;

            Assert.IsNull(date);
        }

        [Test]
        public void CachedValueIsReturned()
        {
            var searcher = new Mock<ISearcher>();
            var expectedDate = new DateTime(2019, 1, 30);
            var cache = new Mock<ICacheStrategy>();
            cache.Setup(x => x.ReadFromCache(It.IsAny<string>())).Returns(expectedDate);
            var dateFromExamine = new ExpiryDateFromExamine(1, searcher.Object, cache.Object);

            var date = dateFromExamine.ExpiryDate;

            Assert.AreEqual(expectedDate, date);
        }
    }
}