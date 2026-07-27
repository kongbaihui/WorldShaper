using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseLeaderboardService : MonoBehaviour
{
    private const string PlayerNameKey = "PlayerName";
    private const string PlayerAccountNamespace = "worldshaper/player/";
    private static readonly Guid UrlNamespaceId =
        new Guid("6ba7b811-9dad-11d1-80b4-00c04fd430c8");

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
            p_player_id = CreatePlayerAccountId(playerName),
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

    private static string CreatePlayerAccountId(string playerName)
    {
        string canonicalName = playerName
            .Normalize(NormalizationForm.FormC);
        byte[] namespaceBytes = UrlNamespaceId.ToByteArray();
        SwapGuidByteOrder(namespaceBytes);

        byte[] nameBytes = Encoding.UTF8.GetBytes(
            PlayerAccountNamespace + canonicalName);
        byte[] hashInput = new byte[namespaceBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, hashInput, 0, namespaceBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, hashInput, namespaceBytes.Length, nameBytes.Length);

        byte[] hash;
        using (SHA1 sha1 = SHA1.Create())
        {
            hash = sha1.ComputeHash(hashInput);
        }

        byte[] accountIdBytes = new byte[16];
        Buffer.BlockCopy(hash, 0, accountIdBytes, 0, accountIdBytes.Length);

        // RFC 4122 version 5 (name-based SHA-1) UUID.
        accountIdBytes[6] = (byte)((accountIdBytes[6] & 0x0F) | 0x50);
        accountIdBytes[8] = (byte)((accountIdBytes[8] & 0x3F) | 0x80);
        SwapGuidByteOrder(accountIdBytes);

        return new Guid(accountIdBytes).ToString("D");
    }

    private static void SwapGuidByteOrder(byte[] guidBytes)
    {
        Swap(guidBytes, 0, 3);
        Swap(guidBytes, 1, 2);
        Swap(guidBytes, 4, 5);
        Swap(guidBytes, 6, 7);
    }

    private static void Swap(byte[] bytes, int leftIndex, int rightIndex)
    {
        byte value = bytes[leftIndex];
        bytes[leftIndex] = bytes[rightIndex];
        bytes[rightIndex] = value;
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
