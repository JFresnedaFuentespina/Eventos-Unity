using System;
using System.IO;
using System.Net;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    string username;
    string password;
    public string loginUrl = "https://difreenet9.azurewebsites.net/api/Auth/login";
    int timeoutMs = 10000;
    string bearerToken;
    public TMP_InputField usernameTextInput;
    public TMP_InputField passwordTextInput;
    public Button loginButton;
    void Start()
    {
        loginButton.onClick.AddListener(LoginBlocking);
    }
    public void LoginBlocking()
    {
        username = usernameTextInput.text;
        password = passwordTextInput.text;
        var result = new LoginResult();

        try
        {
            var cred = new Credentials(username, password);
            string json = JsonUtility.ToJson(cred);
            byte[] body = Encoding.UTF8.GetBytes(json);

            var req = (HttpWebRequest)WebRequest.Create(loginUrl);
            req.Method = "POST";
            req.ContentType = "application/json; charset=utf-8";
            req.ContentLength = body.Length;
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;

            using (var reqStream = req.GetRequestStream())
            {
                reqStream.Write(body, 0, body.Length);
            }

            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                string respText = sr.ReadToEnd();
                result.StatusCode = (int)resp.StatusCode;
                result.RawResponse = respText;
                result.Success = resp.StatusCode >= HttpStatusCode.OK
                 && resp.StatusCode < HttpStatusCode.BadRequest;

                // Try to extract token from the response body
                result.Token = ExtractTokenFromJson(respText);
                bearerToken = result.Token;
                Debug.Log("Login successful: " + bearerToken);
            }
        }
        catch (WebException wex)
        {
            result.Success = false;
            if (wex.Response is HttpWebResponse errorResp)
            {
                result.StatusCode = (int)errorResp.StatusCode;
                try
                {
                    using (var er = new StreamReader(errorResp.GetResponseStream(), Encoding.UTF8))
                    {
                        result.RawResponse = er.ReadToEnd();
                    }
                }
                catch { /* ignore read errors */ }
                result.Error = $"HTTP {(int)errorResp.StatusCode} {errorResp.StatusDescription}: {wex.Message}";

                // Attempt to parse token from the error body too (some APIs return useful json on error)
                if (!string.IsNullOrEmpty(result.RawResponse))
                    result.Token = ExtractTokenFromJson(result.RawResponse);
                bearerToken = result.Token;
                Debug.Log("Error login successful? " + result.Token);
            }
            else
            {
                result.Error = wex.Message;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }
    }
    private static string ExtractTokenFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            // Try direct token/access_token fields using Unity's JsonUtility
            var direct = JsonUtility.FromJson<TokenResponseDirect>(json);
            if (!string.IsNullOrEmpty(direct.token)) return direct.token;
            if (!string.IsNullOrEmpty(direct.access_token)) return direct.access_token;

            // Some APIs nest token in a data object: try that shape
            var nested = JsonUtility.FromJson<TokenResponseNested>(json);
            if (nested != null && nested.data != null)
            {
                if (!string.IsNullOrEmpty(nested.data.token)) return nested.data.token;
                if (!string.IsNullOrEmpty(nested.data.access_token)) return nested.data.access_token;
            }

            // Fallback: crude manual search for "token":"..." or "access_token":"..."
            string token = ParseJsonStringValue(json, "token");
            if (!string.IsNullOrEmpty(token)) return token;
            token = ParseJsonStringValue(json, "access_token");
            if (!string.IsNullOrEmpty(token)) return token;
        }
        catch (Exception)
        {
            // ignore parse errors and return null
        }

        return null;
    }
    [Serializable]
    private struct Credentials
    {
        public string email;
        public string password;

        public Credentials(string username, string password)
        {
            this.email = username;
            this.password = password;
        }
    }

    public class LoginResult
    {
        public bool Success;
        public int StatusCode;
        public string RawResponse;
        public string Error;
        // Parsed JWT token (if any). Null or empty if not found.
        public string Token;
    }
    [Serializable]
    private class TokenResponseDirect
    {
        public string token;
        public string access_token;
    }

    [Serializable]
    private class TokenResponseNested
    {
        public TokenResponseDirect data;
    }

    // Very small helper to find "key":"value" in a JSON string (not a full JSON parser)
    private static string ParseJsonStringValue(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;
        string pattern = $"\"{key}\"";
        int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        int colon = json.IndexOf(':', idx + pattern.Length);
        if (colon < 0) return null;

        // Skip whitespace after colon
        int i = colon + 1;
        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;

        // If value starts with a quote, read string value
        if (i < json.Length && json[i] == '"')
        {
            int startQuote = i;
            int endQuote = json.IndexOf('"', startQuote + 1);
            if (endQuote < 0) return null;
            return json.Substring(startQuote + 1, endQuote - startQuote - 1);
        }

        // Otherwise try to read until comma or brace (for non-quoted values)
        int end = i;
        while (end < json.Length && json[end] != ',' && json[end] != '}' && !char.IsWhiteSpace(json[end])) end++;
        if (end > i)
            return json.Substring(i, end - i).Trim(new char[] { '"', ' ', '\r', '\n', '\t' });

        return null;
    }
}
