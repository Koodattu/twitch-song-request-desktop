namespace TwitchSongRequest.Model
{
    public class SpotifySearch
    {
        public Tracks tracks { get; set; }
    }

    public class SpotifyDevices
    {
        public Device[] devices { get; set; }
    }

    public class SpotifyState
    {
        public Device device { get; set; }
        public string repeat_state { get; set; }
        public bool shuffle_state { get; set; }
        public Context context { get; set; }
        public int timestamp { get; set; }
        public int progress_ms { get; set; }
        public bool is_playing { get; set; }
        public Item item { get; set; }
        public string currently_playing_type { get; set; }
        public Actions actions { get; set; }
    }

    public class Device
    {
        public string id { get; set; }
        public bool is_active { get; set; }
        public bool is_private_session { get; set; }
        public bool is_restricted { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public int volume_percent { get; set; }
        public bool supports_volume { get; set; }
    }

    public class Context
    {
        public string type { get; set; }
        public string href { get; set; }
        public External_Urls external_urls { get; set; }
        public string uri { get; set; }
    }

    public class External_Urls
    {
        public string spotify { get; set; }
    }

    public class Item
    {
        public Album album { get; set; }
        public Artist[] artists { get; set; }
        public string[] available_markets { get; set; }
        public int disc_number { get; set; }
        public int duration_ms { get; set; }
        public bool _explicit { get; set; }
        public External_Ids external_ids { get; set; }
        public External_Urls external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public bool is_playable { get; set; }
        public Linked_From linked_from { get; set; }
        public Restrictions restrictions { get; set; }
        public string name { get; set; }
        public int popularity { get; set; }
        public string preview_url { get; set; }
        public int track_number { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
        public bool is_local { get; set; }
    }

    public class Album
    {
        public string album_type { get; set; }
        public int total_tracks { get; set; }
        public string[] available_markets { get; set; }
        public External_Urls external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public Image[] images { get; set; }
        public string name { get; set; }
        public string release_date { get; set; }
        public string release_date_precision { get; set; }
        public Restrictions restrictions { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
        public Artist[] artists { get; set; }
    }

    public class Image
    {
        public string url { get; set; }
        public int height { get; set; }
        public int width { get; set; }
    }

    public class External_Ids
    {
        public string isrc { get; set; }
        public string ean { get; set; }
        public string upc { get; set; }
    }

    public class Linked_From
    {
    }

    public class Restrictions
    {
        public string reason { get; set; }
    }

    public class Artist
    {
        public External_Urls external_urls { get; set; }
        public Followers followers { get; set; }
        public string[] genres { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public Image[] images { get; set; }
        public string name { get; set; }
        public int popularity { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

    public class Followers
    {
        public string href { get; set; }
        public int total { get; set; }
    }

    public class Actions
    {
        public bool interrupting_playback { get; set; }
        public bool pausing { get; set; }
        public bool resuming { get; set; }
        public bool seeking { get; set; }
        public bool skipping_next { get; set; }
        public bool skipping_prev { get; set; }
        public bool toggling_repeat_context { get; set; }
        public bool toggling_shuffle { get; set; }
        public bool toggling_repeat_track { get; set; }
        public bool transferring_playback { get; set; }
    }

    public class Tracks
    {
        public string href { get; set; }
        public Item[] items { get; set; }
        public int limit { get; set; }
        public string next { get; set; }
        public int offset { get; set; }
        public object previous { get; set; }
        public int total { get; set; }
    }
}
