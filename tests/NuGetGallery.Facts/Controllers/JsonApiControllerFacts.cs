﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using NuGetGallery.Configuration;
using NuGetGallery.Framework;
using NuGetGallery.Security;
using Xunit;

namespace NuGetGallery.Controllers
{
    public class JsonApiControllerFacts
    {
        public class ThePackageOwnerMethods : TestContainer
        {
            public class ThePackageOwnerModificationMethods : TestContainer
            {
                public static IEnumerable<object> ThrowsArgumentNullIfMissing_Data
                {
                    get
                    {
                        foreach (var request in _requests)
                        {
                            foreach (var missingId in _missingData)
                            {
                                yield return new object[]
                                {
                                    request,
                                    missingId,
                                };
                            }
                        }
                    }
                }

                [Theory]
                [MemberData(nameof(ThrowsArgumentNullIfMissing_Data))]
                public void ThrowsArgumentNullIfPackageIdMissing(InvokePackageOwnerModificationRequest request, string id)
                {
                    // Arrange
                    var controller = GetController<JsonApiController>();

                    // Act & Assert
                    Assert.ThrowsAsync<ArgumentException>(() => request(controller, id, "username"));
                }

                [Theory]
                [MemberData(nameof(ThrowsArgumentNullIfMissing_Data))]
                public void ThrowsArgumentNullIfUsernameMissing(InvokePackageOwnerModificationRequest request, string username)
                {
                    // Arrange
                    var controller = GetController<JsonApiController>();

                    // Act & Assert
                    Assert.ThrowsAsync<ArgumentException>(() => request(controller, "package", username));
                }

                [Theory]
                [MemberData(nameof(AllRequests_Data))]
                public async Task ReturnsFailureIfPackageNotFound(InvokePackageOwnerModificationRequest request)
                {
                    // Arrange
                    var controller = GetController<JsonApiController>();

                    // Act
                    var result = await request(controller, "package", "user");
                    dynamic data = ((JsonResult)result).Data;

                    // Assert
                    Assert.False(data.success);
                    Assert.Equal("Package not found.", data.message);
                }

                [Theory]
                [MemberData(nameof(AllCannotManagePackageOwnersByRequests_Data))]
                public async Task ReturnsFailureIfUserIsNotPackageOwner(InvokePackageOwnerModificationRequest request, Func<Fakes, User> getCurrentUser)
                {
                    // Arrange
                    var fakes = Get<Fakes>();
                    var currentUser = getCurrentUser(fakes);
                    var controller = GetController<JsonApiController>();
                    GetMock<HttpContextBase>()
                        .Setup(c => c.User)
                        .Returns(Fakes.ToPrincipal(currentUser));

                    // Act
                    var result = await request(controller, fakes.Package.Id, "nonUser");
                    dynamic data = ((JsonResult)result).Data;

                    // Assert
                    Assert.False(data.success);
                    Assert.Equal("You are not the package owner.", data.message);
                }

                [Theory]
                [MemberData(nameof(AllCanManagePackageOwnersByRequests_Data))]
                public async Task ReturnsFailureIfNewOwnerIsNotRealUser(InvokePackageOwnerModificationRequest request, Func<Fakes, User> getCurrentUser)
                {
                    // Arrange
                    var fakes = Get<Fakes>();
                    var currentUser = getCurrentUser(fakes);
                    var controller = GetController<JsonApiController>();
                    GetMock<HttpContextBase>()
                        .Setup(c => c.User)
                        .Returns(Fakes.ToPrincipal(currentUser));

                    // Act
                    var result = await request(controller, fakes.Package.Id, "nonUser");
                    dynamic data = ((JsonResult)result).Data;

                    // Assert
                    Assert.False(data.success);
                    Assert.Equal("Owner not found.", data.message);
                }

                [Theory]
                [MemberData(nameof(AllCanManagePackageOwnersByRequests_Data))]
                public async Task ReturnsFailureIfNewOwnerIsNotConfirmed(InvokePackageOwnerModificationRequest request, Func<Fakes, User> getCurrentUser)
                {
                    // Arrange
                    var fakes = Get<Fakes>();
                    var currentUser = getCurrentUser(fakes);
                    var controller = GetController<JsonApiController>();
                    GetMock<HttpContextBase>()
                        .Setup(c => c.User)
                        .Returns(Fakes.ToPrincipal(currentUser));
                    fakes.User.UnconfirmedEmailAddress = fakes.User.EmailAddress;
                    fakes.User.EmailAddress = null;

                    // Act
                    var result = await request(controller, fakes.Package.Id, fakes.User.Username);
                    dynamic data = ((JsonResult)result).Data;

                    // Assert
                    Assert.False(data.success);
                    Assert.Equal("Sorry, 'testUser' hasn't verified their email account yet and we cannot proceed with the request.", data.message);
                }

                public class TheAddPackageOwnerMethods : TestContainer
                {
                    private static IEnumerable<InvokePackageOwnerModificationRequest> _addRequests = new InvokePackageOwnerModificationRequest[]
                    {
                        new InvokePackageOwnerModificationRequest(GetAddPackageOwnerConfirmation),
                        new InvokePackageOwnerModificationRequest(AddPackageOwner),
                    };

