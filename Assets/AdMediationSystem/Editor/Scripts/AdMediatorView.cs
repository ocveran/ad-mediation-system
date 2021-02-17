﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;

namespace Virterix.AdMediation.Editor
{
    public class AdMediatorView
    {
        private AdMediationSettingsWindow _settingsWindow;
        private SerializedProperty _mediatorProp;
        private SerializedProperty _tierListProp;
        private Dictionary<string, ReorderableList> _unitListDict = new Dictionary<string, ReorderableList>();
        private ReorderableList _tierReorderableList;
        private AnimBool _showMediator;
        private int _index;
        private AdType _adType;

        private SerializedProperty _mediatorNameProp;
        private SerializedProperty _fetchStrategyProp;
        private SerializedProperty _continueAfterEndSessionProp;
        private SerializedProperty _bannerPositionProp;
        private SerializedProperty _bannerMinDisplayTimeProp;
        private SerializedProperty _deferredFetchDelayProp;
        private SerializedProperty _autoFetchOnHideProp;
        private List<string> _activeNetworks = new List<string>();

        public AdType AdType
        {
            get { return _adType; }
        }

        public FetchStrategyType StrategyType
        {
            get
            {
                FetchStrategyType type = (FetchStrategyType)_fetchStrategyProp.intValue;
                return type;
            }
        }

        private string[] MediationStratageTypes
        {
            get; set;
        }

        private string[] BannerPositionTypes
        {
            get; set;
        }

