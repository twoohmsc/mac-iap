//
// Copyright 2020 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Linq;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IapDesktop.Application.Avalonia.Services.Ssh.Metadata
{
    /// <summary>
    /// A set of authorized keys.
    /// See https://cloud.google.com/compute/docs/instances/adding-removing-ssh-keys.
    /// </summary>
    public class MetadataAuthorizedPublicKeySet
    {
        public const string LegacyMetadataKey = "sshKeys";
        public const string MetadataKey = "ssh-keys";

        /// <summary>
        /// Items, can be a mix of TMetadataAuthorizedPublicKey and
        /// T:UnrecognizedKey.
        /// </summary>
        internal ICollection<object> Items { get; }

        /// <summary>
        /// Authorized keys.
        /// </summary>
        public IEnumerable<MetadataAuthorizedPublicKey> Keys
        {
            get => this.Items.OfType<MetadataAuthorizedPublicKey>();
        }

        //---------------------------------------------------------------------
        // Metadata.
        //---------------------------------------------------------------------

        private MetadataAuthorizedPublicKeySet(ICollection<object> keys)
        {
            this.Items = keys;
        }

        public static MetadataAuthorizedPublicKeySet FromMetadata(Google.Apis.Compute.v1.Data.Metadata data)
        {
            var item = data?.Items?.FirstOrDefault(i => i.Key == MetadataKey);
            if (item != null && !string.IsNullOrEmpty(item.Value))
            {
                return FromMetadata(item.Value);
            }
            else
            {
                return new MetadataAuthorizedPublicKeySet(
                    Array.Empty<object>());
            }
        }

        private static MetadataAuthorizedPublicKeySet FromMetadata(string value)
        {
            return new MetadataAuthorizedPublicKeySet(
                value
                    .Split('\n')
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => {
                        if (MetadataAuthorizedPublicKey.TryParse(line, out var key))
                        {
                            return (object)key!;
                        }
                        else
                        {
                            //
                            // Junk or a malformed key.
                            //
                            return new UnrecognizedContent(line);
                        }
                    }) 
                    .ToList());
        }

        public MetadataAuthorizedPublicKeySet Add(MetadataAuthorizedPublicKey key)
        {
            if (Contains(key))
            {
                return this;
            }
            else
            {
                var newItems = new List<object>(this.Items);
                newItems.Add(key);
                return new MetadataAuthorizedPublicKeySet(newItems);
            }
        }

        public MetadataAuthorizedPublicKeySet RemoveExpiredKeys()
        {
            return new MetadataAuthorizedPublicKeySet(this.Items
                .Where(k =>
                {
                    if (k is ManagedMetadataAuthorizedPublicKey managed)
                    {
                        return managed.Metadata.ExpireOn >= DateTime.UtcNow;
                    }
                    else
                    {
                        //
                        // Keep unmanaged and unrecognized keys, these never expire.
                        //
                        return true;
                    }
                })
                .ToList());
        }

        public bool Contains(MetadataAuthorizedPublicKey key)
        {
            return this.Items.OfType<MetadataAuthorizedPublicKey>().Any(k => k.Equals(key));
        }

        public override string ToString() 
        {
            return string.Join("\n", this.Items);
        }

        //---------------------------------------------------------------------
        // Private types.
        //---------------------------------------------------------------------

        /// <summary>
        /// A line in the key set that can't be parsed as a key.
        /// </summary>
        private class UnrecognizedContent
        {
            /// <summary>
            /// Raw, unparsed value.
            /// </summary>
            private string Value { get; }

            public UnrecognizedContent(string value)
            {
                this.Value = value;
            }

            public override string ToString()
            {
                return this.Value;
            }
        }
    }
}