                    public static IEnumerable<object[]> AllAddRequests_Data
                    {
                        get
                        {
                            foreach (var request in _addRequests)
                            {
                                yield return new object[]
                                {
                                    request,
                                };
                            }
                        }
                    }
                    
                    public static IEnumerable<object[]> AllCanManagePackageOwnersByAddRequests_Data
                    {
                        get
                        {
                            foreach (var request in _addRequests)
                            {
                                foreach (var canManagePackageOwnersUser in _canManagePackageOwnersUsers)
                                {
                                    yield return new object[]
                                    {
                                        request,
                                        canManagePackageOwnersUser,
                                    };
                                }
                            }
                        }
                    }

                    public static IEnumerable<object[]> AllCanManagePackageOwnersPairedWithCanBeAddedByAddRequests_Data
                    {
                        get
                        {
                            foreach (var request in _addRequests)
                            {
                                foreach (var canManagePackageOwnersUser in _canManagePackageOwnersUsers)
                                {
                                    foreach (var canBeAddedUser in _canBeAddedUsers)
                                    {
                                        yield return new object[]
                                        {
                                            request,
                                            canManagePackageOwnersUser,
                                            canBeAddedUser,
                                        };
                                    };
                                }
                            }
                        }
                    }

                    public static IEnumerable<object[]> AllCanManagePackageOwnersPairedWithCannotBeAddedByAddRequests_Data
                    {
                        get
                        {
                            foreach (var request in _addRequests)
                            {
                                foreach (var canManagePackageOwnersUser in _canManagePackageOwnersUsers)
                                {
                                    foreach (var cannotBeAddedUser in _cannotBeAddedUsers)
                                    {
                                        yield return new object[]
                                        {
                                            request,
                                            canManagePackageOwnersUser,
                                            cannotBeAddedUser,
                                        };
                                    };
                                }
                            }
                        }
                    }

                    public static IEnumerable<object[]> AllCanManagePackageOwnersPairedWithCanBeAdded_Data
                    {
                        get
                        {
                            foreach (var canManagePackageOwnersUser in _canManagePackageOwnersUsers)
                            {
                                foreach (var canBeAddedUser in _canBeAddedUsers)
                                {
                                    yield return new object[]
                                    {
                                            canManagePackageOwnersUser,
                                            canBeAddedUser,
                                    };
                                };
                            }
                        }
                    }

                    public static IEnumerable<object[]> PendingOwnerPropagatesPolicy_Data
                    {
                        get
                        {
                            foreach (var canManagePackageOwnersUser in _canManagePackageOwnersUsers)
                            {
                                foreach (var canBeAddedUser in _canBeAddedUsers)
                                {
                                    foreach (var canBePendingUser in _canBeAddedUsers)
                                    {
                                        if (canBeAddedUser == canBePendingUser)
                                        {
                                            continue;
                                        }

                                        yield return new object[]
                                        {
                                                canManagePackageOwnersUser,
                                                canBeAddedUser,
                                                canBePendingUser,
                                        };
                                    }
                                }
                            }
                        }
                    }

                    public static IEnumerable<object[]> ReturnsFailureIfCurrentUserNotFoundByAddRequests_Data
                    {
                        get
                        {
                            return AllCanManagePackageOwnersPairedWithCanBeAddedByAddRequests_Data.Where(o => o[1] != o[2]);
                        }
                    }

                    [Theory]
                    [MemberData(nameof(ReturnsFailureIfCurrentUserNotFoundByAddRequests_Data))]
                    public async Task ReturnsFailureIfCurrentUserNotFound(InvokePackageOwnerModificationRequest request, Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToAdd)
                    {
                        // Arrange
                        var fakes = Get<Fakes>();
                        var currentUser = getCurrentUser(fakes);
                        var userToAdd = getUserToAdd(fakes);
                        var controller = GetController<JsonApiController>();
                        GetMock<HttpContextBase>()
                            .Setup(c => c.User)
                            .Returns(Fakes.ToPrincipal(currentUser));

                        GetMock<IUserService>()
                            .Setup(x => x.FindByUsername(currentUser.Username))
                            .Returns<User>(null);

                        // Act
                        var result = await request(controller, fakes.Package.Id, userToAdd.Username);
                        dynamic data = ((JsonResult)result).Data;

                        // Assert
                        Assert.False(data.success);
                        Assert.Equal("Current user not found.", data.message);
                    }

                    [Theory]
                    [MemberData(nameof(AllCanManagePackageOwnersPairedWithCannotBeAddedByAddRequests_Data))]
                    public async Task ReturnsFailureIfNewOwnerIsAlreadyOwner(InvokePackageOwnerModificationRequest request, Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToAdd)
                    {
                        // Arrange
                        var fakes = Get<Fakes>();
                        var currentUser = getCurrentUser(fakes);
                        var userToAdd = getUserToAdd(fakes);
                        var controller = GetController<JsonApiController>();
                        GetMock<HttpContextBase>()
                            .Setup(c => c.User)
                            .Returns(Fakes.ToPrincipal(currentUser));

                        // Act
                        var result = await request(controller, fakes.Package.Id, userToAdd.Username);
                        dynamic data = ((JsonResult)result).Data;

                        // Assert
                        Assert.False(data.success);
                        Assert.Equal(string.Format(Strings.AddOwner_AlreadyOwner, userToAdd.Username), data.message);
                    }

