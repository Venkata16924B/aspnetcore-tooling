﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LiveShare.Razor.Serialization
{
    internal class ProjectSnapshotHandleProxyJsonConverter : JsonConverter
    {
        public static readonly ProjectSnapshotHandleProxyJsonConverter Instance = new ProjectSnapshotHandleProxyJsonConverter();

        public override bool CanConvert(Type objectType)
        {
            return typeof(ProjectSnapshotHandleProxy).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            var obj = JObject.Load(reader);
            var filePath = obj[nameof(ProjectSnapshotHandleProxy.FilePath)].ToObject<Uri>(serializer);
            var rootNamespace = obj[nameof(ProjectSnapshotHandleProxy.RootNamespace)].ToObject<string>(serializer);
            var projectWorkspaceState = obj[nameof(ProjectSnapshotHandleProxy.ProjectWorkspaceState)].ToObject<ProjectWorkspaceState>(serializer);
            var configuration = obj[nameof(ProjectSnapshotHandleProxy.Configuration)].ToObject<RazorConfiguration>(serializer);
            var rawCSharpLanguageVersion = obj[nameof(ProjectSnapshotHandleProxy.CSharpLanguageVersion)].Value<string>();
            if (!LanguageVersionFacts.TryParse(rawCSharpLanguageVersion, out var csharpLanguageVersion))
            {
                throw new InvalidOperationException($"Cannot parse CSharp langauge version '{rawCSharpLanguageVersion}'.");
            }

            return new ProjectSnapshotHandleProxy(filePath, configuration, rootNamespace, csharpLanguageVersion, projectWorkspaceState);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var handle = (ProjectSnapshotHandleProxy)value;

            writer.WriteStartObject();

            writer.WritePropertyName(nameof(ProjectSnapshotHandleProxy.FilePath));
            writer.WriteValue(handle.FilePath);

            if (handle.RootNamespace == null)
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandleProxy.RootNamespace));
                writer.WriteNull();
            }
            else
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandleProxy.RootNamespace));
                writer.WriteValue(handle.RootNamespace);
            }

            if (handle.ProjectWorkspaceState == null)
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandleProxy.ProjectWorkspaceState));
                writer.WriteNull();
            }
            else
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandleProxy.ProjectWorkspaceState));
                serializer.Serialize(writer, handle.ProjectWorkspaceState);
            }

            writer.WritePropertyName(nameof(ProjectSnapshotHandleProxy.Configuration));
            serializer.Serialize(writer, handle.Configuration);

            writer.WritePropertyName(nameof(ProjectSnapshotHandleProxy.CSharpLanguageVersion));
            serializer.Serialize(writer, handle.CSharpLanguageVersion);

            writer.WriteEndObject();
        }
    }
}
