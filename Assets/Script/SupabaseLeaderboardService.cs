using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseLeaderboardService : MonoBehaviour
{
    private const string PlayerIdKey = "LeaderboardPlayerId";
    private const string PlayerNameKey = "PlayerName";

    [SerializeField] private string projectUrl;
    [SerializeField] private string publishableKey;

    public static SupabaseLeaderboardService Instance { get; private set; }

    [Serializable]
    private class SubmitScoreRequest
    {
        public int p_boss_id;
        public string p_player_id;
        public string p_player_name;
        public int p_completion_ms;
    }

    [Serializable]
    private class LeaderboardRequest
    {
        public int p_boss_id;
        public int p_limit;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        GetOrCreatePlayerId();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SubmitScore(int bossId, int completionMilliseconds)
    {
        if (!TryGetEndpoint("submit_score", out string endpoint, out string configurationError))
        {
            Debug.LogError(configurationError);
            return;
        }

        string playerName = PlayerPrefs.GetString(PlayerNameKey, string.Empty).Trim();
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Leaderboard score was not submitted because PlayerName is empty.");
            return;
        }

        SubmitScoreRequest body = new SubmitScoreRequest
        {
            p_boss_id = bossId,
            p_player_id = GetOrCreatePlayerId(),
            p_player_name = playerName,
            p_completion_ms = Mathf.Max(0, completionMilliseconds)
        };

        StartCoroutine(SubmitScoreRequestCoroutine(endpoint, JsonUtility.ToJson(body)));
    }

    public void LoadLeaderboard(int bossId, Action<string> onSuccess, Action<string> onError)
    {
        if (!TryGetEndpoint("get_leaderboard", out string endpoint, out string configurationError))
        {
            Debug.LogError(configurationError);
            onError?.Invoke(configurationError);
            return;
        }

        LeaderboardRequest body = new LeaderboardRequest
        {
            p_boss_id = bossId,
            p_limit = 10
        };

        StartCoroutine(LoadLeaderboardCoroutine(
            endpoint,
            JsonUtility.ToJson(body),
            onSuccess,
            onError));
    }

    private IEnumerator SubmitScoreRequestCoroutine(string endpoint, string json)
    {
        using (UnityWebRequest request = CreatePostRequest(endpoint, json))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to submit leaderboard score: " + GetRequestError(request));
            }
        }
    }

    private IEnumerator LoadLeaderboardCoroutine(
        string endpoint,
        string json,
        Action<string> onSuccess,
        Action<string> onError)
    {
        using (UnityWebRequest request = CreatePostRequest(endpoint, json))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                string error = GetRequestError(request);
                Debug.LogError("Failed to load leaderboard: " + error);
                onError?.Invoke(error);
            }
        }
    }

    private UnityWebRequest CreatePostRequest(string endpoint, string json)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);
        UnityWebRequest request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = 15
        };
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("apikey", publishableKey.Trim());
        return request;
    }

    private bool TryGetEndpoint(string rpcName, out string endpoint, out string error)
    {
        if (string.IsNullOrWhiteSpace(projectUrl) || string.IsNullOrWhiteSpace(publishableKey))
        {
            endpoint = string.Empty;
            error = "Supabase Project URL or Publishable Key is not configured.";
            return false;
        }

        endpoint = projectUrl.Trim().TrimEnd('/') + "/rest/v1/rpc/" + rpcName;
        error = string.Empty;
        return true;
    }

    private static string GetOrCreatePlayerId()
    {
        string playerId = PlayerPrefs.GetString(PlayerIdKey, string.Empty);
        if (!string.IsNullOrEmpty(playerId))
        {
            return playerId;
        }

        playerId = Guid.NewGuid().ToString();
        PlayerPrefs.SetString(PlayerIdKey, playerId);
        PlayerPrefs.Save();
        return playerId;
    }

    private static string GetRequestError(UnityWebRequest request)
    {
        string responseBody = request.downloadHandler != null
            ? request.downloadHandler.text
            : string.Empty;
        return request.error + " (HTTP " + request.responseCode + ")" +
               (string.IsNullOrEmpty(responseBody) ? string.Empty : ": " + responseBody);
    }
}