                    [Theory]
                    [MemberData(nameof(AllCanManagePackageOwnersPairedWithCanBeAddedByAddRequests_Data))]
                    public async Task ReturnsFailureIfNewOwnerIsAlreadyPending(InvokePackageOwnerModificationRequest request, Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToAdd)
                    {
                        // Arrange
                        var fakes = Get<Fakes>();
                        var currentUser = getCurrentUser(fakes);
                        var userToAdd = getUserToAdd(fakes);
                        var package = fakes.Package;
                        var controller = GetController<JsonApiController>();
                        GetMock<HttpContextBase>()
                            .Setup(c => c.User)
                            .Returns(Fakes.ToPrincipal(currentUser));

                        GetMock<IPackageOwnershipManagementService>()
                            .Setup(x => x.GetPackageOwnershipRequests(package, null, userToAdd))
                            .Returns(new PackageOwnerRequest[] { new PackageOwnerRequest() });

                        // Act
                        var result = await request(controller, package.Id, userToAdd.Username);
                        dynamic data = ((JsonResult)result).Data;

                        // Assert
                        Assert.False(data.success);
                        Assert.Equal(string.Format(Strings.AddOwner_AlreadyOwner, userToAdd.Username), data.message);
                    }

                    public class TheGetAddPackageOwnerConfirmationMethod : TestContainer
                    {
                        public static IEnumerable<object[]> AllCanManagePackageOwnersPairedWithCanBeAdded_Data => TheAddPackageOwnerMethods.AllCanManagePackageOwnersPairedWithCanBeAdded_Data;
                        public static IEnumerable<object[]> PendingOwnerPropagatesPolicy_Data => TheAddPackageOwnerMethods.PendingOwnerPropagatesPolicy_Data;

                        [Theory]
                        [MemberData(nameof(AllCanManagePackageOwnersPairedWithCanBeAdded_Data))]
                        public void ReturnsDefaultConfirmationIfNoPolicyPropagation(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToAdd)
                        {
                            // Arrange
                            var fakes = Get<Fakes>();
                            var currentUser = getCurrentUser(fakes);
                            var userToAdd = getUserToAdd(fakes);
                            var controller = GetController<JsonApiController>();
                            GetMock<HttpContextBase>()
                                .Setup(c => c.User)
                                .Returns(Fakes.ToPrincipal(currentUser));

                            // Act
                            var result = controller.GetAddPackageOwnerConfirmation(fakes.Package.Id, userToAdd.Username);
                            dynamic data = ((JsonResult)result).Data;

                            // Assert
                            Assert.True(data.success);
                            Assert.Equal($"Please confirm if you would like to proceed adding '{userToAdd.Username}' as a co-owner of this package.", data.confirmation);
                        }

                        [Theory]
                        [MemberData(nameof(AllCanManagePackageOwnersPairedWithCanBeAdded_Data))]
                        public void ReturnsDetailedConfirmationIfNewOwnerPropagatesPolicy(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToAdd)
                        {
                            // Arrange
                            var fakes = Get<Fakes>();
                            var currentUser = getCurrentUser(fakes);
                            var userToAdd = getUserToAdd(fakes);
                            var controller = GetController<JsonApiController>();

                            GetMock<HttpContextBase>()
                                .Setup(c => c.User)
                                .Returns(Fakes.ToPrincipal(currentUser));

                            userToAdd.SecurityPolicies = (new RequireSecurePushForCoOwnersPolicy().Policies).ToList();

                            // Act
                            var result = controller.GetAddPackageOwnerConfirmation(fakes.Package.Id, userToAdd.Username);
                            dynamic data = ((JsonResult)result).Data;

                            // Assert
                            Assert.True(data.success);
                            Assert.StartsWith(
                                $"User '{userToAdd.Username}' has the following requirements that will be enforced for all co-owners once the user accepts ownership of this package:",
                                data.policyMessage);
                        }

                        public static IEnumerable<object[]> ReturnsDetailedConfirmationIfCurrentOwnerPropagatesPolicy_Data
                        {
                            get
                            {
                                foreach (var canManagePackageOwnersUser in _canManagePackageOwnersUsers)
                                {
                                    foreach (var canBeAddedUser in _canBeAddedUsers)
                                    {
                                        foreach (var cannotBeAddedUser in _cannotBeAddedUsers)
                                        {
                                            yield return new object[]
                                            {
                                                canManagePackageOwnersUser,
                                                canBeAddedUser,
                                                cannotBeAddedUser,
                                            };
                                        }
                                    }
                                }
                            }
                        }

