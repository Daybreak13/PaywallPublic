using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Paywall.Tools {

    public class SegmentedBar : MonoBehaviour {
        /// The ID of the character linked to this bar
        [field: Tooltip("The ID of the character linked to this bar")]
        [field: SerializeField] public string PlayerID { get; protected set; } = "Player1";

        [field: Header("Objects")]

        /// The prefab for a filled segment of the bar
        [field: Tooltip("The prefab for a filled segment of the bar")]
        [field: SerializeField] public GameObject FilledSegmentPrefab { get; protected set; }
        /// The prefab for an empty segment of the bar
        [field: Tooltip("The prefab for an empty segment of the bar")]
        [field: SerializeField] public GameObject EmptySegmentPrefab { get; protected set; }
        /// The bar's UI mask
        [field: Tooltip("The bar's UI mask")]
        [field: SerializeField] public RectTransform BarMask { get; protected set; }
        /// The rect transform of the ammo bar
        [field: Tooltip("The rect transform of the ammo bar")]
        [field: SerializeField] public RectTransform AmmoBar { get; protected set; }

        [field: Header("Values")]

        /// The maximum value of this bar
        [field: Tooltip("The maximum value of this bar")]
        [field: SerializeField] public float MaximumValue { get; protected set; } = 3;
        /// The minimum value of this bar (currently unused)
        [field: Tooltip("The minimum value of this bar")]
        [field: SerializeField] public float MinimumValue { get; protected set; } = 0;
        /// If true, set an initial fill value on start
        [field: Tooltip("If true, set an initial fill value on start")]
        [field: SerializeField] public bool SetInitialFillValue { get; protected set; }
        /// If true, set an initial fill value on start
        [field: Tooltip("If true, set an initial fill value on start")]
        [field: FieldCondition("SetInitialFillValue", true)]
        [field: SerializeField] public float InitialFillValue { get; protected set; }

        protected List<Transform> _segments = new();
        protected float _barWidth;      // width of the entire bar
        protected float _segmentWidth;  // width of single segment of the bar
        protected HorizontalLayoutGroup _layoutGroup;   // parent of bar segments

        /// <summary>
        /// The bar's current value
        /// </summary>
        public float CurrentValue { get; protected set; }

        protected bool UseExistingSegments { get { return (FilledSegmentPrefab == null); } }

        protected virtual void Start() {
            Initialization();
        }

        /// <summary>
        /// Set initial fill if applicable
        /// </summary>
        protected virtual void Initialization() {
            if (SetInitialFillValue) {
                SetCurrentValue(InitialFillValue);
            } else {
                UpdateBar();
            }
            _barWidth = GetComponent<RectTransform>().sizeDelta.x;
            _segmentWidth = Mathf.Abs(_barWidth / MaximumValue);
            _layoutGroup = AmmoBar.GetComponentInChildren<HorizontalLayoutGroup>();
            foreach (Transform child in _layoutGroup.transform) {
                _segments.Add(child);
            }

            SetActiveSegments();
        }

        /// <summary>
        /// Sets the active segments of the bar
        /// Maximum value = current active segments. All other segments inactive
        /// </summary>
        protected virtual void SetActiveSegments() {
            if (UseExistingSegments) {
                int childCount = _layoutGroup.transform.childCount;
                for (int i = 0; i < childCount; i++) {
                    if (i < MaximumValue) {
                        _layoutGroup.transform.GetChild(i).gameObject.SetActive(true);
                    }
                    else {
                        _layoutGroup.transform.GetChild(i).gameObject.SetActive(false);
                    }
                }
            } else {

            }
        }

        /// <summary>
        /// Sets the current value
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetCurrentValue(float value) {
            if ((value <= MaximumValue) && (value >= MinimumValue)) {
                CurrentValue = value;
                UpdateBar();
            }
        }

        /// <summary>
        /// Adds to the current value
        /// </summary>
        /// <param name="value"></param>
        public virtual void AddToCurrentValue(float value) {
            float tempValue = CurrentValue + value;
            if ((tempValue <= MaximumValue) && (tempValue >= MinimumValue)) {
                CurrentValue += value;
            }
            UpdateBar();
        }

        /// <summary>
        /// Sets the maximum value of the bar
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetMaximumValue(float value) {
            MaximumValue = value;
            _segmentWidth = _barWidth / MaximumValue;
            SetActiveSegments();
        }

        /// <summary>
        /// Sets the minimum value of the bar
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetMinimumValue(float value) {
            MinimumValue = value;
        }

        /// <summary>
        /// Updates the bar
        /// </summary>
        protected virtual void UpdateBar() {
            float newWidth = Mathf.Abs(_barWidth * (CurrentValue / MaximumValue));
            BarMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        }

    }
}
