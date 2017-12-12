﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NuGetGallery.Auditing
{
    /// <summary>
    /// Base class for auditing services.
    /// </summary>
    public abstract class AuditingService : IAuditingService
    {
        /// <summary>
        /// An auditing service instance with no backing store.
        /// </summary>
        public static readonly AuditingService None = new NullAuditingService();

        private static readonly JsonSerializerSettings AuditRecordSerializerSettings;

        static AuditingService()
        {
            var settings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DefaultValueHandling = DefaultValueHandling.Include,
                Formatting = Formatting.Indented,
                MaxDepth = 10,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                TypeNameHandling = TypeNameHandling.None
            };
            settings.Converters.Add(new StringEnumConverter());
            AuditRecordSerializerSettings = settings;
        }

        /// <summary>
        /// Persists the audit record to storage.
        /// </summary>
        /// <param name="record">An audit record.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous save operation.</returns> 
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="record" /> is <c>null</c>.</exception>
        public async Task SaveAuditRecordAsync(AuditRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            var persistedRecord = PrepareTheRecordForPersistence(record);
            var entry = new AuditEntry(persistedRecord, await GetActorAsync());
            var rendered = RenderAuditEntry(entry);

            await SaveAuditRecordAsync(rendered, persistedRecord.GetResourceType(), persistedRecord.GetPath(), persistedRecord.GetAction(), entry.Actor.TimestampUtc);
        }

        /// <summary>
        /// Renders an audit entry as JSON.
        /// </summary>
        /// <param name="entry">An audit entry.</param>
        /// <returns>A JSON string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="entry" /> is <c>null</c>.</exception>
        public virtual string RenderAuditEntry(AuditEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return JsonConvert.SerializeObject(entry, AuditRecordSerializerSettings);
        }

        /// <summary>
        /// Performs the actual saving of audit data to an audit store
        /// </summary>
        /// <param name="auditData">The data to store in the audit record</param>
        /// <param name="resourceType">The type of resource affected by the audit (usually used as the first-level folder)</param>
        /// <param name="filePath">The file-system path to use to identify the audit record</param>
        /// <param name="action">The action recorded in this audit record</param>
        /// <param name="timestamp">A timestamp indicating when the record was created</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous save operation.</returns> 
        protected abstract Task SaveAuditRecordAsync(string auditData, string resourceType, string filePath, string action, DateTime timestamp);

        protected virtual Task<AuditActor> GetActorAsync()
        {
            return AuditActor.GetCurrentMachineActorAsync();
        }

        /// <summary>
        /// Used for cases when the AuditService needs to do more processing of the audit record before saving the data to the storage.
        /// </summary>
        /// <param name="record">The current record.</param>
        /// <returns>The record to be saved.</returns>
        protected virtual AuditRecord PrepareTheRecordForPersistence(AuditRecord record)
        {
            return record;
        }

        private class NullAuditingService : AuditingService
        {
            protected override Task SaveAuditRecordAsync(string auditData, string resourceType, string filePath, string action, DateTime timestamp)
            {
                return Task.FromResult(0);
            }
        }
    }
}