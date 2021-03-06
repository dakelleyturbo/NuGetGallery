﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using NuGetGallery.Areas.Admin;
using NuGetGallery.Areas.Admin.Models;
using NuGetGallery.Configuration;
using Moq;
using Xunit;

namespace NuGetGallery.Services
{
    public class SupportRequestServiceFacts
    {
        public class TheDeleteSupportRequestsAsync
        {
            [Fact]
            public async Task DeleteRequestsNullInput()
            {
                // Arrange
                TestSupportRequestDbContext supportRequestContext = new TestSupportRequestDbContext();
                SupportRequestService supportRequestService = new SupportRequestService(supportRequestContext, GetAppConfig());

                // Act + Assert
                await Assert.ThrowsAsync<ArgumentNullException>(() => supportRequestService.DeleteSupportRequestsAsync(null));
            }

            [Fact]
            public async Task DeleteRequestsNormalPath()
            {
                // Arrange
                string userName = "Joe";
                string emailAddress = "Joe@coldmail.com";

                TestSupportRequestDbContext supportRequestContext = new TestSupportRequestDbContext();
                Issue JoesDeleteAccountRequest = new Issue()
                {
                    CreatedBy = userName,
                    Key = 1,
                    IssueTitle = Strings.AccountDelete_SupportRequestTitle,
                    OwnerEmail = emailAddress,
                    IssueStatusId = IssueStatusKeys.New,
                    HistoryEntries = new List<History>() { new History() { EditedBy = userName, IssueId = 1, Key = 1, IssueStatusId = IssueStatusKeys.New } }
                };
                Issue JoesOldIssue = new Issue()
                {
                    CreatedBy = userName,
                    Key = 2,
                    IssueTitle = "Joe's OldIssue",
                    OwnerEmail = emailAddress,
                    IssueStatusId = IssueStatusKeys.Resolved,
                    HistoryEntries = new List<History>() { new History() { EditedBy = userName, IssueId = 2, Key = 2, IssueStatusId = IssueStatusKeys.New },
                                                           new History() { EditedBy = userName, IssueId = 2, Key = 2, IssueStatusId = IssueStatusKeys.Resolved }}
                };
                Issue randomIssue = new Issue()
                {
                    CreatedBy = $"{userName}_second",
                    Key = 3,
                    IssueTitle = "Second",
                    OwnerEmail = "random",
                    IssueStatusId = IssueStatusKeys.New,
                    HistoryEntries = new List<History>() { new History() { EditedBy = $"{userName}_second", IssueId = 3, Key = 3, IssueStatusId = IssueStatusKeys.New } }
                };
                supportRequestContext.Issues.Add(JoesDeleteAccountRequest);
                supportRequestContext.Issues.Add(JoesOldIssue);
                supportRequestContext.Issues.Add(randomIssue);

                SupportRequestService supportRequestService = new SupportRequestService(supportRequestContext, GetAppConfig());

                // Act
                await supportRequestService.DeleteSupportRequestsAsync(userName);

                //Assert
                Assert.Equal<int>(2, supportRequestContext.Issues.Count());
                Assert.True(supportRequestContext.Issues.Any(issue => string.Equals(issue.CreatedBy, $"{userName}_second")));
                Assert.False(supportRequestContext.Issues.Any(issue => string.Equals(issue.IssueTitle, "Joe's OldIssue")));
                var deleteRequestIssue = supportRequestContext.Issues.Where(issue => issue.Key == 1).FirstOrDefault();
                Assert.NotNull(deleteRequestIssue);
                Assert.Null(deleteRequestIssue.CreatedBy);
                Assert.Null(deleteRequestIssue.HistoryEntries.ElementAt(0).EditedBy);
            }
        }

        static IAppConfiguration GetAppConfig()
        {
            var appConfig = new Mock<IAppConfiguration>();
            appConfig.Setup(m => m.SiteRoot).Returns("SiteRoot");
            appConfig.Setup(m => m.PagerDutyAccountName).Returns("PagerDutyAccountName");
            appConfig.Setup(m => m.PagerDutyAPIKey).Returns("PagerDutyAPIKey");
            appConfig.Setup(m => m.PagerDutyServiceKey).Returns("PagerDutyServiceKey");

            return appConfig.Object;
        }
        internal class TestSupportRequestDbContext : ISupportRequestDbContext
        {
            public TestSupportRequestDbContext()
            {
                Admins = new FakeDbSet<Admin>(new FakeEntitiesContext());
                Issues = new FakeDbSet<Issue>(new FakeEntitiesContext());
                Histories = new FakeDbSet<History>(new FakeEntitiesContext());
                IssueStatus = new FakeDbSet<IssueStatus>(new FakeEntitiesContext());
            }

            public IDbSet<Admin> Admins { get; set; }
            public  IDbSet<Issue> Issues { get; set; }
            public  IDbSet<History> Histories { get; set; }
            public  IDbSet<IssueStatus> IssueStatus { get; set; }

            public async Task CommitChangesAsync()
            {
                await Task.Yield();
                return;
            }
        }
    }
}
