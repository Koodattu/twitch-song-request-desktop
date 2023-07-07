﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TwitchSongRequest.Model
{
    internal class SpotifyAccessToken
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
        [JsonPropertyName("scope")]
        public List<string>? Scope { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }
}