        public AdMediatorView(AdMediationSettingsWindow settingsWindow, int mediatorId, SerializedObject serializedSettings,
            SerializedProperty mediatorProp, UnityAction repaint, AdType adType)
        {
            _settingsWindow = settingsWindow;
            _adType = adType;
            SetProperty(mediatorId, mediatorProp);
            mediatorProp.FindPropertyRelative("_adType").intValue = (int)adType;
            _showMediator = new AnimBool(true);
            _showMediator.valueChanged.AddListener(repaint);
            _tierReorderableList = new ReorderableList(serializedSettings, _tierListProp);
            _tierReorderableList.headerHeight = 1;
            MediationStratageTypes = Enum.GetNames(typeof(FetchStrategyType));
            BannerPositionTypes = Enum.GetNames(typeof(BannerPosition));

            _tierReorderableList.onAddCallback = (ReorderableList list) =>
            {
                ReorderableList.defaultBehaviours.DoAddButton(list);
                var addedElement = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
                var units = addedElement.FindPropertyRelative("_units");
                units.arraySize = 0;
            };
            _tierReorderableList.drawHeaderCallback = rect =>
            {
            };

            float unitElementHegiht = 22;

            _tierReorderableList.drawElementCallback = (elementRect, elementIndex, elementActive, elementFocused) =>
            {
                var element = _tierListProp.GetArrayElementAtIndex(elementIndex);
                var unitsProp = element.FindPropertyRelative("_units");
                string listKey = element.propertyPath;

                ReorderableList unitReorderableList;
                if (_unitListDict.ContainsKey(listKey))
                {
                    unitReorderableList = _unitListDict[listKey];
                }
                else
                {
                    unitReorderableList = new ReorderableList(element.serializedObject, unitsProp);
                    unitReorderableList.elementHeight = unitElementHegiht;
                    _unitListDict[listKey] = unitReorderableList;

                    unitReorderableList.onAddCallback = (ReorderableList list) =>
                    {
                        ReorderableList.defaultBehaviours.DoAddButton(list);
                        SerializedProperty unitElement = unitReorderableList.serializedProperty.GetArrayElementAtIndex(list.count - 1);
                        FetchStrategyType strategyType = (FetchStrategyType)_fetchStrategyProp.intValue;
                        switch (strategyType)
                        {
                            case FetchStrategyType.Sequence:
                                break;
                            case FetchStrategyType.Random:
                                var percentageProp = unitElement.FindPropertyRelative("_percentage");
                                percentageProp.intValue = unitsProp.arraySize == 1 ? 100 : 0;
                                break;
                        }
                    };
                    unitReorderableList.onRemoveCallback = (ReorderableList list) =>
                    {
                        int selectedUnitIndex = unitReorderableList.index;
                        var unitElement = unitReorderableList.serializedProperty.GetArrayElementAtIndex(unitReorderableList.index);
                        int changedValue = -unitElement.FindPropertyRelative("_percentage").intValue;
                        ReorderableList.defaultBehaviours.DoRemoveButton(list);
                        if (unitsProp.arraySize > 0)
                        {
                            var lastUnit = unitsProp.arraySize == 1 ? unitsProp.GetArrayElementAtIndex(unitsProp.arraySize - 1) : null;
                            DistributeUnitPercentageOfRandomStrategy(unitsProp, lastUnit, -1, changedValue);
                        }
                    };
                    unitReorderableList.drawElementCallback = (unitRect, unitIndex, unitActive, unitFocused) =>
                    {
                        SerializedProperty unitElement = unitReorderableList.serializedProperty.GetArrayElementAtIndex(unitIndex);
                        float elementWidth = unitRect.width * 0.5f;
                        float width = Mathf.Clamp(elementWidth - 120, 130, 180);

                        Rect popupRect = unitRect;
                        popupRect.y += 1f;
                        popupRect.width = width;
                        popupRect.height = 20;

                        _settingsWindow.GetActiveNetworks(_adType, ref _activeNetworks);
                        string[] activeNetworks = _activeNetworks.ToArray();

                        var networkNameProp = unitElement.FindPropertyRelative("_networkName");
                        var networkIndexProp = unitElement.FindPropertyRelative("_networkIndex");
                        var networkIdentifierProp = unitElement.FindPropertyRelative("_networkIdentifier");

                        string currNetworkName = "";

                        networkIndexProp.intValue = EditorGUI.Popup(popupRect, networkIndexProp.intValue, activeNetworks);
                        if (networkIndexProp.intValue < activeNetworks.Length)
                        {
                            currNetworkName = activeNetworks[networkIndexProp.intValue];
                            networkNameProp.stringValue = currNetworkName;
                            networkIdentifierProp.stringValue = _settingsWindow.GetNetworkIndentifier(currNetworkName);
                        }

                        string[] adInstanceNames = { };
                        if (!string.IsNullOrEmpty(currNetworkName))
                        {
                            var networkView = _settingsWindow.GetNetworkView(currNetworkName);
                            adInstanceNames = _settingsWindow.GetAdInstancesFromStorage(networkView.Identifier, _adType);
                            if (adInstanceNames == null)
                            {
                                _settingsWindow.UpdateAdInstanceStorage(_adType);
                                adInstanceNames = _settingsWindow.GetAdInstancesFromStorage(networkView.Identifier, _adType);
                            }
                        }

                        popupRect.x += popupRect.width + 2;
                        popupRect.width = width;
                        var instanceIndexProp = unitElement.FindPropertyRelative("_instanceIndex");
                        var instanceNameProp = unitElement.FindPropertyRelative("_instanceName");
                        instanceIndexProp.intValue = EditorGUI.Popup(popupRect, instanceIndexProp.intValue, adInstanceNames);
                        if (instanceIndexProp.intValue < adInstanceNames.Length)
                        {
                            instanceNameProp.stringValue = adInstanceNames[instanceIndexProp.intValue];
                        }

                        Rect paramsRect = popupRect;
                        paramsRect.x += paramsRect.width + 5;
                        paramsRect.width = 110;
                        
                        var prepareOnExitProp = unitElement.FindPropertyRelative("_prepareOnExit");
                        prepareOnExitProp.boolValue = EditorGUI.ToggleLeft(paramsRect, "Prepare On Exit", prepareOnExitProp.boolValue);

                        FetchStrategyType strategyType = (FetchStrategyType)_fetchStrategyProp.intValue;
                        switch (strategyType)
                        {
                            case FetchStrategyType.Sequence:
                                paramsRect.x += paramsRect.width + 5;
                                paramsRect.width = 75;
                                var replacedProp = unitElement.FindPropertyRelative("_replaced");
                                replacedProp.boolValue = EditorGUI.ToggleLeft(paramsRect, "Replaced", replacedProp.boolValue);
                                break;
                            case FetchStrategyType.Random:
                                paramsRect.x += paramsRect.width + 5;
                                paramsRect.width = Mathf.Clamp(unitRect.width - 480, 120, 200f);
                                paramsRect.height = 18;
                                var percentageProp = unitElement.FindPropertyRelative("_percentage");
                                int percentageMinValue = 0;
                                int percentageMaxValue = 100;
                                float previousLabelWidth = EditorGUIUtility.labelWidth;
                                EditorGUIUtility.labelWidth = paramsRect.width - 50f;

                                int previousValue = percentageProp.intValue;
                                percentageProp.intValue = EditorGUI.IntSlider(paramsRect, "", percentageProp.intValue, percentageMinValue, percentageMaxValue);
                                if (previousValue != percentageProp.intValue)
                                {
                                    int changedValue = percentageProp.intValue - previousValue;
                                    DistributeUnitPercentageOfRandomStrategy(unitsProp, unitElement, unitIndex, changedValue);
                                }

                                EditorGUIUtility.labelWidth = previousLabelWidth;
                                break;
                        }
                    };
                    unitReorderableList.drawHeaderCallback = (rect) =>
                    {
                        EditorGUI.LabelField(rect, "Tier " + (elementIndex + 1).ToString());
                    };
                }

                // Setup the inner list
                var height = (unitsProp.arraySize + 3) * EditorGUIUtility.singleLineHeight;
                unitReorderableList.DoList(new Rect(elementRect.x, elementRect.y, elementRect.width, height));
            };
            _tierReorderableList.elementHeightCallback = (int index) =>
            {
                var element = _tierListProp.GetArrayElementAtIndex(index);
                var unitsProp = element.FindPropertyRelative("_units");
                return Mathf.Clamp((unitsProp.arraySize - 1) * unitElementHegiht + 75, 75, Mathf.Infinity);
            };
        }

