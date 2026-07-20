using System;
using CubeChallenge3D.UI.Common;
using CubeChallenge3D.UI.Style;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Stages
{
    public sealed class StageNodePlacementDebugUI
    {
        private readonly RectTransform root;
        private readonly RectTransform mapRoot;
        private readonly StageSlotLayoutConfig config;
        private readonly Action onChanged;
        private Text infoText;
        private int selectedSlotIndex;

        private StageNodePlacementDebugUI(RectTransform parent, StageSlotLayoutConfig config, Action onChanged)
        {
            mapRoot = parent;
            this.config = config;
            this.onChanged = onChanged;

            root = new GameObject("StageNodePlacementDebugUI", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.SetAsLastSibling();

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            BuildToolbar();
            BuildHandles();
            root.gameObject.SetActive(false);
        }

        public static StageNodePlacementDebugUI Create(RectTransform parent, StageSlotLayoutConfig config, Action onChanged)
        {
            return new StageNodePlacementDebugUI(parent, config, onChanged);
        }

        public void SetVisible(bool visible)
        {
            root.gameObject.SetActive(visible);
            if (visible)
            {
                root.SetAsLastSibling();
                RefreshInfo();
            }
        }

        private void BuildToolbar()
        {
            RectTransform panel = CreatePanel(root, "PlacementToolbar", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(270f, -640f), new Vector2(420f, 260f));

            infoText = RuntimeUiFactory.CreateText(panel, "Info", string.Empty, 20, TextAnchor.UpperLeft);
            infoText.rectTransform.offsetMin = new Vector2(18f, 78f);
            infoText.rectTransform.offsetMax = new Vector2(-18f, -16f);
            infoText.color = Color.white;

            Button smaller = CreateToolbarButton(panel, "ScaleMinus", "-", new Vector2(-130f, 30f));
            smaller.onClick.AddListener(() => ChangeSelectedScale(-0.03f));

            Button bigger = CreateToolbarButton(panel, "ScalePlus", "+", new Vector2(-48f, 30f));
            bigger.onClick.AddListener(() => ChangeSelectedScale(0.03f));

            Button save = CreateToolbarButton(panel, "Save", "Save", new Vector2(66f, 30f), new Vector2(110f, 54f));
            save.onClick.AddListener(() => config.Save());

            Button log = CreateToolbarButton(panel, "Log", "Log", new Vector2(190f, 30f), new Vector2(94f, 54f));
            log.onClick.AddListener(() => Debug.Log(config.ToCSharpArrayLog()));
        }

        private void BuildHandles()
        {
            for (int i = 0; i < 8; i++)
            {
                StageSlotLayout slot = config.GetSlot(i);
                Button handle = RuntimeUiFactory.CreateButton(root, $"Slot{i}Handle", (i + 1).ToString(), ToAnchoredPosition(slot.normalizedPosition), new Vector2(74f, 74f));
                Image image = handle.GetComponent<Image>();
                CasualUIStyle.ApplyPanel(image, new Color(1f, 0.15f, 0.55f, 0.78f), 20);
                Outline outline = handle.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(3f, -3f);

                Text label = handle.GetComponentInChildren<Text>();
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                CasualUIStyle.ApplyTextDepth(label, true);

                RectTransform handleRect = handle.GetComponent<RectTransform>();
                handleRect.localScale = Vector3.one * slot.scale;

                StageSlotDragHandle drag = handle.gameObject.AddComponent<StageSlotDragHandle>();
                int captured = i;
                drag.Initialize(mapRoot, handleRect, value =>
                {
                    SelectSlot(captured);
                    StageSlotLayout activeSlot = config.GetSlot(captured);
                    activeSlot.normalizedPosition = value;
                    handleRect.anchoredPosition = ToAnchoredPosition(value);
                    onChanged?.Invoke();
                    RefreshInfo();
                });
                handle.onClick.AddListener(() => SelectSlot(captured));
            }
        }

        private void SelectSlot(int index)
        {
            selectedSlotIndex = Mathf.Clamp(index, 0, 7);
            RefreshInfo();
        }

        private void ChangeSelectedScale(float delta)
        {
            StageSlotLayout slot = config.GetSlot(selectedSlotIndex);
            slot.scale = Mathf.Clamp(slot.scale + delta, 0.35f, 1.6f);
            Transform handle = root.Find($"Slot{selectedSlotIndex}Handle");
            if (handle != null)
            {
                handle.localScale = Vector3.one * slot.scale;
            }

            onChanged?.Invoke();
            RefreshInfo();
        }

        private void RefreshInfo()
        {
            StageSlotLayout slot = config.GetSlot(selectedSlotIndex);
            infoText.text =
                $"Slot {selectedSlotIndex}\n"
                + $"x={slot.normalizedPosition.x:0.0000}\n"
                + $"y={slot.normalizedPosition.y:0.0000}\n"
                + $"scale={slot.scale:0.0000}\n"
                + "Drag pink handles.\n"
                + "Save writes JSON.";
        }

        private Vector2 ToAnchoredPosition(Vector2 normalized)
        {
            Rect rect = mapRoot.rect;
            float width = rect.width > 1f ? rect.width : 1080f;
            float height = rect.height > 1f ? rect.height : 1920f;
            return new Vector2((normalized.x - 0.5f) * width, normalized.y * height);
        }

        private static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            CasualUIStyle.ApplyPanel(rect.GetComponent<Image>(), new Color(0.02f, 0.04f, 0.12f, 0.86f), 18);
            return rect;
        }

        private static Button CreateToolbarButton(RectTransform parent, string name, string label, Vector2 position, Vector2? size = null)
        {
            Button button = RuntimeUiFactory.CreateButton(parent, name, label, position, size ?? new Vector2(70f, 54f));
            CasualUIStyle.ApplyPanel(button.GetComponent<Image>(), new Color(0.15f, 0.35f, 0.95f, 0.94f), 16);
            Text text = button.GetComponentInChildren<Text>();
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            return button;
        }
    }

    public sealed class StageSlotDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform mapRoot;
        private RectTransform handle;
        private Action<Vector2> onDragged;

        public void Initialize(RectTransform mapRoot, RectTransform handle, Action<Vector2> onDragged)
        {
            this.mapRoot = mapRoot;
            this.handle = handle;
            this.onDragged = onDragged;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (mapRoot == null || handle == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRoot, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                return;
            }

            Rect rect = mapRoot.rect;
            float width = rect.width > 1f ? rect.width : 1080f;
            float height = rect.height > 1f ? rect.height : 1920f;
            Vector2 normalized = new Vector2(
                Mathf.Clamp01((local.x / width) + 0.5f),
                Mathf.Clamp01(local.y / height));
            onDragged?.Invoke(normalized);
        }
    }
}
