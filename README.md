#VPNWatcher

##Overview
VPNWatcher is a windows based tool written in C# to restrict torrent or other traffic to your VPN. This project was created by Freddi Netterdon of [www.freddi.de](http://www.freddi.de/) where you can find the original project. With Freddi's permission I have moved the project to github for better collaboration and contributions to polish this project and really compete with the paid alternative software that accomplish the same task.

##Features
-Stores selected VPN interface to reliably detect when VPN connects or disconnects
-Can close and re-open processes by name when VPN connects or disconnects
-Utorrent API integration to automatically Pause or Stop running torrents if VPN connection drops
-Automatically restart Paused or Stopped torrents when VPN reconnects

##Getting Involved
Feel free to fork and make pull requests. Even if you aren't into coding, feedback is great. You can create issues on the github or just email me directly at halfmonty11@gmail.com