using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;

namespace Escc.Umbraco.Expiry.Tests
{
    [TestFixture]
    public class UmbracoPermissionsServiceTests
    {
        const string BROWSE_NODE = "F";
        const string UPDATE = "A";

        [Test]
        public void PermissionsAreInheritedFromParent()
        {
            // Node 1                (Group 1 permissions defined)
            // --> Node 2            (Group 1 permissions inherited)

            var userService = new Mock<IUserService>();
            var contentService = new Mock<IContentService>();

            var nodeLevel1 = new Mock<IContent>();
            nodeLevel1.Setup(x => x.ParentId).Returns(-1);
            var nodeLevel1Permissions = new Mock<EntityPermissionCollection>();
            nodeLevel1Permissions.Object.Add(new EntityPermission(1, 1, new[] { BROWSE_NODE + UPDATE }));

            var nodeLevel2 = new Mock<IContent>();
            nodeLevel2.Setup(x => x.ParentId).Returns(1);
            var nodeLevel2Permissions = new Mock<EntityPermissionCollection>();

            contentService.Setup(x => x.GetById(1)).Returns(nodeLevel1.Object);
            contentService.Setup(x => x.GetPermissionsForEntity(nodeLevel1.Object)).Returns(nodeLevel1Permissions.Object);
            contentService.Setup(x => x.GetById(2)).Returns(nodeLevel2.Object);
            contentService.Setup(x => x.GetPermissionsForEntity(nodeLevel2.Object)).Returns(nodeLevel2Permissions.Object);

            var permissionsService = new UmbracoPermissionsService(userService.Object, contentService.Object);

            var result = permissionsService.GroupsWithPermissionsForPage(2);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(1));
        }

