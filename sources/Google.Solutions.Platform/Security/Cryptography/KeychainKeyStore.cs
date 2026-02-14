//
// Copyright 2024 Google LLC
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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Security.Cryptography;

namespace Google.Solutions.Platform.Security.Cryptography
{
    /// <summary>
    /// macOS Keychain-based implementation of IKeyStore.
    /// Stores SSH keys as encrypted PKCS#8 blobs in the Keychain.
    /// </summary>
    public class KeychainKeyStore : IKeyStore
    {
        private const string ServicePrefix = "com.google.iapdesktop.sshkey";
        private readonly Keychain keychain;

        public KeychainKeyStore()
        {
            this.keychain = new Keychain();
        }

        private string GetKeychainAccount(string name, KeyType type)
        {
            return $"{name}:{type.Algorithm.Algorithm}:{type.Size}";
        }

        public AsymmetricAlgorithm OpenKey(
            IntPtr owner,
            string name,
            KeyType type,
            CngKeyUsages usage,
            bool forceRecreate)
        {
            name.ExpectNotEmpty(nameof(name));

            using (PlatformTraceSource.Log.TraceMethod().WithParameters(name, type))
            {
                var account = GetKeychainAccount(name, type);

                if (!forceRecreate)
                {
                    // Try to find existing key in Keychain
                    var existingKeyData = this.keychain.FindGenericPassword(ServicePrefix, account);
                    if (existingKeyData != null)
                    {
                        try
                        {
                            // Import PKCS#8 key
                            if (type.Algorithm.Algorithm == "RSA")
                            {
                                var rsa = RSA.Create();
                                rsa.ImportPkcs8PrivateKey(existingKeyData, out _);
                                
                                if (rsa.KeySize != type.Size)
                                {
                                    rsa.Dispose();
                                    throw new KeyConflictException(
                                        $"Key {name} exists but uses size {rsa.KeySize}");
                                }

                                PlatformTraceSource.Log.TraceInformation(
                                    "Found existing Keychain key {0}", account);
                                return rsa;
                            }
                            else if (type.Algorithm.Algorithm.StartsWith("ECDSA") || 
                                     type.Algorithm.Algorithm.StartsWith("ECDH"))
                            {
                                var ecdsa = ECDsa.Create();
                                ecdsa.ImportPkcs8PrivateKey(existingKeyData, out _);

                                if (ecdsa.KeySize != type.Size)
                                {
                                    ecdsa.Dispose();
                                    throw new KeyConflictException(
                                        $"Key {name} exists but uses size {ecdsa.KeySize}");
                                }

                                PlatformTraceSource.Log.TraceInformation(
                                    "Found existing Keychain key {0}", account);
                                return ecdsa;
                            }
                            else
                            {
                                throw new ArgumentException($"Unsupported algorithm: {type.Algorithm.Algorithm}");
                            }
                        }
                        catch (CryptographicException e)
                        {
                            throw new InvalidKeyContainerException(
                                "Failed to import key from Keychain. The key data may be corrupted.",
                                e);
                        }
                    }
                }

                // Key not found or forceRecreate is true, create new key
                AsymmetricAlgorithm newKey;
                if (type.Algorithm.Algorithm == "RSA")
                {
                    var rsa = RSA.Create((int)type.Size);
                    newKey = rsa;
                }
                else if (type.Algorithm.Algorithm.StartsWith("ECDSA") || 
                         type.Algorithm.Algorithm.StartsWith("ECDH"))
                {
                    var curve = type.Size switch
                    {
                        256 => ECCurve.NamedCurves.nistP256,
                        384 => ECCurve.NamedCurves.nistP384,
                        521 => ECCurve.NamedCurves.nistP521,
                        _ => throw new ArgumentException($"Unsupported ECDSA key size: {type.Size}")
                    };
                    var ecdsa = ECDsa.Create(curve);
                    newKey = ecdsa;
                }
                else
                {
                    throw new ArgumentException($"Unsupported algorithm: {type.Algorithm.Algorithm}");
                }

                // Export as PKCS#8 and store in Keychain
                var pkcs8Data = newKey.ExportPkcs8PrivateKey();

                try
                {
                    // Delete existing entry if forceRecreate
                    if (forceRecreate)
                    {
                        this.keychain.DeleteGenericPassword(ServicePrefix, account);
                    }

                    this.keychain.AddGenericPassword(ServicePrefix, account, pkcs8Data);

                    PlatformTraceSource.Log.TraceInformation(
                        "Created new Keychain key {0}", account);

                    return newKey;
                }
                catch (Exception e)
                {
                    newKey.Dispose();
                    throw new KeyStoreUnavailableException(
                        $"Failed to store key in Keychain: {e.Message}");
                }
            }
        }

        public void DeleteKey(string name)
        {
            using (PlatformTraceSource.Log.TraceMethod().WithParameters(name))
            {
                // We need to try deleting all possible key type combinations
                // since we don't know which one was used
                var keyTypes = new[]
                {
                    new KeyType(CngAlgorithm.Rsa, 3072),
                    new KeyType(CngAlgorithm.ECDsaP256, 256),
                    new KeyType(CngAlgorithm.ECDsaP384, 384),
                    new KeyType(CngAlgorithm.ECDsaP521, 521)
                };

                foreach (var type in keyTypes)
                {
                    try
                    {
                        var account = GetKeychainAccount(name, type);
                        this.keychain.DeleteGenericPassword(ServicePrefix, account);
                    }
                    catch
                    {
                        // Ignore errors - key might not exist for this type
                    }
                }
            }
        }
    }
}
