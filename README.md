# VNyan integration with HTTPSiraStatus
This plugin allows VNyan nodegraphs to react to events in Beat Saber such as song start, end and fail. It also provides information about the song.
It is intended to be used in conjunction with [LIVnyan](https://github.com/LumKitty/LIVnyan), my plugin for using VNyan as your model renderer in VR games

## Installation and usage
Install to VNyan in the usual way, by copying the included DLLs to VNyan\Items\Assemblies  
Don't forget to enable third party plugins in VNyan: Menu -> Settings -> Misc -> Additional Settings -> Allow third party mods/plugins
Requires [HTTPSiraStatus](https://github.com/denpadokei/HttpSiraStatus)  
Once Beat Saber is running, click the "NyanSaber" button from VNyan plugins menu to connect to Beat Saber. Click it again to abort or disconnect. 
Alternatively use the connection control triggers documented below:  

## Network configuration
After first use, NyanSaber.cfg will appear in your VNyan profile directory (default: C:\Users\You\AppData\LocalLow\Suvidriel\VNyan)  
```URL``` - URL for connecting to Beat Saber  
```RetryInterval``` - How long to wait between retries in milliseconds (default: 1000, i.e. 1 second)  
```MaxRetries"``` - Number of retry attempts (default: 5)
```RetryOnDisconnect``` - Auto reconnect if disconnected from Beat Saber (default: false)

## Triggers generated while running
```_lum_bs_songstart```  
```_lum_bs_songend```  
```_lum_bs_songfail```  
```_lum_bs_songquit```  
```_lum_bs_songpause```  
```_lum_bs_songresume```  

num1 - Song difficulty: 1 = easy, 2 = normal, 3 = hard, 4 = expert, 5 = expert+. 0 = unknown  
num2 - Song BPM  
num3 - Song rating: 0 = safe 1 = anything else (I don't know what the valid values are yet!)  
text1 - Song name in this format: Artist - Song (Mix)  
text2 - Song colour information as JSON (see the JSON values section below)  
text3 - Other song information as JSON  

## Connection status triggers
```_lum_bs_connected``` - Called upon successful connection to BSDataPuller  
```_lum_bs_disconnected``` - Called when disconnected. Num1=0 - Disconnection was requested. Num1=1 - Disconnected involuntarily  
```_lum_bs_connectfailed``` - Could not connect to BSDataPuller  
```_lum_bs_connectaborted``` - Disconnection was requested before connection was successful  

## Triggers to control connection
```_lum_bs_connect``` - Connect to BSDataPuller  
The default connection parameters can be overridden here. These will not be saved!  
num1 - Maximum number of retries  
num2 - Time to wait between retries (in ms. 1000 = 1 second)  
num2 - 1 = Auto retry if disconnected. -1 = Do not do this  
text1 = Websocket URL  
```_lum_bs_connect``` - Disconnect from BSDataPuller or abort an ongoing connection attempt  

## Parameters
```_lum_bs_connected``` - 1 if connected to BSDataPuller. 0 if not connected  

## JSON values
All JSON values are suitable for use with the JSON to Dictionary node in VNyan  

### Song colour information
All values are hex codes in the form #RRGGBB  
  
left - Left sabre colour  
right - Right sabre colour  
obstacles  
environment0  
environment1  
environment0boost  
environment1boost  

### Other song information
songname  
songsubname - The mix of the song? (not actually sure what this is for yet)  
songauthor - Artist name  
mappers - Comma separated list of mappers  
lighters - Comma separated list of lighters  
contentrating - Text version of the song's content rating. Only value I know is "safe". Please tell me what others there are!  
duration - Length of song in seconds  
durationtext - length of song as a string e.g. 4:20  
maptype  
environment  
difficulty - String: easy/normal/hard/expert/expertplus  
difficultylabel - Custom difficulty label that some mappers use  
njs  

## Shameless self promotion
# https://twitch.tv/LumKitty
This plugin is free, but please consider sending a follow or a raid my way. If you somehow make millions using this, consider sending some my way too! :3
