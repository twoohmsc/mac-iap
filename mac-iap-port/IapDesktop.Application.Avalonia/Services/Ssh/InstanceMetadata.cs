using Google.Apis.Compute.v1.Data;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using IapDesktop.Application.Avalonia.Services.Ssh.Metadata;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;

using ComputeMetadata = Google.Apis.Compute.v1.Data.Metadata;

namespace IapDesktop.Application.Avalonia.Services.Ssh
{
    public class InstanceMetadata
    {
        private readonly IComputeEngineClient computeClient;
        private readonly InstanceLocator instanceLocator;
        private readonly Instance instanceDetails;
        private readonly Project projectDetails;

        public const string EnableOsLoginFlag = "enable-oslogin";
        public const string EnableOsLoginWithSecurityKeyFlag = "enable-oslogin-sk";
        public const string BlockProjectSshKeysFlag = "block-project-ssh-keys";

        private InstanceMetadata(
            IComputeEngineClient computeClient,
            InstanceLocator instanceLocator,
            Instance instanceDetails,
            Project projectDetails)
        {
            this.computeClient = computeClient;
            this.instanceLocator = instanceLocator;
            this.instanceDetails = instanceDetails;
            this.projectDetails = projectDetails;
        }

        public static async Task<InstanceMetadata> GetAsync(
            IComputeEngineClient computeClient,
            InstanceLocator instanceLocator,
            CancellationToken token)
        {
            var instanceTask = computeClient.GetInstanceAsync(instanceLocator, token);
            var projectTask = computeClient.GetProjectAsync(instanceLocator.Project, token);

            await Task.WhenAll(instanceTask, projectTask).ConfigureAwait(false);

            return new InstanceMetadata(
                computeClient,
                instanceLocator,
                await instanceTask,
                await projectTask);
        }

        public bool IsOsLoginEnabled
        {
            get
            {
                // Check instance metadata first
                var instanceValue = GetMetadata(this.instanceDetails.Metadata, EnableOsLoginFlag);
                if (instanceValue != null)
                {
                    return bool.TryParse(instanceValue, out var val) && val;
                }

                // Fallback to project metadata
                var projectValue = GetMetadata(this.projectDetails.CommonInstanceMetadata, EnableOsLoginFlag);
                if (projectValue != null)
                {
                    return bool.TryParse(projectValue, out var val) && val;
                }

                return false; // Default is false
            }
        }

         public bool AreProjectSshKeysBlocked
        {
            get
            {
                var instanceValue = GetMetadata(this.instanceDetails.Metadata, BlockProjectSshKeysFlag);
                 if (instanceValue != null)
                {
                    return bool.TryParse(instanceValue, out var val) && val;
                }
                
                // Fallback to project metadata
                var projectValue = GetMetadata(this.projectDetails.CommonInstanceMetadata, BlockProjectSshKeysFlag);
                if (projectValue != null)
                {
                    return bool.TryParse(projectValue, out var val) && val;
                }

                return false;
            }
        }

        private static string? GetMetadata(ComputeMetadata? metadata, string key)
        {
            return metadata?.Items?.FirstOrDefault(i => i.Key == key)?.Value;
        }

        public async Task AddPublicKeyToMetadata(
            MetadataAuthorizedPublicKey key,
            CancellationToken token)
        {
            // Simple logic: if project keys are blocked, use instance metadata.
            // Otherwise use project metadata (common instance metadata).
            // NOTE: This assumes we have permission to update project metadata. 
            // In the full implementation we check permissions. Here we check "BlockProjectSshKeys".
            
            bool useInstanceMetadata = AreProjectSshKeysBlocked;

            if (useInstanceMetadata)
            {
                 await this.computeClient.UpdateMetadataAsync(
                    this.instanceLocator,
                    metadata =>
                    {
                        var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata)
                            .RemoveExpiredKeys()
                            .Add(key);
                        
                        // Update or add the item
                         var existingItem = metadata.Items?.FirstOrDefault(i => i.Key == MetadataAuthorizedPublicKeySet.MetadataKey);
                         if (existingItem != null)
                         {
                             existingItem.Value = keySet.ToString();
                         }
                         else
                         {
                             if (metadata.Items == null) metadata.Items = new List<ComputeMetadata.ItemsData>();
                             metadata.Items.Add(new ComputeMetadata.ItemsData { 
                                 Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                                 Value = keySet.ToString()
                             });
                         }
                    },
                    token).ConfigureAwait(false);
            }
            else
            {
                 await this.computeClient.UpdateCommonInstanceMetadataAsync(
                    this.instanceLocator.Project,
                    metadata =>
                    {
                         var keySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata)
                            .RemoveExpiredKeys()
                            .Add(key);
                        
                         // Update or add the item
                         var existingItem = metadata.Items?.FirstOrDefault(i => i.Key == MetadataAuthorizedPublicKeySet.MetadataKey);
                         if (existingItem != null)
                         {
                             existingItem.Value = keySet.ToString();
                         }
                         else
                         {
                             if (metadata.Items == null) metadata.Items = new List<ComputeMetadata.ItemsData>();
                             metadata.Items.Add(new ComputeMetadata.ItemsData { 
                                 Key = MetadataAuthorizedPublicKeySet.MetadataKey,
                                 Value = keySet.ToString()
                             });
                         }
                    },
                    token).ConfigureAwait(false);
            }
        }
    }
}
