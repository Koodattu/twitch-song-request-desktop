<div align="center">
    <br />
    <img src="https://i.imgur.com/0o9iHKU.png" alt="Logo" width="140" height="90">

  <h3 align="center">Twitch Song Requests Bot (desktop)</h3>

  <p align="center">
   A Windows desktop Twitch song request bot application with multi-platform support for song requests.
    <br />
    <br />
  </p>
</div>

## Project description

Twitch Song Request Desktop is a Windows-based application designed to seamlessly integrate song requests for your Twitch channel from Spotify and YouTube.

Written in C# and WPF.

**Disclaimer:** Please ensure to respect copyright laws and abide by the terms of service of the supported platforms (Spotify and YouTube) while using this application.

![](https://i.imgur.com/tl3li6s.png)

## Features 

- **Channel Points Integration:** The application reads song requests from redeemed channel points and refunds points when unable to play requested songs.
- **Multi-platform Song Request Support:** Supports song requests from both Spotify and YouTube.
- **Spotify Compatibility:** Song requests from Spotify are directly played via Spotify player.
- **YouTube Playback:** YouTube song requests are played within the application, which allows you to change the playback device.

## Setup

1. Install .NET 6.0 binaries if necessary
2. Run the application, click on Setup in the top right corner
    1. For the appliation to work you will need to connect your Twitch streaming account, create a channel point reward
    2. Twitch bot account can be used for replying in chat, but you can also use the account you stream on
    3. If Spotify is not connected, only YouTube song requests will work
4. Enter your Twitch Client ID and Twitch Client Secret
    1. Use the arrow on the right hand side to open Twitch developer console
    2. Create an application, name and category can be anything, redirect url must be http://localhost:8080
    3. Manage the created application, copy the ID, generate a new secret by clicking on 'new secret' and copy the secret
5. Connect the Twitch account you stream on by select the browser you use and click on connect
    1. Multiple browser options are supported if you use different accounts in different browsers
6. Connect Twitch bot if you wish
7. Enter reward name and click on create
    1. You can edit the reward after creation, price, icon, name etc.
    2. You can use the blue arrow button on the right to quickly open channel point rewards settings
8. Connect Spotify account in the same way by entering your Spotify client id and secret
    1. Click on the blue arrow button on the right
    2. Create app, everything else can be whatever but redirect uri and website must be http://localhost:8080
    3. Client ID and secret are inside application settings, you can see the secret by clicking on the view secret link

## Authors

Juha Ala-Rantala ([Koodattu](https://github.com/Koodattu/))

## Version History

* 1.0.0.0
    * First release

## Tools used

* Visual Studio
* Postman

## License

Distributed under the MIT License. See `LICENSE` file for more information.

## Acknowledgments

* [AdonisUI](https://github.com/benruehl/adonis-ui)
* [CefSharp](https://github.com/cefsharp/CefSharp)
* [MVVM CommunityToolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
* [HardCodet NotifyIcon WPF](https://github.com/hardcodet/wpf-notifyicon)
* [NAudio](https://github.com/naudio/NAudio)
* [NLog](https://github.com/NLog/NLog)
* [RestSharp](https://github.com/restsharp/RestSharp)
* [TwitchLib](https://github.com/TwitchLib/TwitchLib)
