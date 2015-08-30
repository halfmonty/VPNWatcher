VPNWatcher
---------------------

VPNWatcher is a small tool to make sure that configured programs only
run when you are connected do a VPN. It's designed to quietly run in the background
and only jump into action when you lose your VPN connection.

The configuration is simple:

1. Choose your VPN interface from the list and hit the save button
2. Specify the apps you want to close when you lose your VPN connection and/or setup
   the torrent actions
3. Done!

When everything runs fine, click "Start minimzed" and "Start with Windows".
It will autostart with a small trayicon from now on.

Thats it!




Frequently asked questions
---------------------

### What language was VPNWatcher coded in?

It's a C# WPF project and requires the .NET Framework 4.0 to run.


### Whats the general workflow of this tool after it is configured?

- fetch a list of connected interfaces every 2 seconds
- if your configured VPN is online: everything is fine, show an ok sign
- if not: find the configured apps, close them and show a warning sign or perform
    the configured torrent actions 


### I'm afraid it uses too much resources, can I reduce this somehow?

It doesn't, the CPU usage is very small. If neccessary, open up the config file
and increase the timespan between the checks (see "TimerInMilliSeconds" in the config).


### I'm paranoid, I want to check for a VPN loss every millisecond!

You can do so by setting TimerInMilliSeconds to 1 in the config, which is the minimun.

Note that this might stress your CPU, since VPNWatcher will check for a VPN loss a thousand times per second now ...



### What does the "Interfaces" textbox mean?

If shows all network interfaces that are currently online. If you connect to your
VPN, you will see a new interface popping up in the list.


### Can I see somehow if my textbox entrys are correct?

Yes. After you selected your VPN using the save button, a green "ok" sign will show up.
If you disconnect from your VPN, a red "warning" sign will show.


### I don't know what to insert in the "Apps" textbox.

The names of the applications to kill, without the file ending. For example, open the taskmanager
(CTRL+ALT+DEL, taskmanager) and check the processes tab. uTorrent shows up as "uTorrent.exe *32" or
"uTorrent.exe", so you need to enter "uTorrent" (without the quotes).
One line per application.



### How can I pause/stop torrents when the VPN drops?

For this to work, you need to enable the Web UI in uTorrent (Options, Preferences, Advanced, Web UI). Choose a 
Username, Password and Alternative listening port.

In VPNWatcher, check the uTorrent checkbox and insert the data you just configued in utorrent:
- Address: http://localhost:YOUR-CONFIGURED-PORT/gui
- Username: your configured username
- Password: your configured username

A green check shows up when your credentials are working, now you can select whether you
want torrents paused or stopped upon vpn loss.



### When VPNWatcher is started minimized, how do I open it up again?

Whenever VPNWatcher is minimzed, a trayicon will show up. Either doubleclick on it or
rightclick and hit "Open".



### Where do I find the config file, and what do the config settings mean exactly?

The config file is lcoated next to the VPNWatcher.exe and is named "VPNWatcher.exe.Config". 
If you manually want to edit it, close VPNWatcher and use any text editor you like.

- TimerInMilliSeconds: Checks every x milliseconds for a network change (default: 2000)
- ChosenActionIndex: The "App Action" selection. Leave it for 0 at the moment, might be extended in the future **TODO FIXME**
- ConsoleMaxSize: Wipes the "Log" textbox every X characters (default: 10000)
- Applications: List of applications you want to kill when the VPN connection drops, separated with a #
- IgnoreIPV6: I might add IPV6 support in the future, at the moment this does nothing
- StartMinimized: Start with only the trayicon showing (default: false)
- VPNInterfaceID: Internal ID of the interface you selected
- VPNInterfaceName: Name of the interface you selected as displayed in the line below the interface list
- DebugMode: If set to "true", spams massive logs into the Log textbox and also copies everything
             into the clipboard when you exit the application (so you can easily send me logs in case
             you need help).
- StrictInterfaceHandling: **TODO DESCRIBE IT**
- **TODO utorrent stuff**



### Why are there more options in the config than in the program (debugmode, timer...)?

I don't consider them very userfriendly, in 99% of all cases they are fine like they are.

Also I want to keep the GUI as simple as possible.


### I'm interested in the programming of VPMWatch, can I have the sourcecode?

Sure, its on Github: https://github.com/halfmonty/VPNWatcher

Feel free to fork and submit pull requests.


### What external libraries are you using?
uTorrentAPI for .NET - https://utorrentapi.codeplex.com/



### I have additonal ideas or questions, can I contact you?

Sure, there are currently two developers playing around with VPNWatcher.
Mike is currently reachable via mikehubley -A-T- @ gmail -D-O-T- com, Freddi at muff99 -A-T- outlook -D-O-T- com





Changelog
---------------------

### 1.X - ddmm15

  - configure the timer in milliseconds instead of seconds
  - restore a closed app upon vpn reconnect
  - utorrent Web UI support to pause/stop torrents
  - autostart by checkbox
  - sourcecode cleanup
  - **TODO ???**
   
### 1.1 - 120613

  - support for interfaces instead of IP ranges (big thanks to TorrentFreak's Ernesto)
  - fix for VPNWatcher doing nothing when started minimized
  - simpler UI with less buttons
  - autosave for all config values except the interface name
  - icons fixed (less resources, smaller binary) 
  - sourcecode cleanup

### 1.0 - 100613

  - initial release