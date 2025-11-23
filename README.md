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
```LogLevel``` - Can be set from 1-4.  
0 - Basic connection and startup info only  
1 - Log names of triggers as they're called  
2 - Log the output values of triggers. This will include raw JSON which can get quite large. Recommended only when setting up your node graphs etc.  
3 - Also include the raw JSON input. Not useful unless you are debugging. This will make your log huge  
4 - Also include the details of certain internal function. Not useful unless your name is LumKitty!  
69 - Also include the songCover entry which is a base64 encoded PNG image. Only enable this if you hate yourself!  

## Song events
```_lum_bs_songstart``` - Song has started  
```_lum_bs_songfail``` - Song failed  
```_lum_bs_songquit``` - Manually exited (e.g. from the pause menu)  
```_lum_bs_songpause``` - Pause menu opened   
```_lum_bs_songresume``` - Pause menu closed and play resumed 

All events in this category will include the following information on the trigger nodes:  
num1 - Song difficulty: 1 = easy, 2 = normal, 3 = hard, 4 = expert, 5 = expert+. 0 = unknown  
num2 - Song BPM  
num3 - Song duration in miliseconds  
text1 - Song name in this format: Artist - Song (Mix)  
text2 - Song colour information as JSON (see the JSON values section below)  
text3 - Other song information as JSON  

## Performance events
```_lum_bs_softfailed``` - Energy depleted but you have No Fail enabled  
```_lum_bs_notecut``` - You have successfully hit a note block  
```_lum_bs_bombmissed``` - You avoided hitting a bomb with your saber  
```_lum_bs_notemissed``` - You missed a note block  
```_lum_bs_bombcut``` - You hit a bomb with your saber  
```_lum_bs_obstacleenter``` - You hit a wall with your head  
```_lum_bs_obstacleexit``` - You got out of the wall  
```_lum_bs_scorechanged``` - Triggers when your score changes

All events in this category will include the following information on the trigger nodes:  
num1 - Current combo  
num2 - Current missed note count  
num3 - Current score (only for note and bomb events)  
text1 - Current rank  
text2 - Performance JSON, compatible with JSONtoDictionary node:
text3 - Note cute JSON (only for note and bomb events):

## Other events
```_lum_bs_menu``` - Triggers when a song ends by any means and the player returns to the menu.
This will be triggered in addition to fail or quit events if that is how the song ended.
No data is sent with this event

## Connection status triggers
```_lum_bs_connected``` - Called upon successful connection to Beat Saber  
```_lum_bs_disconnected``` - Called when disconnected. Num1=0 - Disconnection was requested. Num1=1 - Disconnected involuntarily  
```_lum_bs_connectfailed``` - Could not connect to Beat Saber  
```_lum_bs_connectaborted``` - Disconnection was requested before connection was successful  

## Triggers to control connection
```_lum_bs_connect``` - Connect to Beat Saber  
The default connection parameters can be overridden here. These will not be saved!  
num1 - Maximum number of retries  
num2 - Time to wait between retries (in ms. 1000 = 1 second)  
num2 - 1 = Auto retry if disconnected. -1 = Do not do this  
text1 = Websocket URL  
```_lum_bs_connect``` - Disconnect from HTTPSiraStatus or abort an ongoing connection attempt  

## Events that are filtered by default
These events are disabled by default, but can be enabled by editing the BlockedEvents setting in NyanSaber.cfg
```_lum_bs_beatmap``` - Triggered by lighting events in the map. May get very spammy and cause performance issues  
num1 - Event version (can be either 2 or 3 depending on what the song uses)  
num2 - Type. See the [HTTPSiraStatus documentation](https://github.com/denpadokei/HttpSiraStatus/blob/master/protocol.md#beatmap-event-object) for details  
num3 - GroupID. See the [Group Lighting documentation](https://bsmg.wiki/mapping/intermediate-lighting.html) for details  
text1 - Beatmap event details as JSON compatible with JSON to dictionary node. Contains the raw data from HTTPSiraStatus  
text2 - Previous beatmap event details for same type  
text3 - Next beatmap event details for same type  
WARNING: This trigger contains raw data which can vary by event type. I am not a Beat Saber lighter, and don't know what most of it does.
Someone more experienced than me could probably make a VNyan world that reacts to these events, but I am not going to!  

```_lum_bs_notefullycut``` - Performance Event fired a few frames after a Notecut Event with additional info  
```_lum_bs_notemisseddetails``` - Preformance Event fired a few frames after a note is missed, with additional info  
```_lum_bs_energychanged``` - Performance Event. Undocumented in HTTPSiraStatus, assumed to be called when your health changes. Disabled by default because I don't know what it does!  

## Parameters
```_lum_bs_connected``` - 1 if connected to HTTPSiraStatus. 0 if not connected  

## JSON values
All JSON values are suitable for use with the JSON to Dictionary node in VNyan. For full details see the [HTTPSiraStatus documentation](https://github.com/denpadokei/HttpSiraStatus/blob/master/protocol.md#standard-objects)  

### Song colour information JSON
All values are hex codes in the form #RRGGBB - This is compatible with VNyan's text to colour node and [Jayo's Poiyomi Plugin](https://github.com/jayo-exe/JayoPoiyomiPlugin)  
  
sabera - Left sabre colour  
saberb - Right sabre colour  
obstacle - wall colours  
environment0 - Colours for the current environment  
environment1  
environment0boost - Alternative light&object colours that can be set by the mapper  
environment1boost  

### Song information JSON
songname  
songsubname - The mix of the song? (not actually sure what this is for yet)  
songauthor - Artist name  
mappers - Comma separated list of mappers  
lighters - Comma separated list of lighters  
length - Length of song in seconds  
lengthtext - length of song as a string e.g. 4:20  
maptype  
environment  
difficulty - String: easy/normal/hard/expert/expertplus  
difficultylabel - Custom difficulty label that some mappers use  
njs  

### Performance JSON  
rawscore  
score  
currentmaxscore  
rank  
relativescore  
passednotes  
hitnotes  
missednotes  
passedbombs  
hitbombs  
combo  
maxcombo  
multiplier  
miltiplierprogress  
batteryenergy  
currentsongtime  
softfailed  

### NoteCut JSON
noteid  
notetype  
notecutdirection  
noteline  
notelayer  
speedok  
wascuttoosoon  
initialscore  
finalscore - (only for _lum_bs_notemisseddetails which is disabled by default, otherwise 0)  
cutdistancescore  
multiplier  
saberspeed  
saberdirx  
saberdiry  
saberdirz  
sabertype  
swingrating  
timedeviation  
cutpointx  
cutpointy  
cutpointz  
cutnormalx  
cutnormaly  
cutnormalz  
cutdistancetocenter  
timetonextbasicnote  

## Shameless self promotion
# https://twitch.tv/LumKitty
This plugin is free, but please consider sending a follow or a raid my way. If you somehow make millions using this, consider sending some my way too! :3