        private int ResolveSelectedNetworkIndex(int selectedNetworkId, string selectedNetworkName, string[] activeNetworks)
        {
            int result = selectedNetworkId;
            string currNetworkName = selectedNetworkId < activeNetworks.Length ? activeNetworks[selectedNetworkId] : "";
            if (selectedNetworkName.Length > 0 && currNetworkName != selectedNetworkName)
            {
                for (int i = 0; i < activeNetworks.Length; i++)
                {
                    if (selectedNetworkName == activeNetworks[i])
                    {
                        result = i;
                        break;
                    }
                }
            }
            return result;
        }

        private void DistributeUnitPercentageOfRandomStrategy(SerializedProperty units, SerializedProperty changedUnit, int changeUnitIndex, int changedValue)
        {
            int maxValue = 100;
            float absChangedValue = Mathf.Abs(changedValue);
            int directionState = changedValue < 0 ? 1 : -1;
            int unitIndex = changeUnitIndex;
            int loopCount = 0;

            int changeTotal = 0;

            if (units.arraySize == 1)
            {
                if (changedUnit != null)
                {
                    var percentageProp = changedUnit.FindPropertyRelative("_percentage");
                    percentageProp.intValue = maxValue;
                }
            }
            else
            {
                for (int i = 0; i < absChangedValue; i++)
                {
                    unitIndex = unitIndex >= units.arraySize - 1 ? 0 : unitIndex + 1;
                    if (unitIndex == changeUnitIndex)
                    {
                        unitIndex++;
                        unitIndex = unitIndex == units.arraySize ? 0 : unitIndex;
                    }

                    var unit = units.GetArrayElementAtIndex(unitIndex);
                    var percentageProp = unit.FindPropertyRelative("_percentage");
                    int percentageValue = percentageProp.intValue;

                    if (directionState < 0)
                    {
                        if (percentageValue > 0)
                        {
                            percentageValue--;
                            percentageProp.intValue = percentageValue;

                            changeTotal++;
                        }
                        else
                        {
                            i--;
                        }
                    }
                    else if (directionState > 0)
                    {
                        if (percentageValue < maxValue)
                        {
                            percentageValue++;
                            percentageProp.intValue = percentageValue;

                            changeTotal++;
                        }
                        else
                        {
                            i--;
                        }
                    }
                    if (++loopCount > 5000)
                    {
                        break;
                    }
                }
            }
        }

        public void FixPopupSelection()
        {
            for (int tierIndex = 0; tierIndex < _tierListProp.arraySize; tierIndex++)
            {
                var tier = _tierListProp.GetArrayElementAtIndex(tierIndex);
                var units = tier.FindPropertyRelative("_units");

                for (int unitIndex = 0; unitIndex < units.arraySize; unitIndex++)
                {
                    var unit = units.GetArrayElementAtIndex(unitIndex);
                    var networkNameProp = unit.FindPropertyRelative("_networkName");
                    var networkIndexProp = unit.FindPropertyRelative("_networkIndex");
                    var networkIdentifierProp = unit.FindPropertyRelative("_networkIdentifier");

                    _settingsWindow.GetActiveNetworks(_adType, ref _activeNetworks);
                    string[] activeNetworks = _activeNetworks.ToArray();

                    int solvedNetworkIndex = ResolveSelectedNetworkIndex(networkIndexProp.intValue, networkNameProp.stringValue, activeNetworks);
                    if (solvedNetworkIndex != networkIndexProp.intValue)
                    {
                        networkIndexProp.intValue = solvedNetworkIndex;
                        networkNameProp.stringValue = activeNetworks[solvedNetworkIndex];
                        networkIdentifierProp.stringValue = _settingsWindow.GetNetworkIndentifier(networkNameProp.stringValue);
                    }
                }
            }
            _tierListProp.serializedObject.ApplyModifiedProperties();
        }

