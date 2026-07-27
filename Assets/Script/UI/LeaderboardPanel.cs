using System;
using System.Text;
using TMPro;
using UnityEngine;

public class LeaderboardPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text rankingText;
    [SerializeField] private TMP_Text statusText;

    private int selectedBossId = 1;
    private int requestVersion;

    [Serializable]
    private class LeaderboardRow
    {
        public int rank_number;
        public string player_name;
        public int completion_ms;
    }

    [Serializable]
    private class LeaderboardResponse
    {
        public LeaderboardRow[] items;
    }

    public void OpenBoss1()
    {
        Open(1);
    }

    public void OpenBoss2()
    {
        Open(2);
    }

    public void Refresh()
    {
        int currentRequestVersion = ++requestVersion;
        SetStatus("Loading");

        if (rankingText != null)
        {
            rankingText.text = string.Empty;
        }

        SupabaseLeaderboardService leaderboardService = SupabaseLeaderboardService.Instance;
        if (leaderboardService == null)
        {
            leaderboardService = FindObjectOfType<SupabaseLeaderboardService>();
        }

        if (leaderboardService == null)
        {
            SetStatus("Network error");
            Debug.LogWarning("SupabaseLeaderboardService was not found.");
            return;
        }

        leaderboardService.LoadLeaderboard(
            selectedBossId,
            json => HandleLeaderboardLoaded(json, currentRequestVersion),
            error => HandleLeaderboardError(error, currentRequestVersion));
    }

    public void Close()
    {
        requestVersion++;
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void Open(int bossId)
    {
        selectedBossId = bossId;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = "Boss " + bossId + " Leaderboard";
        }

        Refresh();
    }

    private void HandleLeaderboardLoaded(string json, int completedRequestVersion)
    {
        if (completedRequestVersion != requestVersion)
        {
            return;
        }

        try
        {
            LeaderboardResponse response =
                JsonUtility.FromJson<LeaderboardResponse>("{\"items\":" + json + "}");
            LeaderboardRow[] rows = response != null ? response.items : null;

            if (rows == null || rows.Length == 0)
            {
                if (rankingText != null)
                {
                    rankingText.text = string.Empty;
                }

                SetStatus("No scores yet");
                return;
            }

            StringBuilder rankingBuilder = new StringBuilder();
            int rowCount = Mathf.Min(10, rows.Length);
            for (int i = 0; i < rowCount; i++)
            {
                LeaderboardRow row = rows[i];
                int rank = row.rank_number > 0 ? row.rank_number : i + 1;
                string playerName = (row.player_name ?? string.Empty)
                    .Replace("<", "＜")
                    .Replace(">", "＞");

                rankingBuilder
                    .Append(rank)
                    .Append(". ")
                    .Append(playerName)
                    .Append("    ")
                    .Append(TimeCount.FormatMilliseconds(row.completion_ms));

                if (i < rowCount - 1)
                {
                    rankingBuilder.AppendLine();
                }
            }

            if (rankingText != null)
            {
                rankingText.text = rankingBuilder.ToString();
            }

            SetStatus(string.Empty);
        }
        catch (Exception exception)
        {
            Debug.LogError("Failed to parse leaderboard response: " + exception.Message);
            SetStatus("Network error");
        }
    }

    private void HandleLeaderboardError(string error, int completedRequestVersion)
    {
        if (completedRequestVersion != requestVersion)
        {
            return;
        }

        SetStatus("Network error");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