                        [Theory]
                        [MemberData(nameof(ReturnsDetailedConfirmationIfCurrentOwnerPropagatesPolicy_Data))]
                        public void ReturnsDetailedConfirmationIfCurrentOwnerPropagatesPolicy(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToAdd, Func<Fakes,User> getExistingOwner)
                        {
                            // Arrange
                            var fakes = Get<Fakes>();
                            var currentUser = getCurrentUser(fakes);
                            var userToAdd = getUserToAdd(fakes);
                            var existingOwner = getExistingOwner(fakes);
                            var controller = GetController<JsonApiController>();
                            GetMock<HttpContextBase>()
                                .Setup(c => c.User)
                                .Returns(Fakes.ToPrincipal(currentUser));
                            existingOwner.SecurityPolicies = (new RequireSecurePushForCoOwnersPolicy().Policies).ToList();

                            // Act
                            var result = controller.GetAddPackageOwnerConfirmation(fakes.Package.Id, userToAdd.Username);
                            dynamic data = ((JsonResult)result).Data;

                            // Assert
                            Assert.True(data.success);
                            Assert.StartsWith(
                                $"Owner(s) '{existingOwner.Username}' has (have) the following requirements that will be enforced for user '{userToAdd.Username}' once the user accepts ownership of this package:",
                                data.policyMessage);
                        }

                        [Theory]
                        [MemberData(nameof(AllCanManagePackageOwnersPairedWithCanBeAdded_Data))]
                        public void DoesNotReturnConfirmationIfCurrentOwnerPropagatesButNewOwnerIsSubscribed(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToAdd)
                        {
                            // Arrange
                            var fakes = Get<Fakes>();
                            var currentUser = getCurrentUser(fakes);
                            var userToAdd = getUserToAdd(fakes);
                            var controller = GetController<JsonApiController>();
                            GetMock<HttpContextBase>()
                                .Setup(c => c.User)
                                .Returns(Fakes.ToPrincipal(currentUser));
                            GetMock<ISecurityPolicyService>().Setup(s => s.IsSubscribed(userToAdd, SecurePushSubscription.Name)).Returns(true);
                            currentUser.SecurityPolicies = (new RequireSecurePushForCoOwnersPolicy().Policies).ToList();

                            // Act
                            var result = controller.GetAddPackageOwnerConfirmation(fakes.Package.Id, userToAdd.Username);
                            dynamic data = ((JsonResult)result).Data;

                            // Assert
                            Assert.True(data.success);
                            Assert.StartsWith($"Please confirm if you would like to proceed adding '{userToAdd.Username}' as a co-owner of this package.",
                                data.confirmation);
                        }

                        [Theory]
                        [MemberData(nameof(PendingOwnerPropagatesPolicy_Data))]
                        public void ReturnsDetailedConfirmationIfPendingOwnerPropagatesPolicy(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToAdd, Func<Fakes, User> getPendingUser)
                        {
                            // Arrange
                            var fakes = Get<Fakes>();
                            var currentUser = getCurrentUser(fakes);
                            var userToAdd = getUserToAdd(fakes);
                            var pendingUser = getPendingUser(fakes);
                            var controller = GetController<JsonApiController>();
                            GetMock<HttpContextBase>()
                                .Setup(c => c.User)
                                .Returns(Fakes.ToPrincipal(currentUser));

                            pendingUser.SecurityPolicies = (new RequireSecurePushForCoOwnersPolicy().Policies).ToList();

                            GetMock<IPackageOwnershipManagementService>()
                                .Setup(p => p.GetPackageOwnershipRequests(fakes.Package, null, null))
                                .Returns((new[] 
                                {
                                    new PackageOwnerRequest()
                                    {
                                        PackageRegistration = fakes.Package,
                                        PackageRegistrationKey = fakes.Package.Key,
                                        NewOwner = pendingUser,
                                        NewOwnerKey = pendingUser.Key
                                    }
                                }));

                            // Act
                            var result = controller.GetAddPackageOwnerConfirmation(fakes.Package.Id, userToAdd.Username);
                            dynamic data = ((JsonResult)result).Data;

                            // Assert
                            Assert.True(data.success);
                            Assert.StartsWith(
                                $"Pending owner(s) '{pendingUser.Username}' has (have) the following requirements that will be enforced for all co-owners, including '{userToAdd.Username}', once ownership requests are accepted:",
                                data.policyMessage);
                        }

                        [Theory]
                        [MemberData(nameof(PendingOwnerPropagatesPolicy_Data))]
                        public void DoesNotReturnConfirmationIfPendingOwnerPropagatesButNewOwnerIsSubscribed(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToAdd, Func<Fakes, User> getPendingUser)
                        {
                            // Arrange
                            var fakes = Get<Fakes>();
                            var currentUser = getCurrentUser(fakes);
                            var userToAdd = getUserToAdd(fakes);
                            var pendingUser = getPendingUser(fakes);
                            var controller = GetController<JsonApiController>();
                            GetMock<HttpContextBase>()
                                .Setup(c => c.User)
                                .Returns(Fakes.ToPrincipal(currentUser));
                            GetMock<ISecurityPolicyService>().Setup(s => s.IsSubscribed(userToAdd, SecurePushSubscription.Name)).Returns(true);

                            pendingUser.SecurityPolicies = (new RequireSecurePushForCoOwnersPolicy().Policies).ToList();
                            var pendingOwnerRequest = new PackageOwnerRequest()
                            {
                                PackageRegistrationKey = fakes.Package.Key,
                                NewOwner = pendingUser
                            };

                            GetMock<IPackageOwnerRequestService>()
                                .Setup(p => p.GetPackageOwnershipRequests(fakes.Package, null, null))
                                .Returns((new[] { pendingOwnerRequest }));

                            // Act
                            var result = controller.GetAddPackageOwnerConfirmation(fakes.Package.Id, userToAdd.Username);
                            dynamic data = ((JsonResult)result).Data;

                            // Assert
                            Assert.True(data.success);
                            Assert.StartsWith($"Please confirm if you would like to proceed adding '{userToAdd.Username}' as a co-owner of this package.",
                                data.confirmation);
                        }

                    }

