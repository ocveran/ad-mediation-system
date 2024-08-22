/*
 * This file is a part of the Yandex Advertising Network
 *
 * Version for iOS (C) 2023 YANDEX
 *
 * You may not use this file except in compliance with the License.
 * You may obtain a copy of the License at https://legal.yandex.com/partner_ch/
 */

using System.Runtime.InteropServices;

namespace YandexMobileAds.Platforms.iOS
{
    #if (UNITY_5 && UNITY_IOS) || UNITY_IPHONE

    internal class AdRequestBridge
    {
        [DllImport("__Internal")]
        internal static extern string YMAUnityCreateAdRequest(
            string locationId,
            string contextQuery,
            string contextTagsId,
            string parametersId,
            string age,
            string gender);

        [DllImport("__Internal")]
        internal static extern string YMAUnityCreateAdRequestConfiguration(string adUnitId,
            string locationId,
            string contextQuery,
            string contextTagsId,
            string parametersId,
            string age,
            string gender);
    }

    #endif
}
