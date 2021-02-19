﻿using UnityEngine;

namespace Virterix.AdMediation
{
    public class AdInstance
    {
        public const string AD_INSTANCE_DEFAULT_NAME = "Default";

        public AdInstance()
        {
        }

        public AdInstance(AdType adType, string adID, string name = AD_INSTANCE_DEFAULT_NAME)
        {
            m_enabledState = true;
            m_adType = adType;
            m_adId = adID;
            Name = name;
        }

        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                m_name = value;
            }
        }
        string m_name;

        public string ParametersName
        {
            get { return m_adInstanceParams != null ? m_adInstanceParams.Name : AdInstanceParameters.AD_INSTANCE_PARAMETERS_DEFAULT_NAME; }
        }

        public bool IsDefault
        {
            get { return Name.Length == 0 || Name == AD_INSTANCE_DEFAULT_NAME; }
        }
        
        public AdType m_adType;
        public string m_adId;
        public AdNetworkAdapter.TimeoutParams? m_timeout;
        public bool m_bannerVisibled;
        public AdNetworkAdapter.AdState m_state = AdNetworkAdapter.AdState.Uncertain;
        public bool m_lastAdPrepared;
        public bool m_enabledState;
        public object m_adView;
        public IAdInstanceParameters m_adInstanceParams;
        public float m_startImpressionTime;
        public float m_displayTime;
        public float m_responseWaitTime = 30f;
        public bool m_wasLastPreparationFailed;

        public void SaveFailedLoadingTime()
        {
            if (m_timeout != null)
            {
                AdNetworkAdapter.TimeoutParams timeoutParameters = m_timeout.Value;
                timeoutParameters.FailedLoadingTime = Time.realtimeSinceStartup;
                m_timeout = timeoutParameters;
            }
        }
    }
} // namespace Virterix.AdMediation