using System.Linq;
using CubeChallenge3D.Save;
using CubeChallenge3D.Save.Records;
using CubeChallenge3D.Stages.Progress;
using CubeChallenge3D.Stages.Services;
using CubeChallenge3D.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Records
{
    public sealed class RecordsPanelUI
    {
        private readonly GameObject root;
        private readonly Text quickPlayText;
        private readonly Text stageText;
        private readonly Text rankingText;
        private readonly QuickPlayRecordStore quickPlayStore = new QuickPlayRecordStore();
        private readonly StageProgressStore stageProgressStore = new StageProgressStore();
        private readonly StageDataLoader stageLoader = new StageDataLoader();

        public RecordsPanelUI(Transform parent)
        {
            Canvas canvas = RuntimeUiFactory.CreateCanvas(parent, "RecordsCanvas", 1470);
            root = canvas.gameObject;
            RectTransform panel = RuntimeUiFactory.CreatePanel(
                root.transform,
                "RecordsPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 760f));

            Text title = RuntimeUiFactory.CreateText(panel, "Title", "Records", 42, TextAnchor.MiddleCenter);
            SetTopRect(title.rectTransform, -62f, 54f);

            quickPlayText = CreateSection(panel, "QuickPlay", -136f, 156f);
            rankingText = CreateSection(panel, "Ranking", -310f, 126f);
            stageText = CreateSection(panel, "Stages", -454f, 156f);

            Button close = RuntimeUiFactory.CreateButton(panel, "CloseButton", "Back", new Vector2(0f, 28f), new Vector2(300f, 58f));
            close.onClick.AddListener(Hide);
            Hide();
        }

        public void Show()
        {
            Refresh();
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        private void Refresh()
        {
            QuickPlayResult bestTime = quickPlayStore.GetBestByTime();
            QuickPlayResult bestMoves = quickPlayStore.GetBestByMoves();
            quickPlayText.text = bestTime == null
                ? $"Best Times\n{UIStrings.NoRecords}"
                : $"Best Times\nPractice  {FormatTime(bestTime.elapsedSeconds)}\nBest Moves  {bestMoves.moveCount}\nCompleted Runs  {quickPlayStore.Count}";

            var stages = stageLoader.LoadAllStages();
            int cleared = stages.Count(stage => stageProgressStore.GetProgress(stage.stageId).isCleared);
            int stars = stages.Sum(stage => stageProgressStore.GetProgress(stage.stageId).stars);
            stageText.text = $"Stage Records\nCleared  {cleared}/{stages.Count}\nTotal Stars  {stars}/{stages.Count * 3}";

            rankingText.text = "Ranking Challenge\nView daily rankings from the challenge screen.";
        }

        private static Text CreateSection(RectTransform parent, string name, float y, float height)
        {
            GameObject sectionObject = new GameObject(name + "Section", typeof(RectTransform), typeof(Image));
            sectionObject.transform.SetParent(parent, false);
            RectTransform section = sectionObject.GetComponent<RectTransform>();
            section.anchorMin = new Vector2(0f, 1f);
            section.anchorMax = new Vector2(1f, 1f);
            section.pivot = new Vector2(0.5f, 1f);
            section.anchoredPosition = new Vector2(0f, y);
            section.sizeDelta = new Vector2(-90f, height);
            sectionObject.GetComponent<Image>().color = new Color(0.09f, 0.12f, 0.15f, 0.98f);
            Text text = RuntimeUiFactory.CreateText(section, "Text", string.Empty, 25, TextAnchor.MiddleLeft);
            text.rectTransform.offsetMin = new Vector2(30f, 18f);
            text.rectTransform.offsetMax = new Vector2(-30f, -18f);
            return text;
        }

        private static void SetTopRect(RectTransform rect, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-100f, height);
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            return $"{minutes:00}:{seconds - (minutes * 60):00.00}";
        }
    }
}