        [Test]
        public void PermissionsAreInheritedFromAncestor()
        {
            // Node 1                (Group 1 permissions defined)
            // --> Node 2            (No permissions defined)
            //     --> Node 3        (Group 1 permissions inherited)

            var userService = new Mock<IUserService>();
            var contentService = new Mock<IContentService>();

            var nodeLevel1 = new Mock<IContent>();
            nodeLevel1.Setup(x => x.ParentId).Returns(-1);
            var nodeLevel1Permissions = new Mock<EntityPermissionCollection>();
            nodeLevel1Permissions.Object.Add(new EntityPermission(1, 1, new[] { BROWSE_NODE + UPDATE }));

            var nodeLevel2 = new Mock<IContent>();
            nodeLevel2.Setup(x => x.ParentId).Returns(1);
            var nodeLevel2Permissions = new Mock<EntityPermissionCollection>();

            var nodeLevel3 = new Mock<IContent>();
            nodeLevel3.Setup(x => x.ParentId).Returns(2);
            var nodeLevel3Permissions = new Mock<EntityPermissionCollection>();

            contentService.Setup(x => x.GetById(1)).Returns(nodeLevel1.Object);
            contentService.Setup(x => x.GetPermissionsForEntity(nodeLevel1.Object)).Returns(nodeLevel1Permissions.Object);
            contentService.Setup(x => x.GetById(2)).Returns(nodeLevel2.Object);
            contentService.Setup(x => x.GetPermissionsForEntity(nodeLevel2.Object)).Returns(nodeLevel2Permissions.Object);
            contentService.Setup(x => x.GetById(3)).Returns(nodeLevel3.Object);
            contentService.Setup(x => x.GetPermissionsForEntity(nodeLevel3.Object)).Returns(nodeLevel3Permissions.Object);

            var permissionsService = new UmbracoPermissionsService(userService.Object, contentService.Object);

            var result = permissionsService.GroupsWithPermissionsForPage(3);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(1));
        }

        [Test]
        public void PermissionsAreInheritedForGroup1EvenIfGroup2HasMoreSpecificPermissions()
        {
            // Node 1                (Group 1 permissions defined)
            // --> Node 2            (No permissions defined)
            //     --> Node 3        (Group 2 permissions defined, Group 1 permissions inherited)

            var userService = new Mock<IUserService>();
            var contentService = new Mock<IContentService>();

            var nodeLevel1 = new Mock<IContent>();
            nodeLevel1.Setup(x => x.ParentId).Returns(-1);
            var nodeLevel1Permissions = new Mock<EntityPermissionCollection>();
            nodeLevel1Permissions.Object.Add(new EntityPermission(1, 1, new[] { BROWSE_NODE + UPDATE }));

            var nodeLevel2 = new Mock<IContent>();
            nodeLevel2.Setup(x => x.ParentId).Returns(1);
            var nodeLevel2Permissions = new Mock<EntityPermissionCollection>();
            nodeLevel2Permissions.Object.Add(new EntityPermission(2, 2, new[] { BROWSE_NODE + UPDATE }));

            contentService.Setup(x => x.GetById(1)).Returns(nodeLevel1.Object);
            contentService.Setup(x => x.GetPermissionsForEntity(nodeLevel1.Object)).Returns(nodeLevel1Permissions.Object);
            contentService.Setup(x => x.GetById(2)).Returns(nodeLevel2.Object);
            contentService.Setup(x => x.GetPermissionsForEntity(nodeLevel2.Object)).Returns(nodeLevel2Permissions.Object);

            var permissionsService = new UmbracoPermissionsService(userService.Object, contentService.Object);

            var result = permissionsService.GroupsWithPermissionsForPage(2);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(1));
            Assert.IsTrue(result.Contains(2));
        }

        [Test]
        public void BrowseNodeOnlyPermissionsStopInheritance()
        {
            // Node 1           (Group 1 has permissions defined)
            // --> Node 2       (Group 1 has Browse Node only, overriding inherited permissions)

            var userService = new Mock<IUserService>();
            var contentService = new Mock<IContentService>();

            var nodeLevel1 = new Mock<IContent>();
            nodeLevel1.Setup(x => x.ParentId).Returns(-1);
            var nodeLevel1Permissions = new Mock<EntityPermissionCollection>();
            nodeLevel1Permissions.Object.Add(new EntityPermission(1, 1, new[] { BROWSE_NODE + UPDATE }));

            var nodeLevel2 = new Mock<IContent>();
            nodeLevel2.Setup(x => x.ParentId).Returns(1);
            var nodeLevel2Permissions = new Mock<EntityPermissionCollection>();
            nodeLevel2Permissions.Object.Add(new EntityPermission(1, 2, new[] { BROWSE_NODE }));

            contentService.Setup(x => x.GetById(1)).Returns(nodeLevel1.Object);
            contentService.Setup(x => x.GetPermissionsForEntity(nodeLevel1.Object)).Returns(nodeLevel1Permissions.Object);
            contentService.Setup(x => x.GetById(2)).Returns(nodeLevel2.Object);
            contentService.Setup(x => x.GetPermissionsForEntity(nodeLevel2.Object)).Returns(nodeLevel2Permissions.Object);

            var permissionsService = new UmbracoPermissionsService(userService.Object, contentService.Object);

            var result = permissionsService.GroupsWithPermissionsForPage(2);

            Assert.AreEqual(0, result.Count);
        }


        [Test]
        public void WhenBrowseNodeOnlyPermissionsStopInheritanceOtherGroupsStillInherit()
        {
            // Node 1           (Groups 1 and 2 have permissions defined)
            // --> Node 2       (Group 1 has Browse Node only, overriding inherited permissions)

            var userService = new Mock<IUserService>();
            var contentService = new Mock<IContentService>();

            var nodeLevel1 = new Mock<IContent>();
            nodeLevel1.Setup(x => x.ParentId).Returns(-1);
            var nodeLevel1Permissions = new Mock<EntityPermissionCollection>();
            nodeLevel1Permissions.Object.Add(new EntityPermission(1, 1, new[] { BROWSE_NODE + UPDATE }));
            nodeLevel1Permissions.Object.Add(new EntityPermission(2, 1, new[] { BROWSE_NODE + UPDATE }));

            var nodeLevel2 = new Mock<IContent>();
            nodeLevel2.Setup(x => x.ParentId).Returns(1);
            var nodeLevel2Permissions = new Mock<EntityPermissionCollection>();
            nodeLevel2Permissions.Object.Add(new EntityPermission(1, 2, new[] { BROWSE_NODE }));

            contentService.Setup(x => x.GetById(1)).Returns(nodeLevel1.Object);
            contentService.Setup(x => x.GetPermissionsForEntity(nodeLevel1.Object)).Returns(nodeLevel1Permissions.Object);
            contentService.Setup(x => x.GetById(2)).Returns(nodeLevel2.Object);
            contentService.Setup(x => x.GetPermissionsForEntity(nodeLevel2.Object)).Returns(nodeLevel2Permissions.Object);

            var permissionsService = new UmbracoPermissionsService(userService.Object, contentService.Object);

            var result = permissionsService.GroupsWithPermissionsForPage(2);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(2));
        }
    }
}
