//
// Copyright 2023 Google LLC
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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;
using Google.Solutions.Platform.Security.Cryptography;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    /// <summary>
    /// Name of CNG key to use for SSH authentication.
    /// </summary>
    [DebuggerDisplay("{Value}")]
    internal class CngKeyName
    {
        public string Value { get; }

        public CngKeyName(
            IOidcSession session,
            SshKeyType keyType)
        {
            session.ExpectNotNull(nameof(session));

            this.Type = keyType switch
            {
                SshKeyType.Rsa3072 => new KeyType(CngAlgorithm.Rsa, 3072),
                SshKeyType.EcdsaNistp256 => new KeyType(CngAlgorithm.ECDsaP256, 256),
                SshKeyType.EcdsaNistp384 => new KeyType(CngAlgorithm.ECDsaP384, 384),
                SshKeyType.EcdsaNistp521 => new KeyType(CngAlgorithm.ECDsaP521, 521),
                _ => throw new ArgumentOutOfRangeException(nameof(keyType))
            };

            //
            // Use a simple, consistent naming scheme across platforms.
            //
            this.Value = $"IAPDESKTOP_{session.Username}_{keyType:x}";
        }

        public KeyType Type { get; }

        public override string ToString()
        {
            return this.Value;
        }
    }
}