                    public class TheAddPackageOwnerMethod : TestContainer
                    {
                        public static IEnumerable<object[]> AllCanManagePackageOwnersPairedWithCanBeAdded_Data = TheAddPackageOwnerMethods.AllCanManagePackageOwnersPairedWithCanBeAdded_Data;
                        public static IEnumerable<object[]> PendingOwnerPropagatesPolicy_Data => TheAddPackageOwnerMethods.PendingOwnerPropagatesPolicy_Data;

                        [Theory]
                        [MemberData(nameof(AllCanManagePackageOwnersPairedWithCanBeAdded_Data))]
                        public async Task CreatesPackageOwnerRequestSendsEmailAndReturnsPendingState(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToAdd)
                        {
                            var fakes = Get<Fakes>();

                            var currentUser = getCurrentUser(fakes);
                            var userToAdd = getUserToAdd(fakes);

                            var controller = GetController<JsonApiController>();

                            var httpContextMock = GetMock<HttpContextBase>();
                            httpContextMock
                                .Setup(c => c.User)
                                .Returns(Fakes.ToPrincipal(currentUser))
                                .Verifiable();

                            var packageOwnershipManagementServiceMock = GetMock<IPackageOwnershipManagementService>();
                            packageOwnershipManagementServiceMock
                                .Setup(p => p.AddPackageOwnershipRequestAsync(fakes.Package, currentUser, userToAdd))
                                .Returns(Task.FromResult(new PackageOwnerRequest { ConfirmationCode = "confirmation-code" }))
                                .Verifiable();

                            var messageServiceMock = GetMock<IMessageService>();
                            messageServiceMock
                                .Setup(m => m.SendPackageOwnerRequest(
                                    currentUser,
                                    userToAdd,
                                    fakes.Package,
                                    TestUtility.GallerySiteRootHttps + "packages/FakePackage/",
                                    TestUtility.GallerySiteRootHttps + $"packages/FakePackage/owners/{userToAdd.Username}/confirm/confirmation-code",
                                    TestUtility.GallerySiteRootHttps + $"packages/FakePackage/owners/{userToAdd.Username}/reject/confirmation-code",
                                    "Hello World! Html Encoded &lt;3",
                                    ""))
                                .Verifiable();

                            JsonResult result = await controller.AddPackageOwner(fakes.Package.Id, userToAdd.Username, "Hello World! Html Encoded <3");
                            dynamic data = result.Data;
                            PackageOwnersResultViewModel model = data.model;

                            Assert.True(data.success);
                            Assert.Equal(userToAdd.Username, model.Name);
                            Assert.True(model.Pending);

                            httpContextMock.Verify();
                            packageOwnershipManagementServiceMock.Verify();
                            messageServiceMock.Verify();
                        }

                        [Theory]
                        [MemberData(nameof(AllCanManagePackageOwnersPairedWithCanBeAdded_Data))]
                        public async Task SendsPackageOwnerRequestEmailWhereNewOwnerPropagatesPolicy(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToAdd)
                        {
                            // Arrange & Act
                            var fakes = Get<Fakes>();
                            var policyMessage = await GetSendPackageOwnerRequestPolicyMessage(fakes, getCurrentUser(fakes), getUserToAdd(fakes), getUserToAdd(fakes));

                            // Assert
                            Assert.StartsWith(
                                "Note: The following policies will be enforced on package co-owners once you accept this request.",
                                policyMessage);
                        }

                        [Theory]
                        [MemberData(nameof(AllCanManagePackageOwnersPairedWithCanBeAdded_Data))]
                        public async Task SendsPackageOwnerRequestEmailWhereCurrentOwnerPropagatesPolicy(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToAdd)
                        {
                            // Arrange & Act
                            var fakes = Get<Fakes>();
                            var policyMessage = await GetSendPackageOwnerRequestPolicyMessage(fakes, getCurrentUser(fakes), getUserToAdd(fakes), fakes.Owner);

                            // Assert
                            Assert.StartsWith(
                                "Note: Owner(s) 'testPackageOwner' has (have) the following policies that will be enforced on your account once you accept this request.",
                                policyMessage);
                        }

