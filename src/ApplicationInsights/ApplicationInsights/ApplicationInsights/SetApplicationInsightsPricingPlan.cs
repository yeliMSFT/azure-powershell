﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Commands.ApplicationInsights.Models;
using Microsoft.Azure.Commands.ResourceManager.Common.ArgumentCompleters;
using Microsoft.Azure.Management.ApplicationInsights.Management.Models;
using Microsoft.Azure.Management.Internal.Resources.Utilities.Models;
using Microsoft.WindowsAzure.Commands.Common.CustomAttributes;
using System.Management.Automation;

namespace Microsoft.Azure.Commands.ApplicationInsights
{
    [GenericBreakingChange("Output type will be updated to match API 2015-05-01", "2.0.0")]
    [Cmdlet("Set", ResourceManager.Common.AzureRMConstants.AzureRMPrefix + "ApplicationInsightsPricingPlan", DefaultParameterSetName = ComponentNameParameterSet, SupportsShouldProcess = true), OutputType(typeof(PSPricingPlan))]
    public class SetApplicationInsightsPricingPlanCommand : ApplicationInsightsBaseCmdlet
    {
        [Parameter(
            Position = 0,
            Mandatory = true,
            ParameterSetName = ComponentObjectParameterSet,
            ValueFromPipeline = true,
            HelpMessage = "Application Insights Component Object.")]
        [CmdletParameterBreakingChange("ApplicationInsightsComponent", ChangeDescription = "Parameter ApplicationInsightsComponent will be removed in upcoming Az.ApplicationInsights 2.0.0")]
        [ValidateNotNull]
        public PSApplicationInsightsComponent ApplicationInsightsComponent { get; set; }

        [Parameter(
            Position = 0,
            Mandatory = true,
            ParameterSetName = ResourceIdParameterSet,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Application Insights Component Resource Id.")]
        [CmdletParameterBreakingChange("ResourceId", ChangeDescription = "Parameter ResourceId will be removed in upcoming Az.ApplicationInsights 2.0.0")]
        [ValidateNotNullOrEmpty]
        public string ResourceId { get; set; }

        [Parameter(
            Position = 0,
            Mandatory = true,
            ParameterSetName = ComponentNameParameterSet,
            HelpMessage = "Resource Group Name.")]
        [ResourceGroupCompleter]
        [ValidateNotNullOrEmpty]
        public string ResourceGroupName { get; set; }

        [Parameter(
            Position = 1,
            Mandatory = true,
            ParameterSetName = ComponentNameParameterSet,
            HelpMessage = "Application Insights Component Name.")]
        [Alias(ApplicationInsightsComponentNameAlias, ComponentNameAlias)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Pricing plan name.")]
        [ValidateSet(PricingPlans.Basic,
            PricingPlans.Enterprise,
            PricingPlans.LimitedBasic,
            IgnoreCase = true)]
        [ValidateNotNullOrEmpty]
        public string PricingPlan { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Daily Cap.")]        
        public double? DailyCapGB { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Stop send notification when hit cap.")]        
        public SwitchParameter DisableNotificationWhenHitCap { get; set; }

        public override void ExecuteCmdlet()
        {
            base.ExecuteCmdlet();

            if (this.ApplicationInsightsComponent != null)
            {
                this.ResourceGroupName = this.ApplicationInsightsComponent.ResourceGroupName;
                this.Name = this.ApplicationInsightsComponent.Name;
            }

            if (!string.IsNullOrEmpty(this.ResourceId))
            {
                ResourceIdentifier identifier = new ResourceIdentifier(this.ResourceId);
                this.ResourceGroupName = identifier.ResourceGroupName;
                this.Name = identifier.ResourceName;
            }

            ApplicationInsightsComponentBillingFeatures features =
                                                this.AppInsightsManagementClient
                                                        .ComponentCurrentBillingFeatures
                                                        .GetWithHttpMessagesAsync(
                                                            this.ResourceGroupName,
                                                            this.Name)
                                                        .GetAwaiter()
                                                        .GetResult()
                                                        .Body;
            if (!string.IsNullOrEmpty(this.PricingPlan))
            {
                if (this.PricingPlan.ToLowerInvariant().Contains("enterprise"))
                {
                    features.CurrentBillingFeatures = new string[] { "Application Insights Enterprise" };
                }
                else if (this.PricingPlan.ToLowerInvariant().Contains("limited"))
                {
                    features.CurrentBillingFeatures = new string[] { "Limited Basic" };
                }
                else
                {
                    features.CurrentBillingFeatures = new string[] { "Basic" };
                }
            }

            if (this.DailyCapGB != null)
            {
                features.DataVolumeCap.Cap = this.DailyCapGB.Value;
            }

            if (this.DisableNotificationWhenHitCap.IsPresent)
            {
                features.DataVolumeCap.StopSendNotificationWhenHitCap = true;
            }
            else            
            {
                features.DataVolumeCap.StopSendNotificationWhenHitCap = false;
            }

            if (this.ShouldProcess(this.Name, "Update Pricing Plan"))
            {
                var putResponse = this.AppInsightsManagementClient
                                        .ComponentCurrentBillingFeatures
                                        .UpdateWithHttpMessagesAsync(
                                            this.ResourceGroupName,
                                            this.Name,
                                            features)
                                        .GetAwaiter()
                                        .GetResult();

                WriteCurrentFeatures(putResponse.Body);
            }
        }
    }
}
