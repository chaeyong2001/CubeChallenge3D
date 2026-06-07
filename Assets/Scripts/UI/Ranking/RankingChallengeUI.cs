using System.Text;
using CubeChallenge3D.Core;
using CubeChallenge3D.GameModes.RankingChallenge;
using CubeChallenge3D.Ranking;
using CubeChallenge3D.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Ranking
{
    public sealed class RankingChallengeUI : MonoBehaviour
    {
        private RankingChallengeGameMode gameMode;
        private Text titleText;
        private Text timerText;
        private Text moveText;
        private Text resultText;
        private Text rankingText;
        private Button startButton;
        private Canvas canvas;
        private readonly Vector2 minPanelSize = new Vector2(620f, 560f);
        private readonly Vector2 maxPanelSize = new Vector2(980f, 980f);

        public void Initialize(RankingChallengeGameMode mode)
        {
            gameMode = mode;
            BuildUi();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void BuildUi()
        {
            if (canvas != null)
            {
                return;
            }

            canvas = RuntimeUiFactory.CreateCanvas(transform, "RankingChallengeCanvas", 1220);
            RectTransform panel = RuntimeUiFactory.CreatePanel(
                canvas.transform,
                "RankingChallengePanel",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -30f),
                new Vector2(760f, 700f));
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.04f, 0.05f, 0.07f, 0.97f);
                panelImage.raycastTarget = true;
            }

            AddDragBar(panel);
            AddResizeHandle(panel);

            titleText = CreateRow(panel, "Title", new Vector2(0f, -76f), 32, 54f);
            timerText = CreateRow(panel, "Timer", new Vector2(0f, -140f), 36, 58f);
            moveText = CreateRow(panel, "Moves", new Vector2(0f, -210f), 28, 48f);
            resultText = CreateRow(panel, "Result", new Vector2(0f, -264f), 26, 48f);
            rankingText = CreateRow(panel, "Ranking", new Vector2(0f, -330f), 24, 230f);
            rankingText.alignment = TextAnchor.UpperCenter;

            startButton = RuntimeUiFactory.CreateButton(panel, "StartButton", "Start Challenge", new Vector2(0f, 34f), new Vector2(380f, 76f));
            startButton.onClick.AddListener(() => gameMode?.StartChallenge());
        }

        private static Text CreateRow(RectTransform parent, string name, Vector2 position, int size, float height)
        {
            GameObject row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(0f, height);
            return RuntimeUiFactory.CreateText(rect, "Text", string.Empty, size, TextAnchor.MiddleCenter);
        }

        private void AddDragBar(RectTransform parent)
        {
            GameObject barObject = new GameObject("DragBar", typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(parent, false);
            RectTransform rect = barObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 56f);

            Image image = barObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.11f, 0.14f, 1f);

            PanelDragHandle handle = barObject.AddComponent<PanelDragHandle>();
            handle.Initialize(parent);
        }

        private void AddResizeHandle(RectTransform parent)
        {
            GameObject handleObject = new GameObject("ResizeHandle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(parent, false);
            RectTransform rect = handleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-12f, 12f);
            rect.sizeDelta = new Vector2(44f, 44f);

            Image image = handleObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.35f);

            PanelResizeHandle handle = handleObject.AddComponent<PanelResizeHandle>();
            handle.Initialize(parent, minPanelSize, maxPanelSize);
        }

        private void Refresh()
        {
            if (gameMode == null || titleText == null)
            {
                return;
            }

            RankingChallengeConfig config = gameMode.Config;
            titleText.text = config != null
                ? $"Daily Challenge  {config.dateUtc}"
                : "Daily Challenge";
            timerText.text = $"Time  {FormatTime(gameMode.ElapsedTime)}";
            moveText.text = $"Moves  {gameMode.MoveCount}";
            resultText.text = gameMode.State == RankingChallengeState.Solved
                ? gameMode.LastSubmitMessage
                : $"State  {gameMode.State}";
            startButton.interactable = gameMode.State != RankingChallengeState.Scrambling;
            rankingText.text = BuildRankingText(config?.challengeId);
        }

        private string BuildRankingText(string challengeId)
        {
            if (string.IsNullOrEmpty(challengeId) || gameMode.LocalRankingStore == null)
            {
                return "Local Ranking\n-";
            }

            var top = gameMode.LocalRankingStore.GetTopByTime(challengeId, 10);
            var builder = new StringBuilder("Local Ranking Top 10\n");
            if (top.Count == 0)
            {
                builder.Append("-");
                return builder.ToString();
            }

            for (int i = 0; i < top.Count; i++)
            {
                RankingSubmission record = top[i];
                builder.Append(i + 1)
                    .Append(". ")
                    .Append(record.playerName)
                    .Append("  ")
                    .Append(FormatTime(record.elapsedSeconds))
                    .Append("  ")
                    .Append(record.moveCount)
                    .Append(" moves");
                if (!record.isVerified)
                {
                    builder.Append("  local");
                }

                if (i < top.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remaining = seconds - (minutes * 60f);
            return $"{minutes:00}:{remaining:00.00}";
        }

        private sealed class PanelDragHandle : MonoBehaviour, IDragHandler
        {
            private RectTransform target;
            private Canvas parentCanvas;

            public void Initialize(RectTransform dragTarget)
            {
                target = dragTarget;
                parentCanvas = dragTarget.GetComponentInParent<Canvas>();
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (target == null)
                {
                    return;
                }

                float scale = parentCanvas != null && parentCanvas.scaleFactor > 0f
                    ? parentCanvas.scaleFactor
                    : 1f;
                target.anchoredPosition += eventData.delta / scale;
            }
        }

        private sealed class PanelResizeHandle : MonoBehaviour, IDragHandler
        {
            private RectTransform target;
            private Vector2 minSize;
            private Vector2 maxSize;

            public void Initialize(RectTransform resizeTarget, Vector2 minimumSize, Vector2 maximumSize)
            {
                target = resizeTarget;
                minSize = minimumSize;
                maxSize = maximumSize;
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (target == null)
                {
                    return;
                }

                Vector2 nextSize = target.sizeDelta + new Vector2(eventData.delta.x, -eventData.delta.y);
                nextSize.x = Mathf.Clamp(nextSize.x, minSize.x, maxSize.x);
                nextSize.y = Mathf.Clamp(nextSize.y, minSize.y, maxSize.y);
                target.sizeDelta = nextSize;
            }
        }
    }
}