                        [Theory]
                        [MemberData(nameof(PendingOwnerPropagatesPolicy_Data))]
                        public async Task SendsPackageOwnerRequestEmailWherePendingOwnerPropagatesPolicy(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToAdd, Func<Fakes, User> getPendingUser)
                        {
                            // Arrange & Act
                            var fakes = Get<Fakes>();

                            var currentUser = getCurrentUser(fakes);
                            var userToAdd = getUserToAdd(fakes);
                            var pendingUser = getPendingUser(fakes);

                            var packageOwnershipManagementServiceMock = GetMock<IPackageOwnershipManagementService>();
                            packageOwnershipManagementServiceMock
                                .Setup(p => p.GetPackageOwnershipRequests(fakes.Package, null, null))
                                .Returns(
                                    new[]
                                    {
                                        new PackageOwnerRequest
                                        {
                                            PackageRegistration = fakes.Package,
                                            RequestingOwner = currentUser,
                                            NewOwner = pendingUser,
                                            ConfirmationCode = "confirmation-code"
                                        }
                                    })
                                .Verifiable();

                            var policyMessage = await GetSendPackageOwnerRequestPolicyMessage(fakes, currentUser, userToAdd, pendingUser);

                            // Assert
                            Assert.StartsWith(
                                $"Note: Pending owner(s) '{pendingUser.Username}' has (have) the following policies that will be enforced on your account once ownership requests are accepted.",
                                policyMessage);
                        }

                        private async Task<string> GetSendPackageOwnerRequestPolicyMessage(Fakes fakes, User currentUser, User userToAdd, User userToSubscribe)
                        {
                            // Arrange
                            var controller = GetController<JsonApiController>();

                            GetMock<HttpContextBase>()
                                .Setup(c => c.User)
                                .Returns(Fakes.ToPrincipal(currentUser));

                            userToSubscribe.SecurityPolicies = (new RequireSecurePushForCoOwnersPolicy().Policies).ToList();

                            var packageOwnershipManagementServiceMock = GetMock<IPackageOwnershipManagementService>();
                            packageOwnershipManagementServiceMock
                                .Setup(p => p.AddPackageOwnershipRequestAsync(fakes.Package, currentUser, userToAdd))
                                .Returns(Task.FromResult(
                                    new PackageOwnerRequest
                                    {
                                        PackageRegistration = fakes.Package,
                                        RequestingOwner = currentUser,
                                        NewOwner = userToAdd,
                                        ConfirmationCode = "confirmation-code"
                                    }))
                                .Verifiable();

                            string actualMessage = string.Empty;
                            GetMock<IMessageService>()
                                .Setup(m => m.SendPackageOwnerRequest(
                                    currentUser,
                                    userToAdd,
                                    fakes.Package,
                                    TestUtility.GallerySiteRootHttps + "packages/FakePackage/",
                                    TestUtility.GallerySiteRootHttps + $"packages/FakePackage/owners/{userToAdd.Username}/confirm/confirmation-code",
                                    TestUtility.GallerySiteRootHttps + $"packages/FakePackage/owners/{userToAdd.Username}/reject/confirmation-code",
                                    string.Empty,
                                    It.IsAny<string>()))
                                .Callback<User, User, PackageRegistration, string, string, string, string, string>(
                                    (from, to, pkg, pkgUrl, cnfUrl, rjUrl, msg, policyMsg) => actualMessage = policyMsg);

                            // Act
                            JsonResult result = await controller.AddPackageOwner(fakes.Package.Id, userToAdd.Username, string.Empty);
                            dynamic data = result.Data;

                            // Assert
                            Assert.True(data.success);
                            Assert.False(String.IsNullOrEmpty(actualMessage));
                            return actualMessage;
                        }
                    }
                }

                public class TheRemovePackageOwnerMethod : TestContainer
                {
                    private static IEnumerable<Func<Fakes, User>> _canBeRemovedUsers = _cannotBeAddedUsers;

                    private static IEnumerable<Func<Fakes, User>> _cannotBeRemovedUsers = _canBeAddedUsers;

                    public static IEnumerable<object[]> AllCanManagePackageOwners_Data => ThePackageOwnerMethods.AllCanManagePackageOwners_Data;

                    public static IEnumerable<object[]> AllCanManagePackageOwnersPairedWithCanBeRemoved_Data
                    {
                        get
                        {
                            foreach (var canManagePackageOwnersUser in _canManagePackageOwnersUsers)
                            {
                                foreach (var canBeRemovedUser in _canBeRemovedUsers)
                                {
                                    yield return new object[]
                                    {
                                        canManagePackageOwnersUser,
                                        canBeRemovedUser,
                                    };
                                }
                            }
                        }
                    }

                    public static IEnumerable<object[]> AllCanManagePackageOwnersPairedWithCannotBeRemoved_Data
                    {
                        get
                        {
                            foreach (var canManagePackageOwnersUser in _canManagePackageOwnersUsers)
                            {
                                foreach (var cannotBeRemovedUser in _cannotBeRemovedUsers)
                                {
                                    yield return new object[]
                                    {
                                        canManagePackageOwnersUser,
                                        cannotBeRemovedUser,
                                    };
                                }
                            }
                        }
                    }

                    public static IEnumerable<object[]> ReturnsFailureIfCurrentUserNotFound_Data
                    {
                        get
                        {
                            return AllCanManagePackageOwnersPairedWithCanBeRemoved_Data.Where(o => o[0] != o[1]);
                        }
                    }