        public void SetProperty(int mediatorId, SerializedProperty mediatorProp)
        {
            _index = mediatorId;
            _mediatorProp = mediatorProp;
            _tierListProp = _mediatorProp.FindPropertyRelative("_tiers");
            _mediatorNameProp = _mediatorProp.FindPropertyRelative("_name");
            _fetchStrategyProp = _mediatorProp.FindPropertyRelative("_fetchStrategyType");
            _continueAfterEndSessionProp = _mediatorProp.FindPropertyRelative("_isContinueAfterEndSession");
            _autoFetchOnHideProp = _mediatorProp.FindPropertyRelative("_isAutoFetchOnHide");
            _bannerPositionProp = _mediatorProp.FindPropertyRelative("_bannerPosition");
            _bannerMinDisplayTimeProp = _mediatorProp.FindPropertyRelative("_bannerMinDisplayTime");
            _deferredFetchDelayProp = _mediatorProp.FindPropertyRelative("_deferredFetchDelay");
            if (_tierReorderableList != null)
            {
                _tierReorderableList.serializedProperty = _tierListProp;
            }
        }

        public void SetupDefaultParameters()
        {
            _mediatorNameProp.stringValue = _index == 0 ? "Default" : "";
            _fetchStrategyProp.intValue = 0;
            _continueAfterEndSessionProp.boolValue = true;
            _autoFetchOnHideProp.boolValue = true;
            _bannerPositionProp.intValue = 0;
            _bannerMinDisplayTimeProp.intValue = 30;
            _deferredFetchDelayProp.intValue = 90;
            _tierListProp.arraySize = 0;
        }

        private bool Collapsed { get; set; }

        public void DrawView(out bool wasDeletionPerform)
        {
            EditorGUILayout.BeginVertical("helpbox");
            string mediatorNamePostfix = _mediatorNameProp.stringValue == "" ? (_index + 1).ToString() : _mediatorNameProp.stringValue;
            string mediatorName = string.Format("Mediator {0}", mediatorNamePostfix);
            Collapsed = EditorGUILayout.BeginFoldoutHeaderGroup(Collapsed, mediatorName);
            _showMediator.target = Collapsed;
            wasDeletionPerform = false;

            if (EditorGUILayout.BeginFadeGroup(_showMediator.faded))
            {
                EditorGUILayout.Space(4);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Name", GUILayout.Width(150));
                _mediatorNameProp.stringValue = EditorGUILayout.TextField(_mediatorNameProp.stringValue, GUILayout.Width(180));

                GUILayout.FlexibleSpace();
                wasDeletionPerform = GUILayout.Button("Delete");

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Mediation Strategy", GUILayout.Width(150));
                _fetchStrategyProp.intValue = EditorGUILayout.Popup(_fetchStrategyProp.intValue, MediationStratageTypes, GUILayout.Width(180));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                _continueAfterEndSessionProp.boolValue = EditorGUILayout.ToggleLeft("Continue After End Session", 
                    _continueAfterEndSessionProp.boolValue, GUILayout.Width(170));
                EditorGUILayout.Space(10, false);
                _autoFetchOnHideProp.boolValue = EditorGUILayout.ToggleLeft("Auto Fetch On Hide", _autoFetchOnHideProp.boolValue, GUILayout.Width(130));

                EditorGUILayout.EndHorizontal();

                if (_adType == AdType.Banner)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField("Banner Position", GUILayout.Width(150));
                    _bannerPositionProp.intValue = EditorGUILayout.Popup(_bannerPositionProp.intValue, BannerPositionTypes, GUILayout.Width(180));
                    EditorGUILayout.Space(10, false);
                    EditorGUILayout.PropertyField(_bannerMinDisplayTimeProp, GUILayout.Width(210));

                    EditorGUILayout.EndHorizontal();
                }
                else if (_adType == AdType.Incentivized)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(_deferredFetchDelayProp, GUILayout.Width(332));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(10, false);
                _tierReorderableList.DoLayoutList();
            }
            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();
        }

    }
} // Virterix.AdMediation.Editor