﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Virterix.AdMediation.Editor
{
    public class ApplovinSettings : BaseAdNetworkSettings
    {
        public string _sdkKey;

        public override bool IsAppIdSupported => false;

        public override Type NetworkAdapterType => typeof(AppLovinAdapter);

        protected override string AdapterScriptName => "AppLovinAdapter";
        protected override string AdapterDefinePeprocessorKey => "_AMS_APPLOVIN";

        public override bool IsAdSupported(AdType adType)
        {
            bool isSupported = adType == AdType.Interstitial || adType == AdType.Incentivized;
            return isSupported;
        }

        public override bool IsCheckAvailabilityWhenPreparing(AdType adType)
        {
            return false;
        }

        public override void SetupNetworkAdapter(AdMediationProjectSettings settings, Component networkAdapter)
        {
            AppLovinAdapter.SetupNetworkNativeSettings(_sdkKey);
        }

        public override Dictionary<string, object> GetSpecificNetworkParameters(AppPlatform platform)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("sdkKey", _sdkKey);
            return parameters;
        }
    }
} // namespace Virterix.AdMediation.Editor