                    [Theory]
                    [MemberData(nameof(ReturnsFailureIfCurrentUserNotFound_Data))]
                    public async Task ReturnsFailureIfCurrentUserNotFound(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToRemove)
                    {
                        // Arrange
                        var fakes = Get<Fakes>();
                        var currentUser = getCurrentUser(fakes);
                        var userToRemove = getUserToRemove(fakes);
                        var controller = GetController<JsonApiController>();
                        GetMock<HttpContextBase>()
                            .Setup(c => c.User)
                            .Returns(Fakes.ToPrincipal(currentUser));

                        GetMock<IUserService>()
                            .Setup(x => x.FindByUsername(currentUser.Username))
                            .Returns<User>(null);

                        // Act
                        var result = await controller.RemovePackageOwner(fakes.Package.Id, userToRemove.Username);
                        dynamic data = result.Data;

                        // Assert
                        Assert.False(data.success);
                        Assert.Equal("Current user not found.", data.message);
                    }

                    [Theory]
                    [MemberData(nameof(AllCanManagePackageOwnersPairedWithCannotBeRemoved_Data))]
                    public async Task ReturnsFailureIfUserIsNotAnOwner(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToRemove)
                    {
                        // Arrange
                        var fakes = Get<Fakes>();
                        var currentUser = getCurrentUser(fakes);
                        var userToRemove = getUserToRemove(fakes);
                        var controller = GetController<JsonApiController>();
                        GetMock<HttpContextBase>()
                            .Setup(c => c.User)
                            .Returns(Fakes.ToPrincipal(currentUser));

                        // Act
                        var result = await controller.RemovePackageOwner(fakes.Package.Id, userToRemove.Username);
                        dynamic data = result.Data;

                        // Assert
                        Assert.False(data.success);
                        Assert.Equal(string.Format(Strings.RemoveOwner_NotOwner, userToRemove.Username), data.message);
                    }

                    [Theory]
                    [MemberData(nameof(AllCanManagePackageOwnersPairedWithCannotBeRemoved_Data))]
                    public async Task RemovesPackageOwnerRequest(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getPendingUser)
                    {
                        // Arrange
                        var fakes = Get<Fakes>();
                        var currentUser = getCurrentUser(fakes);
                        var requestedUser = getPendingUser(fakes);
                        var package = fakes.Package;
                        var controller = GetController<JsonApiController>();
                        GetMock<HttpContextBase>()
                            .Setup(c => c.User)
                            .Returns(Fakes.ToPrincipal(currentUser));

                        var packageOwnershipManagementService = GetMock<IPackageOwnershipManagementService>();

                        packageOwnershipManagementService
                            .Setup(x => x.GetPackageOwnershipRequests(package, null, requestedUser))
                            .Returns(new PackageOwnerRequest[] { new PackageOwnerRequest() });

                        // Act
                        var result = await controller.RemovePackageOwner(package.Id, requestedUser.Username);
                        dynamic data = result.Data;

                        // Assert
                        Assert.True(data.success);

                        packageOwnershipManagementService.Verify(x => x.DeletePackageOwnershipRequestAsync(package, requestedUser));

                        GetMock<IMessageService>()
                            .Verify(x => x.SendPackageOwnerRequestCancellationNotice(currentUser, requestedUser, package));
                    }

                    [Theory]
                    [MemberData(nameof(AllCanManagePackageOwnersPairedWithCanBeRemoved_Data))]
                    public async Task RemovesExistingOwner(Func<Fakes, User> getCurrentUser, Func<Fakes, User> getUserToRemove)
                    {
                        // Arrange
                        var fakes = Get<Fakes>();
                        var currentUser = getCurrentUser(fakes);
                        var userToRemove = getUserToRemove(fakes);
                        var package = fakes.Package;
                        var controller = GetController<JsonApiController>();
                        GetMock<HttpContextBase>()
                            .Setup(c => c.User)
                            .Returns(Fakes.ToPrincipal(currentUser));

                        var packageOwnershipManagementService = GetMock<IPackageOwnershipManagementService>();

                        packageOwnershipManagementService
                            .Setup(x => x.GetPackageOwnershipRequests(package, null, userToRemove))
                            .Returns(Enumerable.Empty<PackageOwnerRequest>());

                        // Act
                        var result = await controller.RemovePackageOwner(package.Id, userToRemove.Username);
                        dynamic data = result.Data;

                        // Assert
                        Assert.True(data.success);

                        packageOwnershipManagementService.Verify(x => x.RemovePackageOwnerAsync(package, currentUser, userToRemove, It.IsAny<bool>()));

                        GetMock<IMessageService>()
                            .Verify(x => x.SendPackageOwnerRemovedNotice(currentUser, userToRemove, package));
                    }
                }

                public delegate Task<ActionResult> InvokePackageOwnerModificationRequest(JsonApiController jsonApiController, string packageId, string username);

                private static Task<ActionResult> GetAddPackageOwnerConfirmation(JsonApiController jsonApiController, string packageId, string username)
                {
                    return Task.Run(() => jsonApiController.GetAddPackageOwnerConfirmation(packageId, username));
                }

                private static async Task<ActionResult> AddPackageOwner(JsonApiController jsonApiController, string packageId, string username)
                {
                    return await jsonApiController.AddPackageOwner(packageId, username, "message");
                }

                private static async Task<ActionResult> RemovePackageOwner(JsonApiController jsonApiController, string packageId, string username)
                {
                    return await jsonApiController.RemovePackageOwner(packageId, username);
                }

                private static IEnumerable<InvokePackageOwnerModificationRequest> _requests = new InvokePackageOwnerModificationRequest[]
                {
                    new InvokePackageOwnerModificationRequest(GetAddPackageOwnerConfirmation),
                    new InvokePackageOwnerModificationRequest(AddPackageOwner),
                    new InvokePackageOwnerModificationRequest(RemovePackageOwner),
                };

                public static IEnumerable<object[]> AllRequests_Data
                {
                    get
                    {
                        foreach (var request in _requests)
                        {
                            yield return new object[]
                            {
                                request
                            };
                        }
                    }
                }

                public static IEnumerable<object[]> AllCanManagePackageOwnersByRequests_Data
                {
                    get
                    {
                        foreach (var request in _requests)
                        {
                            foreach (var canManagePackageOwnersUser in _canManagePackageOwnersUsers)
                            {
                                yield return new object[]
                                {
                                    request,
                                    canManagePackageOwnersUser,
                                };
                            }
                        }
                    }
                }

                public static IEnumerable<object[]> AllCannotManagePackageOwnersByRequests_Data
                {
                    get
                    {
                        foreach (var request in _requests)
                        {
                            foreach (var cannotManagePackageOwnersUser in _cannotManagePackageOwnersUsers)
                            {
                                yield return new object[]
                                {
                                    request,
                                    cannotManagePackageOwnersUser,
                                };
                            }
                        }
                    }
                }
            }

            public class TheGetPackageOwnersMethod : TestContainer
            {
                public static IEnumerable<object[]> AllCanManagePackageOwners_Data => ThePackageOwnerMethods.AllCanManagePackageOwners_Data;

                public static IEnumerable<object[]> AllCannotManagePackageOwners_Data => ThePackageOwnerMethods.AllCannotManagePackageOwners_Data;
                
                public void ReturnsFailureIfPackageNotFound()
                {
                    // Arrange
                    var controller = GetController<JsonApiController>();

                    // Act
                    var result = controller.GetPackageOwners("fakeId", "2.0.0");
                    dynamic data = ((JsonResult)result).Data;

                    // Assert
                    Assert.False(data.success);
                    Assert.Equal("Package not found.", data.message);
                }

                [Theory]
                [MemberData(nameof(AllCannotManagePackageOwners_Data))]
                public void ReturnsFailureIfUserIsNotPackageOwner(Func<Fakes, User> getCurrentUser)
                {
                    // Arrange
                    var fakes = Get<Fakes>();
                    var currentUser = getCurrentUser(fakes);
                    var controller = GetController<JsonApiController>();
                    GetMock<HttpContextBase>()
                        .Setup(c => c.User)
                        .Returns(Fakes.ToPrincipal(currentUser));

                    // Act
                    var result = controller.GetPackageOwners(fakes.Package.Id, fakes.Package.Packages.First().Version);

                    // Assert
                    Assert.IsType(typeof(HttpUnauthorizedResult), result);
                }
            }

            private static Func<Fakes, User> _getFakesUser = (Fakes fakes) => fakes.User;
            private static Func<Fakes, User> _getFakesOwner = (Fakes fakes) => fakes.Owner;
            private static Func<Fakes, User> _getFakesOrganizationOwner = (Fakes fakes) => fakes.OrganizationOwner;
            private static Func<Fakes, User> _getFakesOrganizationAdminOwner = (Fakes fakes) => fakes.OrganizationOwnerAdmin;
            private static Func<Fakes, User> _getFakesOrganizationCollaboratorOwner = (Fakes fakes) => fakes.OrganizationOwnerCollaborator;

            public static IEnumerable<string> _missingData = new[] { null, string.Empty };

            private static IEnumerable<Func<Fakes, User>> _canManagePackageOwnersUsers = new Func<Fakes, User>[]
            {
                _getFakesOwner,
                _getFakesOrganizationOwner,
                _getFakesOrganizationAdminOwner,
            };

            private static IEnumerable<Func<Fakes, User>> _cannotManagePackageOwnersUsers = new Func<Fakes, User>[]
            {
                _getFakesUser,
                _getFakesOrganizationCollaboratorOwner,
            };

            private static IEnumerable<Func<Fakes, User>> _canBeAddedUsers = new Func<Fakes, User>[]
            {
                _getFakesUser,
                _getFakesOrganizationAdminOwner,
                _getFakesOrganizationCollaboratorOwner,
            };

            private static IEnumerable<Func<Fakes, User>> _cannotBeAddedUsers = new Func<Fakes, User>[]
            {
                _getFakesOwner,
                _getFakesOrganizationOwner,
            };

            public static IEnumerable<object[]> AllCanManagePackageOwners_Data
            {
                get
                {
                    foreach (var canManagePackageOwnersUser in _canManagePackageOwnersUsers)
                    {
                        yield return new object[]
                        {
                            canManagePackageOwnersUser,
                        };
                    }
                }
            }

            public static IEnumerable<object[]> AllCannotManagePackageOwners_Data
            {
                get
                {
                    foreach (var cannotManagePackageOwnersUser in _cannotManagePackageOwnersUsers)
                    {
                        yield return new object[]
                        {
                            cannotManagePackageOwnersUser,
                        };
                    }
                }
            }
        }
    }
}